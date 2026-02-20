# Two ways to add a fee at checkout (Worry Free vs Item Protection)

Shoplazza supports (at least) **two different mechanisms** for adding a fee at checkout. Worry-Free Delivery uses one; we use the other because the first isn’t available to our app yet.

---

## Path A: Checkout package + `pkg_set` + `additional_prices` (Worry Free)

**What you see with Worry Free:**

- No visible “product” for the fee. It appears as an **“Insurance fee”** (or similar) in the order summary.
- Toggle ON → API calls happen → total updates.
- **API calls:**
  - **`POST /api/checkout/pkg_set`**  
    Payload typically includes something like `order_token` (your “order id”) and a switch state (e.g. `checked: 1` or `switch_status: "ON"`).  
    This tells the **store** “turn this package on for this order.” The store has a **handler** for Worry Free because they **registered a checkout package** in Partner Center.
  - **`POST /api/checkout/price`**  
    You send the same `order_token` and current checkout state (address, step, etc.).  
    Response includes:
    - `additional_prices`: array of extra fees (e.g. `{ name: "sp", price: "4.00", ... }`).
    - `additional_total`: sum of those fees.
    - `total_price`: full total (subtotal + shipping + additional_total + …).

So: **no product, no line item.** The fee lives in `additional_prices` and the total is calculated by the store when you call `price` after `pkg_set`.

**Why we don’t use this (yet):**  
For our app, **`pkg_set` returns 404**. The store has no handler for our app until we **register a checkout package** for CD Insure in Shoplazza Partner Center. The docs say “Register a checkout package in Partner Center” but don’t spell out the exact steps. Until that’s done, we can’t rely on Path A.

---

## Path B: Real product + Cart API + Cart Transform (what we do)

**What we do:**

1. **Create a product** in the store (e.g. “Item protection”, price 0) via OpenAPI and save its `product_id` and `variant_id`.
2. **Widget toggle ON:**  
   Call the **Cart API** (`POST /api/cart`) to **add that product as a line item** (so it shows up as a real line in the cart).
3. **Cart Transform function:**  
   We upload a small function (WASM) to Shoplazza and **bind** it to the store. When the cart is evaluated (e.g. at checkout), Shoplazza runs our function with the cart JSON. We find the “Item protection” line and set its **price** (e.g. 20% of other items). So the **total** = subtotal (including the protection line at the correct price) + shipping + etc.

So: **we do create a product** and use the **Cart Transform “function thing”** so that:
- The protection appears as a line item (like Worry Free appears as a fee line).
- The line’s price is computed by our logic (percent of cart), not a fixed product price.

**Why we need this:**  
Because `pkg_set` is 404 for us, we can’t use Path A. Path B is the documented way (Create Function → Bind Cart Transform) that works without registering a checkout package. The downside is bind can return 403 until the app has the right permissions in Partner Center, and we have to maintain the product + WASM flow.

---

## Summary

| | Worry Free (Path A) | Item Protection (Path B) |
|---|--------------------|---------------------------|
| **Product** | No visible product; fee is “additional price” | We create one product, add as line item |
| **Toggle ON** | `pkg_set` (order_token + switch) then `price` | Cart API add line, then Cart Transform sets price |
| **Total** | From `price` response (`additional_total` + …) | From cart (line items + Cart Transform) |
| **Requires** | Checkout package registered in Partner Center | Create product + bind Cart Transform (and WASM) |

**You don’t “need” the product and function thing in principle**—Worry Free doesn’t use them. You need them **because** the Worry-Free-style path (checkout package + `pkg_set`) isn’t available to our app until we register that package. If you get Shoplazza to tell you how to **register a checkout package** for CD Insure, we could in theory switch to Path A and then drop the product and Cart Transform.

---

## What to ask Shoplazza

1. **How do we register a “checkout package” for our app** so the store exposes `POST /api/checkout/pkg_set` for us (like Worry-Free Delivery) and the total updates when we call `POST /api/checkout/price`?
2. If that’s not possible, **is our current approach (product + Cart Transform) the correct supported way** for an app that wasn’t granted a checkout package?
