#!/usr/bin/env node
/**
 * Run prisma migrate deploy when using Postgres (e.g. Vercel).
 * Uses schema.postgres.prisma so the correct client and migrations apply.
 * No-op when DATABASE_URL is SQLite or unset (local dev).
 */
const path = require("path");
const { execSync } = require("child_process");

const dbUrl = process.env.DATABASE_URL || "";
const usePostgres =
  dbUrl.startsWith("postgres://") ||
  dbUrl.startsWith("postgresql://") ||
  dbUrl.startsWith("prisma+postgres://");

if (!usePostgres) {
  console.log("[prisma-migrate-deploy] Skipping (not Postgres)");
  process.exit(0);
}

const schemaPath = path.join(__dirname, "..", "prisma", "schema.postgres.prisma");
console.log("[prisma-migrate-deploy] Running migrate deploy...");
execSync(`npx prisma migrate deploy --schema=${schemaPath}`, {
  stdio: "inherit",
  cwd: path.join(__dirname, ".."),
  env: process.env,
});
