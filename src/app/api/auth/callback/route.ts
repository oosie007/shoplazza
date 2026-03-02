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

    const {
      saveInstallation,
      getStoreByShop,
      getStoreInfoFromShoplazza,
      isSupportedCountry,
      getCountryName,
    } = await import("@/lib/shoplazza/store");

    // Fetch store info to validate location
    console.log("[auth/callback] Fetching store info for location validation");
    const storeInfo = await getStoreInfoFromShoplazza(shop, access_token);
    const countryCode = storeInfo.country_code?.toUpperCase();

    // Check if country is supported
    if (!isSupportedCountry(countryCode)) {
      console.warn(
        `[auth/callback] Store in unsupported country: ${countryCode} (${storeInfo.country_name})`
      );
      return NextResponse.json(
        {
          error: "Item Protection is not available in your country",
          supportedCountries: ["UK", "France", "Switzerland", "Netherlands"],
          storeCountry: storeInfo.country_name || "Unknown",
          message:
            "Item Protection is currently only available for merchants in the United Kingdom, France, Switzerland, and Netherlands.",
        },
        { status: 403 }
      );
    }

    console.log(`[auth/callback] Location validated: ${countryCode} (${storeInfo.country_name})`);

    // Save installation with location info
    await saveInstallation(shop, access_token);

    // Update store with location information
    const { prisma } = await import("@/lib/db");
    const store = await getStoreByShop(shop);
    if (store) {
      await prisma.store.update({
        where: { id: store.id },
        data: {
          country_code: countryCode || "",
          country_name: storeInfo.country_name || getCountryName(countryCode || ""),
        },
      });

      // Update settings: location is valid
      if (store.settings) {
        await prisma.storeSettings.update({
          where: { id: store.settings.id },
          data: {
            location_valid: true,
          },
        });
      }

      console.log(`[auth/callback] Store saved with location: ${countryCode}`);
    }

    // Create "Item Protection" product and bind Cart Transform (one product per store; no merchant action)
    try {
      const store = await getStoreByShop(shop);
      const settings = store?.settings as { itemProtectionProductId?: string | null } | undefined;
      if (store?.settings && !settings?.itemProtectionProductId) {
        const base = process.env.NEXT_PUBLIC_APP_URL || request.nextUrl.origin;
        const { ensureItemProtectionProduct } = await import("@/lib/shoplazza/item-protection-product");
        await ensureItemProtectionProduct(shop, access_token, store.settings.id, base);
      }
    } catch (setupErr) {
      console.error("[auth/callback] Item Protection setup failed (non-blocking):", setupErr);
    }

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
