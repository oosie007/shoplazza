-- Fix schema by adding missing columns if they don't exist
-- This is safe to run multiple times

-- Add columns to Store table if they don't exist
ALTER TABLE "Store" ADD COLUMN IF NOT EXISTS "country_code" TEXT;
ALTER TABLE "Store" ADD COLUMN IF NOT EXISTS "country_name" TEXT;

-- Add columns to StoreSettings table if they don't exist  
ALTER TABLE "StoreSettings" ADD COLUMN IF NOT EXISTS "widgetInjectionPoint" TEXT;
ALTER TABLE "StoreSettings" ADD COLUMN IF NOT EXISTS "location_valid" BOOLEAN;
ALTER TABLE "StoreSettings" ADD COLUMN IF NOT EXISTS "supported_shipping_countries" TEXT;

-- Set defaults for any NULL values
UPDATE "Store" SET "country_code" = '' WHERE "country_code" IS NULL;
UPDATE "Store" SET "country_name" = '' WHERE "country_name" IS NULL;
UPDATE "StoreSettings" SET "widgetInjectionPoint" = 'checkout' WHERE "widgetInjectionPoint" IS NULL;
UPDATE "StoreSettings" SET "location_valid" = false WHERE "location_valid" IS NULL;
UPDATE "StoreSettings" SET "supported_shipping_countries" = '[]' WHERE "supported_shipping_countries" IS NULL;

-- Add constraints
ALTER TABLE "Store" ALTER COLUMN "country_code" SET NOT NULL;
ALTER TABLE "Store" ALTER COLUMN "country_name" SET NOT NULL;
ALTER TABLE "StoreSettings" ALTER COLUMN "widgetInjectionPoint" SET NOT NULL;
ALTER TABLE "StoreSettings" ALTER COLUMN "location_valid" SET NOT NULL;
ALTER TABLE "StoreSettings" ALTER COLUMN "supported_shipping_countries" SET NOT NULL;

-- Set defaults
ALTER TABLE "Store" ALTER COLUMN "country_code" SET DEFAULT '';
ALTER TABLE "Store" ALTER COLUMN "country_name" SET DEFAULT '';
ALTER TABLE "StoreSettings" ALTER COLUMN "widgetInjectionPoint" SET DEFAULT 'checkout';
ALTER TABLE "StoreSettings" ALTER COLUMN "location_valid" SET DEFAULT false;
ALTER TABLE "StoreSettings" ALTER COLUMN "supported_shipping_countries" SET DEFAULT '[]';
