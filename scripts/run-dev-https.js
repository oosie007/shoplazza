#!/usr/bin/env node
/**
 * Simple wrapper that starts Next.js dev and prints ngrok instructions.
 * For HTTPS, use ngrok which handles SSL automatically.
 *
 * Usage: npm run dev:https
 */
const path = require("path");
const { spawnSync } = require("child_process");

const appRoot = path.resolve(__dirname, "..");
const nodeModules = path.join(appRoot, "node_modules");
const existing = process.env.NODE_PATH || "";

// Set up environment
const env = {
  ...process.env,
  NODE_PATH: existing ? `${nodeModules}${path.delimiter}${existing}` : nodeModules,
};

console.log(`
╔══════════════════════════════════════════════════════════════════════╗
║                 Next.js Dev Server with ngrok HTTPS                  ║
╚══════════════════════════════════════════════════════════════════════╝

🚀 Starting Next.js dev server on http://localhost:3000...

HTTPS Setup Instructions:
────────────────────────

1. In another terminal, run ngrok to create HTTPS tunnel:

   ngrok http 3000

2. Copy the ngrok URL (looks like: https://abc123def456.ngrok.io)

3. Update your .env.local:

   NEXT_PUBLIC_APP_URL=https://abc123def456.ngrok.io

4. Inject the URL into the extension:

   npm run inject:extension-url

5. Your Next.js backend is now accessible over HTTPS! ✅

Note: ngrok generates a new URL on restart (free tier).
      For a fixed URL, upgrade to a paid ngrok account.

`);

// Start the Next.js dev server
const result = spawnSync(
  "npx",
  ["next", "dev", ...process.argv.slice(2)],
  { stdio: "inherit", env, cwd: appRoot }
);

process.exit(result.status ?? 1);
