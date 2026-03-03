-- Remove location validation and shipping configuration fields (now static config)
-- These are no longer needed as location is checked at install time
-- and shipping countries are now static configuration

-- For SQLite, we cannot directly DROP COLUMN in all versions,
-- but since this is a fresh local dev environment, we can safely proceed.
-- The actual migration will be handled by `db push` on PostgreSQL.

PRAGMA foreign_keys=OFF;

-- Create temporary table with new schema
CREATE TABLE "StoreSettings_new" (
  "id" TEXT NOT NULL PRIMARY KEY,
  "storeId" TEXT NOT NULL UNIQUE,
  "activated" BOOLEAN NOT NULL DEFAULT 0,
  "revenueShareTier" TEXT NOT NULL DEFAULT '5',
  "protectionPercent" INTEGER NOT NULL DEFAULT 5,
  "pricingMode" TEXT NOT NULL DEFAULT 'fixed_percent_all',
  "fixedPercentAll" INTEGER NOT NULL DEFAULT 5,
  "categoryPercents" TEXT NOT NULL DEFAULT '{}',
  "excludedCategoryIds" TEXT NOT NULL DEFAULT '[]',
  "widgetVariant" TEXT NOT NULL DEFAULT 'B',
  "defaultAtCheckout" BOOLEAN NOT NULL DEFAULT 0,
  "checkoutPlacement" TEXT NOT NULL DEFAULT 'regular',
  "enablePoweredByChubb" BOOLEAN NOT NULL DEFAULT 1,
  "offerAtCheckout" BOOLEAN NOT NULL DEFAULT 1,
  "claimPortalConfigured" BOOLEAN NOT NULL DEFAULT 0,
  "itemProtectionProductId" TEXT,
  "itemProtectionVariantId" TEXT,
  "createdAt" DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  "updatedAt" DATETIME NOT NULL,
  CONSTRAINT "StoreSettings_storeId_fkey" FOREIGN KEY ("storeId") REFERENCES "Store" ("id") ON DELETE CASCADE
);

-- Copy data from old table to new table (excluding removed columns)
INSERT INTO "StoreSettings_new" ("id", "storeId", "activated", "revenueShareTier", "protectionPercent", "pricingMode", "fixedPercentAll", "categoryPercents", "excludedCategoryIds", "widgetVariant", "defaultAtCheckout", "checkoutPlacement", "enablePoweredByChubb", "offerAtCheckout", "claimPortalConfigured", "itemProtectionProductId", "itemProtectionVariantId", "createdAt", "updatedAt")
SELECT "id", "storeId", "activated", "revenueShareTier", "protectionPercent", "pricingMode", "fixedPercentAll", "categoryPercents", "excludedCategoryIds", "widgetVariant", "defaultAtCheckout", "checkoutPlacement", "enablePoweredByChubb", "offerAtCheckout", "claimPortalConfigured", "itemProtectionProductId", "itemProtectionVariantId", "createdAt", "updatedAt"
FROM "StoreSettings";

-- Drop old table
DROP TABLE "StoreSettings";

-- Rename new table to original name
ALTER TABLE "StoreSettings_new" RENAME TO "StoreSettings";

-- Remove country fields from Store
CREATE TABLE "Store_new" (
  "id" TEXT NOT NULL PRIMARY KEY,
  "shopDomain" TEXT NOT NULL UNIQUE,
  "accessToken" TEXT NOT NULL,
  "installedAt" DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  "createdAt" DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  "updatedAt" DATETIME NOT NULL
);

-- Copy data from old Store table (excluding country fields)
INSERT INTO "Store_new" ("id", "shopDomain", "accessToken", "installedAt", "createdAt", "updatedAt")
SELECT "id", "shopDomain", "accessToken", "installedAt", "createdAt", "updatedAt"
FROM "Store";

-- Drop old table
DROP TABLE "Store";

-- Rename new table to original name
ALTER TABLE "Store_new" RENAME TO "Store";

PRAGMA foreign_keys=ON;
