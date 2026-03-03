import { NextRequest, NextResponse } from "next/server";
import { getStoreByShop } from "@/lib/shoplazza/store";
import {
  fetchProductCategoryMap,
  fetchStoreCollections,
  buildCollectionIdToCategoryId,
} from "@/lib/shoplazza/product-categories";
import { fetchStoreCategories } from "@/lib/shoplazza/categories";
import { shopParamSchema } from "@/lib/validation/schemas";

/**
 * GET /api/product-categories?shop=...&productIds=id1,id2,id3
 *
 * Returns { productId: categoryId } for each product that has a collection.
 * Used by the checkout widget for per-category premium when CheckoutAPI doesn't provide category per product.
 */
export async function GET(request: NextRequest) {
  const shopResult = shopParamSchema.safeParse(request.nextUrl.searchParams.get("shop") ?? "");
  if (!shopResult.success) {
    return withCors(
      NextResponse.json(
        { error: shopResult.error.flatten().formErrors[0] ?? "Invalid shop" },
        { status: 400 }
      )
    );
  }
  const shop = shopResult.data;
  const productIdsParam = request.nextUrl.searchParams.get("productIds");

  const productIds = productIdsParam
    ? productIdsParam.split(",").map((id) => id.trim()).filter(Boolean)
    : [];
  console.log("[CD Insure] /api/product-categories shop=" + shop + " productIds=" + JSON.stringify(productIds));
  if (productIds.length === 0) {
    return withCors(NextResponse.json({}));
  }

  const store = await getStoreByShop(shop);
  if (!store) {
    return withCors(
      NextResponse.json(
        { error: "Store not found or not installed" },
        { status: 404 }
      )
    );
  }

  // Map collection_id (from API) -> category_id (admin config) so widget categoryPercents match
  let collectionIdToCategoryId: Record<string, string> | null = null;
  try {
    const [categoriesResult, collections] = await Promise.all([
      fetchStoreCategories(store.shopDomain, store.accessToken),
      fetchStoreCollections(store.shopDomain, store.accessToken),
    ]);
    if (categoriesResult.categories.length && collections.length) {
      collectionIdToCategoryId = buildCollectionIdToCategoryId(
        collections,
        categoriesResult.categories
      );
      if (Object.keys(collectionIdToCategoryId).length) {
        console.log("[CD Insure] product-categories collection->category map size:", Object.keys(collectionIdToCategoryId).length);
      }
    }
  } catch (e) {
    console.warn("[CD Insure] product-categories buildCollectionIdToCategoryId failed:", e);
  }

  const map = await fetchProductCategoryMap(
    store.shopDomain,
    store.accessToken,
    productIds,
    collectionIdToCategoryId
  );
  console.log("[CD Insure] /api/product-categories map=" + JSON.stringify(map));
  return withCors(NextResponse.json(map));
}

function withCors(res: NextResponse) {
  res.headers.set("Access-Control-Allow-Origin", "*");
  res.headers.set("Access-Control-Allow-Methods", "GET, OPTIONS");
  res.headers.set("Access-Control-Allow-Headers", "*");
  return res;
}

export async function OPTIONS() {
  const res = new NextResponse(null, { status: 204 });
  return withCors(res);
}
