import { NextRequest, NextResponse } from "next/server";
import { getStoreByShop } from "@/lib/shoplazza/store";
import { bindCartTransformWithResult } from "@/lib/shoplazza/item-protection-product";
import { shopParamSchema } from "@/lib/validation/schemas";

/**
 * POST /api/admin/bind-cart-transform?shop=...
 *
 * Re-binds the Cart Transform function URL for the store. Use to test bind and see
 * the exact Shoplazza API response (so we can see why [cart-transform] is never called).
 */
export async function POST(request: NextRequest) {
  const shopParam = request.nextUrl.searchParams.get("shop") ?? "";
  const parsed = shopParamSchema.safeParse(shopParam);
  if (!parsed.success) {
    return NextResponse.json(
      { ok: false, error: "Missing or invalid shop" },
      { status: 400 }
    );
  }
  const shop = parsed.data;

  const store = await getStoreByShop(shop);
  if (!store?.accessToken) {
    return NextResponse.json(
      { ok: false, error: "Store not found or not installed" },
      { status: 404 }
    );
  }

  const base = process.env.NEXT_PUBLIC_APP_URL || request.nextUrl.origin;
  const callbackUrl = `${base.replace(/\/$/, "")}/api/shoplazza/cart-transform`;

  const result = await bindCartTransformWithResult(shop, store.accessToken, callbackUrl);

  if (result.ok) {
    return NextResponse.json({ ok: true, message: "Cart Transform bound successfully" });
  }
  if ("status" in result) {
    return NextResponse.json({
      ok: false,
      error: "Bind failed",
      status: result.status,
      body: result.body,
    });
  }
  return NextResponse.json({
    ok: false,
    error: result.error,
  });
}
