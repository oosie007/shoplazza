# One path: Shoplazza app merchants can install → config page → widget on checkout

You want: **merchants install your app** → **open your config page** (activate widget, see preview) → **on store checkout the widget shows**. Here is the single path to get there.

---

## What you have (this repo)

| Piece | What it does |
|-------|----------------|
| **Next.js app** | OAuth install, **admin config page** (Activate toggle + preview), APIs (`/api/public-settings`, etc.), and **`/checkout-widget.js`** (the widget script). |
| **Checkout extension** | Small Shoplazza bundle that runs on **checkout**. It injects a div and loads **your app’s** `checkout-widget.js`. The widget then calls your app’s `/api/public-settings?shop=...` and only shows the UI when **offerAtCheckout** is true (what the merchant sets in config). |

So: **app** = config + API + widget script. **Extension** = “load that widget on checkout.” They must both point at the **same app URL** (e.g. your Vercel URL).

---

## What you need to do (in order)

### 1. Deploy your app (so it has a public URL)

- Deploy the Next.js app to **Vercel** (or another host). Follow **[DEPLOY_VERCEL_GITHUB.md](DEPLOY_VERCEL_GITHUB.md)** for GitHub + Vercel + env vars (no secrets in repo).
- After deploy you have a URL like `https://your-app.vercel.app`. The **config page** will be `https://your-app.vercel.app/admin` (merchants get there via Shoplazza Apps → your app).
- Set in **Vercel** env: `DATABASE_URL` (Postgres for production), `SHOPLAZZA_CLIENT_ID`, `SHOPLAZZA_CLIENT_SECRET`, and **`NEXT_PUBLIC_APP_URL`** = `https://your-app.vercel.app` (no trailing slash).
- Run DB migrations once: `npx prisma migrate deploy --schema=prisma/schema.postgres.prisma` with production `DATABASE_URL`.

---

### 2. Tell Shoplazza where your app is (Partner Center)

- Go to [Partner Center](https://partners.shoplazza.com) → **Apps** → your app (create one if you haven’t).
- In **Setup** (or App setup), set:
  - **App URL:** `https://your-app.vercel.app/api/auth`
  - **Redirect URL:** `https://your-app.vercel.app/api/auth/callback`
- Save.  
Now **install** works: when a merchant installs your app, they go through OAuth to your app and land in your admin.

---

### 3. Create and deploy the checkout extension (CLI)

The extension is what makes the **widget appear on checkout**. Shoplazza does **not** let you upload a zip in Partner Center for checkout extensions; you must use the **Shoplazza CLI**.

**On your machine (repo root):**

1. In **`.env.local`** set:
   - `NEXT_PUBLIC_APP_URL=https://your-app.vercel.app` (same as Vercel)
   - `SHOPLAZZA_DEV_TOKEN=...` (from Shoplazza Admin → Apps → **Manage Private Apps** → Create App → copy token)
   - `SHOPLAZZA_DEV_STORE=https://your-store.myshoplaza.com/` (a store you use for testing)

2. **Create the extension once** (optional; creates a local CLI project):
   ```bash
   shoplazza checkout create
   ```
   When prompted: project name (e.g. `cd-insure-item-protection`), store URL, token, extension name.

3. **Push the extension** (this registers it with Shoplazza; without this, deploy says “No extensions available to deploy”):
   ```bash
   shoplazza checkout push
   ```
   In the menu, choose **“Push new extension”** and complete the prompts (select the extension to push, e.g. **cd-insure-item-protection** from this repo). Run from the **repo root** so the CLI sees `extension.config.js` and `extensions/`.

4. **Build and deploy:**
   ```bash
   npm run inject:extension-url
   npm run build:extension
   npm run deploy:extension
   ```
   That injects your Vercel URL into the extension, builds it, and runs `shoplazza checkout deploy`. The extension will then load `https://your-app.vercel.app/checkout-widget.js` on checkout.

---

### 4. Extension and app connection

Shoplazza’s Partner Center **does not clearly expose a “link checkout extension to app”** control in the current UI. The connection may work in one of these ways:

- **By store:** The extension was pushed/deployed for a specific store (the one in `SHOPLAZZA_DEV_STORE`). Stores that **have your app installed** and are the same store (or in the same partner/development setup) may get the extension on checkout without a separate “link” step.
- **Hidden or named differently:** Some partner dashboards put this under **App version**, **Distribution**, **Checkout UI**, or **Extensions** in a submenu. Look under your app’s **Setup**, **Development**, or **Version** pages.
- **Support:** If the widget still doesn’t appear on checkout after install, ask Shoplazza support: *“Where do I attach or link a checkout extension to my app so it runs when merchants install the app?”*

**What to try:** Install your app on the test store (the one you used for push/deploy), turn **Activate** on in your app’s config, then go to checkout. If the widget appears, no separate “link” step may be required for that store. If it doesn’t, you’ll need Shoplazza’s current instructions for associating the checkout extension with the app.

---

### 5. Merchant flow (what you wanted)

1. **Merchant installs your app** from Partner Center (or your app’s install link).
2. **Merchant opens your app** → they see your **config page** (admin). There they can:
   - **Activate** the widget (this sets **offerAtCheckout** and % in your DB).
   - See the **preview** (order summary with Item protection line when on).
3. **Customer goes to checkout** on that store:
   - Shoplazza loads your **checkout extension** (because it’s linked to your app).
   - The extension loads **your** `checkout-widget.js` from your Vercel URL.
   - The widget calls **your** `/api/public-settings?shop=...`; if **offerAtCheckout** is true, the widget is shown.

So: **install app** → **config page (activate + preview)** → **checkout shows widget**. All of that works once steps 1–4 are done. **Note:** The widget appears on **desktop** checkout. Shoplazza’s mobile checkout is a separate flow where Checkout UI Extensions are not loaded; that’s a platform limitation.

---

## Checklist

| Step | Done |
|------|------|
| App deployed (e.g. Vercel) with `NEXT_PUBLIC_APP_URL`, OAuth env vars, Postgres | |
| Partner Center: App URL + Redirect URL set to your app domain | |
| Extension pushed (`shoplazza checkout push` → “Push new extension”) then deployed (`npm run deploy:extension`) | |
| Extension available on checkout for your test store (link step may be automatic or under a different Partner Center menu) | |
| Test: install app on store → open config → Activate → go to checkout → widget visible | |

---

## If something doesn’t work

- **Widget not on checkout:** Extension must be pushed + deployed (step 3). Config “Activate” must be on. If it still doesn’t show, the extension may need to be associated with the app (see step 4; ask Shoplazza support if Partner Center has no obvious option).
- **Config page / install broken:** Partner Center App URL and Redirect URL must match your deployed app (step 2). Env vars must be set in Vercel.
- **“No extensions available to deploy”:** Run **`shoplazza checkout push`** and choose **“Push new extension”** to register the extension; then run `npm run deploy:extension` again.
- **Preview in config:** That’s just your admin UI; it doesn’t require the extension. The **real** widget on checkout uses the extension + `checkout-widget.js` + `/api/public-settings`.

For more detail: **[SETUP.md](SETUP.md)** (local/ngrok), **[DEPLOY_VERCEL_GITHUB.md](DEPLOY_VERCEL_GITHUB.md)** (Vercel), **[CHECKOUT_EXTENSION_SETUP.md](CHECKOUT_EXTENSION_SETUP.md)** (extension dev/deploy), **[WIDGET_TESTING.md](WIDGET_TESTING.md)** (sharing with testers).
