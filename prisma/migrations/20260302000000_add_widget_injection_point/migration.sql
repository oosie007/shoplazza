-- AlterTable - Add column as nullable first
ALTER TABLE "StoreSettings" ADD COLUMN "widgetInjectionPoint" TEXT;

-- Update all existing rows with default value
UPDATE "StoreSettings" SET "widgetInjectionPoint" = 'checkout' WHERE "widgetInjectionPoint" IS NULL;

-- Make column NOT NULL
ALTER TABLE "StoreSettings" ALTER COLUMN "widgetInjectionPoint" SET NOT NULL;

-- Set default for future inserts
ALTER TABLE "StoreSettings" ALTER COLUMN "widgetInjectionPoint" SET DEFAULT 'checkout';
