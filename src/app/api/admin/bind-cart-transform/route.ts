import { NextRequest, NextResponse } from "next/server";
import { getStoreByShop } from "@/lib/shoplazza/store";
import { bindCartTransformWithResult } from "@/lib/shoplazza/item-protection-product";
import { shopParamSchema } from "@/lib/validation/schemas";

/**
 * POST /api/admin/bind-cart-transform?shop=...
 *
 * Creates the Cart Transform function (uploaded JS code per Shoplazza docs) and binds it.
 * No URL: we use Create Function + Bind with function_id.
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

  const result = await bindCartTransformWithResult(shop, store.accessToken);

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
