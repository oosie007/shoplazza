# CD Insure Checkout Extension

Shoplazza **Checkout UI Extension** that shows the Item Protection widget on checkout (Contact Information and Payment steps). When merchants install your app, Shoplazza injects this extension automatically.

## Prerequisites

- [Shoplazza CLI](https://www.shoplazza.dev/docs/getting-started-2) installed: `npm install -g shoplazza-cli`
- Your app URL (where `checkout-widget.js` is served), e.g. your ngrok URL or production domain

## Setup

1. **Set your app URL**  
   Edit `config.js` and set `APP_URL`, or set env when building:
   ```bash
   export APP_URL=https://your-app.ngrok-free.app
   # or
   export NEXT_PUBLIC_APP_URL=https://your-app.ngrok-free.app
   ```

2. **Build the extension**
   ```bash
   cd checkout-extension
   npm run build
   ```
   This writes `dist/extension.js` with your app URL inlined.

## Development (Shoplazza CLI)

1. **Create / link extension**  
   If you haven’t already, create a checkout extension project:
   ```bash
   shoplazza checkout create
   ```
   When prompted, enter your store URL and token (from **Apps → Manage Private Apps → Create App**). You can then replace the generated extension code with this project’s `dist/extension.js` and `extension.json`, or copy `extension.json` and the built script into the generated project.

2. **Local dev**
   ```bash
   shoplazza checkout dev
   ```
   Open the checkout in the browser and run in the console:
   ```js
   CheckoutAPI.extension.DEV_switchDevMode();
   ```
   You should see the Item Protection widget after the contact information (and before payment when you reach that step).

3. **Preview**
   ```bash
   shoplazza checkout push
   ```
   Use the preview URL to test on the store.

4. **Deploy**
   ```bash
   shoplazza checkout deploy
   ```
   After deploy, stores that have your app installed will get the widget on checkout. Use **Activate** in your app’s Configuration to control whether the widget is shown (your widget script reads `offerAtCheckout` from the app API).

## Partner Center (upload zip)

To upload the extension without using the CLI:

1. From the **repo root**: `npm run zip:extension`
2. This creates **`checkout-extension/cd-insure-checkout-extension.zip`** (contains `extension.json` + `dist/extension.js`).
3. In **Partner Center** → your app (CD_Insure) → **Checkout** / **Extensions** → Add extension → upload the zip.
4. Publish the app. New installs will get the extension on checkout; “Activate” in your app config toggles visibility via your backend.

See **[docs/CHECKOUT_EXTENSION_SETUP.md](../docs/CHECKOUT_EXTENSION_SETUP.md)** for full step-by-step (CLI and Partner Center).

## Files

| File | Purpose |
|------|--------|
| `extension.json` | Shoplazza extension config (`templateName: checkout`, `extensionName`). |
| `src/extension.js` | Entry: calls `extend()` for `ContactInformation::RenderAfter` and `SectionPayment::RenderBefore`, injects a div and loads your `checkout-widget.js`. |
| `config.js` | `APP_URL` for the widget script (build-time). |
| `build.js` | Replaces `__APP_URL__` in the source and outputs `dist/extension.js`. |

## Extension points used

- **Checkout::ContactInformation::RenderAfter** – Widget below contact/shipping (same as “Worry-Free Delivery”).
- **Checkout::SectionPayment::RenderBefore** – Widget before the payment section so it appears on the payment step as well.
