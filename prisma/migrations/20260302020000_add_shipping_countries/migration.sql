-- AlterTable - Add column as nullable first
ALTER TABLE "StoreSettings" ADD COLUMN "supported_shipping_countries" TEXT;

-- Update all existing rows to have the default value
UPDATE "StoreSettings" SET "supported_shipping_countries" = '[]' WHERE "supported_shipping_countries" IS NULL;

-- Now make it NOT NULL
ALTER TABLE "StoreSettings" ALTER COLUMN "supported_shipping_countries" SET NOT NULL;

-- Set the default for future inserts
ALTER TABLE "StoreSettings" ALTER COLUMN "supported_shipping_countries" SET DEFAULT '[]';
