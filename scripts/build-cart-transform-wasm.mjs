#!/usr/bin/env node
/**
 * Compile scripts/cart-transform-function.js to public/cart-transform.wasm
 * using javy-cli (Javy). Required for Shoplazza Partner API Create Function.
 * Run: npm run build:cart-transform-wasm
 */
import { execSync } from "child_process";
import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(__dirname, "..");
const src = path.join(root, "scripts", "cart-transform-function.js");
const out = path.join(root, "public", "cart-transform.wasm");

if (!fs.existsSync(src)) {
  console.error("Source not found:", src);
  process.exit(1);
}
const publicDir = path.dirname(out);
if (!fs.existsSync(publicDir)) {
  fs.mkdirSync(publicDir, { recursive: true });
}

console.log("Building cart-transform.wasm with javy-cli...");
execSync(`npx javy-cli compile "${src}" -o "${out}"`, {
  cwd: root,
  stdio: "inherit",
});
console.log("Wrote", out);
