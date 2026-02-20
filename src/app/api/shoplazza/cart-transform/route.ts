import { NextRequest, NextResponse } from "next/server";
import { getStoreByShop } from "@/lib/shoplazza/store";

/**
 * POST /api/shoplazza/cart-transform
 *
 * Shoplazza Cart Transform Function callback. When a store has our cart-transform
 * function bound, Shoplazza POSTs the cart here. We find the Item Protection
 * line (by itemProtectionProductId) and return operations.update to set its
 * price to the computed premium (fixedPercentAll % of other lines' subtotal).
 *
 * Input (Shoplazza): { cart: { line_items: [...] }, shop?: string }
 * Output: { operations: { update: [ { id, price: { adjustment_fixed_price } } ] } }
 *
 * @see https://www.shoplazza.dev/v2024.07/reference/bind-cart-transform-function
 * @see docs/CART_TRANSFORM_FLOW.md
 */

/** GET: health check so you can confirm the callback URL is reachable (e.g. from a browser). */
export async function GET() {
  console.info("[cart-transform] GET health check");
  return NextResponse.json({ ok: true, message: "Cart Transform endpoint is reachable" });
}

export async function POST(request: NextRequest) {
  console.info("[cart-transform] POST received");
  let body: unknown;
  try {
    body = await request.json();
  } catch {
    return NextResponse.json(
      { error: "Invalid JSON" },
      { status: 400 }
    );
  }

  const cart = (body as any)?.cart;
  const lineItems = Array.isArray(cart?.line_items) ? cart.line_items : [];
  const shop =
    (body as any)?.shop ??
    request.headers.get("x-shop-domain") ??
    request.headers.get("x-shop");

  if (!shop || typeof shop !== "string") {
    return NextResponse.json(
      { error: "Missing shop (send in body.shop or header x-shop-domain)" },
      { status: 400 }
    );
  }

  const store = await getStoreByShop(shop.trim());
  const settings = store?.settings as any;
  if (!store || !settings) {
    return NextResponse.json(
      { error: "Store not found or not installed" },
      { status: 404 }
    );
  }

  const protectionProductId = settings.itemProtectionProductId
    ? String(settings.itemProtectionProductId).trim()
    : "";
  if (!protectionProductId) {
    return NextResponse.json({
      operations: { update: [] },
    });
  }

  const fixedPercentAll = Number(settings.fixedPercentAll) || 0;
  let protectionLine: { id: string; product_id: string } | null = null;
  let subtotalOther = 0;

  for (const line of lineItems) {
    const product = line?.product ?? {};
    const productId =
      line.product_id != null ? String(line.product_id) :
      product.product_id != null ? String(product.product_id) :
      product.productId != null ? String(product.productId) : "";
    const price = parseFloat(
      product.price ?? product.price_amount ?? line.price ?? line.final_price ?? "0"
    ) || 0;
    const quantity = parseInt(String(line.quantity ?? "1"), 10) || 1;
    const lineTotal = price * quantity;

    if (productId === protectionProductId) {
      const lineId = String(line.id ?? line.item_id ?? "");
      protectionLine = { id: lineId, product_id: productId };
    } else {
      subtotalOther += lineTotal;
    }
  }

  if (!protectionLine || !protectionLine.id) {
    return NextResponse.json({
      operations: { update: [] },
    });
  }

  const premium = +(subtotalOther * (fixedPercentAll / 100)).toFixed(2);
  const adjustment = Math.max(0, Math.min(999999999, premium)).toFixed(2);

  console.info("[cart-transform]", { shop, protectionLineId: protectionLine.id, subtotalOther, fixedPercentAll, adjustment });
  return NextResponse.json({
    operations: {
      update: [
        {
          id: protectionLine.id,
          price: {
            adjustment_fixed_price: adjustment,
          },
        },
      ],
    },
  });
}
