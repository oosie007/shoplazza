# Cart Transform API: How It Works (Shoplazza)

This doc summarizes the [Shoplazza Function API](https://www.shoplazza.dev/v2024.07/reference/overview-29) and [Cart Transform API](https://www.shoplazza.dev/v2024.07/reference/bind-cart-transform-function) so we can add Item Protection as a line item and update cart totals **without asking merchants to create a product**.

## References (read these in detail)

- **Overview:** https://www.shoplazza.dev/v2024.07/reference/overview-29  
- **Function execution logic:** https://www.shoplazza.dev/v2024.07/reference/function-execution-logic  
- **Bind Cart Transform Function:** https://www.shoplazza.dev/v2024.07/reference/bind-cart-transform-function  
- **Cart Transform Function List:** https://www.shoplazza.dev/v2024.07/reference/cart-transform-function-list-copy  

## What the Cart Transform does

- **One cart transform function per store.** You bind it via the store’s OpenAPI:  
  `POST https://{shopdomain}.myshoplaza.com/openapi/2024-07/function/cart-transform`
- When the store needs cart pricing (cart page, checkout), Shoplazza sends the **cart** to your function and expects an **operations** object back.
- **Supported operation today: `update`** – adjust an **existing** line item’s price (e.g. set `adjustment_fixed_price`). There is **no documented “add”** for a new line; adding a line is done via the **Cart API** (frontend).

So the flow is:

1. **Add/remove the line:** Widget toggle uses the **Cart API** to add or remove the Item Protection product (we need one product per store; we can create it via Admin API on install).
2. **Set the line’s price:** We bind a **Cart Transform function** for that store. When the cart is read, Shoplazza calls our function with the cart; we find the Item Protection line and return `operations.update` with that line’s `id` and `price.adjustment_fixed_price` = computed premium (e.g. % of subtotal). Cart totals then reflect the correct premium.

## Input (what Shoplazza sends to our function)

```json
{
  "cart": {
    "line_items": [
      {
        "product": {
          "product_id": "1231",
          "variant_id": "1231",
          "price": "10.00",
          "title": "test product",
          "metafields": [{"namespace": "custom-option", "key": "adjust-10-price", "value": "true"}]
        },
        "id": "1",
        "quantity": 1,
        "properties": "{\"Color\":\"Red\"}"
      }
    ]
  }
}
```

- `cart.line_items[].id` is the line item ID we must use in `operations.update[].id`.
- `cart.line_items[].product.product_id` / `variant_id` / `price` / `title` identify the product.
- **Functions cannot call external servers**; all logic must use the cart (and optionally product metafields).

## Output (what we must return)

```json
{
  "operations": {
    "update": [
      {
        "id": "1",
        "price": {
          "adjustment_fixed_price": "20.00"
        }
      }
    ]
  }
}
```

- `adjustment_fixed_price`: string, range 0–999999999.
- If multiple operations target the same line `id`, only the first is applied.

## End-to-end flow for Item Protection

1. **On app install (or first use):**  
   - Create an “Item Protection” product in the store via **OpenAPI (Create Product)** (e.g. price 0, title “Item protection”, one default variant).  
   - Save `product_id` and `variant_id` in our store settings.  
   - **Bind our Cart Transform function** for this store: call  
     `POST https://{shopdomain}.myshoplaza.com/openapi/2024-07/function/cart-transform`  
     with the store’s access token and the payload required by Shoplazza (e.g. our callback URL or function reference – see their Bind docs).

2. **When the customer toggles the widget ON:**  
   - Frontend uses **Cart API** `POST /api/cart` with the stored `product_id` and `variant_id` to add the Item Protection line.

3. **When the cart is read (cart page or checkout):**  
   - Shoplazza calls **our** cart-transform endpoint (the one we registered when binding) with the cart JSON.  
   - We identify the store (e.g. from request header or body if Shoplazza sends it).  
   - We load store settings (`itemProtectionProductId`, `fixedPercentAll`, etc.).  
   - We find the line where `product.product_id === itemProtectionProductId`.  
   - We compute premium (e.g. sum of other line items’ totals × `fixedPercentAll` / 100).  
   - We return `operations.update` with that line’s `id` and `adjustment_fixed_price` = computed premium.

4. **When the customer toggles OFF:**  
   - Frontend uses **Cart API** `DELETE /api/cart/{variant_id}` to remove the Item Protection line.

Result: merchants never create a product; we create one and use Cart API + Cart Transform to add the line and set its price so cart totals update correctly.

## What we need from the Shoplazza docs

- **Exact request body for “Bind Cart Transform Function”** (e.g. do we send a callback URL, or function code, or a function ID?).  
- **How Shoplazza invokes our function** (POST to our URL? which headers/body fields identify the shop?).  
- **Whether we can create a product via OpenAPI** with the right scopes so we can create “Item Protection” on install.

---

## Implementation summary (done)

| Step | Where | What |
|------|--------|------|
| **Create “Item Protection” product** | On install: `src/app/api/auth/callback/route.ts` | After OAuth, if store has no `itemProtectionProductId`, we call `createItemProtectionProduct(shop, accessToken)` from `src/lib/shoplazza/item-protection-product.ts`, which POSTs to `https://{shop}/openapi/2024-07/products` with title "Item protection", one variant at price 0, `published: false`. We then save `productId` and `variantId` in `StoreSettings`. |
| **Bind Cart Transform** | Same flow, right after saving ids | We call `bindCartTransform(shop, accessToken, callbackUrl)` with `callbackUrl = {APP_URL}/api/shoplazza/cart-transform`. That POSTs to `https://{shop}/openapi/2024-07/function/cart-transform` with `function_url` / `url` in the body. If the Bind API expects a different body, adjust in `item-protection-product.ts`. |
| **Add/remove line when widget toggles** | `public/checkout-widget.js` | Existing `applyPremiumViaCartAPI(enabled)` uses Cart API to add (POST /api/cart) or remove (GET cart, then DELETE /api/cart/{variant_id}) the Item Protection product. No price is sent; the line is added/removed only. |
| **Set the line’s price** | `src/app/api/shoplazza/cart-transform/route.ts` | When Shoplazza calls our callback with the cart, we find the line whose `product_id` matches `itemProtectionProductId`, compute premium = (other lines’ subtotal) × `fixedPercentAll` / 100, and return `operations.update` with that line’s `id` and `adjustment_fixed_price`. |

**Scopes:** We added `write_product` to `src/lib/shoplazza/auth.ts` so the app can create the product. Existing installs will need to re-authorize to grant the new scope (or add the product manually once and set ids in settings).

**If Create Product or Bind fails:** The auth callback still redirects to /admin; we only log and skip. The merchant can later configure Item Protection product/variant IDs in the admin UI (optional workaround) or reinstall after fixing scopes/API.
