# Shoplazza Purchase Protection Plugin – Documentation Overview

This document summarizes the [Shoplazza Dev Center](https://www.shoplazza.dev/reference/overview-23) and related docs for building a **public app** that adds **purchase protection** at checkout and an **admin UI** for policies and claims.

---

## 1. Public app creation and OAuth handshake

### 1.1 Create the app (Partner Center)

- Log in at [partners.shoplazza.com](https://partners.shoplazza.com/) → **Apps** → **Create app**.
- Set an **App name** (shown in store admin).
- **Client ID** and **Client Secret** are generated automatically (see App settings).

### 1.2 App URL and Redirect URL (required for testing)

In **App setup** → **Setup** you configure:

| Setting        | Purpose |
|----------------|--------|
| **App URL**    | Where Shoplazza sends the merchant during install. Used for the **authorization request**. Shoplazza passes store name, store ID, HMAC, and installation source (e.g. `https://your-app.com/api/auth`). |
| **Redirect URL** | Where the merchant lands **after** they accept permissions. Your app’s “home” (e.g. `https://your-app.com/api/auth/callback`). Required to unlock **Run validate** and testing. |

- You must complete OAuth **before** doing anything else, even if the merchant has installed/uninstalled before.
- Use **OAuth 2.0** only; no asking merchants for private API keys (public apps only).
- Avoid OAuth in pop-ups; redirect in the same window so the merchant ends up on your Redirect URL.

### 1.3 Install flow (high level)

1. Merchant clicks **Install app** (App Store or “Test your app” with a development store).
2. Shoplazza redirects to your **App URL** with query parameters (e.g. `shop`, `hmac`, `code`, `timestamp` – exact names in install/signature docs).
3. Your backend **verifies HMAC** (see Signature & Verification), then exchanges the `code` for an **access token** (see “3. Develop your application service” / token exchange in the reference).
4. You store **shop domain** (or shop ID) and **access token** per store.
5. You redirect the merchant to your **Redirect URL** (e.g. your admin UI or a “success” page).

The exact OAuth endpoints and parameters are in the reference under **Authentication** and **3.Develop your application service**; the Help Center confirms App URL vs Redirect URL and that Client ID/Secret come from the partner dashboard.

---

## 2. Signature and verification (HMAC)

- **Algorithm**: HMAC-SHA256.
- **Webhooks**: Header `X-Shoplazza-Hmac-Sha256`; value is HMAC of the **raw request body** using your **Client Secret**. Verify by recomputing HMAC on your side and comparing.
- **Install / callback**: Install request to your App URL is also signed (verify before exchanging the code). Details are in the **Signature & Verification** reference.
- **Best practice**: Optionally check that a `timestamp` query param is within a reasonable window to limit replay.

---

## 3. Admin UI inside Shoplazza admin

- Your app is shown in the merchant’s **Store Admin**; when they open it, Shoplazza loads your app in an **iframe**.
- The iframe `src` includes a **host** parameter (often base64). You need this for **App Bridge** and **session token**.
- **Session token** (reference: [Session Token](https://shoplazza.dev/reference/session-token)):
  - Short-lived (e.g. 1 minute); get a new one for each request from the frontend.
  - Obtained via **App Bridge** (e.g. `getSessionToken()`), then sent in a header to **your backend**.
  - Your backend validates the token (JWT signed with the shared secret) to know the request is from the embedded app and which merchant/shop it is. It does **not** replace the stored **access token** for calling Shoplazza APIs; you still use the access token for Admin API calls.
- **App Bridge** (reference: App Bridge → Overview, Actions) is used for:
  - Getting the session token.
  - Optional UI actions (e.g. Contextual Save Bar, Back link, Redirect) for a native admin feel.

So: **Admin UI** = your frontend (React/Next/etc.) loaded in the iframe → uses App Bridge to get session token → sends it to your backend → backend verifies and uses the stored access token for that shop to call Shoplazza APIs and your own DB (policies, claims).

---

## 4. Checkout widget (purchase protection % of cart)

### 4.1 Checkout API (not theme script tags)

- **Script tags** can target storefront pages (e.g. cart, product) but **not** checkout or thank-you pages (see [Script Tag](https://shoplazza.dev/reference/script-tag) and display_scope).
- Checkout is extended via the **Checkout UI Extension** and the **Checkout API**: [Checkout API](https://www.shoplazza.dev/docs/checkout-api).

### 4.2 Checkout API – what you need for “% of cart”

- **Prices**: `CheckoutAPI.store.getPrices()` returns a `CheckoutPrices` object: `subtotalPrice`, `shippingPrice`, `taxPrice`, `totalPrice`, `paymentDue`, etc., and **`additionalPrices`** (array).
- **Price updates**: `CheckoutAPI.store.onPricesChange(cb)` so your widget can react when cart or other fees change.
- **Additional prices**: `OrderInfo` and `CheckoutPrices` include `additionalPrices?: AdditionalPrice[]`. So the platform supports **extra line items/fees** at checkout; your extension would add one “Purchase protection” fee (e.g. X% of subtotal or total).
- The exact API to **set** or **push** an `AdditionalPrice` from the extension (e.g. a single “add purchase protection” call) is in the Checkout UI Extension docs or extension configuration; the Checkout API doc confirms that such fees exist and are part of the order.

So the **checkout widget** is a **Checkout UI Extension** that:

1. Renders a small UI (checkbox/toggle + short copy like “Add purchase protection (X% of cart)”).
2. Uses `CheckoutAPI.store.getPrices()` to get current totals.
3. Computes the protection amount (e.g. percentage of `totalPrice` or `subtotalPrice`).
4. Uses the extension API to add/update an **additional price** (purchase protection).
5. Subscribes to `onPricesChange` to keep the protection amount in sync when the cart changes.

### 4.3 Order and “thank you” page

- `CheckoutAPI.store.getOrderInfo()` gives `orderNo`, `status`, `financialStatus`, `additionalPrices`, etc.
- On the thank-you page you can read order details to confirm the protection was applied and, if needed, send them to your backend (e.g. to create a “policy” record when `status` is placed/paid).

---

## 5. Webhooks (policies sold and data sync)

- **orders/create**: Fired when an order is created. Payload includes order details, line items, pricing, customer, etc. Requires **`order`** access scope.
- **Headers**: `X-Shoplazza-Topic`, `X-Shoplazza-Hmac-Sha256`, `X-Shoplazza-Shop-Domain`, `X-Shoplazza-Api-Version`, `X-Shoplazza-Deduplication-ID`.
- **Verify**: Always verify `X-Shoplazza-Hmac-Sha256` with your Client Secret and raw body.

Use **orders/create** (and optionally **orders/paid** if available) to:

- Detect when an order contains your purchase protection fee (e.g. via `additionalPrices` or a line item / metafield you define).
- Create a **policy** record in your DB (store_id, order_id, amount, customer, etc.) for the “policies sold” list in admin.

**Claims** are your own business logic: you can expose a “File a claim” flow (storefront or link in email) and store claims in your DB, then show them in the admin UI next to policies.

---

## 6. APIs and scopes you’ll need

| Need | Scope / API |
|------|-------------|
| OAuth & install | App URL + Redirect URL; HMAC verification; token exchange (reference: 3. Develop your application service). |
| Orders (read/create/update) | `order` scope; Order APIs (list, get, create, update, etc.). |
| Webhooks (orders/create) | `order` scope; Webhook API (create webhook, topic `orders/create`). |
| Admin UI (per-store data) | Session token for auth; your backend uses stored **access token** for Shoplazza APIs. |
| Checkout extension | Checkout UI Extension (and Checkout API as above). No extra scope beyond what the extension gets in its config. |
| Optional: Script tag (e.g. cart page) | `write_script_tags` if you inject scripts on storefront (not checkout). |

Access scopes are configured in the app (Partner Center / App setup). The reference lists them under **Access Scopes** ([scope-list](https://shoplazza.dev/reference/scope-list)).

---

## 7. Suggested architecture (high level)

1. **Backend (your server)**  
   - **App URL** (e.g. `/api/auth`): Receives install redirect; verifies HMAC; exchanges code for access token; stores shop + token; redirects to Redirect URL.  
   - **Redirect URL** (e.g. `/admin` or `/`): Loads your admin app (or redirects to it).  
   - **Session token verification**: Endpoint used by the admin frontend; validates JWT from App Bridge; returns shop-scoped data or calls Shoplazza APIs with stored access token.  
   - **Webhook endpoint** (e.g. `/webhooks/orders`): Verifies HMAC; parses `orders/create`; if order has purchase protection, creates policy in DB.  
   - **Policies & claims API**: List/filter policies and claims for the admin UI (by shop, date, etc.).

2. **Admin UI (embedded in Shoplazza)**  
   - Served at a URL that Shoplazza loads in the iframe (often same host as Redirect URL or a path like `/admin`).  
   - Uses App Bridge to get session token; sends it to your backend on each request.  
   - Two main views: **Policies sold** (table/list from your DB) and **Claims** (table/list + maybe status updates).  
   - Optional: settings page (e.g. protection % default, enabled/disabled) stored per shop in your DB or via Shoplazza metafields.

3. **Checkout UI Extension**  
   - Renders the purchase protection widget.  
   - Uses Checkout API to get prices and add an **additional price** (percentage of cart).  
   - Optionally listens to `onPricesChange` and step/order info so the fee stays correct and you can later correlate with webhook payloads (e.g. `orderNo`, `additionalPrices`).

4. **Database (your DB)**  
   - **Stores**: shop_domain (or id), access_token, installed_at.  
   - **Policies**: id, store_id, order_id, order_number, amount, currency, customer info, created_at.  
   - **Claims**: id, policy_id (or order_id), status, description, created_at, updated_at.

---

## 8. References (official)

- [Shoplazza Dev Center – Overview](https://www.shoplazza.dev/reference/overview-23)  
- [Quick Construction of Public Apps](https://shoplazza.dev/reference/quick-construction-of-public-apps)  
- [Building Public App (Help Center)](https://helpcenter.shoplazza.com/hc/en-us/articles/4409360434201)  
- [2.Create A Public APP](https://shoplazza.dev/reference/2create-a-public-app)  
- [3.Develop your application service](https://shoplazza.dev/reference/3develop-your-application-service)  
- [Authentication](https://shoplazza.dev/reference/app-auth)  
- [Signature & Verification](https://shoplazza.dev/reference/signverify-sign)  
- [Installation and setup](https://shoplazza.dev/reference/installation-and-setup)  
- [Session Token](https://shoplazza.dev/reference/session-token)  
- [Checkout API](https://www.shoplazza.dev/docs/checkout-api)  
- [Webhooks](https://shoplazza.dev/reference/webhooks), [Webhook Properties](https://shoplazza.dev/reference/webhook-properties)  
- [Access Scopes](https://shoplazza.dev/reference/scope-list)  
- [App blocks for themes](https://shoplazza.dev/docs/app-blocks-for-themes) (for storefront; checkout uses Checkout UI Extension)

---

## 9. Gaps to fill from Shoplazza docs

- **Exact OAuth params** on the App URL (query param names for shop, code, hmac, timestamp).  
- **Token exchange**: URL, method, body/params for exchanging `code` for access token.  
- **Checkout UI Extension**: How to create and deploy it (e.g. `shoplazza-cli`, extension type, `extension.toml` or equivalent), and the **exact API to add/update `AdditionalPrice`** from the extension.  
- **App Bridge** in Shoplazza: Exact script URL, `createApp()` / `getSessionToken()` API, and host param handling.  
- **Webhook registration**: Create Webhook API endpoint and request shape (topic `orders/create`, notification URL).

These are all in the Shoplazza reference; once you have the exact endpoints and payloads from the docs (or “Try it” in the reference), you can wire the above flow end-to-end.
