#!/usr/bin/env node
/**
 * Sync Prisma schema to database.
 * Uses schema.postgres.prisma for Postgres (e.g. Vercel).
 * No-op when DATABASE_URL is SQLite or unset (local dev).
 * 
 * Uses 'db push' instead of 'migrate deploy' to avoid migration history issues.
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
try {
  execSync(`npx prisma db push --skip-generate --schema=${schemaPath}`, {
    stdio: "inherit",
    cwd: path.join(__dirname, ".."),
    env: process.env,
  });
} catch (error) {
  console.error("[prisma-migrate-deploy] db push failed, trying migrate deploy...");
  execSync(`npx prisma migrate deploy --schema=${schemaPath}`, {
    stdio: "inherit",
    cwd: path.join(__dirname, ".."),
    env: process.env,
  });
}
