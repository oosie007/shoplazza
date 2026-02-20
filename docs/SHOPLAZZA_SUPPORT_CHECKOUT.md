# Shoplazza support: checkout extension and order summary update

We have a **checkout UI extension** that shows an "Item protection" toggle on checkout. There is no separate "package" in Shoplazza's UI – the extension is what we use.

**What we need:** When the customer toggles Item Protection ON on the checkout page, the **order summary** (line items + total on the right) should update to show the fee and new total, the same way it does when they select shipping.

If the extension only appears with a preview link (not in normal/incognito checkout), you may also need to ask Shoplazza how to **link the extension to the app** so it runs when the app is installed.

---

## Who to contact

- **Partner Center support:** [partners@shoplazza.com](mailto:partners@shoplazza.com)
- Or use the support/help option inside [Partner Center](https://partners.shoplazza.com) (e.g. Help, Live chat, or Contact).

---

## Copy-paste request (order summary update from extension)

You can send something like this (adjust store/extension names if needed):

**Subject:** Checkout extension: how to update order summary and total when customer toggles fee

**Body:**

We have a Shoplazza app and a **checkout UI extension** (deployed via CLI: `shoplazza checkout push` / `shoplazza checkout deploy`). The extension shows an "Item protection" fee toggle on checkout.

When the customer toggles the fee ON, we need the **order summary** (right-hand side: line items and total) to update to include our fee and the new total – the same behaviour as when they select a shipping method. Right now the summary does not update.

We are already:
- Calling `CheckoutAPI.store.setAdditionalPrices` / `updateAdditionalPrices` when those exist
- Sending `additional_prices` in `POST /api/checkout/price`
- Adding the product via the Cart API (the cart updates, but the checkout order summary does not)

The public Checkout API docs only show read/listen methods (`getPrices()`, `onPricesChange()`). **Which API or method should our checkout extension use to add our fee and make the order summary and total update** when the customer toggles it? (e.g. a specific method on `CheckoutAPI.store`, or another endpoint we should call?)

Our app is installed on the test store **oostest.myshoplaza.com**. Extension name/ID: **cd-insure-item-protection-v2** (we can provide the extension ID from the CLI if needed).

Thank you.

---

(If the widget only appears with a preview link and not in normal checkout, add: "We also need to know how to link this extension to our app in Partner Center so it runs for all visitors when the app is installed.")

---

After they reply, update this doc or your runbook with the steps they give you.
