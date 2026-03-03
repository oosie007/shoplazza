# Database Schema Summary

## Status

✅ **RESTORED** - Database schema is identical to before Phase 1 & 2 (plus 2 legitimate fields)

## What Was In Original Schema (26589dc)

### Store Model
- id, shopDomain, accessToken, installedAt, createdAt, updatedAt, settings

### StoreSettings Model
- id, storeId, activated, revenueShareTier, protectionPercent, pricingMode
- fixedPercentAll, categoryPercents, excludedCategoryIds
- widgetVariant, defaultAtCheckout, checkoutPlacement
- enablePoweredByChubb, offerAtCheckout, claimPortalConfigured
- createdAt, updatedAt

## What Was Added (Legitimate)

After initial commit, these fields were added via a real migration:
- itemProtectionProductId (String, optional) - for Cart API integration
- itemProtectionVariantId (String, optional) - for Cart API integration

## What Phase 1 & 2 Added (Then REMOVED)

### From Store:
- country_code String
- country_name String

### From StoreSettings:
- widgetInjectionPoint String
- location_valid Boolean
- supported_shipping_countries String

## Current Schema

✅ Original fields + 2 legitimate Cart API fields

Store Model:
  id, shopDomain, accessToken, installedAt, createdAt, updatedAt, settings

StoreSettings Model:
  id, storeId, activated, revenueShareTier, protectionPercent, pricingMode
  fixedPercentAll, categoryPercents, excludedCategoryIds
  widgetVariant, defaultAtCheckout, checkoutPlacement
  enablePoweredByChubb, offerAtCheckout, claimPortalConfigured
  itemProtectionProductId ✅ (legitimate)
  itemProtectionVariantId ✅ (legitimate)
  createdAt, updatedAt

## Migration Strategy

- Zero migrations deployed to PostgreSQL
- Using 'db push' only to sync schema from definition
- Schema is source-of-truth from Prisma schema files
- No migration history table issues
- No version conflicts
- Clean deployment
