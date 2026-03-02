-- AlterTable
ALTER TABLE "Store" ADD COLUMN "country_code" TEXT NOT NULL DEFAULT '';
ALTER TABLE "Store" ADD COLUMN "country_name" TEXT NOT NULL DEFAULT '';

-- AlterTable
ALTER TABLE "StoreSettings" ADD COLUMN "location_valid" BOOLEAN NOT NULL DEFAULT false;
