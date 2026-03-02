# CD Insure: Location & Order Management Features

## Quick Reference Guide

### 1️⃣ Store Location Validation

**What:** Only allow app in UK, France, Switzerland, Netherlands
**Where:** Checked during OAuth install callback
**Admin UI:** Shows store country with ✅ or ❌ status
**Activation:** Blocked for unsupported countries

```
Install → Check Country → ✅ Supported or ❌ Block → Save to DB
```

---

### 2️⃣ Supported Shipping Countries

**What:** Merchant configures which countries can buy Item Protection
**Where:** Admin panel → "Shipping Countries" section
**Options:**
- Auto-detect region (recommended)
- Manual country selection

**Widget Behavior:**
- Checks shipping address against configuration
- Disables widget if country not supported
- Shows message explaining restriction

```
Checkout Page
  ↓
Get Shipping Address from CheckoutAPI
  ↓
Check Against Store's Supported Countries
  ↓
Enable or Disable Widget
```

---

### 3️⃣ Address Collection After Payment

**When:** After customer completes payment
**What Collected:**
- ✅ Shipping address (from CheckoutAPI)
- ✅ Billing address (from Shoplazza Order API)
- ✅ Order ID, amount, customer email

**Flow:**
```
Order Placed (Status = "finished")
  ↓
Widget Detects Payment Success
  ↓
Sends Data to /api/checkout/order-addresses
  ↓
Backend Fetches Full Order from Shoplazza
  ↓
Saves to ProtectionOrder Table
  ↓
Ready for Insurance Processing
```

**Database Storage:**
- All address fields preserved as JSON
- Country codes extracted for filtering
- Timestamps for audit trail

---

### 4️⃣ Order Dispatch Notification

**Trigger:** Shoplazza webhook when order is shipped
**Webhook:** `orders/fulfilled`
**What Happens:**
1. Shoplazza sends webhook with order ID
2. Backend updates order status → "dispatched"
3. Backend can trigger fulfillment workflow
4. Insurance backend notified (future)

```
Order Shipped on Shoplazza
  ↓
Webhook → /api/webhooks/order-fulfilled
  ↓
Update ProtectionOrder.status = "dispatched"
  ↓
Trigger Fulfillment Workflow
```

**Database Update:**
```typescript
status: "dispatched"
fulfillmentStatus: "fulfilled"  // from Shoplazza
```

---

### 5️⃣ Order Delivered Notification

**Trigger:** Shoplazza webhook when order marked as completed
**Webhook:** `orders/completed` or `orders/updated` with `status=completed`
**What Happens:**
1. Shoplazza sends delivery confirmation
2. Backend updates order status → "delivered"
3. Backend can trigger delivery workflow
4. Insurance claim window opens (future)

```
Package Delivered
  ↓
Third-party Shipping Sync (ShipStation, AfterShip, Track123)
  ↓
Shoplazza Updates Order Status
  ↓
Webhook → /api/webhooks/order-completed
  ↓
Update ProtectionOrder.status = "delivered"
  ↓
Trigger Delivery Workflow
```

**Database Update:**
```typescript
status: "delivered"
fulfillmentStatus: "completed"
```

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│              INSTALLATION FLOW                          │
├─────────────────────────────────────────────────────────┤
│  OAuth Callback                                         │
│    ↓                                                    │
│  1️⃣ Check Store Location (GB|FR|CH|NL)                 │
│    ↓                                                    │
│  Register Webhooks (orders/fulfilled, orders/completed) │
│    ↓                                                    │
│  Save Store + StoreSettings                            │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│              ADMIN CONFIGURATION                        │
├─────────────────────────────────────────────────────────┤
│  Settings Page                                          │
│    ↓                                                    │
│  1️⃣ View Store Location (immutable)                     │
│    ↓                                                    │
│  2️⃣ Configure Supported Shipping Countries              │
│    ↓                                                    │
│  Activate/Deactivate Feature                           │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│              CHECKOUT FLOW                              │
├─────────────────────────────────────────────────────────┤
│  Customer on Checkout Page                             │
│    ↓                                                    │
│  2️⃣ Widget Checks Shipping Address Country              │
│    ↓                                                    │
│  Widget Enabled or Disabled                            │
│    ↓                                                    │
│  Customer Selects Item Protection & Pays              │
│    ↓                                                    │
│  3️⃣ Payment Success - Collect Addresses                 │
│    ↓                                                    │
│  Send to /api/checkout/order-addresses                 │
│    ↓                                                    │
│  Save to ProtectionOrder Table                         │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│              ORDER TRACKING WEBHOOKS                    │
├─────────────────────────────────────────────────────────┤
│  Shoplazza Webhook Events                              │
│    ↓                                                    │
│  4️⃣ orders/fulfilled                                    │
│     /api/webhooks/order-fulfilled                      │
│     status = "dispatched"                              │
│    ↓                                                    │
│  5️⃣ orders/completed                                    │
│     /api/webhooks/order-completed                      │
│     status = "delivered"                               │
│    ↓                                                    │
│  Update ProtectionOrder + Trigger Workflows            │
└─────────────────────────────────────────────────────────┘
```

---

## Data Model

```typescript
Store {
  // ... existing fields
  country_code: "GB" | "FR" | "CH" | "NL"
  country_name: string
}

StoreSettings {
  // ... existing fields
  location_valid: boolean
  supported_shipping_countries: string[] // JSON: ["GB", "FR", ...]
  allow_all_in_region: boolean
}

ProtectionOrder (NEW TABLE) {
  id: string
  storeId: string

  // Order Reference
  shoplazzaOrderId: string
  shoplazzaOrderNo: string

  // Customer Contact
  customerEmail: string
  customerPhone?: string

  // Addresses
  shippingAddress: Address // JSON
  billingAddress?: Address // JSON
  shippingCountry: string   // ISO code
  billingCountry?: string   // ISO code

  // Protection
  protectionEnabled: boolean
  protectionAmount: decimal

  // Totals
  orderTotal: decimal

  // Status Tracking
  status: "pending" | "active" | "dispatched" | "delivered" | "cancelled"
  shoplazzaStatus: string
  fulfillmentStatus: string

  // Timestamps
  createdAt: datetime
  updatedAt: datetime
}

Address {
  id: string
  email: string
  phone: string
  phone_area_code: string
  first_name: string
  last_name: string
  company?: string
  address: string
  address1?: string
  city: string
  province: string
  province_code: string
  country: string
  country_code: string // ISO 3166-1 alpha-2
  zip: string
  is_default: boolean
}
```

---

## API Endpoints

### Installation
```
POST /api/auth/callback?code=...&shop=...
  → Validate location
  → Register webhooks
  → Save store
```

### Admin Configuration
```
GET  /api/settings?shop=...
  ← Returns location_valid, supported_countries

PATCH /api/settings?shop=...
  → Update supported_shipping_countries
```

### Public Widget Settings
```
GET /api/public-settings/:domain
  ← Returns supported_shipping_countries, activated status
```

### Address Collection (Post-Payment)
```
POST /api/checkout/order-addresses
{
  shop: "store.myshoplaza.com",
  order_id: "123",
  order_no: "#001",
  shipping_address: { ... },
  created_at: "2026-03-02T..."
}
  → Save ProtectionOrder
  → Return orderId
```

### Webhooks (Shoplazza → Backend)
```
POST /api/webhooks/order-fulfilled
  ← { data: { shop, id, order_no, fulfillment_status, ... } }
  → Update status = "dispatched"

POST /api/webhooks/order-completed
  ← { data: { shop, id, order_no, completed_at, ... } }
  → Update status = "delivered"
```

### Admin Query
```
GET /api/admin/order-status?shop=...&orderId=...
  → Fetch current order status from Shoplazza
  ← { status, fulfillment_status, financial_status, ... }
```

---

## Implementation Phases

### Phase 1: Location Validation (Simple)
- Store location check during install
- Admin UI display status
- Activation lock for unsupported countries
- **Effort:** 2-3 days
- **Blocks:** Can't activate elsewhere

### Phase 2: Address Collection (Medium)
- Listen for payment in widget
- POST addresses to backend
- Fetch billing address from Shoplazza API
- Store in ProtectionOrder table
- **Effort:** 3-4 days
- **Enables:** Insurance backend integration

### Phase 3: Webhooks (Medium)
- Register during install
- Create handlers for fulfilled/completed
- Database updates
- Error handling + logging
- **Effort:** 3-4 days
- **Enables:** Order tracking

### Phase 4: Shipping Countries (Simple)
- Admin UI toggle/selection
- Widget validation check
- Database schema
- **Effort:** 2-3 days
- **Enables:** Country-specific restrictions

### Phase 5: Testing & Refinement
- Sandbox testing in all 4 countries
- Webhook delivery verification
- Address field validation
- Production readiness
- **Effort:** 3-4 days

---

## Key Considerations

### Security
- ✅ Location check on backend (trust Shoplazza API)
- ✅ Validate shop domain in webhook handlers
- ✅ Store addresses encrypted at rest (consider)
- ✅ Rate limit webhook processing

### Data Privacy
- ⚠️ Collect only necessary address fields
- ⚠️ Comply with GDPR/local regulations
- ⚠️ Implement data retention policies
- ⚠️ Log address access for audit trail

### Reliability
- ✅ Idempotent webhook handlers (same webhook twice = safe)
- ✅ Retry logic for failed webhook deliveries
- ✅ Fallback: Manual query via /api/admin/order-status
- ✅ Error logging for debugging

### Performance
- ✅ Cache store location (immutable after install)
- ✅ Cache shipping countries in StoreSettings
- ✅ Async webhook processing
- ✅ Use in-memory cache for frequently accessed data

---

## Testing Checklist

- [ ] Install app in UK store → ✅ succeeds
- [ ] Install app in unsupported country → ❌ blocked
- [ ] Admin shows correct location + status
- [ ] Can toggle supported countries
- [ ] Widget disables for unsupported shipping country
- [ ] Addresses collected after payment
- [ ] Billing address fetched from Shoplazza API
- [ ] Webhook received for order shipped
- [ ] Order status updated to "dispatched"
- [ ] Webhook received for order delivered
- [ ] Order status updated to "delivered"
- [ ] Test webhook retry on error
- [ ] Test idempotency (duplicate webhooks)

---

## Future Extensions

Once these features are live:

1. **Claim Portal Integration**
   - Use delivered status to unlock claim eligibility
   - Show claim history per order

2. **Insurance Backend Sync**
   - Push ProtectionOrder data to insurance API
   - Sync claims back to Shoplazza admin

3. **Customer Notifications**
   - Email on order dispatch
   - Email on delivery (claim window open)
   - SMS status updates

4. **Admin Dashboard**
   - Order tracking status
   - Claim statistics by country
   - Payment reconciliation

5. **Compliance Reporting**
   - Generate reports by country/region
   - Tax compliance (VAT, GST)
   - Regulatory reporting

---

## Questions & Support

For detailed implementation questions, refer to:
- `docs/IMPLEMENTATION_PLAN_LOCATION_ORDERS.md` - Full technical specs
- `docs/CHECKOUT_EXTENSION_SETUP.md` - Widget integration
- Shoplazza API docs: https://www.shoplazza.dev/docs/
- Webhook reference: https://www.shoplazza.dev/reference/webhooks
