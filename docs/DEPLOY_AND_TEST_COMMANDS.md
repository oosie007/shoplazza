# Step-by-step deploy and test commands

Run these from a terminal. Use the **repo root** unless a step says otherwise.

---

## 1. Environment

Ensure `.env.local` has:

- `NEXT_PUBLIC_APP_URL` = your production app URL (e.g. `https://shoplazza-nu.vercel.app`) — no trailing slash
- `DATABASE_URL` = production Postgres URL (for migrations and app)
- Shoplazza OAuth vars if you use them from env

---

## 2. Deploy the Next.js app (e.g. Vercel)

**Option A – Vercel (Git push)**

```bash
git add -A
git commit -m "Item Protection: auto-create product, Cart Transform, no merchant setup"
git push
```

(Vercel will run `npm run build`, which includes `prisma migrate deploy` for Postgres.)

**Option B – Local build only (no deploy)**

```bash
npm run build
```

---

## 3. Inject app URL into the extension

From **repo root**:

```bash
npm run inject:extension-url
```

You should see: `Injected APP_URL into extension: https://...`

---

## 4. Push and deploy the checkout extension

From **repo root**, go into the extension project and run the Shoplazza CLI:

```bash
cd cd-insure-item-protection3
npm i
shoplazza checkout push
```

When prompted, choose **Push new extension** (or the option that uploads your extension). Select **itemprotect_extension** if asked.

Then:

```bash
shoplazza checkout deploy
```

Select **itemprotect_extension** (or the name shown), pick the version, confirm. You should see something like: `Successfully deployed the extension '...'`.

Then go back to repo root:

```bash
cd ..
```

---

## 5. Test end-to-end

1. Open your test store (e.g. `https://oostest.myshoplaza.com`).
2. Add a product to cart → go to **checkout** (normal checkout URL, not dev/preview).
3. Confirm the Item Protection widget appears (e.g. in the order summary).
4. Turn the toggle **ON**:
   - A cart line for Item Protection should appear.
   - The total should update (e.g. subtotal + protection fee).
5. Turn the toggle **OFF**: the line should disappear and the total update again.

**If the total doesn’t update or no line appears:**

- Open DevTools → **Network**. Check:
  - `GET .../api/public-settings?shop=...` → 200 and response includes `itemProtectionProductId` and `itemProtectionVariantId`.
  - If those IDs are null, product creation or ensure-step may have failed; check app logs (e.g. Vercel logs) for errors from `ensureItemProtectionProduct` or Cart Transform bind.

---

## One-line summary

After first-time extension push, from repo root:

```bash
npm run inject:extension-url && cd cd-insure-item-protection3 && shoplazza checkout deploy && cd ..
```

(You still need to have run `shoplazza checkout push` at least once from inside `cd-insure-item-protection3` so the extension is registered.)
