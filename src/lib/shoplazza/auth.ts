import crypto from "crypto";

const clientId = process.env.SHOPLAZZA_CLIENT_ID!;
const clientSecret = process.env.SHOPLAZZA_CLIENT_SECRET!;

/**
 * OAuth scopes – must match Shoplazza’s Access Scopes table exactly.
 * See: https://www.shoplazza.dev/reference/access-scopes-copy
 * Note: The published scope list has no "function" scope; Cart Transform API 403 may need
 * a different permission (e.g. write_price_rules) or Partner Center / support confirmation.
 */
export const SCOPES = [
  "read_shop",
  "write_shop",
  "read_product",
  "write_product",
  "read_collection",
  "read_order",
  "write_order",
  "read_customer",
  "read_payment_info",
  "read_price_rules",
  "write_price_rules",
].join(" ");

/**
 * Verify HMAC on install/callback query string.
 * Shoplazza sends: shop, hmac, code, timestamp (and possibly state).
 * Build message from sorted params (excluding hmac), then HMAC-SHA256 with client secret.
 * Tries both hex and base64 encoding (platform may use either).
 */
export function verifyHmac(query: Record<string, string | undefined>): boolean {
  const receivedHmac = query.hmac;
  if (!receivedHmac || !clientSecret) return false;

  const sorted = Object.keys(query)
    .filter((k) => k !== "hmac" && query[k] != null)
    .sort()
    .map((k) => `${k}=${query[k]}`)
    .join("&");

  const computedHex = crypto
    .createHmac("sha256", clientSecret)
    .update(sorted)
    .digest("hex");
  const computedBase64 = crypto
    .createHmac("sha256", clientSecret)
    .update(sorted)
    .digest("base64");

  try {
    if (
      crypto.timingSafeEqual(
        Buffer.from(receivedHmac, "hex"),
        Buffer.from(computedHex, "hex")
      )
    )
      return true;
  } catch {
    // ignore invalid hex
  }
  try {
    if (
      crypto.timingSafeEqual(
        Buffer.from(receivedHmac, "base64"),
        Buffer.from(computedBase64, "base64")
      )
    )
      return true;
  } catch {
    // ignore invalid base64
  }
  return false;
}

/**
 * Build the Shoplazza authorize URL for the given shop.
 * Merchant will be sent here to grant permissions; then Shoplazza redirects to redirect_uri with ?code=...
 */
export function getAuthorizeUrl(shop: string, redirectUri: string): string {
  const params = new URLSearchParams({
    response_type: "code",
    client_id: clientId,
    redirect_uri: redirectUri,
    scope: SCOPES,
  });
  return `https://${shop}/admin/oauth/authorize?${params.toString()}`;
}

/**
 * Exchange authorization code for access_token.
 * Token URL: https://{shop}/admin/oauth/token
 */
export async function exchangeCodeForToken(
  shop: string,
  code: string,
  redirectUri: string
): Promise<{ access_token: string; scope?: string }> {
  const tokenUrl = `https://${shop}/admin/oauth/token`;
  const body = new URLSearchParams({
    grant_type: "authorization_code",
    code,
    redirect_uri: redirectUri,
    client_id: clientId,
    client_secret: clientSecret,
  });

  const res = await fetch(tokenUrl, {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: body.toString(),
  });

  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Token exchange failed: ${res.status} ${text}`);
  }

  const data = (await res.json()) as {
    access_token: string;
    scope?: string;
    refresh_token?: string;
  };
  return { access_token: data.access_token, scope: data.scope };
}

export function getRedirectUri(): string {
  const base = process.env.NEXT_PUBLIC_APP_URL || "http://localhost:3000";
  return `${base.replace(/\/$/, "")}/api/auth/callback`;
}

const PARTNER_TOKEN_URL = "https://partners.shoplazza.com/partner/oauth/token";

/**
 * Get a partner-level access token for Partner API (e.g. Create Function).
 * Uses client_credentials grant with app Client ID and Secret.
 * @see Shoplazza-REFERENCE: ShoplazzaAuthService.GetPartnerTokenAsync
 */
export async function getPartnerToken(): Promise<string> {
  if (!clientId || !clientSecret) {
    throw new Error("SHOPLAZZA_CLIENT_ID or SHOPLAZZA_CLIENT_SECRET is missing");
  }
  const res = await fetch(PARTNER_TOKEN_URL, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      client_id: clientId,
      client_secret: clientSecret,
      grant_type: "client_credentials",
    }),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Partner token failed: ${res.status} ${text}`);
  }
  const data = (await res.json()) as { access_token?: string };
  if (!data.access_token) {
    throw new Error("Partner token response missing access_token");
  }
  return data.access_token;
}
