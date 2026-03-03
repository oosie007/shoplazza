# Push a new checkout extension (v2) to test live checkout

A second extension **cd-insure-item-protection-v2** is in `extensions/cd-insure-item-protection-v2/` with the same widget code and `APP_URL = https://shoplazza-nu.vercel.app`. Use it to see if a new push/deploy gets the widget running on live checkout (without dev mode).

## Steps (from repo root)

1. **Push the new extension** (registers it with Shoplazza):

   ```bash
   shoplazza checkout push
   ```

   - When the CLI shows a menu, choose **“Push new extension”** (or the option that adds a new extension).
   - If it asks which extension to push, select **cd-insure-item-protection-v2**.
   - Use store URL `https://oostest.myshoplaza.com/` and token from `.env.local` (`SHOPLAZZA_DEV_TOKEN`) if prompted.

2. **Deploy the new extension**:

   ```bash
   shoplazza checkout deploy
   ```

   - When asked **“Please select an extension to deploy”**, choose **cd-insure-item-protection-v2** (it should appear after step 1).
   - Select the version (e.g. v1.0) and confirm deploy.

3. **Test without dev mode**:

   - Stop any `npm run dev:extension` (Ctrl+C).
   - Open a **normal** checkout URL in a new tab: go to your store → add to cart → checkout (do not use the dev URL from the CLI).
   - Check Network tab for a request to `https://shoplazza-nu.vercel.app/checkout-widget.js`. If it appears and the widget shows, the new extension is live.

If the CLI does not list **cd-insure-item-protection-v2** when pushing, run **`shoplazza checkout create`** first with project name **cd-insure-item-protection-v2**, store URL, and token. Then copy the contents of `extensions/cd-insure-item-protection-v2/src/index.js` into the created project’s extension `src/index.js` and run **push** and **deploy** again.
