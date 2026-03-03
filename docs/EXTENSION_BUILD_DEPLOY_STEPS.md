# Checkout extension: build and deploy (step-by-step)

Do this from the **repo root** (`shoplaza/`). Use the **same terminal** for all steps.

---

## 1. Check environment

In **`.env.local`** you must have:

```bash
NEXT_PUBLIC_APP_URL=https://shoplazza-nu.vercel.app
SHOPLAZZA_DEV_TOKEN=<your_private_app_token>
SHOPLAZZA_DEV_STORE=https://oostest.myshoplaza.com/
```

- **NEXT_PUBLIC_APP_URL** = your live app (Vercel). No trailing slash. The widget and `/api/public-settings` are loaded from this URL.
- **SHOPLAZZA_DEV_TOKEN** = from the test store: Shoplazza Admin → Apps → Manage Private Apps → create or open app → copy token.
- **SHOPLAZZA_DEV_STORE** = the store where you deploy (e.g. `https://oostest.myshoplaza.com/`).

---

## 2. Inject app URL into extension source

This writes `NEXT_PUBLIC_APP_URL` from `.env.local` into `extensions/cd-insure-item-protection/src/index.js`.

```bash
npm run inject:extension-url
```

You should see: **`Injected APP_URL into extension: https://shoplazza-nu.vercel.app`**

---

## 3. Build the extension bundle

This builds `checkout-extension/dist/extension.js` with the same app URL (from `.env.local`).

```bash
npm run build:extension
```

You should see: **`Built checkout extension with APP_URL: https://shoplazza-nu.vercel.app`**

---

## 4. Register the extension (if the CLI says “No extensions available”)

If later **deploy** says there are no extensions to deploy, register first:

```bash
shoplazza checkout create
```

When prompted:

- **Project name:** `cd-insure-item-protection`
- **Store URL:** `https://oostest.myshoplaza.com/`
- **Token:** paste your `SHOPLAZZA_DEV_TOKEN`

Then run **push** so the extension appears in the deploy list:

```bash
shoplazza checkout push
```

Choose **“Push new extension”** (or similar) and select **cd-insure-item-protection** if asked.

---

## 5. Deploy the extension

```bash
shoplazza checkout deploy
```

- When asked **“Please select an extension to deploy”**, choose **cd-insure-item-protection**.
- When asked **version**, choose the one offered (e.g. **v1.0 (Published)**).
- When asked **“Are you sure you want to deploy?”**, choose **Yes**.

You should see: **`Successfully deployed the extension 'cd-insure-item-protection(v1.0)'.`**

---

## 6. Test on the store

1. **Do not** run `npm run dev:extension` (no local extension server).
2. Open your store in a **new tab**: `https://oostest.myshoplaza.com`
3. Add a product to cart → go to **checkout** (use the normal checkout URL, not a preview link).
4. You should see the Item Protection widget (or “Item protection – loading settings…” if the app/store config isn’t ready).
5. In DevTools → **Network**, filter by `checkout-widget.js` and `public-settings`. You should see:
   - `https://shoplazza-nu.vercel.app/checkout-widget.js` → **200**
   - `https://shoplazza-nu.vercel.app/api/public-settings?shop=oostest.myshoplaza.com` → **200**

If you still see requests to an **old** Vercel URL, the store may be using a cached bundle; try a hard refresh or incognito.

---

## One-line deploy (after first-time create/push)

Once the extension is registered, you can do inject + build + deploy in one go:

```bash
npm run deploy:extension
```

Then in the CLI select **cd-insure-item-protection** and confirm. This runs steps 2, 3, and 5 together.

---

## If the CLI created the extension in a folder at repo root (e.g. cd-insure-item-protection3)

When you run **`shoplazza checkout create`**, the CLI creates a **new project folder at repo root** (e.g. `cd-insure-item-protection3/`), not under `extensions/`. When you run **push** or **deploy** from the **repo root**, the CLI only sees the extensions in `extensions/`, so your new one does **not** appear in the list.

**Fix:** Run **push** and **deploy from inside the new project folder**:

```bash
cd cd-insure-item-protection3
shoplazza checkout push
```

Select **“Push new extension”** and the extension (e.g. **itemprotect_extension**) if asked. Then:

```bash
shoplazza checkout deploy
```

Select that extension and confirm. Your widget code and `APP_URL` are already in `extensions/itemprotect_extension/src/index.js` in that folder (with the correct Vercel URL). After deploy, test checkout on the store (normal URL, no dev mode).
