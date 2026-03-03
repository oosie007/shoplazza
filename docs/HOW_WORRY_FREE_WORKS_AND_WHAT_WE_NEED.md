# How Worry Free Does It (and Why We Can’t Yet)

## How Worry Free works

Worry Free **does not add a product to the cart**. It does not use the Cart API at all.

1. **Checkout package**  
   Shoplazza has a **checkout package** registered for Worry Free. So the store’s checkout “knows” about their fee.

2. **Toggle ON**  
   The Worry Free widget calls:
   - **`POST /api/insurance/v1/product/pkg_set`** (or similar app-specific route) → **200 OK**  
     The store has a handler for this. It marks “add Worry Free fee to this order.”
   - **`POST /api/checkout/price`**  
     The store recalculates the order including the fee and returns the new total. The checkout UI updates (cart-only refresh, no full page reload).

3. **Result**  
   The fee appears as an “Insurance fee” (or similar) line and the total updates. No cart line item, no product, no Cart Transform. Everything is done via the **checkout package** and the store’s built-in support.

## Why our item never “gets into the cart” the same way

We **don’t have** a checkout package. So:

- We call **`POST /api/checkout/pkg_set`** → **404**. The store has no handler for our app.
- We fall back to **Cart API**: we add a real “Item protection” product as a line with `POST /api/cart`. That returns **200** and the line is in the **cart** on the server.
- But the **checkout page** often shows an **order** that was created when the customer entered checkout. That order may not include later cart changes. So:
  - Cart has the line (our API says 200).
  - Checkout UI still shows the old total and no new line, and doesn’t refresh like Worry Free.

So it’s not that our code is wrong: we’re using the only path we have (Cart API), while Worry Free uses a path that’s only available when a **checkout package** is registered.

## What we need from Shoplazza (one ask)

**Ask Shoplazza (support or partner team):**

> We have a checkout UI extension that adds “Item protection” (like Worry-Free Delivery). For Worry Free, the store exposes a `pkg_set`-style API and the fee is added to the order and the total updates without a full page refresh. For our app, `POST /api/checkout/pkg_set` returns 404 with “Register a checkout package in Partner Center.”  
>  
> **What are the exact steps to register a checkout package for our app** so that:
> 1. The store accepts our fee (e.g. via something like `pkg_set` or an app-specific route), and  
> 2. When we call the price endpoint, the order total and the fee line update on the checkout page (like Worry Free)?  
>  
> We need this so our “Item protection” toggle can add the fee and update the total the same way Worry-Free Delivery does, without relying on adding a product to the cart.

Once that’s done, we can switch our widget to the same flow as Worry Free (pkg_set + price only, no Cart API for this fee), and the item will “get into the order” and the total will update correctly.

## Why the price API response has empty additional_prices

We send **POST /api/checkout/price** with `additional_prices: [{ name: "cd_insure_item_protection", price: "40.00", fee_title: "Item protection" }]`. The store receives it but does not apply it (no checkout package). The response has `additional_prices: []`, `additional_total: "0.00"`, and total = subtotal + shipping only. So the request is correct; the store ignores our fee until a package is registered.

## Summary

| | Worry Free | Us (current) |
|---|------------|--------------|
| **Adds a product to cart?** | No | Yes (Cart API, 200) |
| **Checkout package?** | Yes (registered) | No (404) |
| **Store handler for our fee?** | Yes | No |
| **Total / fee line update on page?** | Yes (nice refresh) | No (checkout shows old order) |
| **Fix** | — | Register a checkout package for our app |

The only way to get the same behavior as Worry Free is to get a **checkout package** registered for our app. There is no way to “trick” the store into accepting our pkg_set or to force the checkout UI to show cart updates without that.
