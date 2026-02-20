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

export type CreateProductResult =
  | { productId: string; variantId: string }
  | { error: string };

/**
 * Create a single product "Item protection" with one variant at price 0.
 * Requires write_product scope.
 * @returns { productId, variantId } or { error: string } so callers can show the exact failure.
 */
export async function createItemProtectionProduct(
  shop: string,
  accessToken: string
): Promise<{ productId: string; variantId: string } | null> {
  const result = await createItemProtectionProductWithError(shop, accessToken);
  return "error" in result ? null : result;
}

export async function createItemProtectionProductWithError(
  shop: string,
  accessToken: string
): Promise<CreateProductResult> {
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

    const text = await res.text();
    if (!res.ok) {
      console.error("[item-protection-product] Create product failed:", res.status, text);
      return { error: `Create product failed: ${res.status} ${text}` };
    }

    let data: { product?: { id?: string; variants?: Array<{ id?: string }> }; id?: string; variants?: Array<{ id?: string }> };
    try {
      data = JSON.parse(text) as typeof data;
    } catch {
      return { error: `Invalid JSON response: ${text.slice(0, 200)}` };
    }
    const product = data.product ?? data;
    const productId = product.id != null ? String(product.id) : null;
    const variants = product.variants ?? [];
    const variantId = variants[0]?.id != null ? String(variants[0].id) : null;

    if (!productId || !variantId) {
      console.error("[item-protection-product] Create product response missing id/variant:", data);
      return { error: `Response missing id/variant: ${JSON.stringify(data).slice(0, 300)}` };
    }

    return { productId, variantId };
  } catch (err) {
    const msg = err instanceof Error ? err.message : String(err);
    console.error("[item-protection-product] Create product error:", err);
    return { error: msg };
  }
}

/**
 * Ensure the store has an Item Protection product and its IDs saved.
 * If settings.itemProtectionProductId is missing, creates the product via OpenAPI,
 * updates StoreSettings, and binds the Cart Transform callback.
 * Call this from install callback or from public-settings so merchants never have to configure anything.
 */
export async function ensureItemProtectionProduct(
  shop: string,
  accessToken: string,
  storeSettingsId: string,
  callbackBaseUrl: string
): Promise<{ productId: string; variantId: string } | null> {
  const result = await ensureItemProtectionProductWithError(
    shop,
    accessToken,
    storeSettingsId,
    callbackBaseUrl
  );
  return "error" in result ? null : result;
}

/**
 * Same as ensureItemProtectionProduct but returns { error: string } on failure so callers can display it.
 */
export async function ensureItemProtectionProductWithError(
  shop: string,
  accessToken: string,
  storeSettingsId: string,
  callbackBaseUrl: string
): Promise<
  | { productId: string; variantId: string }
  | { error: string }
> {
  const created = await createItemProtectionProductWithError(shop, accessToken);
  if ("error" in created) return created;
  const { prisma } = await import("@/lib/db");
  await prisma.storeSettings.update({
    where: { id: storeSettingsId },
    data: {
      itemProtectionProductId: created.productId,
      itemProtectionVariantId: created.variantId,
    },
  });
  const callbackUrl = `${callbackBaseUrl.replace(/\/$/, "")}/api/shoplazza/cart-transform`;
  const bound = await bindCartTransform(shop, accessToken, callbackUrl);
  if (!bound) {
    console.warn("[item-protection-product] Cart Transform bind failed; product was created and IDs saved.");
  }
  return created;
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
