import { prisma } from "@/lib/db";

export async function saveInstallation(shop: string, accessToken: string) {
  const store = await prisma.store.upsert({
    where: { shopDomain: shop },
    create: {
      shopDomain: shop,
      accessToken,
      settings: { create: {} },
    },
    update: {
      accessToken,
      installedAt: new Date(),
    },
    include: { settings: true },
  });

  // Ensure settings exist (in case older installs predate settings relation)
  if (!store.settings) {
    await prisma.storeSettings.create({
      data: {
        storeId: store.id,
      },
    });
  }
}

export async function getStoreByShop(shop: string) {
  return prisma.store.findUnique({
    where: { shopDomain: shop },
    include: { settings: true },
  });
}

export async function getAllShops(): Promise<string[]> {
  const rows = await prisma.store.findMany({ select: { shopDomain: true } });
  return rows.map((r) => r.shopDomain);
}
