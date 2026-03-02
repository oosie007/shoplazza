# Webhooks Analysis: Which Features Actually Need Them?

## Executive Summary

**Short Answer:** Only features **4 (Order Dispatch)** and **5 (Order Delivered)** actually need webhooks.

Features **1, 2, 3** don't need webhooks at all - they use synchronous request/response patterns.

---

## Feature-by-Feature Analysis

### ❌ Feature 1: Store Location Validation
**Do we need webhooks?** NO

**Why:**
- Location is checked **once during install**
- It's immutable - never changes after installation
- Synchronous check: `GET store info → validate → reject or proceed`

**Implementation:**
```typescript
// During OAuth callback (synchronous)
const storeInfo = await getStoreInfo(shop, accessToken);
const SUPPORTED = ["GB", "FR", "CH", "NL"];

if (!SUPPORTED.includes(storeInfo.country_code)) {
  return reject(); // Block install
}
```

**No webhook needed.** ✅

---

### ❌ Feature 2: Supported Shipping Countries Configuration
**Do we need webhooks?** NO

**Why:**
- Merchant configures this **in Admin UI**
- It's a one-time setting update via PATCH request
- Synchronous: `Admin UI → PATCH /api/settings → save to DB`
- Widget fetches it when loading via `GET /api/public-settings`

**Implementation:**
```typescript
// Admin UI: User changes country selection
updateSetting({ supported_shipping_countries: ["GB", "FR", ...] })

// Widget: Fetch config
GET /api/public-settings/:shop
  ← { supported_shipping_countries: [...] }
```

**No webhook needed.** ✅

---

### ❌ Feature 3: Address Collection After Payment
**Do we need webhooks?** NO

**Why:**
- Widget has full access to address data **at checkout**
- Widget already has all necessary data at payment time
- Widget can POST data immediately after payment success
- No need to wait for external notification

**Implementation:**
```typescript
// Widget at checkout (after payment)
CheckoutAPI.store.onOrderStatusChange((status) => {
  if (status === "finished" || status === "placed") {
    // Widget already has:
    // - Order ID
    // - Shipping address (from CheckoutAPI)

    // POST immediately to backend
    fetch("/api/checkout/order-addresses", {
      method: "POST",
      body: JSON.stringify({
        order_id: orderInfo.id,
        shipping_address: CheckoutAPI.address.getShippingAddress(),
      })
    });

    // Backend fetches billing address from Shoplazza API
    const fullOrder = await fetch(
      `https://${shop}/openapi/2024-07/orders/${orderId}`,
      { headers: { "Access-Token": accessToken } }
    );
    // Billing address available in response
  }
});
```

**No webhook needed.** ✅

**Alternative if you wanted webhook:**
You *could* wait for Shoplazza's `orders/created` webhook instead of having the widget POST, but:
- ⚠️ Adds latency (widget POST is instant)
- ⚠️ More complex (two systems communicating)
- ⚠️ Less reliable (depends on webhook delivery)

**Recommendation:** Use widget POST (synchronous), not webhook.

---

### ✅ Feature 4: Order Dispatch Notification
**Do we need webhooks?** YES ✅

**Why:**
- Event happens **outside your app** (merchant marks order as shipped)
- You have **no way to detect** this without webhooks
- You need **async notification** when it happens
- Merchant controls timing - could be seconds, hours, or days after purchase

**Implementation:**
```typescript
// Shoplazza detects order shipped
// → Sends webhook to your app

// Your app receives:
POST /api/webhooks/order-fulfilled
{
  data: {
    shop: "store.myshoplaza.com",
    id: "order_123",
    fulfillment_status: "fulfilled",
    updated_at: "2026-03-02T..."
  }
}

// Your app updates DB
ProtectionOrder.update({
  id: order_123,
  status: "dispatched"
})
```

**Webhook is necessary.** ✅ No alternative.

---

### ✅ Feature 5: Order Delivered Notification
**Do we need webhooks?** YES ✅

**Why:**
- Event happens **outside your app** (package is delivered)
- Shoplazza learns about delivery from **third-party shipping** (ShipStation, AfterShip, Track123)
- You have **no way to detect** this without webhooks
- Timing is unpredictable - could be days/weeks after dispatch

**Implementation:**
```typescript
// Customer receives package
// Shipping service marks delivered
// → Service syncs to Shoplazza
// → Shoplazza sends webhook

POST /api/webhooks/order-completed
{
  data: {
    shop: "store.myshoplaza.com",
    id: "order_123",
    status: "completed",
    fulfillment_status: "fulfilled",
    completed_at: "2026-03-10T..."
  }
}

// Your app unlocks claim window
ProtectionOrder.update({
  id: order_123,
  status: "delivered"
})
```

**Webhook is necessary.** ✅ No alternative.

---

## Summary Table

| Feature | Need Webhooks? | Why | Implementation |
|---------|---|---|---|
| 1️⃣ Location Validation | ❌ NO | One-time check at install | Synchronous: OAuth callback |
| 2️⃣ Shipping Countries | ❌ NO | Merchant config via UI | Synchronous: PATCH + fetch |
| 3️⃣ Address Collection | ❌ NO | Widget has access at checkout | Synchronous: Widget POST after payment |
| 4️⃣ Order Dispatch | ✅ YES | Event from external system | Async: Webhook `orders/fulfilled` |
| 5️⃣ Order Delivered | ✅ YES | Event from external system | Async: Webhook `orders/completed` |

---

## What Actually Needs Webhooks

### Only 2 Webhook Events Required:

**1. `orders/fulfilled`** (Order Shipped)
```typescript
POST /api/webhooks/order-fulfilled
// Triggered when: Merchant marks order as shipped
// Data: order_id, fulfillment_status, timestamps
// Action: Update ProtectionOrder.status = "dispatched"
```

**2. `orders/completed`** (Order Delivered)
```typescript
POST /api/webhooks/order-completed
// Triggered when: Third-party shipping marks delivered
// Data: order_id, status, timestamps
// Action: Update ProtectionOrder.status = "delivered"
```

---

## Webhook Registration Strategy

### During Install:
```typescript
// Register ONLY these 2 webhooks
const webhooks = [
  {
    topic: "orders/fulfilled",
    address: `${appUrl}/api/webhooks/order-fulfilled`,
    description: "Order dispatched - trigger fulfillment workflow"
  },
  {
    topic: "orders/completed",
    address: `${appUrl}/api/webhooks/order-completed`,
    description: "Order delivered - unlock claims"
  }
];

for (const webhook of webhooks) {
  await registerWebhook(shop, accessToken, webhook);
}
```

### Error Handling:
```typescript
// Webhook registration fails? → Log but don't block install
// Webhook delivery fails? → Shoplazza retries automatically
// Your app handles: → Idempotent updates (same webhook twice = safe)
```

---

## Alternative: Skip Webhooks Entirely? 🤔

**Could you skip webhooks for dispatch/delivery?**

**Option A: Polling (Not Recommended)**
```typescript
// Every 5 minutes, query all orders
GET /api/admin/order-status?shop=...&orderId=...

// Check if status changed from "active" → "dispatched" or "dispatched" → "delivered"
// Pros: Simpler, no webhook setup
// Cons: Latency (up to 5 min delay), API calls, not scalable
```

**Option B: Manual Admin Button (Not Recommended)**
```typescript
// Admin clicks "Mark Dispatched" button in dashboard
// Pros: Immediate, no webhook setup
// Cons: Merchant has to do manual work, error-prone, not scalable
```

**Option C: Webhooks (Recommended) ✅**
```typescript
// Shoplazza automatically notifies you
// Pros: Real-time, automatic, scalable, no manual work
// Cons: Need to handle webhook delivery (Shoplazza handles retries)
```

**Recommendation:** Use webhooks for dispatch/delivery. It's the right pattern.

---

## Implementation Checklist

### For Features 1, 2, 3 (No Webhooks Needed):
- [ ] Location validation during OAuth callback
- [ ] Admin UI for country configuration
- [ ] Widget fetches config at load
- [ ] Widget POSTs addresses after payment
- [ ] Backend receives and stores addresses

### For Features 4, 5 (Webhooks Required):
- [ ] Register `orders/fulfilled` webhook during install
- [ ] Register `orders/completed` webhook during install
- [ ] Create `/api/webhooks/order-fulfilled` handler
- [ ] Create `/api/webhooks/order-completed` handler
- [ ] Handle webhook retries (idempotent)
- [ ] Log webhook events for debugging
- [ ] Test webhook delivery with sandbox store

---

## Code Organization

```
/src/app/api
  /auth
    /callback        ← Feature 1 (location check)
  /settings
    /route.ts        ← Feature 2 (shipping countries)
  /checkout
    /order-addresses ← Feature 3 (address collection)
  /webhooks          ← Features 4 & 5 (NEW)
    /order-fulfilled
    /order-completed
```

---

## Real-World Flow (Without Webhooks Myths)

**Installation:**
```
1. Merchant: "Install app"
2. OAuth Callback: Check location (GB|FR|CH|NL) ✅ Sync
3. Register webhooks for dispatch/delivery ✅ Sync
4. Save store ✅ Sync
→ Install complete
```

**Admin Configuration:**
```
1. Merchant: "Configure supported countries"
2. Admin UI: POST to /api/settings ✅ Sync
3. Save to DB ✅ Sync
→ Settings updated immediately
```

**Checkout:**
```
1. Customer: Adds Item Protection to cart
2. Widget: Monitors CheckoutAPI ✅ Real-time
3. Customer: Completes payment
4. Widget: POST /api/checkout/order-addresses ✅ Sync
5. Backend: Fetch billing address, save order ✅ Sync
→ Addresses stored immediately
```

**Order Lifecycle:**
```
1. Merchant: Ships order (marks as shipped)
2. Shoplazza: Detects status change
3. Webhook: POST /api/webhooks/order-fulfilled ✅ Async (delayed)
4. Your app: Update status = "dispatched" ✅ Async
→ Status updated (seconds to minutes delay)

5. Customer: Receives package
6. Shipping service: Marks delivered
7. Shoplazza: Syncs from third-party
8. Webhook: POST /api/webhooks/order-completed ✅ Async (delayed)
9. Your app: Update status = "delivered" ✅ Async
→ Status updated (seconds to minutes delay)
```

---

## Key Insight

**Webhooks are NOT for:**
- ❌ Location validation (one-time, at install)
- ❌ Configuration (merchant controls timing)
- ❌ Address collection (widget has the data)

**Webhooks ARE for:**
- ✅ External events (order shipped, delivery confirmed)
- ✅ Async notifications (happens outside your app)
- ✅ State changes (order status updates)

**The rule:** If your app can detect/control the action → use synchronous API. If something external triggers it → use webhooks.

---

## Bottom Line

**Only implement webhooks for:**
1. `orders/fulfilled` - Order dispatched
2. `orders/completed` - Order delivered

**For everything else, use synchronous request/response patterns.**

This keeps your implementation simpler, faster, and more reliable.
