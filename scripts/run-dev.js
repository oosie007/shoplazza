#!/usr/bin/env node
/**
 * Run next dev with NODE_PATH set to this app's node_modules so that
 * Turbopack/PostCSS workers (which may run with cwd = workspace root) can
 * resolve 'tailwindcss' and avoid "Can't resolve 'tailwindcss' in '.../dev'".
 */
const path = require("path");
const { spawnSync } = require("child_process");

const appRoot = path.resolve(__dirname, "..");
const nodeModules = path.join(appRoot, "node_modules");
const existing = process.env.NODE_PATH || "";
const env = {
  ...process.env,
  NODE_PATH: existing ? `${nodeModules}${path.delimiter}${existing}` : nodeModules,
};

const result = spawnSync(
  "npx",
  ["next", "dev", ...process.argv.slice(2)],
  { stdio: "inherit", env, cwd: appRoot }
);
process.exit(result.status ?? 1);
