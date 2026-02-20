import fs from "fs";
import path from "path";

/**
 * Create "Item Protection" product in the merchant store via Shoplazza OpenAPI
 * and register a Cart Transform function (per Shoplazza docs: Create Function with
 * uploaded code, then Bind Cart Transform with function_id). No external URL.
 *
 * Create Product: https://www.shoplazza.dev/reference/create-product-v2025-06
 * Function API: https://www.shoplazza.dev/v2024.07/reference/function-execution-logic
 * Create Function: POST .../openapi/2024-07/function (body: name, code, runtime)
 * Bind Cart Transform: POST .../openapi/2024-07/function/cart-transform (body: function_id)
 */
const PRODUCTS_OPENAPI_VERSION = "2025-06";
const CART_TRANSFORM_OPENAPI_VERSION = "2024-07";

/**
 * Javy-compatible JavaScript for Cart Transform. Reads cart JSON from stdin,
 * finds the "Item protection" line (by product title), gets percent from
 * product metafield cd_insure.percent or default 20, computes premium and
 * writes operations JSON to stdout. No external calls; ECMAScript 2020 sync only.
 * @see https://www.shoplazza.dev/v2024.07/reference/function-execution-logic
 */
const CART_TRANSFORM_FUNCTION_CODE = `
function readInput() {
  var chunkSize = 1024;
  var inputChunks = [];
  var totalBytes = 0;
  while (1) {
    var buffer = new Uint8Array(chunkSize);
    var bytesRead = Javy.IO.readSync(0, buffer);
    totalBytes += bytesRead;
    if (bytesRead === 0) break;
    inputChunks.push(buffer.subarray(0, bytesRead));
  }
  var combined = new Uint8Array(totalBytes);
  var offset = 0;
  for (var i = 0; i < inputChunks.length; i++) {
    combined.set(inputChunks[i], offset);
    offset += inputChunks[i].length;
  }
  return JSON.parse(new TextDecoder().decode(combined));
}
function writeOutput(obj) {
  var json = JSON.stringify(obj);
  var bytes = new TextEncoder().encode(json);
  Javy.IO.writeSync(1, bytes);
}
var input = readInput();
var cart = input.cart || {};
var lineItems = cart.line_items || [];
var protectionLineId = null;
var subtotalOther = 0;
var percent = 20;
for (var i = 0; i < lineItems.length; i++) {
  var line = lineItems[i];
  var product = line.product || {};
  var title = (product.title || product.product_title || "").trim();
  if (title === "Item protection") {
    protectionLineId = String(line.id || line.item_id || "");
    var mfs = product.metafields || [];
    for (var j = 0; j < mfs.length; j++) {
      if (mfs[j].namespace === "cd_insure" && mfs[j].key === "percent") {
        var v = parseFloat(mfs[j].value);
        if (!isNaN(v) && v >= 0 && v <= 100) percent = v;
        break;
      }
    }
  } else {
    var price = parseFloat(product.price || product.price_amount || line.price || line.final_price || "0") || 0;
    var qty = parseInt(String(line.quantity || "1"), 10) || 1;
    subtotalOther += price * qty;
  }
}
var result = { operations: { update: [] } };
if (protectionLineId) {
  var premium = Math.round(subtotalOther * percent / 100 * 100) / 100;
  premium = Math.max(0, Math.min(999999999, premium));
  result.operations.update.push({
    id: protectionLineId,
    price: { adjustment_fixed_price: premium.toFixed(2) }
  });
}
writeOutput(result);
`;

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
  const bindResult = await createAndBindCartTransformFunction(shop, accessToken);
  if (!bindResult.ok) {
    const detail = "status" in bindResult ? `${bindResult.status} ${bindResult.body}` : bindResult.error;
    console.warn("[item-protection-product] Cart Transform create/bind failed; product was created and IDs saved.", detail);
  }
  return created;
}

export type BindCartTransformResult = { ok: true } | { ok: false; status: number; body: string } | { ok: false; error: string };

const PARTNER_API_BASE = "https://partners.shoplazza.com/openapi/2024-07";

const cartTransformHeaders = (accessToken: string) => ({
  "Content-Type": "application/json",
  Accept: "application/json",
  "access-token": accessToken,
  Authorization: `Bearer ${accessToken}`,
});

/**
 * Create function via Partner API (how Shoplazza-REFERENCE does it).
 * POST https://partners.shoplazza.com/openapi/2024-07/functions with multipart/form-data:
 * namespace=cart_transform, name=..., source_code=..., file=(WASM binary).
 * Auth: partner token + app-client-id. Returns function id.
 * @see docs/CART_TRANSFORM_PARTNER_API.md
 */
async function createFunctionViaPartnerAPI(
  name: string,
  sourceCode: string
): Promise<{ ok: true; id: string } | { ok: false; status: number; body: string } | null> {
  let partnerToken: string;
  try {
    const { getPartnerToken } = await import("@/lib/shoplazza/auth");
    partnerToken = await getPartnerToken();
  } catch (e) {
    console.warn("[item-protection-product] Partner token failed (skip Partner API create):", e instanceof Error ? e.message : e);
    return null;
  }
  const clientId = process.env.SHOPLAZZA_CLIENT_ID;
  if (!clientId) {
    console.warn("[item-protection-product] SHOPLAZZA_CLIENT_ID missing for Partner API");
    return null;
  }
  const wasmPath = path.join(process.cwd(), "public", "cart-transform.wasm");
  if (!fs.existsSync(wasmPath)) {
    const msg =
      "WASM file required for Cart Transform. Run: npm run build:cart-transform-wasm (ensures public/cart-transform.wasm exists).";
    console.warn("[item-protection-product]", msg);
    return { ok: false, status: 400, body: msg };
  }
  const wasmBuffer = fs.readFileSync(wasmPath);
  const url = `${PARTNER_API_BASE}/functions`;
  const form = new FormData();
  form.append("namespace", "cart_transform");
  form.append("name", name);
  form.append("source_code", sourceCode);
  form.append("file", new Blob([wasmBuffer]), "cart-transform.wasm");

  const res = await fetch(url, {
    method: "POST",
    headers: {
      "Access-Token": partnerToken,
      "app-client-id": clientId,
    },
    body: form,
  });
  const text = await res.text();
  if (!res.ok) {
    console.warn("[item-protection-product] Partner API Create function failed:", res.status, "body:", text?.slice(0, 400));
    return { ok: false, status: res.status, body: text };
  }
  try {
    const data = text ? JSON.parse(text) : {};
    const rawId = data?.id ?? data?.data?.id ?? data?.function_id ?? data?.data?.function_id;
    if (rawId != null) {
      const id = String(rawId);
      console.info("[item-protection-product] Partner API Create function succeeded, id:", id);
      return { ok: true, id };
    }
    console.warn("[item-protection-product] Partner API response missing function id:", text?.slice(0, 200));
    return { ok: false, status: 500, body: text || "Response missing function id" };
  } catch {
    return { ok: false, status: 500, body: text };
  }
}

/**
 * Create Function (Store API – often 404). POST .../openapi/2024-07/function
 * Body: { name, code, runtime }. Returns function id from response.
 * @see https://www.shoplazza.dev/v2024.07/reference/create-function
 */
async function createFunctionWithCode(
  host: string,
  accessToken: string,
  payload: { name: string; code: string; runtime?: string }
): Promise<{ ok: true; id: string } | { ok: false; status: number; body: string }> {
  const body = {
    name: payload.name,
    code: payload.code,
    runtime: payload.runtime ?? "javascript",
  };
  const urlsToTry = [
    `https://${host}/openapi/${CART_TRANSFORM_OPENAPI_VERSION}/function`,
    `https://${host}/openapi/${CART_TRANSFORM_OPENAPI_VERSION}/function/cart-transform/`,
  ];
  for (const url of urlsToTry) {
    const res = await fetch(url, {
      method: "POST",
      headers: cartTransformHeaders(accessToken),
      body: JSON.stringify(body),
    });
    const text = await res.text();
    if (!res.ok) {
      console.warn("[item-protection-product] Create function failed:", res.status, "URL:", url, "body:", text?.slice(0, 300));
      if (res.status === 404 && urlsToTry.indexOf(url) < urlsToTry.length - 1) continue;
      return { ok: false, status: res.status, body: text };
    }
    try {
      const data = text ? JSON.parse(text) : {};
      const id = data?.id ?? data?.data?.id ?? data?.function_id ?? data?.data?.function_id;
      if (id != null && typeof id === "string") {
        console.info("[item-protection-product] Create function succeeded, id:", id, "URL:", url);
        return { ok: true, id };
      }
      console.warn("[item-protection-product] Create function response missing id:", text?.slice(0, 200));
      return { ok: false, status: 500, body: text || "Response missing function id" };
    } catch {
      return { ok: false, status: 500, body: text };
    }
  }
  return { ok: false, status: 404, body: "Create function returned 404 for all tried URLs" };
}

/**
 * Bind Cart Transform with a function id. POST .../openapi/2024-07/function/cart-transform
 * Store API expects body: { id: string } (function id from Partner API Create).
 * @see https://www.shoplazza.dev/v2024.07/reference/bind-cart-transform-function
 */
async function bindCartTransformByFunctionId(
  host: string,
  accessToken: string,
  functionId: string
): Promise<{ ok: true } | { ok: false; status: number; body: string }> {
  const url = `https://${host}/openapi/${CART_TRANSFORM_OPENAPI_VERSION}/function/cart-transform`;
  const id = String(functionId);
  const res = await fetch(url, {
    method: "POST",
    headers: cartTransformHeaders(accessToken),
    body: JSON.stringify({ id }),
  });
  const text = await res.text();
  if (!res.ok) {
    console.warn("[item-protection-product] Bind cart-transform failed:", res.status, "URL:", url, "body:", text?.slice(0, 300));
    return { ok: false, status: res.status, body: text };
  }
  console.info("[item-protection-product] Bind cart-transform succeeded:", res.status);
  return { ok: true };
}

/**
 * Create the Item Protection cart-transform function (upload code) and bind it.
 * Tries Partner API first (Create at partners.shoplazza.com, then Bind at store), then store API create.
 * @see docs/CART_TRANSFORM_PARTNER_API.md and Shoplazza-REFERENCE
 */
export async function createAndBindCartTransformFunction(
  shop: string,
  accessToken: string
): Promise<BindCartTransformResult> {
  const host = normalizeShop(shop);
  const name = "item-protection-cart-transform";

  let functionId: string | null = null;

  const partnerResult = await createFunctionViaPartnerAPI(name, CART_TRANSFORM_FUNCTION_CODE);
  if (partnerResult?.ok === true) {
    functionId = partnerResult.id;
  } else if (partnerResult && !partnerResult.ok) {
    return { ok: false, status: partnerResult.status, body: partnerResult.body };
  }

  if (!functionId) {
    const created = await createFunctionWithCode(host, accessToken, {
      name,
      code: CART_TRANSFORM_FUNCTION_CODE,
      runtime: "javascript",
    });
    if (!created.ok) {
      return { ok: false, status: created.status, body: created.body };
    }
    functionId = created.id;
  }

  const bindResult = await bindCartTransformByFunctionId(host, accessToken, functionId);
  if (!bindResult.ok) {
    return { ok: false, status: bindResult.status, body: bindResult.body };
  }
  return { ok: true };
}

/** @deprecated Use createAndBindCartTransformFunction. Kept for API compatibility. */
export async function bindCartTransform(shop: string, accessToken: string, _callbackUrl?: string): Promise<boolean> {
  const result = await createAndBindCartTransformFunction(shop, accessToken);
  return result.ok;
}

export async function bindCartTransformWithResult(
  shop: string,
  accessToken: string,
  _callbackUrl?: string
): Promise<BindCartTransformResult> {
  return createAndBindCartTransformFunction(shop, accessToken);
}
