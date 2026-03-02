#!/usr/bin/env node
/**
 * Run prisma db push when using Postgres (e.g. Vercel).
 * Uses schema.postgres.prisma so the correct client and schema apply.
 * No-op when DATABASE_URL is SQLite or unset (local dev).
 * 
 * Note: Using 'db push' instead of 'migrate deploy' to handle cases where
 * the _prisma_migrations table is corrupted or out of sync.
 * 'db push' is more forgiving and directly syncs the schema.
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
console.log("[prisma-migrate-deploy] Running prisma db push...");
execSync(`npx prisma db push --skip-generate --schema=${schemaPath}`, {
  stdio: "inherit",
  cwd: path.join(__dirname, ".."),
  env: process.env,
});
