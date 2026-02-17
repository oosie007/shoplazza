# Worry-Free Purchase Plugin – Step-by-Step Implementation Plan

This document breaks down building an **exact replica** of the Worry-Free Purchase plugin into ordered steps. It maps each screen and feature from the reference UI to concrete tasks.

---

## Overview of What You’re Building

| Area | What it does |
|------|----------------|
| **Checkout widget** | Toggle at checkout: “Worry-Free Purchase” with shield icon, description “Protect against loss, damage, delay”, price (e.g. % of cart), and a switch. When ON, an “Insurance fee” line is added and the total updates. |
| **Admin – Configuration** | Activate/Deactivate the plugin, “View terms” link, benefits list, intro copy, revenue-sharing options (Activate 15% / Deactivate 5%), Claim portal setup (“Configure it now”), optional metric cards, and a preview of the checkout widget. |
| **Admin – Policy** | Table of all policies sold: search (order ID / policy ID / claim no.), filters (Claim type, Claim status), columns: Associated order ID, Created at, Insurance ID, Insured items, Declared value, Premium, Currency, Status (Active/Expired). |
| **Admin – Claims** | List and manage claims (type, status, linked to policy/order). |
| **Admin – Invoices** | Billing/invoices for the plugin (optional for v1). |

---

## Phase 1: Foundation (App + Auth + Backend)

### Step 1.1 – Shoplazza app and OAuth

1. **Partner Center**
   - Create app at [partners.shoplazza.com](https://partners.shoplazza.com/) → Apps → Create app.
   - Note **Client ID** and **Client Secret** from App settings.

2. **URLs**
   - **App URL**: e.g. `https://your-domain.com/api/auth` (where Shoplazza sends the merchant on install).
   - **Redirect URL**: e.g. `https://your-domain.com/api/auth/callback` or `https://your-domain.com/admin` (where the merchant lands after accepting).
   - Add both in App setup → Setup so you can run “Validate” and test.

3. **Backend – Install handler (App URL)**
   - Implement `GET (or POST) /api/auth` that:
     - Reads query params (shop, hmac, code, timestamp – confirm names in Shoplazza docs).
     - Verifies HMAC (HMAC-SHA256 with Client Secret).
     - Exchanges `code` for an access token (Shoplazza token exchange endpoint).
     - Stores in your DB: `shop_domain`, `access_token`, optionally `shop_id`.
     - Redirects the merchant to your **Redirect URL**.

4. **Backend – Callback / redirect**
   - Redirect URL can serve your admin app (e.g. `/admin`) or a simple “Installation complete” page that links to the app in Shoplazza.

**Deliverable:** Merchant can install the app; you have a stable access token per shop.

---

### Step 1.2 – Database and store settings

1. **Tables**
   - **stores**: `id`, `shop_domain`, `access_token`, `installed_at`, `created_at`, `updated_at`.
   - **store_settings**: `id`, `store_id`, `activated` (boolean), `revenue_share_tier` (e.g. `"15"` vs `"5"`), `protection_percent` (e.g. 5), `claim_portal_configured` (boolean), `created_at`, `updated_at`.
   - **policies**: `id`, `store_id`, `order_id`, `order_number`, `insurance_id` (your policy number, e.g. `1200000270`), `insured_items` (text or JSON), `declared_value`, `premium`, `currency`, `status` (`active` / `expired`), `created_at`, `updated_at`.
   - **claims**: `id`, `store_id`, `policy_id`, `claim_no`, `claim_type`, `claim_status`, `description`, `created_at`, `updated_at`.

2. **Default settings**
   - On install (or first load of Configuration), create `store_settings` with `activated = false`, `protection_percent = 5` (or your default), `revenue_share_tier = "5"`.

**Deliverable:** Schema ready for Configuration, Policy, and Claims.

---

### Step 1.3 – Webhooks (orders → policies)

1. **Register webhook**
   - After storing the access token, call Shoplazza’s “Create webhook” API: topic `orders/create`, address `https://your-domain.com/webhooks/orders`.

2. **Webhook endpoint**
   - `POST /webhooks/orders`:
     - Read raw body; verify `X-Shoplazza-Hmac-Sha256` with Client Secret.
     - Parse JSON; identify the shop (e.g. `X-Shoplazza-Shop-Domain` or payload field).
     - Determine if this order has purchase protection (e.g. `additionalPrices` entry for your fee, or a specific line item / metafield).
     - If yes: create a row in **policies** (store_id, order_id, order_number, declared_value from order total, premium from fee amount, currency, status `active`, generate `insurance_id`).

**Deliverable:** Every paid order that included the protection fee gets a policy row for the Policy table.

---

## Phase 2: Admin UI (Embedded in Shoplazza)

### Step 2.1 – Embedding and session token

1. **App entry URL**
   - The URL Shoplazza loads in the iframe (e.g. `https://your-domain.com/admin` or the path you set in the partner dashboard). Serve your admin SPA here.

2. **App Bridge + session token**
   - In your admin frontend:
     - Load Shoplazza App Bridge (script from Shoplazza docs; use `host` from iframe query).
     - On each API call to your backend, call `getSessionToken()`, put it in a header (e.g. `Authorization: Bearer <session_token>`), then send the request.

3. **Backend – session verification**
   - Endpoint that admin calls (e.g. `/api/session` or per-request middleware):
     - Validate the session token (JWT with Client Secret).
     - Resolve shop from token (or from request context Shoplazza injects).
     - Load `access_token` for that shop from **stores**; use it for any Shoplazza API calls.
     - Return shop-scoped data (settings, policies, claims) from your DB.

**Deliverable:** Admin UI loads inside Shoplazza; every request is authenticated and shop-scoped.

---

### Step 2.2 – Layout and navigation

1. **Shell**
   - Left sidebar: logo + “Worry-Free Purchase”, then nav links: **Configuration**, **Policy**, **Claims**, **Invoices**.
   - Main content area: one column for the active section (Configuration, Policy, or Claims).
   - Use the same layout on every admin page so it matches the reference.

2. **Routing**
   - Routes: `/admin` (or `/`) → default to Configuration; `/admin/config`, `/admin/policy`, `/admin/claims`, `/admin/invoices`. Or use hash routing if the host is fixed.

**Deliverable:** Navigation and layout match the reference; switching sections works.

---

### Step 2.3 – Configuration page

1. **Top metrics (optional for v1)**
   - Three cards: “Estimated conversion rate” (e.g. 5%), “Rate with ‘Default purchase’ off” (e.g. 100%), “Rate with ‘Default purchase’ on” (e.g. 100%). Can be placeholders or computed later from your analytics.

2. **Worry-Free Purchase card**
   - Title “Worry-Free Purchase” + status pill: **Activated** (green) or **Deactivated** (grey).
   - Short description: “Allow your customers to add Worry-Free Purchase services on the checkout page to protect their orders from loss, damage, and theft.” + **View terms** link (your terms URL).
   - Button: **Activate** or **Deactivate** (toggles `store_settings.activated` via your API).

3. **Benefits list**
   - Three bullets with green checkmarks:
     - Protecting orders from issues such as loss, damage, and delay.
     - Reduce and eliminate customer complaints arising from shipping-related issues.
     - Increase your conversion rate and obtain premium sharing.

4. **Worry-Free Purchase Introduction**
   - Heading “Worry-Free Purchase Introduction”.
   - Radio options:
     - “Activate To earn a sharing of 15%” (when plugin is activated).
     - “Deactivate To earn a sharing of 5%” (when plugin is deactivated).
   - Persist selection in `store_settings.revenue_share_tier` (or similar).

5. **Claim portal**
   - Text: “To provide consumers with ‘Claim portal’ and ‘View policy’ options in the order details, you will need to configure the ‘Worry-Free Purchase’ card in Online Store. **Configure it now**.”
   - “Configure it now” links to Shoplazza theme/order customization or your doc (exact target from Shoplazza docs).

6. **Intro copy**
   - “Overview” subsection with the paragraph: “Worry-Free Purchase is a service provided by a third party to ensure the smooth transportation of goods, covering potential issues such as loss, damage, and delay. Businesses can enhance customer trust and improve conversions by offering this…”

7. **Checkout widget preview**
   - Right side (or below on mobile): a static or simple interactive preview of the checkout widget (order summary with “Insurance fee”, “Worry-Free Purchase” toggle, price). Can be HTML/CSS only; no need for real Checkout API here.

**API needed**
- `GET /api/settings` → returns `activated`, `revenue_share_tier`, `protection_percent`, etc. for the current shop.
- `PATCH /api/settings` → body `{ activated, revenue_share_tier, … }`; update `store_settings`.

**Deliverable:** Configuration page matches the reference; activate/deactivate and revenue share option persist.

---

### Step 2.4 – Policy page

1. **Title**
   - “Manage your own Worry-Free Purchase as an upsell.”

2. **Search**
   - Input: placeholder “Please enter the order ID / policy ID / claim no.” Search across `order_number`, `insurance_id`, and claim numbers; filter the policies (and optionally show related claims).

3. **Filters**
   - **Claim type** dropdown (e.g. damage, loss, delay, theft – can be claim types you define).
   - **Claim status** dropdown (e.g. Pending, Approved, Rejected).  
   These can filter the *policies* that have at least one claim matching, or a combined view; clarify if the reference shows “policies with claims” or “all policies + claim filters”. For a replica, implement both: policy list + optional filter by claim type/status when a policy has claims.

4. **Table**
   - Columns: **Associated order ID** (order_number), **Created at** (sortable), **Insurance ID**, **Insured items** (product names or summary), **Declared value**, **Premium**, **Currency**, **Status** (badge: Active = green, Expired = grey).

5. **Policy button**
   - “Policy” button with checkmark icon (e.g. primary action or tab indicator). Matches reference.

**API**
- `GET /api/policies?search=&claimType=&claimStatus=&page=&limit=` → list policies for the shop with optional search and filters; return total for pagination.

**Deliverable:** Policy table with search, filters, and status badges; data from **policies** (and webhook-created rows).

---

### Step 2.5 – Claims page

1. **List view**
   - Table or cards: claim number, linked policy/order, claim type, claim status, date, short description.
   - Search/filter by claim no., status, type.

2. **Detail / actions (optional)**
   - Open a claim to see full description and update status (e.g. Pending → Approved/Rejected).

**API**
- `GET /api/claims?search=&type=&status=&page=&limit=`
- `PATCH /api/claims/:id` (e.g. update `claim_status`).

**Deliverable:** Claims list (and optional detail) fed from **claims** table.

---

### Step 2.6 – Invoices page (optional for v1)

- Placeholder or simple list of “invoices” (e.g. your revenue share or subscription charges). Can be Phase 2.

---

## Phase 3: Checkout Widget (Checkout UI Extension)

### Step 3.1 – Extension project and deploy

1. **Create Checkout UI Extension**
   - Use Shoplazza’s recommended way (e.g. `shoplazza-cli` or app project that includes an extension). Define the extension so it runs on the checkout page (target from Shoplazza docs).

2. **Deploy**
   - Extension must be deployed and linked to your app so that when the app is installed, the checkout UI is injected.

**Deliverable:** Extension runs in Shoplazza checkout.

---

### Step 3.2 – Widget UI and pricing

1. **UI (match reference)**
   - Blue shield icon (left).
   - Title: “Worry-Free Purchase” + small info (?) icon that can show a tooltip/modal with short copy.
   - Line: “Protect against loss, damage, delay.”
   - Price on the right (e.g. “$0.98” or “$10.00”).
   - Blue toggle switch (right): ON = protection added, OFF = not added.

2. **Pricing logic**
   - Call `CheckoutAPI.store.getPrices()` to get `subtotalPrice` or `totalPrice` (and currency).
   - Fetch store setting for protection % (via your backend from the extension, or a fixed default if you can’t pass config yet).  
   - Premium = (subtotal or total) × (protection_percent / 100). Round to 2 decimals.
   - Subscribe to `CheckoutAPI.store.onPricesChange()` and recompute when cart changes.

3. **Add fee to checkout**
   - Use the Shoplazza Checkout API to add an **additional price** (single line: “Worry-Free Purchase” or “Insurance fee”) with the computed amount. Exact method name is in Checkout UI Extension docs (e.g. something like `setAdditionalPrice` or adding to `additionalPrices`).
   - When toggle is OFF, remove or set that additional price to 0.

4. **Visibility**
   - Only show the widget (or only add the fee) when the store has `activated = true`. You may need to pass this from your backend to the extension (e.g. app proxy or a small config endpoint the extension calls).

**Deliverable:** Checkout shows the Worry-Free Purchase block; toggle adds/removes the insurance fee; total updates; order payload includes your fee so the webhook can create a policy.

---

### Step 3.3 – Store settings in checkout

- If the extension can call your backend (same-origin or CORS): `GET /api/settings/public?shop=...` returning only `activated` and `protection_percent` (no secrets). Use for visibility and premium calculation. If not, hardcode default % and rely on app install = enabled for that shop.

---

## Phase 4: Claim portal and “View policy” (post-purchase)

### Step 4.1 – Where it appears

- Reference says: “Claim portal” and “View policy” in **order details** (for the consumer). That’s usually the storefront “Order status” or “Account → Order history” page, or an email link.

### Step 4.2 – Implementation options

1. **Theme / App block**
   - If Shoplazza supports app blocks or script tags on the “order status” or “order details” page, add a “Worry-Free Purchase” card with:
     - “View policy” → link to your hosted policy page (e.g. `https://your-domain.com/policy/:orderId?token=...`).
     - “Claim portal” → link to your claim form (e.g. `https://your-domain.com/claim?order=...&token=...`).
   - “Configure it now” in Configuration can point to Shoplazza’s theme editor or instructions to add this block.

2. **Email**
   - After order paid, send an email (via Shoplazza or your backend) with “View policy” and “File a claim” links.

3. **Your hosted pages**
   - **View policy**: Page that shows policy details (order, coverage, premium, status) for a given order/token.
   - **Claim portal**: Form to submit a claim (order/policy, claim type, description); creates a row in **claims** and shows “Submitted.”

**Deliverable:** Customers can view their policy and submit a claim; claims show in Admin → Claims.

---

## Phase 5: Polish and compliance

- **Terms**: “View terms” on Configuration points to a hosted terms-of-service page.
- **Policy status**: Background job or cron to mark policies as `expired` after X days (if you have a coverage period).
- **Validation**: Run Shoplazza’s “Validate” in Partner Center and fix any install/OAuth or permission issues.
- **App listing**: Prepare listing content (description, screenshots) for the App Store if you’re going public.

---

## Order of implementation (summary)

1. **Phase 1**: App + OAuth, DB, webhooks (install + orders → policies).
2. **Phase 2**: Admin UI (session token, layout, Configuration, Policy, Claims).
3. **Phase 3**: Checkout UI Extension (widget + additional price + toggle).
4. **Phase 4**: Claim portal + View policy (theme/email + hosted pages).
5. **Phase 5**: Terms, policy expiry, validation, listing.

This order gives you a working install, policies in the admin, and the checkout experience first; then you add the consumer-facing claim/policy experience and polish.

For Shoplazza-specific details (OAuth params, token exchange, Checkout API method to add the fee, App Bridge script and host), keep using [SHOPLAZZA_PLUGIN_OVERVIEW.md](./SHOPLAZZA_PLUGIN_OVERVIEW.md) and the official Shoplazza reference docs.
