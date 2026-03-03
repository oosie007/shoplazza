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

### 3. Cart API 200 but nothing appears on checkout / total doesn’t update

If the console shows **Cart API response: status 200** and **Item Protection line added (200)** but the order summary on the **checkout page** doesn’t show the protection line or the total doesn’t change:

- Check the console for **`[CD Insure] Price refetch status`** and **`Price response total=… line_items=…`**. That shows whether the store’s price endpoint returned updated data and we called `onPricesChange`. If `line_items` doesn’t increase after toggle ON, the **checkout** may be using a different cart/order than `/api/cart` (e.g. a snapshot from when the customer entered checkout).
- **pkg_set 404** is expected (we don’t have a checkout package). It doesn’t block the Cart API or Cart Transform; the 404 is for the alternate “Worry-Free style” path.

**What to try:**

1. **Turn on Item Protection on the cart page, then go to checkout**  
   On the **store’s cart page** (before clicking “Checkout”), use the widget to turn Item Protection ON so the line is in the cart when you enter checkout. If the line and total then appear correctly, the issue is that checkout isn’t reflecting live changes to `/api/cart` until you re-enter or refresh.

2. **After toggling on checkout, refresh the page**  
   Toggle Item Protection ON, then reload the checkout page (or go to the next step and back). See if the protection line and total appear after the refresh.

3. **Ask Shoplazza**  
   Confirm whether checkout displays the same cart as `POST /api/cart` or a separate “order”/cart, and if there’s an API to add a line to the checkout order during checkout.

### 4. Checkout page shows different products than GET /api/cart

If the **checkout page** shows one set of line items but **GET /api/cart** (or the cart response in Network) shows different items:

- Checkout may be using a **different cart or order session** than the one the Cart API updates (e.g. a snapshot from when they entered checkout). Use the workarounds in §3 above.

### 5. pkg_set 404

You can ignore **pkg_set 404**. We don’t use that path; we use the Cart API (add/remove line) and Cart Transform (set price). The 404 is expected unless a checkout package is registered.

### 6. Cart Transform (price stays $0, total doesn’t include protection)

If the Item Protection **line** is in the cart (you see it in the cart API response with price "0.00") but the **checkout total doesn’t include the $40**:

- Our **Cart Transform** is supposed to set that line’s price. Check your **Vercel (or app) logs** for `[cart-transform]` when you load checkout or the cart. If that log **never** appears, Shoplazza is not calling our callback (bind may have failed or the store may not use it on checkout). If it **does** appear, the log shows the line id and the price we’re returning.
- **400 "Field with name 'file' is required" (Partner API Create Function):** The Cart Transform create step requires a WASM file. Run **`npm run build:cart-transform-wasm`** so that `public/cart-transform.wasm` exists, then redeploy. If the file is missing, the admin UI shows a clear message to run that script.
- **404 on Create Function or Bind:** The API endpoint may not exist for your app or store. Check Vercel logs for the exact **URL** that returned 404 (we now log it). Confirm the path with [Shoplazza Create Function](https://www.shoplazza.dev/v2024.07/reference/create-function) and [Bind Cart Transform](https://www.shoplazza.dev/v2024.07/reference/bind-cart-transform-function). You may need to enable the Function API or register something in **Partner Center** for your app.
- **“Register a checkout package in Partner Center” (console):** If the checkout shows this, the store may need a **checkout package** registered for your app in Shoplazza Partner Center. Check your app’s checkout/function settings in Partner Center and complete any “register package” or “checkout extension” steps.
- **403 Forbidden on bind cart-transform:** The store’s access token doesn’t have permission for the Function/Cart Transform API. Shoplazza’s OAuth scope list does **not** include `read_function`/`write_function` (requesting them returns "Scopes invalid"). Fix: In **Shoplazza Partner Center** → your app → enable **Function API** or **Cart Transform** for the app if available, then reinstall the app so the store gets a token that can call the bind endpoint. If no such option exists, contact Shoplazza support for how to get bind permission.
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
