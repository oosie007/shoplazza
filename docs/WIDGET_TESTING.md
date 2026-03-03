# Let others test the checkout widget

After your app is on Vercel, use these steps so **anyone** can see the Item Protection widget on checkout (no ngrok, no dev mode).

---

## 1. Point the extension at your Vercel app

The widget script is loaded from your app URL. For testers, that must be your **Vercel URL** (e.g. `https://your-project.vercel.app`).

**On your machine:**

1. In **`.env.local`** set (or update):
   ```bash
   NEXT_PUBLIC_APP_URL=https://YOUR_VERCEL_URL
   ```
   Use your real Vercel URL, no trailing slash (e.g. `https://shoplazza-xxx.vercel.app`).

2. Inject that URL into the extension and build (from repo root):
   ```bash
   npm run inject:extension-url
   npm run build:extension
   ```
   This updates `extensions/cd-insure-item-protection/src/index.js` and builds `checkout-extension/dist/extension.js` with your Vercel URL.

---

## 2. Deploy the extension (CLI only)

Shoplazza **does not provide a zip upload for checkout extensions in Partner Center**. You must create and deploy the extension using the **Shoplazza CLI**.

**A. Create the extension first (one-time)**

If you see **“No extensions available to deploy”**, the extension hasn’t been created yet. From the **repo root** (where `extension.config.js` and `extensions/` are):

1. Ensure **`.env.local`** has:
   ```bash
   SHOPLAZZA_DEV_TOKEN=your_private_app_token
   SHOPLAZZA_DEV_STORE=https://your-store.myshoplaza.com/
   ```
   (Token: Shoplazza Admin → **Apps** → **Manage Private Apps** → Create App → copy token.)

2. Run:
   ```bash
   shoplazza checkout create
   ```
   When prompted, enter:
   - **Project name:** e.g. `cd-insure-item-protection`
   - **Store URL:** e.g. `https://your-store.myshoplaza.com/`
   - **Token:** paste your private app token

   That registers the extension with Shoplazza so it appears in the extension list.

**B. Deploy (or push then deploy)**

```bash
npm run deploy:extension
```

This runs `inject:extension-url` then `shoplazza checkout deploy`. If the CLI offers **push** vs **deploy**, use **push** to upload the code and **deploy** to make it live; our script runs `deploy`. For updates, run `npm run deploy:extension` again after changing the app URL or extension code.

---

## 3. Link the extension to your app (Partner Center)

- In [Partner Center](https://partners.shoplazza.com) → **Apps** → your app, open **Setup** or **Development**.
- Find **Checkout** or **Extensions** and **link** or **add** the extension you deployed via the CLI.
- Without this, installing the app does not enable the widget on checkout. (If the UI doesn’t show a clear “Extensions” or “Checkout” section, check the app’s version or contact Shoplazza support for where to attach the checkout extension.)

---

## 4. How testers see the widget

1. **They install your app** on their store:
   - You share the app’s install link from Partner Center (e.g. “Test your app” or the public app page), or
   - They use a store where you’ve already installed the app.

2. **They (or you) open your app’s config** (e.g. Apps → your app → Configuration) and **Activate** Item Protection, set a %, and save.

3. **They go to checkout** on that store (as a customer or in incognito):
   - Add a product to cart → go to checkout.
   - The Item Protection widget should appear (e.g. after Contact information, before Payment).

4. **If it doesn’t show:**
   - Confirm the app is installed and the extension is linked to the app (step 3).
   - Confirm Item Protection is **Activated** in your app’s Configuration.
   - Confirm `NEXT_PUBLIC_APP_URL` in Vercel matches the URL you used when building the extension (step 1).

5. **Widget not showing on real mobile (phone/tablet):** If the widget **does** show when you use Chrome DevTools “mobile” device emulation but **does not** on an actual phone (Chrome or Safari), that confirms Shoplazza serves a **different checkout experience for real mobile devices**. Extension points run on the desktop-style checkout (and in emulation you’re still on that); on a real device they use a mobile flow that does not load Checkout UI Extensions. “Request desktop site” on the phone often still doesn’t switch to the extension-enabled flow. This is a **platform limitation**. To get the widget on real mobile, Shoplazza would need to support checkout extensions in their mobile checkout; contact their support or partner team to request or confirm.  
   **In our code:** The `shoplazza-extension-ui` package has no mobile-specific options; we register on multiple extension points, but if the mobile flow never loads extensions, that doesn’t help.

---

## Troubleshooting on a real mobile device

If the widget works in desktop Chrome (or with mobile emulation) but not on a real phone, use these to see **logs and errors** on the device.

### Option A: On-page debug panel (no computer needed)

The widget can show a small **floating debug log** on the checkout page so you can see on the phone whether the script ran and where it failed.

**Enable debug:**

1. **From the same store on desktop:** Open your store (e.g. `https://your-store.myshoplaza.com`), open the browser console (F12 → Console), and run:
   ```js
   localStorage.setItem('cd_insure_debug', '1');
   ```
   Then on your **phone**, open the same store, add to cart, and go to checkout. The debug panel (green text, bottom-right) will appear if the widget script runs.

2. **Or** add `?cd_debug=1` to the checkout URL on the phone (only if you can get that URL, e.g. by sharing a link that goes straight to checkout).

**What you’ll see:**

- **No panel at all** → The widget script never ran. Either the extension isn’t injected on that page (e.g. Shoplazza’s mobile checkout doesn’t load extensions) or the script was blocked (CSP, ad blocker, slow/offline).
- **“script loaded” then “fetchSettings fail” or “fetchSettings network err”** → Script runs but the request to your app (e.g. `/api/public-settings`) failed: check Network tab for that URL (blocked, CORS, timeout, or app down).
- **“script loaded” → “fetchSettings ok” → “mount…” → “premium=… rendering”** → Widget logic ran; if you still don’t see the card, the mount target or CSS might be different on mobile.

Turn off debug when done: on the same store (desktop or mobile), run `localStorage.removeItem('cd_insure_debug');` or clear site data.

### Option B: Remote debugging (full Console + Network)

**iPhone (Safari):**

1. On the iPhone: **Settings → Safari → Advanced** → turn on **Web Inspector**.
2. Connect the iPhone to your Mac with a USB cable.
3. On the phone: open Safari and go to the store’s **checkout** page.
4. On the Mac: open **Safari** → menu **Develop** → **[Your iPhone name]** → select the checkout page.
5. The Web Inspector window opens. Use **Console** for logs and **Network** to see if `checkout-widget.js` and `/api/public-settings` load and any errors.

**Android (Chrome):**

1. On the phone: **Settings → Developer options** → enable **USB debugging**.
2. Connect the phone to your computer with USB; allow debugging when prompted.
3. On the phone: open **Chrome** and go to the store’s **checkout** page.
4. On the computer: open **Chrome** and go to **chrome://inspect**.
5. Under “Remote Target”, find the checkout tab and click **Inspect**.
6. Use **Console** and **Network** as above.

**What to check:**

- **Network:** Does `checkout-widget.js` (from your app URL) load? Status 200 or blocked/failed? Does the request to `/api/public-settings?shop=...` complete (200) or fail (CORS, timeout, 4xx/5xx)?
- **Console:** Any red errors (CSP, script blocked, `CheckoutAPI is not defined`, etc.)?
- If the script and API both load but the widget doesn’t appear, the page DOM might differ on mobile (different layout/selectors); the debug panel messages will narrow it down.

---

## Quick checklist

| Step | Done |
|------|------|
| `NEXT_PUBLIC_APP_URL` in `.env.local` = your Vercel URL |
| `npm run inject:extension-url` and `npm run build:extension` (or `deploy:extension`) |
| Extension created (`shoplazza checkout create`) then deployed (`npm run deploy:extension`) |
| Extension linked to app in Partner Center |
| App installed on the test store; Item Protection **Activated** in config |
| Test: go to store checkout → widget visible |

---

## What to share with testers

You can send something like:

- **“Install the app”** – [Link to your app in Partner Center or the install URL]
- **“Then open the app → Configuration, turn Item Protection ON and save.”**
- **“On the storefront, add a product to cart and go to checkout. You should see the Item Protection option.”**

No need to share ngrok, dev mode, or local URLs—everything runs from your Vercel app and the deployed extension.
