# Merchant journey: widget loads automatically at checkout

For the widget to appear **automatically** when a customer (or you in incognito) goes to checkout—with no dev mode or special steps—you need the extension **deployed** with your **production app URL** and **linked to your app** in Partner Center.

---

## What you need

| Step | What | Why |
|------|------|-----|
| 1 | **Production app URL** | The extension injects a script that loads `checkout-widget.js` from this URL. If it’s localhost or ngrok, only you (with that tunnel) can load it. For all customers, use a public HTTPS URL (e.g. `https://your-app.com`). |
| 2 | **Extension deployed with that URL** | The extension bundle is built with `APP_URL` inside it. For deploy, that must be your production URL so every store checkout loads the script from your app. |
| 3 | **Extension linked to your app** | In Partner Center, the checkout extension must be attached to your app so that when a merchant **installs the app**, the extension is active on their store’s checkout. |
| 4 | **Merchant activates in your config** | Your app’s config page saves “Activate” and %; `/api/public-settings` returns `offerAtCheckout: true`. The widget only shows when that’s true. |

---

## 1. Set production app URL

In **`.env.local`** (and in production env vars if you host the app):

```bash
NEXT_PUBLIC_APP_URL=https://your-production-app.com
```

Use the real URL where your Next.js app and `checkout-widget.js` are served (e.g. Vercel, your domain). No trailing slash.

---

## 2. Deploy the extension with that URL

From the **shoplaza** repo root.

**Option A – CLI deploy (recommended)**

```bash
npm run deploy:extension
```

This runs `inject:extension-url` (copies `NEXT_PUBLIC_APP_URL` from `.env.local` into the extension source) then `shoplazza checkout deploy`. The deployed extension will load the widget from your production URL.

**Option B – Manual**

1. Edit **`extensions/cd-insure-item-protection/src/index.js`** and set `APP_URL` to your production URL.
2. Run:

   ```bash
   shoplazza checkout deploy
   ```

**Option C – Partner Center zip**

1. Set `NEXT_PUBLIC_APP_URL` in `.env.local` (or in `checkout-extension/config.js`).
2. Run:

   ```bash
   npm run build:extension
   npm run zip:extension
   ```

3. In Partner Center → your app → Checkout / Extensions, upload **`checkout-extension/cd-insure-checkout-extension.zip`** (or the path your zip script uses).

---

## 3. Link extension to your app (Partner Center)

- Go to [Partner Center](https://partners.shoplazza.com) → **Apps** → your app (**CD_Insure**).
- Open **App setup** / **Development** and find **Checkout** or **Extensions**.
- Add or link your **Checkout extension** (the one you deployed or uploaded).  
  This makes the extension active on checkout **when the app is installed** on a store.

If the extension is not linked to the app, installing the app alone will not enable the widget on checkout.

---

## 4. Merchant flow (what the merchant sees)

1. **Install app** → OAuth → lands in your app (e.g. config page).
2. **Config page** → Turn **Activate** on and set the **%** (e.g. 1% of cart). Save.  
   Your app stores this and `/api/public-settings?shop=...` returns `offerAtCheckout: true` and the percentage.
3. **Customer goes to checkout** (same store, any browser, including incognito):
   - Store has the app installed → Shoplazza loads your **checkout extension**.
   - Extension injects a script tag: `src="<APP_URL>/checkout-widget.js"` (your production URL).
   - Browser loads `checkout-widget.js` from your app.
   - Widget calls `GET <APP_URL>/api/public-settings?shop=<store>`.
   - If `offerAtCheckout` is true, the widget is shown (toggle + copy + price).

So: **Activate + % in your admin** is what controls whether the widget appears; the extension only runs if the app is installed and the extension is deployed and linked.

---

## 5. Checklist (widget loads in incognito)

- [ ] `NEXT_PUBLIC_APP_URL` in `.env.local` is your **production** HTTPS URL (not localhost, not ngrok).
- [ ] You ran **`npm run deploy:extension`** (or built the zip with that URL and uploaded it) so the extension points at that URL.
- [ ] In **Partner Center**, the checkout extension is **linked to your app**.
- [ ] **Test store**: app is installed; in your app config, **Activate** is on and % is set.
- [ ] Open the **test store checkout in incognito** (or another browser). The widget should load without running any console command.

If the widget still doesn’t load in incognito, check:

- **Network tab**: Is `checkout-widget.js` requested from your production URL? If it’s 404 or blocked, fix the URL or hosting.
- **Console**: Any errors? Is `[CD Insure] checkout-widget.js loaded` present? If not, the script didn’t run (wrong URL or extension not active).
- **Partner Center**: Is the extension definitely attached to the app and the app installed on the test store?
