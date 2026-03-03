#!/usr/bin/env node
/**
 * When the app lives in a parent folder that has its own package-lock.json (e.g. Cursor workspace root),
 * Next.js / PostCSS may resolve modules from the parent and fail to find "tailwindcss".
 * This script symlinks tailwindcss (and @tailwindcss/postcss) into the parent's node_modules
 * so resolution from the parent directory finds them.
 */
const fs = require("fs");
const path = require("path");

const appRoot = path.resolve(__dirname, "..");
const parentRoot = path.join(appRoot, "..");
const parentNodeModules = path.join(parentRoot, "node_modules");
const packages = ["tailwindcss", "@tailwindcss/postcss"];

let didSomething = false;
if (!fs.existsSync(parentNodeModules)) {
  fs.mkdirSync(parentNodeModules, { recursive: true });
  didSomething = true;
}

for (const pkg of packages) {
  const target = path.join(appRoot, "node_modules", pkg);
  const link = path.join(parentNodeModules, pkg);
  if (!fs.existsSync(target)) continue;
  const linkDir = path.dirname(link);
  if (!fs.existsSync(linkDir)) {
    fs.mkdirSync(linkDir, { recursive: true });
    didSomething = true;
  }
  if (fs.existsSync(link)) {
    try {
      const stat = fs.lstatSync(link);
      if (stat.isSymbolicLink()) continue; // already our link
    } catch (_) {}
    continue; // real package exists, don't overwrite
  }
  try {
    fs.symlinkSync(target, link, "dir");
    didSomething = true;
  } catch (e) {
    console.warn("[ensure-parent-node-modules] Could not symlink", pkg, e.message);
  }
}

if (didSomething) {
  console.log("[ensure-parent-node-modules] Symlinked tailwindcss (and @tailwindcss/postcss) into parent node_modules so PostCSS can resolve from workspace root.");
}
