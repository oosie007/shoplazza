import { prisma } from "@/lib/db";

/** Normalize shop to hostname (e.g. oostest.myshoplaza.com) so lookup works regardless of protocol/path. */
export function normalizeShopDomain(shop: string): string {
  let s = shop.trim().toLowerCase();
  try {
    if (s.startsWith("http://") || s.startsWith("https://")) {
      const u = new URL(s);
      s = u.hostname;
    }
    if (s.endsWith("/")) s = s.slice(0, -1);
    return s || shop.trim();
  } catch {
    return shop.trim().toLowerCase();
  }
}

export async function saveInstallation(shop: string, accessToken: string) {
  const shopDomain = normalizeShopDomain(shop);
  const store = await prisma.store.upsert({
    where: { shopDomain },
    create: {
      shopDomain,
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
  const normalized = normalizeShopDomain(shop);
  const exact = await prisma.store.findUnique({
    where: { shopDomain: normalized },
    include: { settings: true },
  });
  if (exact) return exact;
  const all = await prisma.store.findMany({ include: { settings: true } });
  return all.find((s) => normalizeShopDomain(s.shopDomain) === normalized) ?? null;
}

export async function getAllShops(): Promise<string[]> {
  const rows = await prisma.store.findMany({ select: { shopDomain: true } });
  return rows.map((r) => r.shopDomain);
}
