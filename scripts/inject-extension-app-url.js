#!/usr/bin/env node
/**
 * Injects NEXT_PUBLIC_APP_URL from .env.local into the checkout extension source
 * so the deployed extension loads checkout-widget.js from your production app URL.
 * Run before "shoplazza checkout deploy" so the widget loads automatically for
 * all customers (including incognito).
 */
const fs = require("fs");
const path = require("path");

const repoRoot = path.join(__dirname, "..");
const envPath = path.join(repoRoot, ".env.local");
const envExample = path.join(repoRoot, ".env");
const extPath = path.join(repoRoot, "extensions", "cd-insure-item-protection", "src", "index.js");

function readAppUrl() {
  for (const p of [envPath, envExample]) {
    if (fs.existsSync(p)) {
      const content = fs.readFileSync(p, "utf8");
      const m = content.match(/NEXT_PUBLIC_APP_URL\s*=\s*(.+)/);
      if (m) return m[1].replace(/^["'\s]+|["'\s]+$/g, "").trim();
    }
  }
  return process.env.NEXT_PUBLIC_APP_URL || "";
}

const appUrl = readAppUrl();
if (!appUrl || appUrl.startsWith("http://localhost") || appUrl.includes("ngrok")) {
  console.warn(
    "inject-extension-app-url: NEXT_PUBLIC_APP_URL is missing or not a production URL.\n" +
    "Set it in .env.local to your production app URL (e.g. https://your-app.com) so the widget loads for all customers."
  );
}

if (!fs.existsSync(extPath)) {
  console.error("Extension file not found:", extPath);
  process.exit(1);
}

let code = fs.readFileSync(extPath, "utf8");
const replacement = appUrl || "https://your-app-url.com";
code = code.replace(
  /const APP_URL = ['"].*?['"];/,
  `const APP_URL = '${replacement.replace(/'/g, "\\'")}';`
);
fs.writeFileSync(extPath, code, "utf8");
console.log("Injected APP_URL into extension:", replacement || "(none – set NEXT_PUBLIC_APP_URL in .env.local)");
