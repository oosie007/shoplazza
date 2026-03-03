# Item Protection – Shoplazza app

Purchase-protection app for Shoplazza: merchants configure coverage in an **admin portal**; customers see an **Item Protection** toggle on **checkout**. When they turn it on, the goal is for the **checkout order summary** to add the fee as a line and update the total. The widget and extension work; the missing piece is getting the **summary and total to update** (see “Where we’re stuck” below).

---

## For the new developer

- **This README** is the main entry point: architecture, admin, extension, widget, and what’s blocked.
- **Admin portal:** Next.js app at `/admin` (Configuration, Policy, Claims, Settlement). Merchants set pricing, create the Item Protection product, and bind the Cart Transform.
- **Checkout:** A **Checkout UI Extension** (Shoplazza) injects a container and loads **`checkout-widget.js`** from this app. The widget fetches settings, shows the toggle, and on toggle calls Cart API + store price API; the **order summary and total do not update** with current platform behaviour.
- **Stuck:** The public Checkout API has no way for the extension to add a fee line or set prices. We need Shoplazza to tell us the correct API or flow (or to accept our `additional_prices` in the price response). See the section **“Where we’re stuck”** and **“What we need”** below.
- The **`docs/`** folder has many `.md` files (setup, extension, troubleshooting, support request text). Use them for deeper dives; this README is the single overview.

---

## What the app does

| Who | Where | What |
|-----|--------|------|
| **Merchant** | Admin portal (`/admin`, etc.) | Configure protection (pricing, default-on), create Item Protection product, bind Cart Transform, view Policy/Claims/Settlement (placeholders). |
| **Customer** | Store checkout | See Item Protection widget (Contact + Payment steps). Toggle on/off. **Intended:** fee appears in order summary and total updates. **Current:** cart can be updated; order summary and total do not. |

---

## Architecture overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Shoplazza Admin (merchant)                                             │
│  Opens app in iframe or redirect → ?shop=store.myshoplaza.com           │
└─────────────────────────────────────────────────────────────────────────┘
                                      │
                                      ▼
┌─────────────────────────────────────────────────────────────────────────┐
│  This app (Next.js)                                                       │
│  • Admin UI: src/app/admin/ (Configuration, Policy, Claims, Settlement)   │
│  • API: src/app/api/ (auth, settings, public-settings, ensure-product,  │
│         bind-cart-transform, checkout/apply-fee, cart-transform, …)      │
│  • DB: Prisma + PostgreSQL (stores, settings)                            │
│  • Static: public/checkout-widget.js (served at APP_URL/checkout-widget.js) │
└─────────────────────────────────────────────────────────────────────────┘
                                      │
         Merchant installs app       │       Checkout page loads extension
         & configures                │       Extension loads widget script
                                      ▼
┌─────────────────────────────────────────────────────────────────────────┐
│  Store checkout (customer)                                               │
│  • Shoplazza runs Checkout UI Extension (our extension)                  │
│  • Extension injects #cd-insure-widget-root + <script src="APP_URL/      │
│    checkout-widget.js">                                                  │
│  • Widget: GET public-settings → render toggle → on toggle: Cart API,     │
│    POST /api/checkout/price with additional_prices, etc.                  │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Admin portal

**Tech:** Next.js App Router, React, Tailwind. Layout: sidebar + main content (`src/app/admin/layout.tsx`).

| Route | Purpose |
|-------|--------|
| **/admin** (Configuration) | Main config: activate app, pricing (fixed % or per-category), widget options, “default at checkout”. Buttons: **Create Item Protection product** (creates product in store via Shoplazza API, saves product/variant IDs in settings) and **Re-bind Cart Transform** (creates/binds WASM function so Shoplazza can set the protection line price when the cart is evaluated). |
| **/admin/policy** | List/search policies (insured orders). Placeholder. |
| **/admin/claims** | List/manage claims. Placeholder. |
| **/admin/settlement** | Revenue share and payouts. Placeholder. |

**Auth:** App is opened from Shoplazza with `?shop=...`. OAuth in `src/app/api/auth/` and `api/auth/callback/`. Store is identified by `shop`; install and settings stored in DB.

**Key APIs used by Configuration:**

- `POST /api/admin/ensure-item-protection` – create Item Protection product in store, store IDs.
- `POST /api/admin/bind-cart-transform` – create + bind Cart Transform function (WASM in `public/cart-transform.wasm`; source `scripts/cart-transform-function.js`).

---

## Checkout extension (how the widget gets on the page)

The widget appears **only on the checkout page** because a **Shoplazza Checkout UI Extension** injects it. There is no separate “package” in the Shoplazza UI; the extension is what we use.

- **Source:** `checkout-extension/src/extension.js`
- **Build:** `npm run build:extension` (injects `NEXT_PUBLIC_APP_URL` into the script).
- **Deploy:** Shoplazza CLI, e.g. `shoplazza checkout deploy` (one-time `shoplazza checkout create` first).

**What the extension does:**

1. Calls Shoplazza’s `extend()` for two extension points:
   - `Checkout::ContactInformation::RenderAfter`
   - `Checkout::SectionPayment::RenderBefore`
2. For each, it injects:
   - A div: `#cd-insure-widget-root`
   - A short inline script that sets `window.CD_INSURE_APP_URL` and `window.SHOPLAZZA_SHOP_DOMAIN`
   - `<script src="{APP_URL}/checkout-widget.js">`

So the **extension** only provides the mount point and loads the widget script. All UI and logic live in **`public/checkout-widget.js`**.

---

## Checkout widget (`public/checkout-widget.js`)

Single self-contained script (no React). Runs on the store’s checkout origin.

**Load and init:**

1. Reads `window.CD_INSURE_APP_URL` and `window.SHOPLAZZA_SHOP_DOMAIN`.
2. Fetches **`/api/public-settings?shop=...`** from this app (pricing mode, %, product/variant IDs, default-at-checkout).
3. Uses **CheckoutAPI** when present (`getPrices()`, `getOrderInfo()`, `summary.getProductList()`, etc.) to compute premium and react to price changes.
4. Renders the toggle and copy into `#cd-insure-widget-root`.

**When the customer toggles ON:**

1. **CheckoutAPI.store** – `setAdditionalPrices(payload)` / `updateAdditionalPrices(payload)` if they exist (not in public docs).
2. **window.PaymentEC** – `setAdditionalPrices(payload)` when present.
3. **POST /api/checkout/price** – sends checkout state + `additional_prices: [{ name: "cd_insure_item_protection", price: "<amount>", fee_title: "Item protection" }]`. The store responds with `additional_prices: []` and a total that does **not** include our fee.
4. **Cart API** – if product/variant IDs are set in Configuration: GET cart → if no Item Protection line, POST add it. Cart updates (200), but the **checkout order summary** is tied to the **order** created at checkout entry, so it does **not** show the new line or a new total.

**When the customer toggles OFF:** Cart API removes the line; price request is sent with `additional_prices: []`.

**Debug:** Add `?cd_debug=1` to the checkout URL or `localStorage.setItem('cd_insure_debug','1')`; widget logs to console and shows a small debug panel (including `CheckoutAPI.store` method names).

---

## Where we’re stuck: order summary and total don’t update

**Goal:** When the customer toggles Item Protection **ON** on the checkout page, the **checkout order summary** (right side: line items and total) should add our fee as a line and recalculate the total (like when they select shipping).

**What actually happens:**

- The **cart** can be updated (we add/remove the Item Protection product via Cart API).
- The **checkout order summary** does **not** change: it reflects the **order** created when the customer entered checkout, not the live cart.
- The documented **Checkout API** only has **`getPrices()`** and **`onPricesChange(cb)`**. There is no documented way for the extension to *set* prices or *add* a line. The summary is driven by whatever the **store** returns (e.g. from its price endpoint).
- We send **`additional_prices`** in **POST /api/checkout/price**; the store responds with **`additional_prices: []`** and a total that excludes our fee.

So we’re blocked on **platform behaviour**: we need either an API that lets the extension add a fee line and update the total, or the store must accept our `additional_prices` and return them (and the updated total) in the price response.

---

## What we need to get the widget toggle to add a line and update totals

One of the following (or equivalent) is needed:

1. **An extension API** that adds a fee / line and updates the total  
   e.g. a documented method on `CheckoutAPI.store` (or similar) that we call from the widget so the checkout summary and total update.

2. **The store applying our fee in its price response**  
   So when we send **`additional_prices`** in **POST /api/checkout/price**, the store includes that fee in the response and in the total, and the checkout UI refreshes from that.

3. **Documented flow** for apps that add a fee at checkout (e.g. how other apps do it), so we can implement the same flow.

**Next step:** Contact Shoplazza (e.g. **partners@shoplazza.com** or Partner Center support) and ask: *“We have a checkout UI extension with a fee toggle. When the customer toggles it ON, we need the order summary and total to update (like shipping). Which API or flow should we use to add our fee and update the summary?”*  
The file **`docs/SHOPLAZZA_SUPPORT_CHECKOUT.md`** has a copy-paste support request and a list of questions you can send.

---

## Repo layout (quick reference)

| Path | Purpose |
|------|--------|
| **src/app/admin/** | Admin portal pages (Configuration, Policy, Claims, Settlement). |
| **src/app/api/** | API routes: auth, settings, public-settings, checkout/apply-fee, admin/ensure-item-protection, admin/bind-cart-transform, shoplazza/cart-transform, etc. |
| **src/components/admin/** | Admin UI (ConfigurationContent, CheckoutWidgetPreview, AdminNav, etc.). |
| **src/lib/shoplazza/** | Shoplazza auth, store API, item-protection product creation, Cart Transform create/bind. |
| **public/checkout-widget.js** | Checkout widget (loaded by the extension on the store’s checkout page). |
| **checkout-extension/** | Checkout UI Extension: injects container and script tag for `checkout-widget.js`. |
| **scripts/** | Helpers (cart-transform WASM build, inject extension app URL, etc.). |
| **docs/** | Extra docs (setup, extension setup, troubleshooting, Shoplazza support text). Use when you need more detail; this README is the main overview. |

---

## Running the app

1. **Env** – Copy `.env.example` to `.env.local`. Set Shoplazza **Client ID**, **Client Secret**, and **`NEXT_PUBLIC_APP_URL`** (public app URL, e.g. ngrok for local dev).
2. **DB** – Prisma + PostgreSQL. Run migrations as needed.
3. **Dev** – `npm run dev`. Use a tunnel (e.g. ngrok) and set Partner Center App URL / Redirect URL to that tunnel.
4. **Extension** – `npm run build:extension`. Deploy with Shoplazza CLI (e.g. `shoplazza checkout deploy`). Extension loads the widget from `NEXT_PUBLIC_APP_URL/checkout-widget.js`.

For detailed setup and Partner Center configuration, see **`docs/SETUP.md`** and **`docs/CHECKOUT_EXTENSION_SETUP.md`**.
