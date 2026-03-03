# Security

## Configuration

- **Secrets:** Use `.env.local` for all secrets (copy from `.env.example`). Never commit `.env` or `.env.local`. No sensitive data is stored in `NEXT_PUBLIC_*` variables; `NEXT_PUBLIC_APP_URL` is the public app URL only.
- **Extension CLI:** `extension.config.js` reads `SHOPLAZZA_DEV_TOKEN` and `SHOPLAZZA_DEV_STORE` from `.env.local`; do not hardcode tokens.

## Headers & CSP

- **middleware.ts** sets security headers on all requests: `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`, `Referrer-Policy`, `Permissions-Policy`, and a strict `Content-Security-Policy`. HSTS is set when the request is served over HTTPS.
- **next.config.ts** adds the same security headers as a fallback.

## Rate limiting

- **Auth** (`/api/auth/*`) and **settings** (`/api/settings`, non-GET) are rate-limited in middleware (in-memory, per-instance). For production at scale, use Redis and `@upstash/ratelimit`; set `UPSTASH_REDIS_REST_URL` and `UPSTASH_REDIS_REST_TOKEN` and switch the middleware to use Upstash.

## Input validation

- All API inputs (query params and JSON bodies) are validated with **Zod** before use. See `src/lib/validation/schemas.ts` and the API route handlers.

## Dependencies

- **package-lock.json** is committed; keep it in version control to lock dependency versions.
- Run `npm audit` periodically. Current moderate findings are in the ESLint/ajv dev-dependency chain; fixing them would require a major ESLint downgrade. Runtime dependencies are audited and updated as needed.
