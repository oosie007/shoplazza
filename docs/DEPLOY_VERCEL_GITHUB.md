# Deploy to Vercel and GitHub (step-by-step)

This guide gets the app on GitHub and Vercel **without committing any secrets**. All sensitive values stay in environment variables.

---

## Before you start

- **Never commit** `.env`, `.env.local`, or any file containing real `SHOPLAZZA_CLIENT_SECRET`, tokens, or database URLs.
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

The app uses **SQLite** locally (`file:./dev.db`). Vercel’s serverless environment cannot use a local SQLite file, so you need a **hosted database**.

**Option A – Vercel Postgres (simplest)**

1. Go to [vercel.com](https://vercel.com) → your account → **Storage** → **Create Database** → **Postgres**.
2. Create the DB and attach it to your project (or note the connection string for Step 6).
3. You’ll get a `POSTGRES_URL` (or `DATABASE_URL`) to use in Step 6.

**Option B – Neon / PlanetScale / other**

1. Create a Postgres (or compatible) database.
2. Copy the connection URL (e.g. `postgresql://user:pass@host/db?sslmode=require`).
3. You’ll add it in Vercel as `DATABASE_URL` in Step 6.

**Prisma:** For Postgres you must point Prisma at Postgres and run migrations. After adding the Postgres URL:

- In `prisma/schema.prisma`, set `provider = "postgresql"` (and keep `url = env("DATABASE_URL")`).
- Run: `npx prisma migrate deploy` (or push) **before** or **after** first deploy (see Step 7).

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
2. Add each variable below. Use **Production** (and optionally Preview) as needed. **Never put these in the repo.**

| Name | Value | Notes |
|------|--------|--------|
| `SHOPLAZZA_CLIENT_ID` | Your app’s Client ID | Partner Center → CD_Insure → App settings |
| `SHOPLAZZA_CLIENT_SECRET` | Your app’s Client Secret | **Secret** – never commit |
| `NEXT_PUBLIC_APP_URL` | Your Vercel app URL | e.g. `https://your-project.vercel.app` (no trailing slash) |
| `DATABASE_URL` | Postgres connection URL | From Step 4 (Vercel Postgres or Neon, etc.) |

- For **NEXT_PUBLIC_APP_URL**: after the first deploy, Vercel gives you a URL like `https://xxx.vercel.app`. Set it there and redeploy if you had used a placeholder.
- **Do not** add `SHOPLAZZA_DEV_TOKEN` or `SHOPLAZZA_DEV_STORE` unless you run the Shoplazza CLI against this project (they’re for local extension dev).

---

## Step 7: Switch Prisma to Postgres and deploy

1. In the project, set Prisma to use Postgres. In `prisma/schema.prisma`, set:
   - `provider = "postgresql"` (instead of `"sqlite"`).
2. Commit and push (no secrets in these changes):

   ```bash
   git add prisma/schema.prisma
   git commit -m "Use Postgres for production"
   git push
   ```

3. In Vercel, trigger a new deploy (e.g. **Deployments** → **Redeploy**).
4. After deploy, run migrations against the **production** DB. Use the same `DATABASE_URL` Vercel uses (e.g. from Vercel Postgres dashboard or env):

   ```bash
   # One-time: run migrations (use the production DATABASE_URL from Vercel)
   DATABASE_URL="postgresql://..." npx prisma migrate deploy
   ```

   If you don’t have migrations yet:

   ```bash
   npx prisma migrate dev --name init
   git add prisma/migrations
   git commit -m "Add initial migration"
   git push
   ```

   Then run `npx prisma migrate deploy` with production `DATABASE_URL`.

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
| 4 | Create a hosted DB (Vercel Postgres or Neon, etc.). |
| 5 | Import the GitHub repo in Vercel as a Next.js project. |
| 6 | In Vercel, set `SHOPLAZZA_CLIENT_ID`, `SHOPLAZZA_CLIENT_SECRET`, `NEXT_PUBLIC_APP_URL`, `DATABASE_URL`. |
| 7 | Set Prisma to `postgresql`, push, redeploy, run `prisma migrate deploy` with production `DATABASE_URL`. |
| 8 | In Partner Center, set App URL and Redirect URL to your Vercel domain. |
| 9 | Test the app and verify no secrets are in GitHub. |

All secrets stay in **Vercel Environment Variables** and (locally) in **`.env.local`**; they are never committed to GitHub.
