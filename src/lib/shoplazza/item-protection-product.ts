/**
 * Create "Item Protection" product in the merchant store via Shoplazza OpenAPI
 * and bind our Cart Transform callback so the line's price is set dynamically.
 * Used on app install so merchants never create a product manually.
 *
 * Create Product: https://www.shoplazza.dev/reference/create-product-v2025-06
 * POST https://{subdomain}.myshoplaza.com/openapi/2025-06/products
 * Cart Transform remains under 2024-07.
 */
const PRODUCTS_OPENAPI_VERSION = "2025-06";
const CART_TRANSFORM_OPENAPI_VERSION = "2024-07";

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

/** Fetch default variant id for a product (when create response doesn't include variants). */
async function fetchDefaultVariantId(
  host: string,
  accessToken: string,
  productId: string
): Promise<string | null> {
  const url = `https://${host}/openapi/${PRODUCTS_OPENAPI_VERSION}/products/${productId}`;
  try {
    const res = await fetch(url, {
      headers: {
        Accept: "application/json",
        "access-token": accessToken,
        Authorization: `Bearer ${accessToken}`,
      },
    });
    if (!res.ok) return null;
    const data = (await res.json()) as {
      data?: { product?: { variants?: Array<{ id?: string }> } };
      product?: { variants?: Array<{ id?: string }> };
      variants?: Array<{ id?: string }>;
    };
    const product = data.data?.product ?? data.product ?? data;
    const variants = product?.variants ?? data.variants ?? [];
    const id = variants[0]?.id != null ? String(variants[0].id) : null;
    return id;
  } catch {
    return null;
  }
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
  accessToken: string,
  appBaseUrl?: string
): Promise<{ productId: string; variantId: string } | null> {
  const result = await createItemProtectionProductWithError(shop, accessToken, appBaseUrl);
  return "error" in result ? null : result;
}

export async function createItemProtectionProductWithError(
  shop: string,
  accessToken: string,
  appBaseUrl?: string
): Promise<CreateProductResult> {
  const host = normalizeShop(shop);
  const url = `https://${host}/openapi/${PRODUCTS_OPENAPI_VERSION}/products`;

  const base = (appBaseUrl || process.env.NEXT_PUBLIC_APP_URL || "").replace(/\/$/, "");
  const imageSrc = base ? `${base}/item-protection-product.png` : "https://placehold.co/64x64/191919/white?text=IP";
  const images = [{ src: imageSrc }];

  // 2025-06: has_only_default_variant=true requires options and values to be empty ("This only default variant product option must be empty").
  const product = {
    title: "Item protection",
    brief: "Protection for your order (added at checkout).",
    has_only_default_variant: true,
    options: [],
    values: [],
    images,
    variants: [
      {
        price: 0,
        position: 1,
        sku: "ITEM-PROTECTION",
      },
    ],
    published: true,
    requires_shipping: false,
  };
  const body = { product };

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

    let data: {
      code?: string;
      data?: { product?: { id?: string; variants?: Array<{ id?: string }> }; variants?: Array<{ id?: string }> };
      product?: { id?: string; variants?: Array<{ id?: string }> };
      id?: string;
      variants?: Array<{ id?: string }>;
    };
    try {
      data = JSON.parse(text) as typeof data;
    } catch {
      return { error: `Invalid JSON response: ${text.slice(0, 200)}` };
    }
    // 2025-06 returns { code: "Success", data: { product: { id, ... } } }; older shape is { product: { id, variants } } or root id/variants
    const product = data.data?.product ?? data.product ?? data;
    const productId = product?.id != null ? String(product.id) : null;
    let variants = product?.variants ?? data.data?.variants ?? data.variants ?? [];
    let variantId = variants[0]?.id != null ? String(variants[0].id) : null;

    if (!productId) {
      console.error("[item-protection-product] Create product response missing product id:", data);
      return { error: `Response missing product id: ${JSON.stringify(data).slice(0, 300)}` };
    }

    if (!variantId) {
      variantId = await fetchDefaultVariantId(host, accessToken, productId);
      if (!variantId) {
        console.error("[item-protection-product] Create product response missing variant and fetch failed:", data);
        return { error: `Response missing variant id. Product created (${productId}); add variant ID in app admin.` };
      }
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
  const created = await createItemProtectionProductWithError(shop, accessToken, callbackBaseUrl);
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
  const bindResult = await bindCartTransformWithResult(shop, accessToken, callbackUrl);
  if (!bindResult.ok) {
    const detail = "status" in bindResult ? `${bindResult.status} ${bindResult.body}` : bindResult.error;
    console.warn("[item-protection-product] Cart Transform bind failed; product was created and IDs saved.", detail);
  }
  return created;
}

export type BindCartTransformResult = { ok: true } | { ok: false; status: number; body: string } | { ok: false; error: string };

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
  const result = await bindCartTransformWithResult(shop, accessToken, callbackUrl);
  return result.ok;
}

export async function bindCartTransformWithResult(
  shop: string,
  accessToken: string,
  callbackUrl: string
): Promise<BindCartTransformResult> {
  const host = normalizeShop(shop);
  // Shoplazza docs use base URL with trailing slash for function/cart-transform
  const url = `https://${host}/openapi/${CART_TRANSFORM_OPENAPI_VERSION}/function/cart-transform/`;

  const body = { function_url: callbackUrl, url: callbackUrl };
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
      console.warn("[item-protection-product] Bind cart-transform failed:", res.status, text || "(empty body)", "request URL:", url, "body:", JSON.stringify(body));
      return { ok: false, status: res.status, body: text || `(empty response body)` };
    }
    console.info("[item-protection-product] Bind cart-transform succeeded:", res.status, text.slice(0, 200));
    return { ok: true };
  } catch (err) {
    const msg = err instanceof Error ? err.message : String(err);
    console.warn("[item-protection-product] Bind cart-transform error:", err);
    return { ok: false, error: msg };
  }
}
