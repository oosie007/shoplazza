# Item Protection widget – troubleshooting

## "Nothing added to cart" or cart shows different products

### 1. Check the console when you toggle ON

With the checkout page open and DevTools → **Console** visible:

1. Turn the Item Protection toggle **ON**.
2. Look for these messages (they always run, not only in debug mode):
   - **`[CD Insure] Cart API: POST https://.../api/cart with product_id=... variant_id=...`**  
     Confirms we have product/variant IDs and the URL we’re calling.
   - **`[CD Insure] Cart API response: status 200`** (or another status).  
     Confirms the request completed.

If you see **Cart API skipped – no Item Protection product/variant ID**:
- The app doesn’t have the IDs for this store. In the app admin → Configuration, click **“Create Item Protection product now”** and ensure the two ID fields are filled. Then reload checkout.

If you see **Cart API response: status 406** (or 400, 404):
- The response body is logged right after. That message tells you why the store rejected the add (e.g. wrong format, product unavailable).

### 2. Check the Network tab

1. DevTools → **Network** → filter **Fetch/XHR**.
2. Toggle Item Protection **ON**.
3. Find the **POST** request to **`/api/cart`** (or full URL ending in `/api/cart`).
4. Click it and check:
   - **Request URL** – should be the same origin as the checkout page (e.g. `https://oostest.myshoplaza.com/api/cart` or with a locale prefix).
   - **Request payload** – should include `product_id`, `variant_id`, `quantity: 1`.
   - **Response** – status 200 and response body. If 200, the response usually includes the updated cart with a new line for “Item protection”.

### 3. Checkout page shows different products than GET /api/cart

If the **checkout page** shows one set of line items (e.g. “Legendary Whitetails…”) but a **GET /api/cart** (or the cart response in Network) shows different items (e.g. “Jabra earphones”):

- Checkout may be using a **different cart or order session** than the one the Cart API updates. For example:
  - The storefront cart (used by `/api/cart`) might be a different session from the “checkout cart” that the checkout UI reads from.
  - Or the cart was copied when you entered checkout, and later changes to the storefront cart don’t appear on the checkout page until it refreshes.

**What to try:**

1. **Add Item Protection on the cart page, then go to checkout**  
   Add the product to cart on the **store’s cart page** (before clicking “Checkout”), then proceed to checkout. If the line appears there, the Cart API is working and the issue is how checkout binds to that cart.

2. **Refresh checkout after toggling**  
   Toggle Item Protection ON, then refresh the checkout page (or go to the next step and back). See if the protection line appears after the refresh.

3. **Ask Shoplazza**  
   Confirm whether, on the checkout domain, `POST /api/cart` updates the **same** cart that the checkout UI displays, or if checkout uses a separate “order”/cart and there is a different API to add lines during checkout.

### 4. pkg_set 404

You can ignore **pkg_set 404**. We don’t use that path; we use the Cart API (add/remove line) and Cart Transform (set price). The 404 is expected unless a checkout package is registered.

### 5. Cart Transform (price stays $0, total doesn’t include protection)

If the Item Protection **line** is in the cart (you see it in the cart API response with price "0.00") but the **checkout total doesn’t include the $40**:

- Our **Cart Transform** is supposed to set that line’s price. Check your **Vercel (or app) logs** for `[cart-transform]` when you load checkout or the cart. If that log **never** appears, Shoplazza is not calling our callback (bind may have failed or the store may not use it on checkout). If it **does** appear, the log shows the line id and the price we’re returning.
- **400 "Field with name 'file' is required" (Partner API Create Function):** The Cart Transform create step requires a WASM file. Run **`npm run build:cart-transform-wasm`** so that `public/cart-transform.wasm` exists, then redeploy. If the file is missing, the admin UI shows a clear message to run that script.
- **404 on Create Function or Bind:** The API endpoint may not exist for your app or store. Check Vercel logs for the exact **URL** that returned 404 (we now log it). Confirm the path with [Shoplazza Create Function](https://www.shoplazza.dev/v2024.07/reference/create-function) and [Bind Cart Transform](https://www.shoplazza.dev/v2024.07/reference/bind-cart-transform-function). You may need to enable the Function API or register something in **Partner Center** for your app.
- **“Register a checkout package in Partner Center” (console):** If the checkout shows this, the store may need a **checkout package** registered for your app in Shoplazza Partner Center. Check your app’s checkout/function settings in Partner Center and complete any “register package” or “checkout extension” steps.
- **403 Forbidden on bind cart-transform:** The store’s access token doesn’t have permission for the Function/Cart Transform API. (1) In **Shoplazza Partner Center** → your app → ensure **Function API** or **Cart Transform** is enabled for the app. (2) We request `read_function` and `write_function` in `src/lib/shoplazza/auth.ts`; if your scope list uses different names, add them there. (3) **Reinstall the app** on the store (or re-authorize) so the store gets a new token with the new scopes. Until then, bind will keep returning 403.
- **Re-bind and verify:** In app admin → Configuration, click **“Re-bind Cart Transform”**. If it fails, the UI shows status and body; check Vercel logs for `[item-protection-product] Create function failed` or `Bind cart-transform failed` – we now log the **exact URL** that returned 404/403 so you can confirm with Shoplazza docs.
- **pkg_set 404** is unrelated; we don’t use that. We use Cart API (add line) + Cart Transform (set price).

---

**Quick checklist**

| Check | What to look for |
|-------|-------------------|
| Console when toggle ON | `[CD Insure] Cart API: POST ...` and `Cart API response: status ...` |
| Network → POST /api/cart | Status 200 and response body with an “Item protection” line |
| Different products on page vs cart API | Try adding protection on cart page then checkout, or refresh; ask Shoplazza if checkout uses a different cart |
| No product/variant IDs | App admin → “Create Item Protection product now” and ensure IDs are saved |
