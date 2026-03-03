# Cart vs Checkout: Why the Checkout Summary Doesn’t Update

## What you’re seeing

- **Cart** = the basket before checkout. Adding items with **POST /api/cart** works; many apps do this without talking to Shoplazza.
- **Checkout page** = the flow after the customer clicks “Checkout”. The **checkout summary** (right side: line items, subtotal, total) is what you want to update when they toggle Item Protection.

So the situation is: you’re **on the checkout page** and you want the **checkout summary** (values and line items) to update when they toggle. That’s different from “adding to the cart” on the cart page.

## Why the summary doesn’t update when you’re on checkout

On many platforms (including how Shoplazza appears to work):

1. When the customer clicks **Checkout**, the store creates an **order** from the current **cart** (a snapshot at that moment).
2. The **checkout summary** is driven by that **order**, not by the live cart.
3. When we call **POST /api/cart** from the **checkout page**, we add the Item Protection line to the **cart** (and get 200).
4. The **checkout** UI, however, is still showing the **order** that was created when they entered checkout. That order didn’t include Item Protection, so the summary doesn’t change.

So:

- **Cart** is updated (our API call works).
- **Checkout summary** is not, because it’s bound to the **order** snapshot, not the live cart.

That’s why “loads of people” can add items to the **cart** without talking to Shoplazza, but the **checkout summary** on the checkout page still doesn’t update when we add the line from there.

## What does work: add Item Protection before checkout

If the line is in the **cart before** they go to checkout, the **order** is created with that line, so the checkout summary shows it:

1. Customer is on the **cart page** (not checkout).
2. They turn Item Protection **ON** (widget on cart page, or they use our app’s cart-page flow if we add it).
3. **POST /api/cart** adds the Item Protection line to the cart.
4. Customer clicks **Checkout**.
5. The **order** is created from the **current cart** (including Item Protection).
6. The **checkout summary** shows the correct line items and total.

So the reliable way to get the **checkout summary** to show Item Protection is: **add it on the cart page, then go to checkout.**

## What we’d need for “toggle on checkout page → summary updates”

To have the **checkout summary** (and line items) update when they toggle **on the checkout page**, one of these would need to be true:

1. **Checkout uses live cart**  
   The checkout summary would have to be driven by the **current cart** (e.g. refetched after our POST /api/cart), so that adding a line from the checkout page immediately shows up. That’s a Shoplazza platform behavior.

2. **API to update the current order**  
   There would need to be an API that adds a line (or an additional price) to the **current checkout order** (not just the cart). We don’t have that; our `additional_prices` in the price request are ignored because we don’t have a checkout package.

3. **Checkout package (like Worry Free)**  
   With a registered checkout package, the store would accept our fee (e.g. via pkg_set or recognized additional_prices) and the **price** response and checkout summary would include it. That’s the “Worry Free” path.

So: adding items to the **cart** is supported and works; updating the **checkout summary** while already on the **checkout page** depends on how Shoplazza ties checkout to cart/order and whether we have a checkout package. The safe, working approach today is: **add Item Protection on the cart page, then go to checkout.**

---

## Duplicate "Item protection" lines and $0.00 on cart

If the **cart page** shows multiple "Item protection" lines (e.g. six) each at **$0.00**:

- **Duplicates:** Each toggle (or each add) was adding a new line. The widget now **checks the cart first**: if Item Protection is already in the cart, it does not POST add again, so you get at most one line. For existing carts with multiple lines, remove the extras manually (trash icon) and leave one.
- **$0.00:** The Item Protection product is created with price 0. The **Cart Transform** function is supposed to set the line’s price (e.g. % of cart) when the cart is evaluated. If the cart page still shows $0.00, either:
  - The Cart Transform is **not bound** for this store (run “Re-bind Cart Transform” in app admin and check for errors), or
  - Shoplazza does not run the Cart Transform on the **cart page** (only at checkout). In that case the price would only update when they proceed to checkout (if bind is working there).
