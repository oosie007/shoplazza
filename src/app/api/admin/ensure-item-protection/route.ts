import { NextRequest, NextResponse } from "next/server";
import { getStoreByShop } from "@/lib/shoplazza/store";
import { ensureItemProtectionProductWithError } from "@/lib/shoplazza/item-protection-product";
import { shopParamSchema } from "@/lib/validation/schemas";

/**
 * POST /api/admin/ensure-item-protection?shop=...
 *
 * Creates the "Item Protection" product in the store (if missing), saves IDs, and binds Cart Transform.
 * Call from the app admin when product IDs are empty so the merchant can see the exact error if it fails.
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
  if (!store?.settings || !store.accessToken) {
    return NextResponse.json(
      { ok: false, error: "Store not found or not installed" },
      { status: 404 }
    );
  }

  const base = process.env.NEXT_PUBLIC_APP_URL || request.nextUrl.origin;
  const result = await ensureItemProtectionProductWithError(
    shop,
    store.accessToken,
    store.settings.id,
    base
  );

  if ("error" in result) {
    return NextResponse.json(
      { ok: false, error: result.error },
      { status: 200 }
    );
  }

  return NextResponse.json({
    ok: true,
    productId: result.productId,
    variantId: result.variantId,
  });
}
