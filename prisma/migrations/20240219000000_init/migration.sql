-- CreateTable
CREATE TABLE "Store" (
    "id" TEXT NOT NULL,
    "shopDomain" TEXT NOT NULL,
    "accessToken" TEXT NOT NULL,
    "installedAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updatedAt" TIMESTAMP(3) NOT NULL,

    CONSTRAINT "Store_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "StoreSettings" (
    "id" TEXT NOT NULL,
    "storeId" TEXT NOT NULL,
    "activated" BOOLEAN NOT NULL DEFAULT false,
    "revenueShareTier" TEXT NOT NULL DEFAULT '5',
    "protectionPercent" INTEGER NOT NULL DEFAULT 5,
    "pricingMode" TEXT NOT NULL DEFAULT 'fixed_percent_all',
    "fixedPercentAll" INTEGER NOT NULL DEFAULT 5,
    "categoryPercents" TEXT NOT NULL DEFAULT '{}',
    "excludedCategoryIds" TEXT NOT NULL DEFAULT '[]',
    "widgetVariant" TEXT NOT NULL DEFAULT 'B',
    "defaultAtCheckout" BOOLEAN NOT NULL DEFAULT false,
    "checkoutPlacement" TEXT NOT NULL DEFAULT 'regular',
    "enablePoweredByChubb" BOOLEAN NOT NULL DEFAULT true,
    "offerAtCheckout" BOOLEAN NOT NULL DEFAULT true,
    "claimPortalConfigured" BOOLEAN NOT NULL DEFAULT false,
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updatedAt" TIMESTAMP(3) NOT NULL,

    CONSTRAINT "StoreSettings_pkey" PRIMARY KEY ("id")
);

-- CreateIndex
CREATE UNIQUE INDEX "Store_shopDomain_key" ON "Store"("shopDomain");

-- CreateIndex
CREATE UNIQUE INDEX "StoreSettings_storeId_key" ON "StoreSettings"("storeId");

-- AddForeignKey
ALTER TABLE "StoreSettings" ADD CONSTRAINT "StoreSettings_storeId_fkey" FOREIGN KEY ("storeId") REFERENCES "Store"("id") ON DELETE CASCADE ON UPDATE CASCADE;
