# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**CD_Insure** is a Shoplazza purchase protection application providing checkout widgets and admin UI for managing item protection policies and claims. It's a Next.js 16 app with Prisma ORM, TypeScript, React 19, and Tailwind CSS.

Key components:
- **Main app**: OAuth-based merchant portal, admin UI, API routes for settings/checkout
- **Checkout Extension**: Shoplazza UI Extension that auto-injects an item protection widget on checkout
- **Widget**: JavaScript loaded by the extension to render protection options at checkout

## Development Commands

### Core Development
- **`npm run dev`** – Start dev server (port 3000). Uses custom `scripts/run-dev.js` to handle parent workspace node_modules.
- **`npm run build`** – Production build (runs Prisma generate + migrate + Next.js build)
- **`npm run start`** – Run production server

### Linting & Validation
- **`npm run lint`** – Run ESLint (uses Next.js ESLint config)

### Checkout Extension (UI Extension)
- **`npm run dev:extension`** – Dev mode for Checkout UI Extension (uses Shoplazza CLI: `shoplazza checkout dev --id cd-insure-item-protection`)
- **`npm run build:extension`** – Build extension (from `checkout-extension/` subdirectory)
- **`npm run zip:extension`** – Create deployable zip
- **`npm run deploy:extension`** – Full deploy: inject app URL + build + deploy
- **`npm run inject:extension-url`** – Update extension app URL from `NEXT_PUBLIC_APP_URL` env var

### Database & Prisma
- **`npm run postinstall`** – Auto-runs after npm install; ensures Prisma client is generated
- Prisma client is generated to `src/generated/prisma/`
- Schema: `prisma/schema.prisma` (SQLite by default; use `DATABASE_URL` env var)
- Migrations stored in `prisma/migrations/`

### Build artifacts and wasm
- **`npm run build:cart-transform-wasm`** – Build cart transform WebAssembly module (for partner API integration)

## Architecture & Key Concepts

### OAuth Flow & Store Installation
1. **App URL** (`/api/auth`): Merchant clicks "Install" in Shoplazza Partner Center → redirects to `/api/auth?shop=...`
2. **Authorize**: App redirects merchant to Shoplazza's authorize endpoint
3. **Callback** (`/api/auth/callback`): Shoplazza redirects back with `code` → exchange for access token
4. **Store Creation**: Token stored in Prisma `Store` model with unique `shopDomain`

Key files:
- `src/lib/shoplazza/auth.ts` – OAuth helpers (getAuthorizeUrl, token exchange)
- `src/app/api/auth/route.ts` – App URL entry point
- `src/app/api/auth/callback/route.ts` – OAuth callback handler

### Admin UI & Settings
- **Admin pages** live in `src/app/admin/` – embedded by Shoplazza via iframe
- Settings API: `src/app/api/settings/` – Zod-validated endpoints for reading/updating `StoreSettings`
- Widget configuration: pricing mode, categories, exclusions, widget variant, checkout placement

Key models:
- `Store` – merchant store record (id, shopDomain, accessToken)
- `StoreSettings` – per-store configuration (pricing, categories, widget behavior, claim portal)

### Checkout & Public API
- **Checkout API** (`src/app/api/checkout/`): Public endpoints used by the widget at checkout
- `POST /api/checkout/create` – create protection order
- `GET /api/public-settings/:domain` – fetch store settings by shop domain (public, no auth)
- **Product Categories** (`src/app/api/product-categories/`): Fetch available categories from merchant's store

### Shoplazza API Integration
- `src/lib/shoplazza/store.ts` – Helper to make authenticated requests to Shoplazza API (Storefront, Partner, Cart Transform)
- Uses `accessToken` from Store model to call Shoplazza endpoints
- Includes cart transform logic for adding item protection as line item

### Extension System
- **Checkout Extension** (`checkout-extension/`): Shoplazza UI Extension built with their SDK
  - `checkout-extension/src/index.ts` – Extension entry point
  - Injects checkout widget on Contact and Payment steps
  - Loads `public/checkout-widget.js` from main app
- Widget communicates with main app via XHR/fetch to `/api/checkout/` and `/api/public-settings/`

### Security Headers & CSP
- **middleware.ts**: Sets security headers on all requests
  - CSP allows `frame-ancestors *` for `/`, `/admin`, `/api/auth` (embedded in Shoplazza iframe)
  - CSP `frame-ancestors 'none'` for other routes
  - X-Frame-Options, HSTS (on HTTPS), X-Content-Type-Options, Referrer-Policy, Permissions-Policy
- **next.config.ts**: Fallback security headers
- Allows ngrok/Shoplazza dev origins in dev mode

### Rate Limiting
- **In-memory** rate limiting in middleware for `/api/auth/*` (30 req/min) and `/api/settings/*` non-GET (60 req/min)
- Per-instance; scales poorly in multi-process deployments
- For production: switch to `@upstash/ratelimit` + Redis (set `UPSTASH_REDIS_REST_URL` / `UPSTASH_REDIS_REST_TOKEN`)

### Input Validation
- All API inputs validated with **Zod** before use
- Schemas: `src/lib/validation/schemas.ts`
- API routes validate query params and JSON bodies

### Environment Variables
- **`.env.local`** (not committed): Copy from `.env.example`
  - `DATABASE_URL` – SQLite connection string (local dev: `file:./dev.db`)
  - `NEXT_PUBLIC_APP_URL` – Public app URL (e.g., ngrok tunnel for dev)
  - `SHOPLAZZA_CLIENT_ID`, `SHOPLAZZA_CLIENT_SECRET` – OAuth credentials from Partner Center
  - `SHOPLAZZA_DEV_TOKEN`, `SHOPLAZZA_DEV_STORE` – Optional, for Shoplazza CLI extension dev

## Project Structure

```
shoplazza/
├── src/
│   ├── app/
│   │   ├── api/
│   │   │   ├── auth/                    # OAuth: App URL + callback
│   │   │   ├── checkout/                # Checkout widget API
│   │   │   ├── settings/                # Admin settings API (auth required)
│   │   │   ├── public-settings/         # Widget public settings
│   │   │   ├── categories/              # Category fetching
│   │   │   ├── product-categories/      # Product category mapping
│   │   │   └── admin/                   # Admin routes
│   │   ├── admin/                       # Admin UI pages (iframed)
│   │   ├── layout.tsx
│   │   ├── page.tsx                     # Root landing page
│   │   └── globals.css
│   ├── components/                      # React components
│   ├── lib/
│   │   ├── shoplazza/                   # Shoplazza API wrappers
│   │   │   ├── auth.ts
│   │   │   ├── store.ts
│   │   │   ├── categories.ts
│   │   │   └── product-categories.ts
│   │   ├── validation/
│   │   │   └── schemas.ts               # Zod schemas
│   │   ├── config/
│   │   ├── db.ts                        # Prisma client
│   │   └── ...
│   └── middleware.ts                    # Security headers, rate limiting
├── prisma/
│   ├── schema.prisma
│   └── migrations/
├── checkout-extension/                  # Shoplazza UI Extension
│   ├── src/
│   ├── package.json
│   ├── build.js
│   ├── config.js
│   └── zip.js
├── cd-insure-item-protection3/          # Legacy/alternative extension
├── scripts/
│   ├── run-dev.js                       # Dev server startup
│   ├── ensure-parent-node-modules.js    # Workspace handling
│   ├── prisma-generate.js
│   ├── prisma-migrate-deploy.js
│   ├── inject-extension-app-url.js
│   └── build-cart-transform-wasm.mjs
├── public/
│   └── checkout-widget.js               # Checkout widget script
├── docs/
│   ├── README.md                        # Documentation index
│   ├── SETUP.md                         # Local setup
│   ├── IMPLEMENTATION_STEPS.md           # Feature roadmap
│   ├── CHECKOUT_EXTENSION_SETUP.md      # Extension setup
│   └── ... (more detailed docs)
├── extension.config.js                  # Shoplazza CLI config (reads .env.local)
├── next.config.ts
├── tsconfig.json
├── postcss.config.mjs                   # Tailwind CSS config
├── eslint.config.mjs
├── package.json
└── SECURITY.md                          # Security practices
```

## Important Implementation Details

### Extension Subdirectory Dependencies
- `checkout-extension/` is a sibling package with its own `package.json` and dependencies
- Build via `npm run build:extension` (runs `checkout-extension/build.js`)
- Uses `extension.config.js` (repo root) to read `SHOPLAZZA_DEV_TOKEN` / `SHOPLAZZA_DEV_STORE`

### Monorepo / Parent Workspace Handling
- `scripts/ensure-parent-node-modules.js` handles cases where repo is nested in parent workspace
- If `node_modules` is missing locally, script symlinks to parent or creates local copy
- Ensures Tailwind CSS and other deps resolve correctly even from parent workspace root

### Database Initialization
- Prisma client auto-generated via `postinstall` script
- No existing migrations needed for fresh install (schema defines tables)
- Migrations should be committed; use `npm run build` to apply migrations on deploy

### Widget / Extension Communication
- Checkout widget (`public/checkout-widget.js`) loads from main app
- Extension injects script tag pointing to `${NEXT_PUBLIC_APP_URL}/checkout-widget.js`
- Widget communicates via XHR/fetch:
  - `GET /api/public-settings/:shopDomain` – fetch pricing/config
  - `POST /api/checkout/create` – create protection order

## Common Development Tasks

### Local Development Setup
1. Copy `.env.example` to `.env.local` and fill in OAuth credentials from Shoplazza Partner Center
2. Set `NEXT_PUBLIC_APP_URL` (e.g., ngrok tunnel: `https://abc123.ngrok.io`)
3. `npm install`
4. `npm run dev` and expose with `ngrok http 3000`
5. In Shoplazza Partner Center, set **App URL** and **Redirect URL** as described in `docs/SETUP.md`
6. Use "Test your app" to install on development store

### Testing the Checkout Extension Locally
1. Set `SHOPLAZZA_DEV_TOKEN` and `SHOPLAZZA_DEV_STORE` in `.env.local` (for Shoplazza CLI)
2. Run `npm run dev:extension` from repo root (uses Shoplazza CLI)
3. Or manually build with `npm run build:extension` and upload via Partner Center

### Deploying Extension to Production
1. Ensure `NEXT_PUBLIC_APP_URL` is set to production domain
2. Run `npm run deploy:extension` (injects URL, builds, and deploys via Shoplazza CLI)

### Database Migrations
- Modify `prisma/schema.prisma`
- Run `npx prisma migrate dev --name <migration_name>` (generates migration file)
- Commit migration to version control
- On deploy, `npm run build` applies migrations automatically

## Known Patterns & Gotchas

- **CSP in dev**: `'unsafe-inline'` and `'unsafe-eval'` are allowed in dev so Next.js HMR and iframe work inside Shoplazza admin
- **Frame ancestors**: Shoplazza can embed `/`, `/admin`, and `/api/auth`; other routes have `frame-ancestors 'none'`
- **SQLite in dev**: Default; switch `DATABASE_URL` to PostgreSQL connection string for production
- **Rate limiting at scale**: In-memory store doesn't scale; switch to Upstash + Redis in production
- **Extension config**: `extension.config.js` reads `.env.local` at runtime; never commit secrets there
- **Middleware caching**: `/`, `/admin` have `Cache-Control: no-store` to prevent stale security headers
- **Access token security**: Only in `Store` model (server-side); never exposed to frontend

## Related Documentation

- See `docs/README.md` for full documentation index
- `SECURITY.md` for security practices and considerations
- `docs/SETUP.md` for detailed local development setup
- `docs/IMPLEMENTATION_STEPS.md` for feature roadmap
- `docs/CHECKOUT_EXTENSION_SETUP.md` for extension build and deployment details
