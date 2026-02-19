import { NextRequest, NextResponse } from "next/server";
import { getAuthorizeUrl, getRedirectUri } from "@/lib/shoplazza/auth";

/**
 * GET /api/auth
 * App URL – Shoplazza sends merchants here when they click "Install app".
 * Query params: shop (required), optionally hmac, timestamp.
 *
 * We redirect the merchant to Shoplazza's authorize page. After they approve,
 * Shoplazza redirects to our Redirect URL (/api/auth/callback) with code + shop + hmac.
 */
export async function GET(request: NextRequest) {
  const shop = request.nextUrl.searchParams.get("shop");

  if (!shop) {
    return NextResponse.json(
      { error: "Missing shop parameter" },
      { status: 400 }
    );
  }

  const redirectUri = getRedirectUri();
  const authorizeUrl = getAuthorizeUrl(shop, redirectUri);
  return NextResponse.redirect(authorizeUrl);
}
