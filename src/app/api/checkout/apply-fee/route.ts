import { NextRequest, NextResponse } from "next/server";
import { getStoreByShop } from "@/lib/shoplazza/store";

/**
 * POST /api/checkout/apply-fee
 *
 * Called by the checkout widget when the customer toggles item protection.
 * Body: { shop, order_token, amount, label?, enabled }.
 *
 * The competitor plugin uses a similar flow: widget POSTs to their backend
 * (e.g. store domain /api/insurance/v1/product/pkg_set), which then updates
 * the checkout. Shoplazza’s checkout total is updated when the store’s
 * frontend calls POST /api/checkout/price; the fee must be applied via
 * either:
 * - A Shoplazza Admin/Store API that adds an additional price to the
 *   checkout for this order_token, or
 * - An App Proxy so the store domain forwards to this endpoint and
 *   Shoplazza includes our fee when recalculating price.
 *
 * Until the exact Shoplazza API is confirmed, we validate the request and
 * return 200 so the widget does not break. Add the actual Shoplazza call
 * here when available.
 */
export async function POST(request: NextRequest) {
  let rawBody: unknown;
  try {
    rawBody = await request.json();
  } catch {
    return withCors(NextResponse.json({ error: "Invalid JSON" }, { status: 400 }));
  }
  const { applyFeeSchema } = await import("@/lib/validation/schemas");
  const parseResult = applyFeeSchema.safeParse(rawBody);
  if (!parseResult.success) {
    const msg = parseResult.error.flatten().formErrors[0] ?? "Validation failed";
    return withCors(NextResponse.json({ error: msg }, { status: 400 }));
  }
  const body = parseResult.data;
  const shop = body.shop;
  const orderToken = body.order_token;
  const amount = body.amount?.trim() ?? "";
  const enabled = !!body.enabled;

  const store = await getStoreByShop(shop);
  if (!store?.accessToken) {
    return withCors(
      NextResponse.json({ error: "Store not found or not installed" }, { status: 404 })
    );
  }

  // Optional: call Shoplazza to add/remove the fee for this checkout.
  // Example (replace with real endpoint when documented):
  // const apiUrl = `https://${shop}/admin/api/.../checkouts/${orderToken}`;
  // await fetch(apiUrl, { method: "PATCH", headers: { Authorization: `Bearer ${store.accessToken}` }, body: JSON.stringify({ additional_prices: enabled ? [{ name: "item_protection", price: amount, fee_title: body.label || "Item protection" }] : [] }) });
  if (enabled && amount) {
    console.info("[apply-fee]", { shop, order_token: orderToken, amount, label: body.label });
  }

  return withCors(NextResponse.json({ ok: true }));
}

function withCors(res: NextResponse) {
  res.headers.set("Access-Control-Allow-Origin", "*");
  res.headers.set("Access-Control-Allow-Methods", "POST, OPTIONS");
  res.headers.set("Access-Control-Allow-Headers", "Content-Type");
  return res;
}

export async function OPTIONS() {
  const res = new NextResponse(null, { status: 204 });
  return withCors(res);
}
