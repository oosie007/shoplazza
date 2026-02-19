import { NextRequest, NextResponse } from "next/server";
import {
  verifyHmac,
  exchangeCodeForToken,
  getRedirectUri,
} from "@/lib/shoplazza/auth";
import { z } from "zod";

const callbackQuerySchema = z.object({
  shop: z.string().min(1),
  code: z.string().min(1),
});

/**
 * GET /api/auth/callback
 * Redirect URL – Shoplazza sends the merchant here after they approve, with code, shop, hmac.
 * We verify HMAC, exchange code for access_token, store the installation, then redirect to /admin.
 */
export async function GET(request: NextRequest) {
  const searchParams = request.nextUrl.searchParams;
  const queryResult = callbackQuerySchema.safeParse({
    shop: searchParams.get("shop"),
    code: searchParams.get("code"),
  });
  if (!queryResult.success) {
    return NextResponse.json(
      { error: "Missing shop or code" },
      { status: 400 }
    );
  }
  const { shop, code } = queryResult.data;

  const query: Record<string, string> = {};
  searchParams.forEach((value, key) => {
    query[key] = value;
  });

  if (!verifyHmac(query)) {
    return NextResponse.json({ error: "Invalid HMAC" }, { status: 400 });
  }

  const redirectUri = getRedirectUri();

  try {
    const { access_token } = await exchangeCodeForToken(shop, code, redirectUri);

    const { saveInstallation } = await import("@/lib/shoplazza/store");
    await saveInstallation(shop, access_token);

    // Redirect to admin (Phase 2 will serve the embedded UI)
    const base = process.env.NEXT_PUBLIC_APP_URL || request.nextUrl.origin;
    const adminUrl = new URL("/admin", base);
    adminUrl.searchParams.set("shop", shop);

    return NextResponse.redirect(adminUrl.toString());
  } catch (e) {
    console.error("Token exchange error:", e);
    return NextResponse.json(
      { error: e instanceof Error ? e.message : "Token exchange failed" },
      { status: 500 }
    );
  }
}
