import { NextRequest, NextResponse } from "next/server";
import { getStoreByShop } from "@/lib/shoplazza/store";
import { shopParamSchema } from "@/lib/validation/schemas";

/**
 * GET /api/admin/cart-transform-status?shop=...
 *
 * Check if Cart Transform is bound for a store by querying Shoplazza's API
 */
export async function GET(request: NextRequest) {
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

  // Query Shoplazza for current cart transform status
  const host = shop.includes(".") ? shop : `${shop}.myshoplaza.com`;
  const listUrl = `https://${host}/openapi/2024-07/function/cart-transform`;

  try {
    const res = await fetch(listUrl, {
      method: "GET",
      headers: {
        Accept: "application/json",
        "access-token": store.accessToken,
        Authorization: `Bearer ${store.accessToken}`,
      },
    });

    const text = await res.text();
    console.info("[cart-transform-status] List response:", res.status, text.slice(0, 500));

    if (!res.ok) {
      return NextResponse.json({
        ok: false,
        status: res.status,
        message: "Failed to list cart transform functions",
        response: text.slice(0, 300),
      });
    }

    const data = JSON.parse(text);
    return NextResponse.json({
      ok: true,
      data: data,
      message: "Cart transform function status retrieved",
    });
  } catch (err) {
    const msg = err instanceof Error ? err.message : String(err);
    console.error("[cart-transform-status] Error:", msg);
    return NextResponse.json({
      ok: false,
      error: msg,
    });
  }
}
