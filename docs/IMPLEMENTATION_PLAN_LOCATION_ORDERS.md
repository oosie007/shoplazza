# Implementation Plan: Location Validation & Order Management

## Overview
This document outlines the implementation strategy for:
1. Store location validation (4 supported countries)
2. Supported shipping countries configuration
3. Address collection after payment
4. Order dispatch/delivery notifications via webhooks

---

## 1. Store Location Validation

### Requirement
Only allow app activation if store is in one of four supported countries:
- 🇬🇧 UK
- 🇫🇷 France
- 🇨🇭 Switzerland
- 🇳🇱 Netherlands

### Implementation Strategy

#### 1.1 Backend: Store Location Check (On Install)

**File:** `src/app/api/auth/callback/route.ts`

```typescript
// After receiving access token and creating Store record

const { getStoreInfo } = await import("@/lib/shoplazza/store");
const storeInfo = await getStoreInfo(shop, accessToken);

const SUPPORTED_COUNTRIES = ["GB", "FR", "CH", "NL"]; // ISO 3166-1 alpha-2

if (!SUPPORTED_COUNTRIES.includes(storeInfo.country_code)) {
  // Reject installation or mark as unsupported
  return NextResponse.json(
    {
      error: "Item Protection is only available in UK, France, Switzerland, and Netherlands",
      supportedCountries: ["UK", "France", "Switzerland", "Netherlands"],
      storeCountry: storeInfo.country_name,
    },
    { status: 403 }
  );
}

// Proceed with store creation
```

#### 1.2 Database Schema: Store Location

**File:** `prisma/schema.prisma`

```prisma
model Store {
  // ... existing fields

  // Store location (set during install, never changed)
  country_code    String   @default("") // ISO 3166-1 alpha-2 (GB, FR, CH, NL)
  country_name    String   @default("") // Full country name

  createdAt       DateTime @default(now())
  updatedAt       DateTime @updatedAt
}

model StoreSettings {
  // ... existing fields

  // Location validation
  location_valid  Boolean  @default(false) // Is store in supported country?

  createdAt       DateTime @default(now())
  updatedAt       DateTime @updatedAt
}
```

#### 1.3 Admin UI: Display Location Status

**File:** `src/components/admin/ConfigurationContent.tsx`

```typescript
// Add new section to show store location and validation status

<section className="rounded-xl border border-zinc-200 bg-white p-5 shadow-sm">
  <h2 className="text-lg font-medium text-zinc-900">Store Location</h2>
  <p className="mt-1 text-sm text-zinc-600">
    Item Protection availability by country.
  </p>

  <div className="mt-3 space-y-2">
    <div className="flex items-center gap-3 rounded-md bg-blue-50 p-3">
      <span className="text-2xl">
        {store.country_code === "GB" ? "🇬🇧" :
         store.country_code === "FR" ? "🇫🇷" :
         store.country_code === "CH" ? "🇨🇭" :
         store.country_code === "NL" ? "🇳🇱" : "🌍"}
      </span>
      <div className="flex-1">
        <p className="font-medium text-zinc-900">{store.country_name}</p>
        {store.location_valid ? (
          <p className="text-xs text-green-700">✅ Supported location</p>
        ) : (
          <p className="text-xs text-red-700">❌ Not supported</p>
        )}
      </div>
    </div>
  </div>
</section>
```

#### 1.4 Activation Lock: Only Activate if Location Valid

```typescript
// In updateSetting handler, prevent activation for unsupported countries

if (body.activated === true && !settings.location_valid) {
  return NextResponse.json(
    {
      error: "Cannot activate. Item Protection is not available in your store's location.",
      supportedCountries: ["UK", "France", "Switzerland", "Netherlands"],
      storeCountry: settings.country_name,
    },
    { status: 403 }
  );
}
```

---

## 2. Supported Shipping Countries Configuration

### Requirement
Allow merchants to configure which shipping countries they support for Item Protection.

### Implementation Strategy

#### 2.1 Database Schema

**File:** `prisma/schema.prisma`

```prisma
model StoreSettings {
  // ... existing fields

  // Supported shipping countries (JSON array of ISO 3166-1 alpha-2 codes)
  // Example: ["GB", "FR", "CH", "NL", "DE", "BE"]
  supported_shipping_countries String @default("[]") // JSON string: string[]

  // Override: Allow all countries in store's region?
  allow_all_in_region  Boolean  @default(true)
}
```

#### 2.2 Validation Schema

**File:** `src/lib/validation/schemas.ts`

```typescript
export const settingsPatchSchema = z.object({
  // ... existing fields
  supported_shipping_countries: z.array(z.string().length(2)).optional(),
  allow_all_in_region: z.boolean().optional(),
});
```

#### 2.3 Admin UI: Country Selection

```typescript
// In ConfigurationContent.tsx - Add new section

<section className="rounded-xl border border-zinc-200 bg-white p-5 shadow-sm">
  <h2 className="text-lg font-medium text-zinc-900">Shipping Countries</h2>
  <p className="mt-0.5 text-sm text-zinc-600">
    Select which countries can purchase Item Protection.
  </p>

  <div className="mt-3 space-y-2">
    <label className="flex items-center gap-2 text-sm">
      <input
        type="checkbox"
        checked={settings?.allow_all_in_region ?? true}
        onChange={(e) =>
          updateSetting({ allow_all_in_region: e.target.checked })
        }
      />
      <span>Allow all countries in primary region (Recommended)</span>
    </label>
  </div>

  {!settings?.allow_all_in_region && (
    <div className="mt-3 border-t border-zinc-200 pt-3">
      <p className="text-sm font-medium text-zinc-900 mb-2">
        Select specific countries:
      </p>
      <div className="grid grid-cols-2 gap-2">
        {/* Country checkboxes */}
        {EUROPEAN_COUNTRIES.map(country => (
          <label key={country.code} className="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              checked={
                (settings?.supported_shipping_countries ?? []).includes(country.code)
              }
              onChange={(e) => {
                const current = settings?.supported_shipping_countries ?? [];
                const updated = e.target.checked
                  ? [...current, country.code]
                  : current.filter(c => c !== country.code);
                updateSetting({ supported_shipping_countries: updated });
              }}
            />
            <span>{country.flag} {country.name}</span>
          </label>
        ))}
      </div>
    </div>
  )}
</section>
```

#### 2.4 Widget: Check Supported Countries at Checkout

```typescript
// In checkout-widget.js

function checkShippingCountrySupported(shippingAddress) {
  const shippingCountry = shippingAddress?.country_code;

  if (!shippingCountry) {
    console.warn("No shipping country available");
    return false;
  }

  const supported = settings.supported_shipping_countries || [];

  if (settings.allow_all_in_region) {
    // Check against region (e.g., EU countries)
    return EU_COUNTRIES.includes(shippingCountry);
  }

  return supported.includes(shippingCountry);
}

// Disable widget if shipping country not supported
if (!checkShippingCountrySupported(CheckoutAPI.address.getShippingAddress())) {
  disableWidget("Item Protection not available for this shipping country");
}
```

---

## 3. Collect Shipping & Billing Address After Payment

### Requirement
Capture customer's shipping and billing addresses and submit to backend API after successful payment.

### Implementation Strategy

#### 3.1 Address Collection Points

**Option A: At Checkout (Before Payment)**
```typescript
// Get shipping address from CheckoutAPI
const shippingAddress = CheckoutAPI.address.getShippingAddress();

// Store in widget state for later submission
window.cdInsureCheckout = {
  shippingAddress: shippingAddress,
  orderInfo: CheckoutAPI.store.getOrderInfo(),
};

// Listen for order completion
CheckoutAPI.store.onOrderStatusChange((status) => {
  if (status === "finished") {
    // Order placed successfully - submit data to backend
    submitOrderData();
  }
});
```

**Option B: After Payment Success (Order Confirmation)**
```typescript
// Listen to order status changes
CheckoutAPI.store.onOrderStatusChange((newStatus) => {
  if (newStatus === "finished" || newStatus === "placed") {
    const orderInfo = CheckoutAPI.store.getOrderInfo();
    const shippingAddress = CheckoutAPI.address.getShippingAddress();

    // Submit to backend with order ID
    fetch(`${APP_URL}/api/checkout/order-addresses`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        shop: SHOP_DOMAIN,
        order_id: orderInfo.id,
        order_no: orderInfo.order_no,
        shipping_address: shippingAddress,
        // Note: billing_address not available in Checkout API
        // Retrieve via Order API on backend instead
        created_at: new Date().toISOString(),
      }),
    });
  }
});
```

#### 3.2 Backend API: Receive Order Addresses

**File:** `src/app/api/checkout/order-addresses/route.ts`

```typescript
/**
 * POST /api/checkout/order-addresses
 *
 * Receive order data (shipping address, payment confirmation) from checkout widget.
 * Called after successful payment.
 */
export async function POST(request: NextRequest) {
  const body = (await request.json()) as any;

  const { shop, order_id, order_no, shipping_address } = body;

  if (!shop || !order_id) {
    return NextResponse.json({ error: "Missing order data" }, { status: 400 });
  }

  const store = await getStoreByShop(shop);
  if (!store) {
    return NextResponse.json({ error: "Store not found" }, { status: 404 });
  }

  try {
    // Fetch complete order from Shoplazza API (includes billing address)
    const orderData = await fetchOrderFromShoplazza(shop, order_id, store.accessToken);

    // Save order data to database
    const { prisma } = await import("@/lib/db");
    const savedOrder = await prisma.protectionOrder.create({
      data: {
        storeId: store.id,
        shoplazzaOrderId: order_id,
        shoplazzaOrderNo: order_no,
        customerEmail: shipping_address.email,
        shippingAddress: JSON.stringify(shipping_address),
        billingAddress: JSON.stringify(orderData.billing_address),
        shippingCountry: shipping_address.country_code,
        billingCountry: orderData.billing_address?.country_code,
        orderTotal: parseFloat(orderData.total_price || "0"),
        protectionAmount: 0, // Will be updated when protection selected
        status: "pending", // awaiting confirmation
      },
    });

    return NextResponse.json({
      ok: true,
      message: "Order data received",
      orderId: savedOrder.id,
    });
  } catch (error) {
    console.error("[order-addresses] Error:", error);
    return NextResponse.json(
      { error: "Failed to process order" },
      { status: 500 }
    );
  }
}

// Helper: Fetch complete order from Shoplazza
async function fetchOrderFromShoplazza(
  shop: string,
  orderId: string,
  accessToken: string
) {
  const host = shop.includes(".") ? shop : `${shop}.myshoplaza.com`;
  const url = `https://${host}/openapi/2024-07/orders/${orderId}`;

  const response = await fetch(url, {
    method: "GET",
    headers: {
      "Access-Token": accessToken,
      "Content-Type": "application/json",
    },
  });

  if (!response.ok) {
    throw new Error(`Shoplazza API error: ${response.status}`);
  }

  const data = await response.json();
  return data.data || data; // Handle different response formats
}
```

#### 3.3 Database: Store Order Data

**File:** `prisma/schema.prisma`

```prisma
model ProtectionOrder {
  id                    String   @id @default(cuid())
  storeId               String
  store                 Store    @relation(fields: [storeId], references: [id])

  // Shoplazza order identifiers
  shoplazzaOrderId      String
  shoplazzaOrderNo      String

  // Customer contact
  customerEmail         String
  customerPhone         String?

  // Addresses (stored as JSON)
  shippingAddress       String   // JSON: Address object
  billingAddress        String?  // JSON: Address object

  // Country codes for filtering
  shippingCountry       String   // ISO 3166-1 alpha-2
  billingCountry        String?

  // Protection details
  protectionEnabled     Boolean  @default(false)
  protectionAmount      Decimal

  // Order totals
  orderTotal            Decimal

  // Status tracking
  status                String   @default("pending") // pending, active, cancelled, fulfilled, delivered, returned
  shoplazzaStatus       String?
  fulfillmentStatus     String?

  // Timestamps
  createdAt             DateTime @default(now())
  updatedAt             DateTime @updatedAt
}
```

#### 3.4 Types: Define Address Structures

**File:** `src/lib/types/address.ts`

```typescript
export interface Address {
  id: string;
  email: string;
  phone: string;
  phone_area_code: string;
  first_name: string;
  last_name: string;
  company?: string;
  address: string;
  address1?: string;
  city: string;
  province: string;
  province_code: string;
  country: string;
  country_code: string; // ISO 3166-1 alpha-2
  zip: string;
  is_default: boolean;
}

export interface OrderAddressPayload {
  shop: string;
  order_id: string;
  order_no: string;
  shipping_address: Address;
  created_at: string;
}
```

---

## 4. Order Dispatch Notifications

### Requirement
Receive notifications when orders are dispatched/shipped.

### Implementation Strategy

#### 4.1 Webhook Setup

**File:** `src/lib/shoplazza/webhooks.ts`

```typescript
/**
 * Create webhooks for order events
 * Called during store installation
 */
export async function setupWebhooks(
  shop: string,
  accessToken: string,
  appUrl: string
) {
  const host = shop.includes(".") ? shop : `${shop}.myshoplaza.com`;
  const baseUrl = `https://${host}/openapi/2024-07`;

  const webhooks = [
    {
      topic: "orders/fulfilled",
      address: `${appUrl}/api/webhooks/order-fulfilled`,
      description: "Order dispatched/shipped",
    },
    {
      topic: "orders/updated",
      address: `${appUrl}/api/webhooks/order-updated`,
      description: "Order status updated",
    },
  ];

  for (const webhook of webhooks) {
    try {
      const response = await fetch(`${baseUrl}/webhooks`, {
        method: "POST",
        headers: {
          "Access-Token": accessToken,
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          topic: webhook.topic,
          address: webhook.address,
          description: webhook.description,
        }),
      });

      if (response.ok) {
        const data = await response.json();
        console.log(`[webhooks] Created ${webhook.topic}:`, data);
      } else {
        const error = await response.text();
        console.error(`[webhooks] Failed to create ${webhook.topic}:`, error);
      }
    } catch (error) {
      console.error(`[webhooks] Error setting up ${webhook.topic}:`, error);
    }
  }
}
```

#### 4.2 Webhook Handler: Order Fulfilled

**File:** `src/app/api/webhooks/order-fulfilled/route.ts`

```typescript
import { NextRequest, NextResponse } from "next/server";

/**
 * POST /api/webhooks/order-fulfilled
 *
 * Webhook from Shoplazza when order is fulfilled/shipped.
 * Updates order status in our database and triggers fulfillment workflow.
 */
export async function POST(request: NextRequest) {
  console.log("[webhook] order-fulfilled received at", new Date().toISOString());

  let body: any;
  try {
    body = await request.json();
  } catch {
    return NextResponse.json(
      { error: "Invalid JSON" },
      { status: 400 }
    );
  }

  const { data } = body;
  const { shop, id, order_no, fulfillment_status, updated_at } = data;

  if (!shop || !id) {
    return NextResponse.json(
      { error: "Missing required fields" },
      { status: 400 }
    );
  }

  try {
    const { getStoreByShop } = await import("@/lib/shoplazza/store");
    const store = await getStoreByShop(shop);

    if (!store) {
      console.warn(`[webhook] Store not found for shop: ${shop}`);
      return NextResponse.json({ ok: true }); // Return 200 to prevent retries
    }

    const { prisma } = await import("@/lib/db");

    // Update order status in our database
    const protectionOrder = await prisma.protectionOrder.updateMany({
      where: {
        storeId: store.id,
        shoplazzaOrderId: id,
      },
      data: {
        status: "dispatched", // or "fulfilled"
        fulfillmentStatus: fulfillment_status,
        updatedAt: new Date(),
      },
    });

    console.log(
      `[webhook] Updated ${protectionOrder.count} order(s) for ${order_no}: status=dispatched`
    );

    // TODO: Trigger fulfillment workflow
    // - Notify customer via email
    // - Send to insurance backend
    // - Update tracking information

    return NextResponse.json({ ok: true, message: "Order updated" });
  } catch (error) {
    const msg = error instanceof Error ? error.message : String(error);
    console.error("[webhook] Error processing fulfilled order:", msg);

    // Return 200 anyway - webhook should not fail
    return NextResponse.json({
      ok: false,
      error: msg,
      message: "Webhook processed but with errors",
    });
  }
}
```

#### 4.3 Webhook Handler: Order Updated

**File:** `src/app/api/webhooks/order-updated/route.ts`

```typescript
/**
 * POST /api/webhooks/order-updated
 *
 * General order update webhook - handles status changes
 */
export async function POST(request: NextRequest) {
  console.log("[webhook] order-updated received");

  let body: any;
  try {
    body = await request.json();
  } catch {
    return NextResponse.json({ error: "Invalid JSON" }, { status: 400 });
  }

  const { data } = body;
  const {
    shop,
    id,
    order_no,
    financial_status,
    fulfillment_status,
    status,
    updated_at,
  } = data;

  if (!shop || !id) {
    return NextResponse.json({ error: "Missing fields" }, { status: 400 });
  }

  try {
    const { getStoreByShop } = await import("@/lib/shoplazza/store");
    const { prisma } = await import("@/lib/db");

    const store = await getStoreByShop(shop);
    if (!store) {
      return NextResponse.json({ ok: true });
    }

    // Map Shoplazza status to our status
    let ourStatus = "pending";
    if (fulfillment_status === "fulfilled") {
      ourStatus = "dispatched";
    } else if (financial_status === "paid" && !fulfillment_status) {
      ourStatus = "active";
    } else if (status === "cancelled") {
      ourStatus = "cancelled";
    }

    // Update database
    await prisma.protectionOrder.updateMany({
      where: {
        storeId: store.id,
        shoplazzaOrderId: id,
      },
      data: {
        status: ourStatus,
        shoplazzaStatus: status,
        fulfillmentStatus: fulfillment_status,
      },
    });

    console.log(
      `[webhook] Updated order ${order_no}: status=${ourStatus}, fulfillment=${fulfillment_status}`
    );

    return NextResponse.json({ ok: true });
  } catch (error) {
    const msg = error instanceof Error ? error.message : String(error);
    console.error("[webhook] Error:", msg);
    return NextResponse.json({ ok: false, error: msg });
  }
}
```

---

## 5. Order Delivered Notifications

### Requirement
Receive notifications when orders are delivered.

### Implementation Strategy

#### 5.1 Delivery Tracking Methods

**Option A: Shoplazza Order Status**
```
Orders progress through status:
1. fulfilled (shipped)
2. completed (delivered) ← We can listen for this
```

**Option B: Third-Party Integration**
- ShipStation, AfterShip, Track123 integration
- These services sync delivery status back to Shoplazza
- Shoplazza updates order status to "completed"

#### 5.2 Webhook: Order Completed

**File:** `src/app/api/webhooks/order-completed/route.ts`

```typescript
/**
 * POST /api/webhooks/order-completed
 *
 * Webhook from Shoplazza when order is delivered/completed.
 */
export async function POST(request: NextRequest) {
  console.log("[webhook] order-completed received");

  let body: any;
  try {
    body = await request.json();
  } catch {
    return NextResponse.json({ error: "Invalid JSON" }, { status: 400 });
  }

  const { data } = body;
  const { shop, id, order_no, completed_at } = data;

  if (!shop || !id) {
    return NextResponse.json({ error: "Missing fields" }, { status: 400 });
  }

  try {
    const { getStoreByShop } = await import("@/lib/shoplazza/store");
    const { prisma } = await import("@/lib/db");

    const store = await getStoreByShop(shop);
    if (!store) return NextResponse.json({ ok: true });

    // Update order as delivered
    const updated = await prisma.protectionOrder.updateMany({
      where: {
        storeId: store.id,
        shoplazzaOrderId: id,
      },
      data: {
        status: "delivered",
        fulfillmentStatus: "completed",
      },
    });

    console.log(
      `[webhook] Order ${order_no} marked as delivered`,
      completed_at
    );

    // TODO: Trigger delivery workflow
    // - Update claim eligibility (delivery confirmation)
    // - Notify customer
    // - Sync with insurance backend

    return NextResponse.json({ ok: true });
  } catch (error) {
    const msg = error instanceof Error ? error.message : String(error);
    console.error("[webhook] Error:", msg);
    return NextResponse.json({ ok: false, error: msg });
  }
}
```

#### 5.3 Webhook Registration During Install

**File:** `src/app/api/auth/callback/route.ts`

```typescript
// After creating store, setup all webhooks

const { setupWebhooks } = await import("@/lib/shoplazza/webhooks");
await setupWebhooks(shop, accessToken, appUrl);

// Webhooks to register:
// - orders/fulfilled (dispatched)
// - orders/updated (status changes)
// - orders/completed (delivered) - if available
```

#### 5.4 Status Query Endpoint

**File:** `src/app/api/admin/order-status/route.ts`

```typescript
/**
 * GET /api/admin/order-status?shop=...&orderId=...
 *
 * Manual query to get current order status from Shoplazza
 */
export async function GET(request: NextRequest) {
  const shop = request.nextUrl.searchParams.get("shop") ?? "";
  const orderId = request.nextUrl.searchParams.get("orderId") ?? "";

  if (!shop || !orderId) {
    return NextResponse.json(
      { error: "Missing shop or orderId" },
      { status: 400 }
    );
  }

  try {
    const { getStoreByShop } = await import("@/lib/shoplazza/store");
    const store = await getStoreByShop(shop);

    if (!store?.accessToken) {
      return NextResponse.json({ error: "Store not found" }, { status: 404 });
    }

    // Fetch from Shoplazza
    const host = shop.includes(".") ? shop : `${shop}.myshoplaza.com`;
    const url = `https://${host}/openapi/2024-07/orders/${orderId}`;

    const response = await fetch(url, {
      method: "GET",
      headers: {
        "Access-Token": store.accessToken,
        "Content-Type": "application/json",
      },
    });

    if (!response.ok) {
      return NextResponse.json(
        { error: "Failed to fetch order" },
        { status: response.status }
      );
    }

    const data = await response.json();
    const order = data.data || data;

    return NextResponse.json({
      ok: true,
      orderId: order.id,
      orderNo: order.order_no,
      status: order.status,
      fulfillment_status: order.fulfillment_status,
      financial_status: order.financial_status,
      created_at: order.created_at,
      updated_at: order.updated_at,
    });
  } catch (error) {
    const msg = error instanceof Error ? error.message : String(error);
    console.error("[order-status] Error:", msg);
    return NextResponse.json({ error: msg }, { status: 500 });
  }
}
```

---

## Summary: Implementation Timeline

### Phase 1: Location Validation (Week 1)
- ✅ Store location check during install
- ✅ Admin UI display location status
- ✅ Activation lock for unsupported countries
- ✅ Database schema update
- ✅ Prisma migration

### Phase 2: Address Collection (Week 2)
- ✅ Listen for payment completion in widget
- ✅ Collect shipping address from CheckoutAPI
- ✅ Submit to backend via new API endpoint
- ✅ Fetch billing address from Shoplazza Order API
- ✅ Store in ProtectionOrder database table

### Phase 3: Webhook Infrastructure (Week 3)
- ✅ Implement webhook registration during install
- ✅ Create order-fulfilled handler
- ✅ Create order-completed handler
- ✅ Add error handling and logging

### Phase 4: Shipping Countries Config (Week 3-4)
- ✅ Admin UI for country selection
- ✅ Database schema for supported countries
- ✅ Widget validation against shipping country
- ✅ Disable widget if country unsupported

### Phase 5: Testing & Refinement (Week 5)
- ✅ Test with sandbox stores in all 4 countries
- ✅ Verify webhook delivery
- ✅ Test address collection
- ✅ Production deployment

---

## Key API Endpoints Summary

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/auth/callback` | POST | Install hook - validate location, setup webhooks |
| `/api/settings` | GET/PATCH | Admin UI - location status, country selection |
| `/api/public-settings/:domain` | GET | Widget - fetch config (shipping countries) |
| `/api/checkout/order-addresses` | POST | Widget → Backend - submit addresses after payment |
| `/api/webhooks/order-fulfilled` | POST | Shoplazza → Backend - order dispatched |
| `/api/webhooks/order-updated` | POST | Shoplazza → Backend - status changed |
| `/api/webhooks/order-completed` | POST | Shoplazza → Backend - order delivered |
| `/api/admin/order-status` | GET | Admin - query current order status |

---

## Database Schema Summary

```typescript
// Store location (immutable)
Store.country_code    // "GB", "FR", "CH", "NL"
Store.country_name    // "United Kingdom", etc.

// Settings configuration
StoreSettings.location_valid           // true/false
StoreSettings.supported_shipping_countries // JSON: ["GB", "FR", ...]
StoreSettings.allow_all_in_region      // boolean

// Order tracking
ProtectionOrder.shoplazzaOrderId       // Order ID from Shoplazza
ProtectionOrder.shippingAddress        // JSON: full address object
ProtectionOrder.billingAddress         // JSON: full address object
ProtectionOrder.shippingCountry        // ISO code
ProtectionOrder.billingCountry         // ISO code
ProtectionOrder.status                 // pending|active|dispatched|delivered
ProtectionOrder.fulfillmentStatus      // Shoplazza fulfillment status
```

---

## Next Steps

1. **Review this plan** with your team
2. **Prioritize phases** based on your timeline
3. **Start with Phase 1** (location validation) - simplest and blocks activation
4. **Move to Phase 2** (address collection) - core for insurance backend
5. **Implement Phase 3** (webhooks) - essential for tracking

All endpoints follow your existing patterns and maintain consistency with the current codebase.
