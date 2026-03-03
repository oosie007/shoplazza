# Deploy to Vercel and GitHub (step-by-step)

This guide gets the app on GitHub and Vercel **without committing any secrets**. All sensitive values stay in environment variables.

---

## Before you start

- **Never commit** `.env`, `.env.local`, or any file containing real `SHOPLAZZA_CLIENT_SECRET`, tokens, or database URLs.
- **Never paste** real connection strings or API keys into the repo, this doc, or chat. If you did, **rotate those credentials now** (new DB password, new API key).
- The repo is set up so only **`.env.example`** (placeholders only) is committed. Everything else is in `.gitignore`.

---

## Step 1: Confirm nothing sensitive is staged

On your machine, from the project root:

```bash
cd /Users/johanoosthuizen/dev/shoplaza

# See what would be committed (should NOT list .env or .env.local)
git status

# Double-check: .env.example is NOT ignored (so it can be committed)
git check-ignore -v .env.example
# Should output nothing, or "!.env.example" so it's allowed
```

If `.env` or `.env.local` appear in `git status`, **do not add them**. They are in `.gitignore`; if they still show up, they were added before. Run:

```bash
git reset HEAD .env .env.local 2>/dev/null || true
```

---

## Step 2: Create the GitHub repository

1. Go to [github.com/new](https://github.com/new).
2. Choose a **Repository name** (e.g. `shoplaza-item-protection`).
3. Set visibility to **Private** if the code is not public.
4. **Do not** add a README, .gitignore, or license (you already have them).
5. Click **Create repository**.

---

## Step 3: Push your code to GitHub

From the project root:

```bash
cd /Users/johanoosthuizen/dev/shoplaza

# If this folder is not yet a git repo:
git init
git add .
# Ensure no env files with secrets are added
git status
# You should see .env.example listed, and NOT .env or .env.local

git commit -m "Initial commit: Shoplaza Item Protection app"

# Add the GitHub repo as remote (replace with your repo URL)
git remote add origin https://github.com/YOUR_USERNAME/YOUR_REPO_NAME.git

# Push (main branch)
git branch -M main
git push -u origin main
```

**Check again:** On GitHub, open the repo and confirm there are **no** files named `.env`, `.env.local`, or anything with real secrets. Only `.env.example` with placeholders should be there.

---

## Step 4: Database for Vercel (required)

**Local:** The app keeps using **SQLite** for testing: `DATABASE_URL="file:./dev.db"` in `.env.local`. No change to local workflow.

**Vercel:** Serverless cannot use a local file, so you need a **hosted Postgres** database. The app auto-detects: if `DATABASE_URL` starts with `postgres://`, `postgresql://`, or `prisma+postgres://`, it uses the Postgres schema and runs migrations.

**Option A – Prisma Postgres (Vercel / Prisma Data Platform)**

1. In [Vercel](https://vercel.com) → your project → **Storage** → **Create Database** → choose **Prisma Postgres** (or use [Prisma Data Platform](https://prisma.io/data-platform) and connect to Vercel).
2. Attach the database to your project. Vercel will add env vars such as:
   - `DATABASE_URL` (direct Postgres URL)
   - `POSTGRES_URL` (same)
   - `PRISMA_DATABASE_URL` (Prisma Accelerate URL; use this if you use Accelerate)
3. **Never paste these URLs into the repo, docs, or chat.** Add them only in **Vercel → Settings → Environment Variables**.
4. Use **one** of them as `DATABASE_URL` in Vercel (see Step 6). If you use Prisma Accelerate, set `DATABASE_URL` to the `PRISMA_DATABASE_URL` value.

**Option B – Vercel Postgres (standard Postgres)**

1. [vercel.com](https://vercel.com) → **Storage** → **Create Database** → **Postgres**.
2. Create and attach to the project; copy the connection URL only into Vercel env (never into code or docs).

**Option C – Neon / other Postgres**

1. Create a Postgres database and copy the URL (e.g. `postgresql://...?sslmode=require`).
2. Add it only in Vercel as `DATABASE_URL` (Step 6).

**Security:** If you ever pasted a real connection string or API key into the repo, a doc, or a chat, **rotate those credentials immediately** (new password, new API key) and never commit them.

---

## Step 5: Import the project in Vercel

1. Go to [vercel.com](https://vercel.com) and sign in (GitHub if possible).
2. Click **Add New…** → **Project**.
3. **Import** the GitHub repo you created (e.g. `YOUR_USERNAME/YOUR_REPO_NAME`).
4. Leave **Root Directory** as `.` (or set to the app root if you use a monorepo).
5. **Framework Preset:** Next.js (should be auto-detected).
6. **Build Command:** `next build` (default).
7. **Output Directory:** leave default.
8. Do **not** add env vars in this screen; use **Environment Variables** in the next step.

Click **Deploy** once; it may fail until env vars and DB are set. You’ll fix that in the next steps.

---

## Step 6: Add environment variables in Vercel (no secrets in code)

1. In Vercel, open your project → **Settings** → **Environment Variables**.
2. Add each variable below. Use **Production** (and optionally Preview) as needed. **Never put these in the repo or in docs.**

| Name | Value | Notes |
|------|--------|--------|
| `SHOPLAZZA_CLIENT_ID` | Your app’s Client ID | Partner Center → CD_Insure → App settings |
| `SHOPLAZZA_CLIENT_SECRET` | Your app’s Client Secret | **Secret** – never commit |
| `NEXT_PUBLIC_APP_URL` | Your Vercel app URL | e.g. `https://your-project.vercel.app` (no trailing slash) |
| `DATABASE_URL` | Postgres connection URL | From Step 4. Use the **direct** URL (`postgres://...` or `POSTGRES_URL`) **or** Prisma Accelerate URL (`PRISMA_DATABASE_URL`). Paste only in Vercel UI. |

- For **NEXT_PUBLIC_APP_URL**: after the first deploy, set it to your real Vercel URL and redeploy if needed.
- **Do not** add `SHOPLAZZA_DEV_TOKEN` / `SHOPLAZZA_DEV_STORE` unless you run the Shoplazza CLI against this project.

---

## Step 7: Deploy and run Postgres migrations

1. The repo already includes:
   - **Local:** `prisma/schema.prisma` (SQLite) when `DATABASE_URL` is `file:./dev.db`.
   - **Production:** `prisma/schema.postgres.prisma` (Postgres) when `DATABASE_URL` is a `postgres://` or `prisma+postgres://` URL.
   - Build runs `prisma generate` with the correct schema from `DATABASE_URL`, then `next build`.
2. In Vercel, trigger a deploy (e.g. **Deployments** → **Redeploy**). The build will use the Postgres schema because `DATABASE_URL` is set to your Postgres URL.
3. **One-time:** Run migrations against the production DB. Use the **same** `DATABASE_URL` value you set in Vercel (paste it only in your terminal or a local `.env` that is not committed):

   ```bash
   # From the project root; DATABASE_URL must be your production Postgres URL
   npx prisma migrate deploy --schema=prisma/schema.postgres.prisma
   ```

   Or, if your Vercel project is linked to Prisma Postgres, you can run migrations from the Vercel/Prisma dashboard. After that, the app will have the required tables.

---

## Step 8: Shoplazza Partner Center (production URLs)

1. In [Partner Center](https://partners.shoplazza.com/) → your app → **Setup**.
2. Set **App URL** to: `https://YOUR_VERCEL_URL/api/auth`.
3. Set **Redirect URL** to: `https://YOUR_VERCEL_URL/api/auth/callback`.
4. Replace `YOUR_VERCEL_URL` with your real Vercel URL (e.g. `https://your-project.vercel.app`).

---

## Step 9: Sanity check

- Visit `https://YOUR_VERCEL_URL/admin` (with `?shop=your-store.myshoplaza.com` if needed). You should see the app (or a redirect to install).
- In GitHub, open the repo and search for:
  - `SHOPLAZZA_CLIENT_SECRET`
  - `your_client_secret`
  - Any real token or real `DATABASE_URL`
- If anything appears, treat those secrets as **compromised**: rotate the client secret and DB credentials, then remove the sensitive data from the repo history (e.g. with `git filter-branch` or BFG) and never commit them again.

---

## Summary

| Step | What you do |
|------|-------------|
| 1 | Confirm no `.env` / `.env.local` committed; only `.env.example` is safe. |
| 2 | Create a new GitHub repo (no README/.gitignore). |
| 3 | `git init`, `git add .`, `git commit`, `git remote add origin`, `git push`. |
| 4 | Create a hosted Postgres DB (Prisma Postgres / Vercel Postgres / Neon, etc.). Never paste connection strings in repo or docs. |
| 5 | Import the GitHub repo in Vercel as a Next.js project. |
| 6 | In Vercel, set `SHOPLAZZA_CLIENT_ID`, `SHOPLAZZA_CLIENT_SECRET`, `NEXT_PUBLIC_APP_URL`, `DATABASE_URL` (Postgres URL from Step 4). |
| 7 | Redeploy; then run `npx prisma migrate deploy --schema=prisma/schema.postgres.prisma` with production `DATABASE_URL` once. |
| 8 | In Partner Center, set App URL and Redirect URL to your Vercel domain. |
| 9 | Test the app and verify no secrets are in GitHub. |

All secrets stay in **Vercel Environment Variables** and (locally) in **`.env.local`**; they are never committed to GitHub.
