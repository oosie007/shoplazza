# CD_Insure – Shoplazza Purchase Protection App

Worry-Free Purchase style plugin for Shoplazza: checkout widget for purchase protection and admin UI for policies and claims.

## Quick start

1. **Env** – Copy `.env.example` to `.env.local` and set your Shoplazza **Client ID** and **Client Secret** (from Partner Center → CD_Insure → App settings). Set `NEXT_PUBLIC_APP_URL` to your public URL (e.g. ngrok for local dev).
2. **Partner Center** – Set **App URL** and **Redirect URL** as in [docs/SETUP.md](docs/SETUP.md).
3. **Run** – `npm run dev` and expose with a tunnel (e.g. `ngrok http 3000`).
4. **Install** – In Partner Center use “Test your app” and install on a development store.

See [docs/README.md](docs/README.md) for the doc index, [docs/SETUP.md](docs/SETUP.md) for setup, and [docs/IMPLEMENTATION_STEPS.md](docs/IMPLEMENTATION_STEPS.md) for the build plan. Security practices (headers, rate limiting, validation, env) are documented in [SECURITY.md](SECURITY.md). Keep `package-lock.json` in version control.

## Checkout extension (widget on every step)

To have the Item Protection widget show **automatically** on checkout (Contact and Payment steps) when merchants install your app—like the competitor’s “Worry-Free Delivery”—build and deploy the **Checkout UI Extension**:

1. Set `NEXT_PUBLIC_APP_URL` in `.env.local` (and optional `SHOPLAZZA_DEV_TOKEN` / `SHOPLAZZA_DEV_STORE` for CLI dev).
2. Build: `npm run build:extension`. For local extension dev: `npm run dev:extension` (from repo root).
3. Use [Shoplazza CLI](https://www.shoplazza.dev/docs/getting-started-2) or Partner Center to add the extension to your app.

See **[docs/CHECKOUT_EXTENSION_SETUP.md](docs/CHECKOUT_EXTENSION_SETUP.md)** and **[docs/README.md](docs/README.md)** for full steps and doc index.

## Project layout

- `src/app/api/auth` – OAuth install (App URL)
- `src/app/api/auth/callback` – OAuth callback (Redirect URL), token exchange, store install
- `src/app/admin` – Admin UI
- `src/lib/shoplazza/` – Auth helpers, store
- `public/checkout-widget.js` – Checkout widget script (loaded by the extension)
- `extensions/cd-insure-item-protection/` – Extension source (used by `npm run dev:extension`)
- `checkout-extension/` – Build/zip for Checkout UI Extension (injects widget on checkout)
- `data/` – Stored installations (gitignored)
