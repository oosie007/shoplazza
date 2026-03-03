# Where we are stuck (single source of truth)

**Goal:** Customer toggles “Item protection” at checkout → a protection line appears and the total updates (like Worry-Free Delivery). No manual product setup for the merchant.

---

## What works

| Piece | Status |
|-------|--------|
| **Extension** | Loads; injects checkout widget. |
| **Widget** | Shows toggle; fetches settings from our app. |
| **Settings / admin** | Saves pricing %, Chubb logo, etc. |
| **Create “Item protection” product** | We create it via OpenAPI; product + variant IDs saved. |
| **Cart API** | Toggle ON → we add the product as a line (POST /api/cart). Toggle OFF → we remove it. |
| **Partner API – Create function** | We get a partner token, POST to `partners.shoplazza.com/openapi/2024-07/functions` with WASM + source_code, get back a function **id**. |
| **WASM build** | `npm run build:cart-transform-wasm` produces `public/cart-transform.wasm`; we send it in the create request. |
| **OAuth / install** | Works (we had to remove invalid scopes like `read_function`). |

So: product exists, line can be added/removed, and we successfully **create** the cart-transform function on the Partner API. The only thing that never succeeds is the next step.

---

## The one thing that doesn’t work: **Bind**

After we create the function we have to **bind** it to the store so Shoplazza runs our logic when calculating the cart.

- **Endpoint:** `POST https://{shop}.myshoplaza.com/openapi/2024-07/function/cart-transform`
- **Auth:** Store access token (OAuth).
- **Body:** We’ve tried `{ "function_id": "<id>" }` and `{ "id": "<id>" }`.

**Result:** Either **400** (e.g. “Field with name 'id' is required” or “Field with name 'file' is required” at create – we fixed the file one) or **403 Forbidden**. Response body is sometimes empty `{}`, so we don’t always get a clear message.

Until **bind** succeeds:

- Shoplazza never runs our cart-transform function.
- The protection line might appear (we add it via Cart API) but its **price** doesn’t get set by us, so the total doesn’t update correctly.

So **we are stuck on bind (400 or 403).** Everything else is in place.

---

## What we tried

1. **Create on Partner API** – Works (with WASM `file`).
2. **Bind body:** `function_id` → 400; tried `id` → still 400 or 403.
3. **OAuth scopes:** Added `read_function` / `write_function` → Shoplazza returned “Scopes invalid”; reverted.
4. **Partner Center:** No clear “enable Function API” or “bind permission” steps; no support to confirm.
5. **pkg_set / Worry-Free path:** We’d prefer to add a fee like Worry Free (no product, no cart-transform). For our app `pkg_set` returns **404** (no checkout package registered). No docs/support on how to register that package.

So we’re blocked on: **bind** (and/or getting the same capabilities via a checkout package).

---

## What would unblock us

1. **Shoplazza support or Partner Center:**  
   - Exact steps to get **bind** to succeed (required body, any app/API enablement, permissions).  
   - Or exact steps to **register a checkout package** so we can use the Worry-Free-style flow and stop depending on bind.

2. **Reference comparison:**  
   If your friend’s reference actually uses the **same** Shoplazza bind endpoint (Partner API create + store bind with function id), then comparing their bind request (URL, headers, body, when it’s called) to ours line-by-line and making ours match. Their `cart-transform-function` folder (protocol-adapter, etc.) is about **input/output format** of the function; the **bind** call itself would be in their app code that talks to `.../function/cart-transform`.

---

## Quick reference

- **Bind code:** `src/lib/shoplazza/item-protection-product.ts` → `bindCartTransformByFunctionId`, called from `createAndBindCartTransformFunction`.
- **Create code:** same file → `createFunctionViaPartnerAPI` (multipart with WASM).
- **Partner API doc:** `docs/CART_TRANSFORM_PARTNER_API.md`.
- **Two paths (Worry Free vs product + Cart Transform):** `docs/TWO_WAYS_TO_ADD_CHECKOUT_FEE.md`.

**In one sentence:** We’re stuck because **bind** (POST to the store’s `.../function/cart-transform` with the function id) always returns 400 or 403 and we have no way to get the right permission or request format without Shoplazza support or a working reference bind to copy.
