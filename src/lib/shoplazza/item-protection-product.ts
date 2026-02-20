/**
 * Create "Item Protection" product in the merchant store via Shoplazza OpenAPI
 * and bind our Cart Transform callback so the line's price is set dynamically.
 * Used on app install so merchants never create a product manually.
 */

const OPENAPI_VERSION = "2024-07";

function normalizeShop(shop: string): string {
  const s = shop.trim().toLowerCase();
  if (s.startsWith("http://") || s.startsWith("https://")) {
    try {
      const u = new URL(s);
      return u.hostname;
    } catch {
      return s;
    }
  }
  return s.includes(".") ? s : `${s}.myshoplaza.com`;
}

/**
 * Create a single product "Item protection" with one variant at price 0.
 * Requires write_product scope.
 * @returns { productId, variantId } or null if creation fails.
 */
export async function createItemProtectionProduct(
  shop: string,
  accessToken: string
): Promise<{ productId: string; variantId: string } | null> {
  const host = normalizeShop(shop);
  const url = `https://${host}/openapi/${OPENAPI_VERSION}/products`;

  const body = {
    title: "Item protection",
    brief: "Protection for your order (added at checkout).",
    has_only_default_variant: true,
    options: [{ name: "Title", values: ["Default"] }],
    images: [],
    variants: [
      {
        option1: "Default",
        price: "0",
        position: 1,
        sku: "ITEM-PROTECTION",
      },
    ],
    published: false,
    requires_shipping: false,
  };

  try {
    const res = await fetch(url, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Accept: "application/json",
        "access-token": accessToken,
        Authorization: `Bearer ${accessToken}`,
      },
      body: JSON.stringify(body),
    });

    if (!res.ok) {
      const text = await res.text();
      console.error("[item-protection-product] Create product failed:", res.status, text);
      return null;
    }

    const data = (await res.json()) as {
      product?: { id?: string; variants?: Array<{ id?: string }> };
      id?: string;
      variants?: Array<{ id?: string }>;
    };
    const product = data.product ?? data;
    const productId = product.id != null ? String(product.id) : null;
    const variants = product.variants ?? [];
    const variantId = variants[0]?.id != null ? String(variants[0].id) : null;

    if (!productId || !variantId) {
      console.error("[item-protection-product] Create product response missing id/variant:", data);
      return null;
    }

    return { productId, variantId };
  } catch (err) {
    console.error("[item-protection-product] Create product error:", err);
    return null;
  }
}

/**
 * Bind our Cart Transform function URL to the store so Shoplazza calls us
 * when the cart is used and we can return operations.update for the Item Protection line.
 * Exact request body may vary by Shoplazza API; we send function_url or url.
 */
export async function bindCartTransform(
  shop: string,
  accessToken: string,
  callbackUrl: string
): Promise<boolean> {
  const host = normalizeShop(shop);
  const url = `https://${host}/openapi/${OPENAPI_VERSION}/function/cart-transform`;

  try {
    const res = await fetch(url, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Accept: "application/json",
        "access-token": accessToken,
        Authorization: `Bearer ${accessToken}`,
      },
      body: JSON.stringify({
        function_url: callbackUrl,
        url: callbackUrl,
      }),
    });

    if (!res.ok) {
      const text = await res.text();
      console.warn("[item-protection-product] Bind cart-transform failed:", res.status, text);
      return false;
    }
    return true;
  } catch (err) {
    console.warn("[item-protection-product] Bind cart-transform error:", err);
    return false;
  }
}
