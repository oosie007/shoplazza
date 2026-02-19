/**
 * Fetch merchant's product categories from Shoplazza Admin API.
 * Uses List Categories API: https://www.shoplazza.dev/reference/categoryapi_listcategory
 * Requires read_product scope.
 */

export type Category = { id: string; name: string };

/** Mock categories when API is unavailable. */
const MOCK_CATEGORIES: Category[] = [
  { id: "cat-apparel", name: "Apparel" },
  { id: "cat-accessories", name: "Accessories" },
  { id: "cat-home", name: "Home" },
  { id: "cat-electronics", name: "Electronics" },
  { id: "cat-sports", name: "Sports & Outdoors" },
];

/** List Categories API uses OpenAPI 2025-06 and requires read_product. */
const CATEGORIES_API_VERSION = "2025-06";

export type FetchCategoriesResult = {
  categories: Category[];
  fromApi: boolean;
  error?: string;
};

/**
 * Fetch categories via GET .../openapi/2025-06/categories (List Categories).
 * Requires read_product scope. Optional: pid, ids query params.
 */
export async function fetchStoreCategories(
  shop: string,
  accessToken: string
): Promise<FetchCategoriesResult> {
  const normalizedShop = shop.includes(".") ? shop : `${shop}.myshoplaza.com`;
  const url = `https://${normalizedShop}/openapi/${CATEGORIES_API_VERSION}/categories`;

  try {
    // Shoplazza List Categories doc uses "access-token" header; some APIs also accept Authorization: Bearer
    const res = await fetch(url, {
      headers: {
        "access-token": accessToken,
        Authorization: `Bearer ${accessToken}`,
        Accept: "application/json",
      },
    });

    if (!res.ok) {
      const text = await res.text();
      const errorMsg = `${res.status} ${res.statusText}${text ? `: ${text.slice(0, 200)}` : ""}`;
      console.warn("[categories] Shoplazza API error:", res.status, url, text);
      return { categories: MOCK_CATEGORIES, fromApi: false, error: errorMsg };
    }

    const data = (await res.json()) as unknown;
    const raw = extractCategoriesArray(data);
    const categories: Category[] = raw
      .filter((c) => c && (c.id != null || (c as { title?: string }).title != null))
      .map((c) => {
        const item = c as { id?: string; title?: string; name?: string };
        return {
          id: String(item.id ?? item.title ?? "").trim() || "unknown",
          name: String(item.title ?? item.name ?? item.id ?? "Unnamed").trim(),
        };
      });

    if (categories.length === 0) {
      const errorMsg = "API returned no categories (or unexpected response shape)";
      console.warn("[categories]", errorMsg, JSON.stringify(data).slice(0, 300));
      return { categories: MOCK_CATEGORIES, fromApi: false, error: errorMsg };
    }

    return { categories, fromApi: true };
  } catch (err) {
    const message = err instanceof Error ? err.message : String(err);
    console.warn("[categories] Fetch failed:", err);
    return { categories: MOCK_CATEGORIES, fromApi: false, error: message };
  }
}

function extractCategoriesArray(data: unknown): Array<{ id?: string; title?: string; name?: string }> {
  if (Array.isArray(data)) return data as Array<{ id?: string; title?: string; name?: string }>;
  if (data && typeof data === "object") {
    const o = data as Record<string, unknown>;
    if (Array.isArray(o.categories)) return o.categories as Array<{ id?: string; title?: string; name?: string }>;
    if (Array.isArray(o.data)) return o.data as Array<{ id?: string; title?: string; name?: string }>;
    if (o.data && typeof o.data === "object" && Array.isArray((o.data as Record<string, unknown>).categories)) {
      return (o.data as { categories: Array<{ id?: string; title?: string; name?: string }> }).categories;
    }
  }
  return [];
}
