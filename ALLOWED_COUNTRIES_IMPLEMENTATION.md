# Allowed Countries Implementation - Complete Fix

## Problem Statement

The checkout widget was unable to validate customer shipping addresses against allowed countries because the backend API endpoint (`/api/public-settings`) was not returning an `allowedCountries` field in the settings response.

When a customer selected a shipping address in an unsupported country (e.g., Greece), the widget remained disabled even when selecting a supported country (e.g., United Kingdom) because:
1. The widget function `isShippingCountrySupported()` checked `settings.allowedCountries`
2. The backend API was not providing this field
3. With no allowed countries in the response, validation always failed

## Solution Implemented

### 1. Database Schema Update (`prisma/schema.prisma`)

Added a new field to the `StoreSettings` model:

```prisma
// Allowed countries for shipping (JSON array: ["GB", "FR", "CH", "NL"])
// This is sent to the checkout widget to validate customer shipping addresses
allowedCountries String @default("[\"GB\",\"FR\",\"CH\",\"NL\"]") // JSON string: string[]
```

**Default Value**: `["GB", "FR", "CH", "NL"]` (matching supported countries)

**Benefits**:
- Centralized configuration in database (not hardcoded in code)
- Can be customized per store in the future
- SQLite-compatible JSON storage as string
- Auto-initialized for new stores

### 2. Prisma Client Generation

Ran `prisma db push --skip-generate` to sync database schema:
- **SQLite** (dev): Updated `dev.db` with new column
- **PostgreSQL** (prod): Will be updated automatically on next deployment

Generated Prisma client types:
```bash
prisma generate
```

All TypeScript types now include `allowedCountries` field with proper typing.

### 3. Backend API Update (`src/app/api/public-settings/route.ts`)

Modified the `/api/public-settings` GET endpoint to include and return `allowedCountries`:

```typescript
// Parse allowedCountries from JSON string
const allowedCountries =
  typeof settings.allowedCountries === "string" && settings.allowedCountries.length
    ? JSON.parse(settings.allowedCountries)
    : ["GB", "FR", "CH", "NL"];

// Include in response
return withCors(NextResponse.json({
  // ... existing fields ...
  allowedCountries,  // NEW FIELD
  // ... more fields ...
}));
```

**Response Example**:
```json
{
  "activated": true,
  "pricingMode": "per_category",
  "fixedPercentAll": 20,
  "categoryPercents": { "607129218744116400": 50 },
  "excludedCategoryIds": [],
  "allowedCountries": ["GB", "FR", "CH", "NL"],
  "widgetVariant": "B",
  "enablePoweredByChubb": true,
  "offerAtCheckout": true,
  "defaultAtCheckout": false,
  "itemProtectionProductId": "...",
  "itemProtectionVariantId": "...",
  "widgetInjectionPoint": "checkout"
}
```

### 4. Widget Frontend (Already Implemented)

The checkout widget (`public/checkout-widget.js`) already has the correct implementation:

```javascript
function isShippingCountrySupported() {
  // Get allowed countries from settings retrieved from backend API
  var allowedCountries = (settings && settings.allowedCountries) ? settings.allowedCountries : [];

  var address = hasCheckoutAPI && CheckoutAPI.address && CheckoutAPI.address.getShippingAddress
    ? CheckoutAPI.address.getShippingAddress()
    : null;
  if (!address) return true;
  var countryCode = address.countryCode || address.country_code || "";
  var isSupported = allowedCountries.indexOf(countryCode) >= 0;
  debugLog("Shipping country check: " + countryCode + " (allowed: " + allowedCountries.join(",") + ") = " + isSupported);
  return isSupported;
}
```

**Features**:
- Retrieves `allowedCountries` from backend-provided settings
- Validates customer's shipping address country against this list
- Falls back to `[]` if field missing (safe fallback)
- Uses ES5 syntax (`indexOf()` instead of `includes()`) for compatibility
- Logs validation result for debugging

### 5. Address Change Listener (Already Implemented)

The widget re-renders when address changes:

```javascript
if (hasCheckoutAPI && CheckoutAPI.address && typeof CheckoutAPI.address.onAddressChange === "function") {
  CheckoutAPI.address.onAddressChange(function () {
    debugLog("Address changed - re-rendering widget to update country validation");
    renderWidget(root);
  });
}
```

This ensures:
- Widget re-validates when customer changes shipping country
- Automatically enables/disables based on new country
- Real-time feedback to customer

## Files Modified

| File | Changes | Type |
|------|---------|------|
| `prisma/schema.prisma` | Added `allowedCountries` field to StoreSettings model | Schema |
| `src/app/api/public-settings/route.ts` | Parse and return `allowedCountries` in API response | Code |
| `src/generated/prisma/models/StoreSettings.ts` | Auto-generated Prisma types (committed) | Generated |

## Database Changes

### SQLite (Development)

**Status**: ✅ Applied via `prisma db push`
- New column `allowedCountries` added to `storeSettings` table
- Default value: `'["GB","FR","CH","NL"]'`
- Verified: `dev.db` updated successfully

### PostgreSQL (Production)

**Status**: ⏳ Will apply on next deployment
- When production is redeployed, `db push` runs automatically in build script
- New column will be created with same default
- Existing stores will have default allowed countries set
- No data loss; backward compatible

## How It Works - Flow Diagram

```
Customer at Checkout
         ↓
   [Selects Address]
         ↓
   Widget loads via checkout extension
         ↓
   fetchSettings() → GET /api/public-settings?shop=...
         ↓
   Response includes "allowedCountries": ["GB", "FR", "CH", "NL"]
         ↓
   onAddressChange listener fires
         ↓
   isShippingCountrySupported() checks:
   - Gets customer's country code from address
   - Looks up in settings.allowedCountries
   - Returns true/false
         ↓
   Widget disabled/enabled in real-time
         ↓
   "Item protection is not available for this order."
   (if country not supported)
```

## Testing Guide

### Local Testing

1. **Start dev server** with SQLite:
   ```bash
   npm run dev
   ```

2. **Verify database**:
   ```bash
   DATABASE_URL="file:./dev.db" npx prisma studio
   ```
   - Check `StoreSettings` table
   - Verify `allowedCountries` column exists
   - Check default values

3. **Test API endpoint**:
   ```bash
   curl "http://localhost:3000/api/public-settings?shop=oostest.myshoplaza.com"
   ```
   - Should include `"allowedCountries": ["GB", "FR", "CH", "NL"]`

4. **Test widget behavior**:
   - Visit checkout page
   - Select unsupported country (e.g., Greece) → widget disables
   - Change to supported country (e.g., UK) → widget re-enables
   - Check console logs for validation messages

### Production Testing

After deployment to production:

1. **Verify schema applied**:
   ```bash
   # In production Postgres database
   \d "StoreSettings"
   # Should show allowedCountries column
   ```

2. **Test API**:
   ```bash
   curl "https://production-domain/api/public-settings?shop=oostest.myshoplaza.com"
   # Should include allowedCountries
   ```

3. **Test checkout** on production store

## Configuration & Customization

### Setting Allowed Countries Per Store

Currently, all stores get the default: `["GB", "FR", "CH", "NL"]`

To customize per store in the future, add an admin endpoint:

```typescript
// Example (not yet implemented)
PATCH /api/settings/allowed-countries
Body: { allowedCountries: ["GB", "FR"] }
```

Then update the field:
```typescript
await prisma.storeSettings.update({
  where: { storeId },
  data: { allowedCountries: JSON.stringify(["GB", "FR"]) }
});
```

### Changing Global Default

Edit `prisma/schema.prisma`:
```prisma
allowedCountries String @default("[\"GB\",\"FR\",\"CH\",\"NL\"]")
```

Then run:
```bash
prisma db push
```

## Validation & Security

### Input Validation

- **Backend**: Settings come from Prisma model, not user input
- **Widget**: Uses array `indexOf()` - safe against injection
- **Frontend**: Shipping country from CheckoutAPI - trusted source

### Backward Compatibility

- **Existing stores**: Get default countries automatically
- **Missing field**: Widget handles gracefully with `[]` fallback
- **Old API responses**: Widget works if field missing (disables protection)

### No Breaking Changes

- All endpoints remain unchanged
- New field is additive (doesn't break existing integrations)
- Default behavior matches current supported countries

## Debugging

### Console Logs

Widget logs country validation:
```
[CD Insure] Shipping country check: GB (allowed: GB,FR,CH,NL) = true
[CD Insure] Shipping country check: GR (allowed: GB,FR,CH,NL) = false
```

### API Response Check

```javascript
fetch('/api/public-settings?shop=...')
  .then(r => r.json())
  .then(settings => {
    console.log('Allowed countries:', settings.allowedCountries);
  });
```

### Database Query

```sql
-- Check stored value
SELECT id, storeId, allowedCountries FROM "StoreSettings" LIMIT 1;
-- Result: allowedCountries = '["GB","FR","CH","NL"]'
```

## Commit History

```
85d9d9c Add allowedCountries field to StoreSettings and expose via public-settings API
  - Add allowedCountries field to Prisma StoreSettings model
  - Update /api/public-settings endpoint to return allowedCountries
  - Widget will use this to validate customer shipping addresses in real-time
```

## Next Steps

1. **Deploy to production** - Builds will auto-run `db push` for PostgreSQL
2. **Monitor checkout** - Verify widget enables/disables correctly
3. **Optional**: Implement admin UI to customize allowed countries per store
4. **Optional**: Add analytics to track unsupported country rejections

## Related Documentation

- **Widget Implementation**: `public/checkout-widget.js` (lines 181-193)
- **Country Configuration**: `src/lib/config/countries.ts`
- **Settings Schema**: `prisma/schema.prisma`
- **API Endpoint**: `src/app/api/public-settings/route.ts`
- **Location Validation**: `src/app/api/auth/callback/route.ts` (merchant level)

---

**Status**: ✅ Implementation Complete - Ready for Production Deployment

**What Works Now**:
- ✅ Widget receives allowed countries from backend
- ✅ Widget validates customer shipping addresses in real-time
- ✅ Widget re-renders when address changes
- ✅ Database schema includes field with proper defaults
- ✅ API endpoint returns field in response
- ✅ ES5 compatible JavaScript
- ✅ No breaking changes

**Waiting For**:
- ⏳ Production deployment to apply PostgreSQL schema changes
