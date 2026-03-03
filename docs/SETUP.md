# CD_Insure – Setup and Partner Center URLs

## 1. Connecting ngrok to your app

ngrok gives you a public URL (e.g. `https://abc1-23-45-67.ngrok-free.app`) that forwards to your laptop. Do this in order:

**Step A – Start your app**

In a terminal, from the project folder:

```bash
cd /Users/johanoosthuizen/dev/shoplaza
npm run dev
```

Leave this running. The app will be at `http://localhost:3000`.

**Step B – Start ngrok**

In a **second** terminal:

```bash
ngrok http 3000
```

ngrok will print something like:

```
Forwarding   https://abc1-23-45-67.ngrok-free.app -> http://localhost:3000
```

Copy that **https** URL (no trailing slash). That’s your **ngrok domain** for the steps below.

**Step C – Put the ngrok URL in `.env.local`**

Open `.env.local` and set `NEXT_PUBLIC_APP_URL` to that exact URL:

```env
SHOPLAZZA_CLIENT_ID=kd9Lz4LDiOV-A_yDb3PXsoscuNlrHILglgrVYUKwIQ8
SHOPLAZZA_CLIENT_SECRET=your_client_secret_here

# Your ngrok URL – no trailing slash
NEXT_PUBLIC_APP_URL=https://abc1-23-45-67.ngrok-free.app
```

Save the file. Restart `npm run dev` so it picks up the new value.

**Step D – Use the same URL in Partner Center**

In [Partner Center](https://partners.shoplazza.com/) → **Apps** → **CD_Insure** → **Setup**, set:

| Field | Value (replace with your ngrok URL) |
|--------|--------------------------------------|
| **App URL** | `https://YOUR-NGROK-URL/api/auth` |
| **Redirect URL** | `https://YOUR-NGROK-URL/api/auth/callback` |

Example: if ngrok gave you `https://abc1-23-45-67.ngrok-free.app`, then:

- App URL: `https://abc1-23-45-67.ngrok-free.app/api/auth`
- Redirect URL: `https://abc1-23-45-67.ngrok-free.app/api/auth/callback`

Save in Partner Center.

**Summary:** Your laptop runs the app (`npm run dev`). ngrok makes it reachable at `https://....ngrok-free.app`. Shoplazza and your `.env.local` both use that same URL so install and redirects work.

---

## 2. Partner Center – App URL and Redirect URL (reference)

In [Partner Center](https://partners.shoplazza.com/) → **Apps** → **CD_Insure** → **Setup** (or App setup):

| Field | Value |
|--------|--------|
| **App URL** | `https://YOUR_TUNNEL_OR_DOMAIN/api/auth` |
| **Redirect URL** | `https://YOUR_TUNNEL_OR_DOMAIN/api/auth/callback` |

Examples (replace with your real tunnel or domain):

- If using ngrok: `https://abc123.ngrok-free.app/api/auth` and `https://abc123.ngrok-free.app/api/auth/callback`
- If using a real domain: `https://cd-insure.yourdomain.com/api/auth` and `https://cd-insure.yourdomain.com/api/auth/callback`

Save and run **Validate** so you can install the app on a development store.

## 3. Run the app locally

```bash
npm run dev
```

Then expose port 3000 with a tunnel, e.g.:

```bash
ngrok http 3000
```

Set `NEXT_PUBLIC_APP_URL` in `.env.local` to the ngrok URL (e.g. `https://abc123.ngrok-free.app`), then set the same base URL in Partner Center for App URL and Redirect URL (with `/api/auth` and `/api/auth/callback` as above).

## 4. Test the install flow

1. In Partner Center → **Test your app** → select a development store → **Install app**.
2. You should be redirected to Shoplazza’s authorize page, then after approving to your `/admin` page.
3. The app stores the shop and access token in `data/stores.json` (do not commit this file).

## 5. Security reminder

- **Client Secret** must stay in `.env.local` only and must not be committed.
- The `data/` folder (with stored tokens) is in `.gitignore`; keep it that way.
