# Vercel Deployment Checklist

## Pre-Deployment Requirements

### ✅ Database Schema
- [x] No Phase 1 & 2 database fields (country_code, country_name, widgetInjectionPoint, location_valid, supported_shipping_countries)
- [x] Only original fields + 2 legitimate Cart API fields (itemProtectionProductId, itemProtectionVariantId)
- [x] SQLite schema (schema.prisma) matches PostgreSQL schema (schema.postgres.prisma) for core models

### ✅ Migrations
- [x] Migrations directory is empty (only .gitkeep)
- [x] No migration files will be deployed
- [x] db push only (never migrate deploy)

### ✅ Build Scripts
- [x] prisma-generate.js - Generates correct Prisma client
- [x] prisma-migrate-deploy.js - Runs db push only (not migrate deploy)
- [x] Both scripts check DATABASE_URL to determine PostgreSQL vs SQLite

### ✅ Configuration Files
- [x] src/lib/config/countries.ts - Static country & shipping config
- [x] src/lib/config/widget.ts - Static widget placement options
- [x] src/lib/config/widget-store.ts - JSON file storage for widget preferences

### ✅ API Endpoints
- [x] /api/settings - Reads/writes widget placement from JSON files
- [x] /api/public-settings - Returns widget placement from JSON files
- [x] /api/store-location - Fetches store country from Shoplazza API

### ✅ Admin UI
- [x] Widget placement radio buttons restored (Checkout/Cart options)
- [x] Store location section shows country from API
- [x] No database fields for removed config

## Vercel Environment Variables Required

```
DATABASE_URL=postgresql://...  (set by Vercel/Prisma Postgres)
NEXT_PUBLIC_APP_URL=...        (your app URL)
SHOPLAZZA_CLIENT_ID=...        (from Partner Center)
SHOPLAZZA_CLIENT_SECRET=...    (from Partner Center)
```

## Build Process (What Vercel Runs)

```bash
npm run build
```

This executes:
```
1. node scripts/prisma-generate.js
   - Reads DATABASE_URL
   - Detects PostgreSQL
   - Generates Prisma client from schema.postgres.prisma

2. node scripts/prisma-migrate-deploy.js
   - Reads DATABASE_URL
   - Detects PostgreSQL
   - Runs: npx prisma db push --schema=./prisma/schema.postgres.prisma
   - Syncs schema from definition (NOT from migrations)

3. next build
   - Builds Next.js application
```

## Expected Behavior on Vercel

1. ✅ Prisma client generated from schema.postgres.prisma
2. ✅ db push syncs schema to PostgreSQL
3. ✅ Schema tables created/updated (no migrations)
4. ✅ Next.js build succeeds
5. ✅ Application starts
6. ✅ Admin UI loads correctly
7. ✅ Merchants can select widget placement
8. ✅ Store location fetched from Shoplazza API

## If Deployment Fails

### Error: "DATABASE_URL not set"
- Solution: Vercel > Settings > Environment Variables > Add DATABASE_URL

### Error: "db push failed"
- Likely cause: Invalid DATABASE_URL or database not accessible
- Check: Vercel database connection string is correct

### Error: "Prisma client generation failed"
- Likely cause: Schema syntax error
- Check: `npx prisma validate --schema=./prisma/schema.postgres.prisma`

### Error: "Next.js build failed"
- Check: Application runs locally with `npm run dev`
- Check: No missing dependencies

## Quick Verification

Before pushing to Vercel, verify locally:

```bash
# 1. Check schema is valid
npx prisma validate --schema=./prisma/schema.postgres.prisma
npx prisma validate --schema=./prisma/schema.prisma

# 2. Generate client
npx prisma generate --schema=./prisma/schema.postgres.prisma

# 3. Run dev server (uses SQLite)
npm run dev

# 4. Check Admin UI loads
# - Navigate to /admin/?shop=test.myshoplaza.com
# - Verify Widget placement section appears with radio buttons
```

## What Will NOT Happen

❌ No migrations deployed
❌ No migration table created
❌ No migration version checks
❌ No database rollbacks
❌ No "already applied" errors
❌ No "migration not found" errors

## Key Point

✅ Database schema comes from Prisma schema file definitions
✅ db push directly syncs schema to PostgreSQL
✅ No migration files involved
✅ Schema is source-of-truth
✅ Simple, clean, reliable deployment
