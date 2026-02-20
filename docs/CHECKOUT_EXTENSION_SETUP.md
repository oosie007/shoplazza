# Checkout Extension – Step-by-Step Setup

Get the Item Protection widget to show automatically on checkout (Contact + Payment steps) when merchants install your app. Two paths: **Shoplazza CLI** or **Partner Center**.

**For the widget to load automatically (e.g. in incognito) when a merchant has activated the app:** see **[Merchant journey: widget loads automatically](MERCHANT_JOURNEY_WIDGET_AUTO_LOAD.md)** — production URL, deploy with `npm run deploy:extension`, and link the extension to your app in Partner Center.

---

## Path A: Shoplazza CLI (recommended for dev and deploy)

### 1. Install the CLI

```bash
npm install -g shoplazza-cli
```

Use Node 14+ if the install fails. Check:

```bash
shoplazza version
```

### 2. Log in to your store

```bash
shoplazza login --store oostest.myshoplazza.com
```

(Use your real store domain, e.g. `oostest.myshoplazza.com`.)  
A browser window will open; log in with the account that has access to the store.

### 3. Get a token (for checkout create)

- In Shoplazza **Admin** (your store): **Apps** → **Manage Private Apps** → **Create App** (or use an existing private app).
- Create an app and copy its **token** (API token). You’ll use this when the CLI asks for “token” during `shoplazza checkout create`.

### 4. Build this repo’s extension

From the **shoplaza** repo root:

```bash
npm run build:extension
```

This reads your app URL from `.env.local` (or `checkout-extension/config.js`) and writes `checkout-extension/dist/extension.js` and `extension.esm.js`.

### 5. Extension in this repo (CLI dev from app root)

The checkout extension lives in **`extensions/cd-insure-item-protection/`** at the **shoplaza repo root**. The CLI must be run from this root so it can resolve the entry file (`src/index.js`).

- **extension.config.js** (repo root) – Reads `SHOPLAZZA_DEV_TOKEN` and `SHOPLAZZA_DEV_STORE` from `.env.local` (see step 3).
- **extensions/cd-insure-item-protection/** – Extension that injects the Item Protection widget at Contact and Payment.

Run the CLI from the **repo root** only (where `extension.config.js` and `extensions/` live).

1. Add `SHOPLAZZA_DEV_TOKEN` and `SHOPLAZZA_DEV_STORE` to `.env.local` (see `.env.example`). Never commit real tokens.
2. Run dev from the repo root: `npm run dev:extension`.

(If you prefer to create a new project with the CLI: run `shoplazza checkout create`, enter project name, store URL, and token when prompted. Then copy this repo’s `checkout-extension/extension.json` and the contents of `extensions/cd-insure-item-protection/` into the generated project.)

So: **generated project = CLI structure + our `extension.json` + our built `extension.js`.**

### 6. Create the extension (one-time; required before deploy)

If **`shoplazza checkout deploy`** says **“No extensions available to deploy”**, the extension hasn’t been created yet. From the repo root run:

```bash
shoplazza checkout create
```

When prompted, enter **project name** (e.g. `cd-insure-item-protection`), **store URL** (e.g. `https://your-store.myshoplaza.com/`), and **token** (from Apps → Manage Private Apps → Create App). That registers the extension so `deploy` and `push` can use it.

### 7. Local dev

From the **shoplaza repo root** (where `extension.config.js` and `extensions/` live):

```bash
npm run dev:extension
# or: shoplazza checkout dev --id cd-insure-item-protection
```

- When asked **“Please select one or more extensions to develop”**, press **Space** (to select **cd-insure-item-protection**), then **Enter**.
- Open the checkout URL the CLI prints.
- In the browser **Developer Console** (F12 or Cmd+Option+J), run:

```js
CheckoutAPI.extension.DEV_switchDevMode();
```

- Refresh or go through checkout steps. You should see the Item Protection widget after **Contact information** and before **Payment**.

### 8. Preview

```bash
shoplazza checkout push
```

Use the preview link to test on the store.

### 9. Deploy

```bash
shoplazza checkout deploy
```

After this, the extension is deployed. If it’s linked to your app in Partner Center, stores that install your app will get the widget on checkout.

---

## Path B: Partner Center (link extension only – no zip upload)

**Note:** Shoplazza’s Partner Center **does not currently offer a zip upload** for checkout extensions. You must create and deploy the extension via the **CLI** (Path A). In Partner Center you only **link** the deployed extension to your app.

- Go to [Partner Center](https://partners.shoplazza.com) → **Apps** → your app (**CD_Insure**).
- Open **App setup** / **Development** and find **Checkout** or **Extensions** (if available).
- **Link** or **add** the extension you deployed via `shoplazza checkout deploy` so that stores installing your app get the widget on checkout.
- Save and publish the app version if required.

### After deploy

- Install (or reinstall) your app on a test store.
- Go to checkout (Contact step). The Item Protection widget should appear.
- **Activate** / **Deactivate** in your app’s **Configuration** controls whether the widget is shown (your widget calls `/api/public-settings` and respects `offerAtCheckout`).

---

## Checklist

- [ ] App URL is set (e.g. in `.env.local` as `NEXT_PUBLIC_APP_URL` or in `checkout-extension/config.js`).
- [ ] `npm run build:extension` runs and prints your APP_URL.
- [ ] Extension created via `shoplazza checkout create` (one-time), then deploy with `shoplazza checkout deploy`. Partner Center has no zip upload for checkout extensions.
- [ ] Dev mode tested with `CheckoutAPI.extension.DEV_switchDevMode()` (CLI path).
- [ ] Extension deployed (`shoplazza checkout deploy` or Partner Center upload).
- [ ] App installed on test store; widget visible on checkout when **Activate** is on.

---

## Troubleshooting

- **Widget not showing**  
  - Confirm **Activate** / “Offer at checkout” is on in your app’s Configuration.  
  - Confirm the store has the app installed and the extension is published/linked to the app.  
  - In dev, ensure you ran `CheckoutAPI.extension.DEV_switchDevMode()` and refreshed.

- **Wrong app URL in widget**  
  - Re-run `npm run build:extension` after changing `NEXT_PUBLIC_APP_URL` in `.env.local` or `checkout-extension/config.js`, then redeploy or re-upload the extension.

- **Insurance line / total not updating when toggle is on**  
  - The widget calls the store's `POST /api/checkout/pkg_set` then `POST /api/checkout/price`. If you see **pkg_set 404** in the Network tab, the store has no handler for your app yet. Apps like "Worry-Free Delivery" register a **checkout package** with Shoplazza so the store exposes `pkg_set` and adds their fee when price is recalculated. In **Partner Center** → your app → look for **Checkout**, **Packages**, or **Extensions** and add/register a checkout package (insurance product) so the store can add your fee. The widget also tries sending `additional_prices` in the price request; if the store accepts that, the total may update without pkg_set.

- **“No extensions available to deploy”**  
  - Run **`shoplazza checkout create`** from the repo root first. Enter project name, store URL, and token when prompted. Then run `shoplazza checkout deploy` again.
- **CLI “checkout create” not found**  
  - Ensure you have the latest Shoplazza CLI. Some versions use **Apps** → **Extensions**: try `shoplazza app generate extension` and choose checkout, then use our `extension.json` and `dist/extension.js` in the generated project.
