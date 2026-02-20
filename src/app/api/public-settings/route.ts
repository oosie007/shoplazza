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

  let store = await getStoreByShop(shop);
  const s = store?.settings as any;
  if (!s || !store) {
    return withCors(
      NextResponse.json(
        { error: "Store not found or not installed" },
        { status: 404 }
      )
    );
  }

  // If we don't have Item Protection product IDs yet, create the product and save them now
  // (e.g. store installed before we added this, or install callback failed). No merchant action needed.
  if (!s.itemProtectionProductId && store.accessToken && store.settings) {
    try {
      const base = process.env.NEXT_PUBLIC_APP_URL || request.nextUrl.origin;
      const { ensureItemProtectionProduct } = await import("@/lib/shoplazza/item-protection-product");
      await ensureItemProtectionProduct(
        shop,
        store.accessToken,
        store.settings.id,
        base
      );
    } catch (err) {
      const msg = err instanceof Error ? err.message : String(err);
      const stack = err instanceof Error ? err.stack : undefined;
      console.error("[public-settings] ensureItemProtectionProduct failed for shop=" + shop + ":", msg, stack || "");
    }
    store = await getStoreByShop(shop);
  }

  const settings = (store?.settings ?? s) as any;
  const categoryPercents =
    typeof settings.categoryPercents === "string" && settings.categoryPercents.length
      ? JSON.parse(settings.categoryPercents)
      : {};
  const excludedCategoryIds =
    typeof settings.excludedCategoryIds === "string" && settings.excludedCategoryIds.length
      ? JSON.parse(settings.excludedCategoryIds)
      : [];

  return withCors(NextResponse.json({
    activated: settings.activated,
    pricingMode: settings.pricingMode,
    fixedPercentAll: settings.fixedPercentAll,
    categoryPercents,
    excludedCategoryIds,
    widgetVariant: settings.widgetVariant,
    enablePoweredByChubb: settings.enablePoweredByChubb,
    offerAtCheckout: settings.offerAtCheckout,
    defaultAtCheckout: settings.defaultAtCheckout,
    itemProtectionProductId: settings.itemProtectionProductId ?? undefined,
    itemProtectionVariantId: settings.itemProtectionVariantId ?? undefined,
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


