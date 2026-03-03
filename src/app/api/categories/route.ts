import { NextRequest, NextResponse } from "next/server";
import { getStoreByShop } from "@/lib/shoplazza/store";
import { fetchStoreCategories } from "@/lib/shoplazza/categories";
import { filterApplicableCategories } from "@/lib/config/categories";
import { shopParamSchema } from "@/lib/validation/schemas";

/**
 * GET /api/categories?shop=...
 * Returns the merchant's product categories (collections) for per-category pricing config.
 * Uses Shoplazza Admin API when store has read_collection scope; otherwise returns mock list for testing.
 * When APPLICABLE_CATEGORY_IDS is set in env, only those categories are returned.
 */
export async function GET(request: NextRequest) {
  const shopResult = shopParamSchema.safeParse(request.nextUrl.searchParams.get("shop") ?? "");
  if (!shopResult.success) {
    return NextResponse.json(
      { error: shopResult.error.flatten().formErrors[0] ?? "Invalid shop" },
      { status: 400 }
    );
  }
  const shop = shopResult.data;

  const store = await getStoreByShop(shop);
  if (!store) {
    return NextResponse.json(
      { error: "Store not found or not installed" },
      { status: 404 }
    );
  }

  const result = await fetchStoreCategories(
    store.shopDomain,
    store.accessToken
  );

  const filtered = filterApplicableCategories(result.categories);

  return NextResponse.json({
    categories: filtered,
    fromApi: result.fromApi,
    error: result.error,
  });
}
