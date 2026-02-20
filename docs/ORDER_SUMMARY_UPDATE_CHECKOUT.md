# Why the checkout order summary doesn’t update when you toggle Item Protection

You want: **on the checkout page**, when the customer toggles Item Protection ON, the **order summary** (line items + total on the right) updates immediately, the same way it does when they select shipping.

You already have a **checkout UI extension** – that’s what shows the widget. There is no separate “checkout package” in Shoplazza’s UI; “package” in our docs referred to another app’s backend API (`pkg_set`), not your extension.

## How shipping updates the summary

When the customer selects a **shipping method**, the store recalculates the order and returns new prices (including shipping). The checkout UI shows that response, so the summary and total update. So the order summary is driven by **what the store returns** from the checkout price API.

## What our widget does today

We use every extension/API path we can find:

- **CheckoutAPI.store** – we call `setAdditionalPrices(payload)` and `updateAdditionalPrices(payload)` when those methods exist (they are not in Shoplazza’s public Checkout API docs).
- **window.PaymentEC** – we call `setAdditionalPrices(payload)` when it exists (may only be on payment step).
- **POST /api/checkout/price** – we send `additional_prices: [{ name: "cd_insure_item_protection", price: "40.00", fee_title: "Item protection" }]`. The store responds with `additional_prices: []` and a total that doesn’t include our fee.
- **Cart API** – we add/remove the Item Protection product as a line; the cart updates but the checkout **order** (and its summary) was created when they entered checkout, so the summary doesn’t reflect the new line.

The **documented** Checkout API only has read/listen methods (`getPrices()`, `onPricesChange(cb)`). It does not document a way for the extension to *set* additional prices or add a fee line so the summary updates.

## What to do

**Ask Shoplazza** (e.g. [partners@shoplazza.com](mailto:partners@shoplazza.com) or Partner Center support):

- “We have a **checkout UI extension** with a fee toggle (Item Protection). When the customer toggles it ON, we need the **order summary and total** to update (like when they select shipping). Which API should the extension use to add our fee and update the summary? For example: is there a `setAdditionalPrices` or similar on `CheckoutAPI.store`, or should we use another endpoint/method?”

Use the copy-paste request in **docs/SHOPLAZZA_SUPPORT_CHECKOUT.md** (updated to ask for the extension API, not “package” registration).

## Debug: see what the store exposes

With **debug on** (e.g. `?cd_debug=1` on the checkout URL, or `localStorage.setItem('cd_insure_debug','1')`), the widget logs the list of **CheckoutAPI.store** method names. That shows what’s actually available on your checkout page so you or Shoplazza can confirm the right method to call.
