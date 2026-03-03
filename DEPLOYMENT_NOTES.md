# Deployment Notes - Database & Migrations

## Current Status

✅ **NO MIGRATIONS** - Empty migrations directory
✅ **SCHEMA SYNC ONLY** - Using `db push` only, never `migrate deploy`
✅ **NO DATABASE CHANGES ON VERCEL** - PostgreSQL schema is unchanged from original

## Build Process

### Step 1: `npm run build`

This runs: `node scripts/prisma-generate.js && node scripts/prisma-migrate-deploy.js && next build`

#### prisma-generate.js
- Checks DATABASE_URL to determine if PostgreSQL or SQLite
- Generates Prisma client from correct schema file
- For Vercel (PostgreSQL): uses `schema.postgres.prisma`
- For local dev (SQLite): uses `schema.prisma`

#### prisma-migrate-deploy.js
- Checks if PostgreSQL via DATABASE_URL
- If PostgreSQL: runs `npx prisma db push --schema=./prisma/schema.postgres.prisma`
- If not PostgreSQL: skips (local dev with SQLite)
- NO migrations ever run
- NO migration deploy attempts

#### next build
- Builds the Next.js application

## Database Schemas

### SQLite (Local Dev)
File: `prisma/schema.prisma`

Models:
- Store (id, shopDomain, accessToken, installedAt, createdAt, updatedAt, settings)
- StoreSettings (id, storeId, activated, revenueShareTier, protectionPercent, pricingMode, fixedPercentAll, categoryPercents, excludedCategoryIds, widgetVariant, defaultAtCheckout, checkoutPlacement, enablePoweredByChubb, offerAtCheckout, claimPortalConfigured, itemProtectionProductId, itemProtectionVariantId, createdAt, updatedAt)

### PostgreSQL (Vercel)
File: `prisma/schema.postgres.prisma`

Same models as SQLite (identical fields).

## What Was REMOVED

Phase 1 & 2 added these fields - they were ALL removed:

### From Store:
```
country_code    String   @default("")
country_name    String   @default("")
```

### From StoreSettings:
```
widgetInjectionPoint  String   @default("checkout")
location_valid        Boolean  @default(false)
supported_shipping_countries String @default("[]")
```

## Where Config Moved

| Config | Was | Now |
|--------|-----|-----|
| Widget Placement (Checkout/Cart) | DB field | JSON file (`.widget-config/{shop}.json`) + Static config (`src/lib/config/widget.ts`) |
| Location Validation | DB field | Static config (`src/lib/config/countries.ts`) |
| Supported Shipping Countries | DB field | Static config (`src/lib/config/countries.ts`) - hardcoded GB, FR, CH, NL |
| Store Country (GB/FR/CH/NL) | DB fields | Fetched from Shoplazza API at runtime |

## Migrations Directory

Empty except for `.gitkeep`:
```
prisma/migrations/
├── .gitkeep  (just a placeholder)
```

No migration files exist. `db push` syncs schema directly without migration files.

## Environment Variables (Vercel)

Required:
- `DATABASE_URL` - PostgreSQL connection string (set by Vercel/Prisma Postgres)

Optional:
- `NEXT_PUBLIC_APP_URL` - Your app's public URL

## Testing Build Locally

```bash
# Simulate PostgreSQL (uses schema.postgres.prisma)
export DATABASE_URL="postgresql://user:pass@localhost:5432/testdb"
npm run build

# Or with SQLite (uses schema.prisma, skips db push)
unset DATABASE_URL
npm run build
```

## If Build Still Fails

1. Check DATABASE_URL is set correctly on Vercel
2. Run `npx prisma db push --schema=./prisma/schema.postgres.prisma` manually
3. Check PostgreSQL connection permissions
4. Verify Prisma client generation: `npx prisma generate --schema=./prisma/schema.postgres.prisma`

## Key Points

🚫 **NEVER**: Run `prisma migrate deploy` on Vercel
✅ **ALWAYS**: Use `npx prisma db push` for schema sync
✅ **ALWAYS**: Schema files are source-of-truth
✅ **ALWAYS**: No migration files deployed
