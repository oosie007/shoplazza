import { NextRequest, NextResponse } from "next/server";
import { getStoreByShop, getStoreInfoFromShoplazza } from "@/lib/shoplazza/store";

/**
 * GET /api/store-location
 * Fetches the current store's location (country) from Shoplazza API.
 * This is called by the Admin UI to display store location status without database queries.
 */
export async function GET(request: NextRequest) {
  try {
    const shop = request.nextUrl.searchParams.get("shop");

    if (!shop) {
      return NextResponse.json(
        { error: "Missing shop parameter" },
        { status: 400 }
      );
    }

    // Get store from database to access the access token
    const store = await getStoreByShop(shop);
    if (!store) {
      return NextResponse.json(
        { error: "Store not found" },
        { status: 404 }
      );
    }

    // Fetch current location from Shoplazza API (real-time)
    const storeInfo = await getStoreInfoFromShoplazza(shop, store.accessToken);

    return NextResponse.json({
      country_code: storeInfo.country_code || null,
      country_name: storeInfo.country_name || null,
    });
  } catch (error) {
    console.error("[store-location] Error fetching store location:", error);
    return NextResponse.json(
      { error: "Failed to fetch store location" },
      { status: 500 }
    );
  }
}
