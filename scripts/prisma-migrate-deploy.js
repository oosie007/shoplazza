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

console.log("[prisma-migrate-deploy] Environment check:");
console.log("[prisma-migrate-deploy] DATABASE_URL is", dbUrl ? "set" : "NOT SET");
if (dbUrl) {
  console.log("[prisma-migrate-deploy] DATABASE_URL starts with:", dbUrl.substring(0, 20) + (dbUrl.length > 20 ? "..." : ""));
}

const usePostgres =
  dbUrl.startsWith("postgres://") ||
  dbUrl.startsWith("postgresql://") ||
  dbUrl.startsWith("prisma+postgres://");

console.log("[prisma-migrate-deploy] usePostgres:", usePostgres);

if (!usePostgres) {
  console.log("[prisma-migrate-deploy] Skipping (not Postgres)");
  process.exit(0);
}

const schemaPath = path.join(__dirname, "..", "prisma", "schema.postgres.prisma");
const cwd = path.join(__dirname, "..");

console.log("[prisma-migrate-deploy] Schema path:", schemaPath);
console.log("[prisma-migrate-deploy] Working directory:", cwd);
console.log("[prisma-migrate-deploy] Running prisma db push...");

try {
  execSync(`npx prisma db push --skip-generate --schema=${schemaPath}`, {
    stdio: "inherit",
    cwd: cwd,
    env: process.env,
  });
  console.log("[prisma-migrate-deploy] ✓ db push succeeded");
  process.exit(0);
} catch (error) {
  console.error("[prisma-migrate-deploy] ✗ db push failed with error:");
  console.error("[prisma-migrate-deploy] Error code:", error.code);
  console.error("[prisma-migrate-deploy] Error status:", error.status);
  console.error("[prisma-migrate-deploy] Error message:", error.message);
  console.error("[prisma-migrate-deploy] Trying migrate deploy as fallback...");

  try {
    execSync(`npx prisma migrate deploy --schema=${schemaPath}`, {
      stdio: "inherit",
      cwd: cwd,
      env: process.env,
    });
    console.log("[prisma-migrate-deploy] ✓ migrate deploy succeeded");
    process.exit(0);
  } catch (fallbackError) {
    console.error("[prisma-migrate-deploy] ✗ migrate deploy also failed:");
    console.error("[prisma-migrate-deploy] Error code:", fallbackError.code);
    console.error("[prisma-migrate-deploy] Error status:", fallbackError.status);
    console.error("[prisma-migrate-deploy] Error message:", fallbackError.message);
    process.exit(1);
  }
}
