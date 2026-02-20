import { NextRequest, NextResponse } from "next/server";
import { getStoreByShop } from "@/lib/shoplazza/store";
import { shopParamSchema } from "@/lib/validation/schemas";

/**
 * GET /api/public-settings?shop=...
 *
 * Read-only subset of settings that the checkout widget can consume.
 * This endpoint should NOT expose secrets, only configuration.
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

  const store = await getStoreByShop(shop);
  const s = store?.settings as any;
  if (!s || !store) {
    return withCors(
      NextResponse.json(
        { error: "Store not found or not installed" },
        { status: 404 }
      )
    );
  }

  const categoryPercents =
    typeof s.categoryPercents === "string" && s.categoryPercents.length
      ? JSON.parse(s.categoryPercents)
      : {};
  const excludedCategoryIds =
    typeof s.excludedCategoryIds === "string" && s.excludedCategoryIds.length
      ? JSON.parse(s.excludedCategoryIds)
      : [];

  return withCors(NextResponse.json({
    activated: s.activated,
    pricingMode: s.pricingMode,
    fixedPercentAll: s.fixedPercentAll,
    categoryPercents,
    excludedCategoryIds,
    widgetVariant: s.widgetVariant,
    enablePoweredByChubb: s.enablePoweredByChubb,
    offerAtCheckout: s.offerAtCheckout,
    defaultAtCheckout: s.defaultAtCheckout,
    itemProtectionProductId: s.itemProtectionProductId ?? undefined,
    itemProtectionVariantId: s.itemProtectionVariantId ?? undefined,
  }));
}

// CORS helper – allow checkout (Shoplazza domain) to call this endpoint
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


