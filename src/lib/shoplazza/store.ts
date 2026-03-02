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

// Supported countries for Item Protection
export const SUPPORTED_COUNTRIES = {
  GB: { code: "GB", name: "United Kingdom" },
  FR: { code: "FR", name: "France" },
  CH: { code: "CH", name: "Switzerland" },
  NL: { code: "NL", name: "Netherlands" },
} as const;

export type CountryCode = keyof typeof SUPPORTED_COUNTRIES;

/**
 * Fetch store info from Shoplazza API
 * Returns basic store information including country/location
 */
export async function getStoreInfoFromShoplazza(
  shop: string,
  accessToken: string
): Promise<{ country_code?: string; country_name?: string; name?: string; id?: string }> {
  const host = shop.includes(".") ? shop : `${shop}.myshoplaza.com`;
  const url = `https://${host}/openapi/2025-06/shop?fields[]`;
  
  try {
    const response = await fetch(url, {
      method: "GET",
      headers: {
        "Access-Token": accessToken,
        "Content-Type": "application/json",
      },
    });

    if (!response.ok) {
      console.error(`[store] Failed to fetch shop info: ${response.status}`);
      return {};
    }

    const data = await response.json();
    const shopData = data.data || data;
    
    return {
      country_code: shopData.country_code || shopData.location?.country_code,
      country_name: shopData.country || shopData.location?.country_name,
      name: shopData.name,
      id: shopData.id,
    };
  } catch (error) {
    console.error("[store] Error fetching shop info:", error);
    return {};
  }
}

/**
 * Validate if store's country is supported
 */
export function isSupportedCountry(countryCode?: string): countryCode is CountryCode {
  if (!countryCode) return false;
  return countryCode in SUPPORTED_COUNTRIES;
}

/**
 * Get readable country name from code
 */
export function getCountryName(countryCode: string): string {
  const country = SUPPORTED_COUNTRIES[countryCode as CountryCode];
  return country ? country.name : countryCode;
}
