import { NextRequest, NextResponse } from "next/server";
import { getStoreByShop } from "@/lib/shoplazza/store";
import { shopParamSchema } from "@/lib/validation/schemas";
import { getWidgetInjectionPoint, saveWidgetInjectionPoint } from "@/lib/config/widget-store";

/**
 * GET /api/settings?shop=...
 * Returns store settings for the given shop (for admin UI).
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
  if (!store?.settings) {
    return NextResponse.json(
      { error: "Store not found or not installed" },
      { status: 404 }
    );
  }

  const s = store.settings as any;
  const categoryPercents =
    typeof s.categoryPercents === "string" && s.categoryPercents.length
      ? JSON.parse(s.categoryPercents)
      : {};
  const excludedCategoryIds =
    typeof s.excludedCategoryIds === "string" && s.excludedCategoryIds.length
      ? JSON.parse(s.excludedCategoryIds)
      : [];

  // Get widget injection point from JSON file
  const widgetInjectionPoint = getWidgetInjectionPoint(shop);

  return NextResponse.json({
    activated: s.activated,
    revenueShareTier: s.revenueShareTier,
    protectionPercent: s.protectionPercent,
    pricingMode: s.pricingMode,
    fixedPercentAll: s.fixedPercentAll,
    categoryPercents,
    excludedCategoryIds,
    widgetVariant: s.widgetVariant,
    defaultAtCheckout: s.defaultAtCheckout,
    checkoutPlacement: s.checkoutPlacement,
    enablePoweredByChubb: s.enablePoweredByChubb,
    offerAtCheckout: s.offerAtCheckout,
    claimPortalConfigured: s.claimPortalConfigured,
    itemProtectionProductId: s.itemProtectionProductId ?? undefined,
    itemProtectionVariantId: s.itemProtectionVariantId ?? undefined,
    widgetInjectionPoint,
  });
}

/**
 * PATCH /api/settings?shop=...
 * Body: partial {
 *   activated, revenueShareTier, protectionPercent,
 *   pricingMode, fixedPercentAll, categoryPercents, excludedCategoryIds,
 *   widgetVariant, defaultAtCheckout, checkoutPlacement,
 *   enablePoweredByChubb, offerAtCheckout, claimPortalConfigured,
 *   widgetInjectionPoint
 * }
 */
export async function PATCH(request: NextRequest) {
  const shopResult = shopParamSchema.safeParse(request.nextUrl.searchParams.get("shop") ?? "");
  if (!shopResult.success) {
    return NextResponse.json(
      { error: shopResult.error.flatten().formErrors[0] ?? "Invalid shop" },
      { status: 400 }
    );
  }
  const shop = shopResult.data;

  const store = await getStoreByShop(shop);
  if (!store?.settings) {
    return NextResponse.json(
      { error: "Store not found or not installed" },
      { status: 404 }
    );
  }

  let rawBody: unknown;
  try {
    rawBody = await request.json();
  } catch {
    return NextResponse.json({ error: "Invalid JSON" }, { status: 400 });
  }
  const parseResult = (await import("@/lib/validation/schemas")).settingsPatchSchema.safeParse(rawBody);
  if (!parseResult.success) {
    const msg = parseResult.error.flatten().formErrors[0] ?? "Validation failed";
    return NextResponse.json({ error: msg }, { status: 400 });
  }
  const body = parseResult.data;

  // Handle widgetInjectionPoint separately (saved to JSON file, not DB)
  if (body.widgetInjectionPoint) {
    try {
      saveWidgetInjectionPoint(shop, body.widgetInjectionPoint);
    } catch (error) {
      console.error("[settings] Error saving widget injection point:", error);
      return NextResponse.json(
        { error: "Failed to save widget placement" },
        { status: 500 }
      );
    }
  }

  const upd: Record<string, unknown> = {};
  const allowed = [
    "activated",
    "revenueShareTier",
    "protectionPercent",
    "pricingMode",
    "fixedPercentAll",
    "categoryPercents",
    "excludedCategoryIds",
    "widgetVariant",
    "defaultAtCheckout",
    "checkoutPlacement",
    "enablePoweredByChubb",
    "offerAtCheckout",
    "claimPortalConfigured",
    "itemProtectionProductId",
    "itemProtectionVariantId",
    "widgetInjectionPoint",
  ] as const;
  for (const key of allowed) {
    if (body[key] === undefined) continue;
    if (key === "categoryPercents" || key === "excludedCategoryIds") {
      upd[key] = JSON.stringify(body[key]);
    } else if (key === "itemProtectionProductId" || key === "itemProtectionVariantId") {
      const v = body[key];
      upd[key] = v === "" || v == null ? null : v;
    } else if (key === "widgetInjectionPoint") {
      // Handle widget injection point - save to JSON file instead of database
      const v = body[key];
      if (v) {
        saveWidgetInjectionPoint(shop, v as any);
      }
    } else {
      upd[key] = body[key];
    }
  }

  const { prisma } = await import("@/lib/db");
  const updated = (await prisma.storeSettings.update({
    where: { id: store.settings.id },
    data: upd,
  })) as any;

  const updatedCategoryPercents =
    typeof updated.categoryPercents === "string" &&
    updated.categoryPercents.length
      ? JSON.parse(updated.categoryPercents)
      : {};
  const updatedExcludedCategoryIds =
    typeof updated.excludedCategoryIds === "string" &&
    updated.excludedCategoryIds.length
      ? JSON.parse(updated.excludedCategoryIds)
      : [];

  // Get widget injection point from JSON file
  const widgetInjectionPoint = getWidgetInjectionPoint(shop);
  // Get updated widget injection point from JSON file
  const updatedWidgetInjectionPoint = getWidgetInjectionPoint(shop);

  return NextResponse.json({
    activated: updated.activated,
    revenueShareTier: updated.revenueShareTier,
    protectionPercent: updated.protectionPercent,
    pricingMode: updated.pricingMode,
    fixedPercentAll: updated.fixedPercentAll,
    categoryPercents: updatedCategoryPercents,
    excludedCategoryIds: updatedExcludedCategoryIds,
    widgetVariant: updated.widgetVariant,
    defaultAtCheckout: updated.defaultAtCheckout,
    checkoutPlacement: updated.checkoutPlacement,
    enablePoweredByChubb: updated.enablePoweredByChubb,
    offerAtCheckout: updated.offerAtCheckout,
    claimPortalConfigured: updated.claimPortalConfigured,
    itemProtectionProductId: updated.itemProtectionProductId ?? undefined,
    itemProtectionVariantId: updated.itemProtectionVariantId ?? undefined,
    widgetInjectionPoint,
    widgetInjectionPoint: updatedWidgetInjectionPoint,
  });
}
