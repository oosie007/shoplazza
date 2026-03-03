# Checkout API: Why "modify" doesn't add the insurance line

Shoplazza’s Checkout Extension overview says you can:

- **Access and modify order information**
- **Use the CheckoutAPI to retrieve order details and adjust checkout behaviors as needed**

So it’s reasonable to expect that `CheckoutAPI` can **modify** the checkout (e.g. add an insurance fee and update the total). In practice it doesn’t work that way with the **published** API.

## What the Checkout API reference actually documents

The [Checkout API reference](https://www.shoplazza.dev/docs/checkout-api) states:

> The Checkout UI Extension provides an API for **retrieving** information.

It only documents **read** operations:

| Area        | Methods documented |
|------------|---------------------|
| Order      | `getOrderInfo()`, `getOrderStatus()`, `getReferInfo()` |
| Prices     | `getPrices()`, `onPricesChange()`, `removePricesChangeCb()` |
| Products   | `getProductList()` |
| Address    | `getShippingAddress()`, `onShippingAddressChange()`, … |
| Step       | `getStep()`, `stepNavTo*()`, `onStepChange()`, … |
| User       | `isLogin()`, `doLogin()`, … |

There is **no** documented method to:

- set or update `additionalPrices`
- add a fee / line item to the checkout
- change the total

So “modify” in the overview is at least ambiguous: the **reference** only supports **retrieving** and **reacting** (e.g. with `onPricesChange`), not **writing** prices or fees.

## What we tried

1. **`CheckoutAPI.store.setAdditionalPrices`** – Not in the docs. We call it when it exists; on your store it either doesn’t exist or doesn’t update the total.
2. **`window.PaymentEC` / `PaymentEl`** – Console shows it as null on the information step; may only be available on the payment step and is undocumented.
3. **Store endpoints** – `POST /api/checkout/pkg_set` then `POST /api/checkout/price`. This is how the working “Worry-Free Delivery” widget updates the total. For our app, **pkg_set returns 404** because the store has no handler for our app until we register a checkout package.

So the **CheckoutAPI** (as documented) does **not** provide a way to add the insurance line and update the total. The behavior that works today is: **package registration + store `pkg_set` + `price`**, not a CheckoutAPI setter.

## What to ask Shoplazza

To align the docs with reality (or get a supported way to add fees), you can ask support or the dev team:

1. **Is there a documented way to add an additional price / fee from a Checkout UI Extension?**  
   - If yes: which method or API (e.g. `setAdditionalPrices`, Function API, something else)?  
   - If the only way is “register a checkout package and use store `pkg_set`”, that should be stated in the Checkout Extension / Checkout API docs.

2. **Where do we register a “checkout package” (e.g. item protection) so the store exposes `pkg_set` for our app and the total updates when we call `price`?**

3. **Does the “modify order information / adjust checkout behaviors” wording in the overview refer to:**  
   - (a) Writing prices/fees via CheckoutAPI, or  
   - (b) Changing our extension’s behavior based on read-only data, or  
   - (c) Another mechanism (e.g. Function API, package registration)?

Once there is an official, documented way to add a fee from an extension (or to register a package so `pkg_set` works), we can wire our widget to that and the insurance line + total will update as expected.
