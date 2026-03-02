-- AlterTable Store - Add location columns as nullable first
ALTER TABLE "Store" ADD COLUMN "country_code" TEXT;
ALTER TABLE "Store" ADD COLUMN "country_name" TEXT;

-- Update all existing rows
UPDATE "Store" SET "country_code" = '' WHERE "country_code" IS NULL;
UPDATE "Store" SET "country_name" = '' WHERE "country_name" IS NULL;

-- Make columns NOT NULL
ALTER TABLE "Store" ALTER COLUMN "country_code" SET NOT NULL;
ALTER TABLE "Store" ALTER COLUMN "country_name" SET NOT NULL;

-- Set defaults for future inserts
ALTER TABLE "Store" ALTER COLUMN "country_code" SET DEFAULT '';
ALTER TABLE "Store" ALTER COLUMN "country_name" SET DEFAULT '';

-- AlterTable StoreSettings - Add location_valid as nullable first
ALTER TABLE "StoreSettings" ADD COLUMN "location_valid" BOOLEAN;

-- Update all existing rows
UPDATE "StoreSettings" SET "location_valid" = false WHERE "location_valid" IS NULL;

-- Make column NOT NULL
ALTER TABLE "StoreSettings" ALTER COLUMN "location_valid" SET NOT NULL;

-- Set default for future inserts
ALTER TABLE "StoreSettings" ALTER COLUMN "location_valid" SET DEFAULT false;
