/**
 * Resolve product IDs to category (collection) IDs for per-category premium.
 * Uses Shoplazza Collection List API with product_id filter.
 * Requires read_collection scope.
 *
 * NOTE: Collections API currently documented under openapi/2022-01.
 * Categories list uses 2025-06, but collections still use 2022-01.
 */

const COLLECTIONS_API_VERSION = "2022-01";

export type ProductCategoryMap = Record<string, string>;

/**
 * Fetch category (first collection) per product. Returns a map productId -> categoryId.
 * Used by the checkout widget to apply per-category premium %.
 */
export type CollectionIdToCategoryId = Record<string, string>;

export async function fetchProductCategoryMap(
  shop: string,
  accessToken: string,
  productIds: string[],
  collectionIdToCategoryId?: CollectionIdToCategoryId | null
): Promise<ProductCategoryMap> {
  const normalizedShop = shop.includes(".") ? shop : `${shop}.myshoplaza.com`;
  const map: ProductCategoryMap = {};
  const uniq = [...new Set(productIds)].map((id) => String(id)).filter(Boolean);
  if (uniq.length === 0) return map;
  const mapCollToCat = collectionIdToCategoryId || null;

  for (const productId of uniq) {
    const pid = String(productId);
    let firstId: string | null = null;

    // 1) Try Collect List API (product_id -> collect records with collection_id) – most reliable for "which collections contain this product"
    const collectsUrl = `https://${normalizedShop}/openapi/${COLLECTIONS_API_VERSION}/collects?product_id=${encodeURIComponent(pid)}&page=1&limit=1`;
    try {
      const resCollects = await fetch(collectsUrl, {
        headers: {
          "access-token": accessToken,
          Authorization: `Bearer ${accessToken}`,
          Accept: "application/json",
        },
      });
      const dataCollects = (await resCollects.json()) as unknown;
      firstId = extractFirstCollectCollectionId(dataCollects);
      const keysCollects = dataCollects && typeof dataCollects === "object" ? Object.keys(dataCollects as object).join(",") : "";
      console.log("[CD Insure] product-categories collects productId=" + pid + " status=" + resCollects.status + " firstId=" + (firstId ?? "null") + " responseKeys=" + keysCollects);
      if (firstId) {
        map[pid] = mapCollToCat && mapCollToCat[firstId] != null ? String(mapCollToCat[firstId]) : firstId;
        continue;
      }
      if (!resCollects.ok) {
        console.warn("[CD Insure] product-categories collects non-OK:", JSON.stringify(dataCollects).slice(0, 300));
      }
    } catch (err) {
      console.warn("[CD Insure] product-categories collects error:", err);
    }

    // 2) Fallback: Collection List API with product_id filter
    if (firstId == null) {
      const url = `https://${normalizedShop}/openapi/${COLLECTIONS_API_VERSION}/collections?product_id=${encodeURIComponent(
        pid
      )}&page=1&limit=1`;
      try {
        const res = await fetch(url, {
          headers: {
            "access-token": accessToken,
            Authorization: `Bearer ${accessToken}`,
            Accept: "application/json",
          },
        });
        const data = (await res.json()) as unknown;
        firstId = extractFirstCollectionId(data);
        const responseKeys = data && typeof data === "object" ? Object.keys(data as object).join(",") : "";
        console.log("[CD Insure] product-categories collections productId=" + pid + " status=" + res.status + " firstId=" + (firstId ?? "null") + " responseKeys=" + responseKeys);
        if (!res.ok) {
          console.warn("[CD Insure] product-categories collections non-OK:", JSON.stringify(data).slice(0, 300));
        } else if (firstId == null) {
          console.warn("[CD Insure] product-categories no collection id. data=" + JSON.stringify(data).slice(0, 400));
        }
        if (firstId) map[pid] = mapCollToCat && mapCollToCat[firstId] != null ? String(mapCollToCat[firstId]) : firstId;
      } catch (err) {
        console.warn("[CD Insure] product-categories collections error:", err);
      }
    }
  }
  return map;
}

/** Parse Collect List API response: array of { collection_id, product_id } -> return first collection_id */
function extractFirstCollectCollectionId(data: unknown): string | null {
  const arr = extractCollectsArray(data);
  const first = arr[0];
  if (first && (first.collection_id != null || (first as { collectionId?: string }).collectionId != null)) {
    return String(
      (first as { collection_id?: string }).collection_id ?? (first as { collectionId?: string }).collectionId
    );
  }
  return null;
}

function extractCollectsArray(data: unknown): Array<{ collection_id?: string; collectionId?: string }> {
  if (Array.isArray(data)) return data as Array<{ collection_id?: string; collectionId?: string }>;
  if (data && typeof data === "object") {
    const o = data as Record<string, unknown>;
    if (Array.isArray(o.collects)) return o.collects as Array<{ collection_id?: string; collectionId?: string }>;
    if (Array.isArray(o.data)) return o.data as Array<{ collection_id?: string; collectionId?: string }>;
    if (
      o.data &&
      typeof o.data === "object" &&
      Array.isArray((o.data as Record<string, unknown>).collects)
    ) {
      return (o.data as { collects: Array<{ collection_id?: string; collectionId?: string }> }).collects;
    }
  }
  return [];
}

function extractFirstCollectionId(data: unknown): string | null {
  const arr = extractCollectionsArray(data);
  const first = arr[0];
  if (first && (first.id != null || (first as { collection_id?: string }).collection_id != null)) {
    return String((first as { id?: string; collection_id?: string }).id ?? (first as { collection_id?: string }).collection_id);
  }
  return null;
}

function extractCollectionsArray(data: unknown): Array<{ id?: string; title?: string; collection_id?: string }> {
  if (Array.isArray(data)) return data as Array<{ id?: string; title?: string; collection_id?: string }>;
  if (data && typeof data === "object") {
    const o = data as Record<string, unknown>;
    if (Array.isArray(o.collections)) return o.collections as Array<{ id?: string; title?: string; collection_id?: string }>;
    if (Array.isArray(o.data)) return o.data as Array<{ id?: string; title?: string; collection_id?: string }>;
    if (
      o.data &&
      typeof o.data === "object" &&
      Array.isArray((o.data as Record<string, unknown>).collections)
    ) {
      return (o.data as { collections: Array<{ id?: string; title?: string; collection_id?: string }> }).collections;
    }
  }
  return [];
}

/** Fetch collections list (id, title) for mapping collection_id -> category_id by name. */
export async function fetchStoreCollections(
  shop: string,
  accessToken: string,
  limit = 100
): Promise<Array<{ id: string; title: string }>> {
  const normalizedShop = shop.includes(".") ? shop : `${shop}.myshoplaza.com`;
  const url = `https://${normalizedShop}/openapi/${COLLECTIONS_API_VERSION}/collections?page=1&limit=${limit}`;
  try {
    const res = await fetch(url, {
      headers: {
        "access-token": accessToken,
        Authorization: `Bearer ${accessToken}`,
        Accept: "application/json",
      },
    });
    if (!res.ok) return [];
    const data = (await res.json()) as unknown;
    const arr = extractCollectionsArray(data);
    return arr
      .filter((c) => c && (c.id != null || (c as { title?: string }).title != null))
      .map((c) => {
        const item = c as { id?: string; title?: string };
        return { id: String(item.id ?? "").trim(), title: String(item.title ?? "").trim() };
      })
      .filter((c) => c.id);
  } catch {
    return [];
  }
}

/** Build mapping collection_id -> category_id by matching collection title to category name (case-insensitive). */
export function buildCollectionIdToCategoryId(
  collections: Array<{ id: string; title: string }>,
  categories: Array<{ id: string; name: string }>
): CollectionIdToCategoryId {
  const out: CollectionIdToCategoryId = {};
  const norm = (s: string) => s.trim().toLowerCase();
  for (const col of collections) {
    const t = norm(col.title);
    if (!t) continue;
    const cat = categories.find((c) => norm(c.name) === t);
    if (cat) out[col.id] = cat.id;
  }
  return out;
}
