import { NextResponse } from "next/server";
import { getRedirectUri } from "@/lib/shoplazza/auth";

/**
 * GET /api/auth/redirect-uri
 * Debug: returns the exact redirect_uri this app sends to Shoplazza.
 * Use this to copy into Partner Center → Redirect URL (must match exactly).
 */
export async function GET() {
  const redirectUri = getRedirectUri();
  return NextResponse.json({
    redirect_uri: redirectUri,
    hint: "Copy the redirect_uri value into Partner Center → Settings → Redirect URL (exact match required).",
  });
}
