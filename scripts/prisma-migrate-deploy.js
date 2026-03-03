#!/usr/bin/env node
/**
 * Sync Prisma schema to database using 'db push' ONLY.
 * For PostgreSQL (Vercel), syncs schema.postgres.prisma directly.
 * For SQLite (local dev), skips this entirely.
 *
 * NEVER uses 'migrate deploy' - only 'db push' for schema sync.
 */
const path = require("path");
const fs = require("fs");
const { execSync } = require("child_process");

const dbUrl = process.env.DATABASE_URL || "";

console.log("\n========================================");
console.log("[prisma-migrate-deploy] SCRIPT STARTED");
console.log("========================================");
console.log("[prisma-migrate-deploy] START");
console.log("[prisma-migrate-deploy] DATABASE_URL:", dbUrl ? "SET (length: " + dbUrl.length + ")" : "NOT SET");
console.log("[prisma-migrate-deploy] Node version:", process.version);
console.log("[prisma-migrate-deploy] CWD:", process.cwd());

// Only run for PostgreSQL (Vercel)
const isPostgres =
  dbUrl.startsWith("postgres://") ||
  dbUrl.startsWith("postgresql://") ||
  dbUrl.startsWith("prisma+postgres://");

console.log("[prisma-migrate-deploy] isPostgres:", isPostgres);

if (!isPostgres) {
  console.log("[prisma-migrate-deploy] Skipping - not PostgreSQL. DATABASE_URL does not match postgres patterns.");
  console.log("[prisma-migrate-deploy] Expected one of: postgres://, postgresql://, prisma+postgres://");
  process.exit(0);
}

console.log("[prisma-migrate-deploy] Running: npx prisma db push --schema=./prisma/schema.postgres.prisma");
console.log("[prisma-migrate-deploy] Working directory:", process.cwd());

try {
  console.log("[prisma-migrate-deploy] ===================================");
  console.log("[prisma-migrate-deploy] Executing: npx prisma db push");
  console.log("[prisma-migrate-deploy] With flags: --skip-generate --schema=./prisma/schema.postgres.prisma");
  console.log("[prisma-migrate-deploy] ===================================");

  execSync("npx prisma db push --skip-generate --schema=./prisma/schema.postgres.prisma", {
    stdio: "inherit",
    cwd: process.cwd(),
  });

  console.log("\n========================================");
  console.log("[prisma-migrate-deploy] SUCCESS - db push completed");
  console.log("========================================\n");
  process.exit(0);
} catch (err) {
  console.error("\n========================================");
  console.error("[prisma-migrate-deploy] FAILED - db push error");
  console.error("========================================");
  console.error("[prisma-migrate-deploy] Exit code:", err.status);
  console.error("[prisma-migrate-deploy] Message:", err.message);
  console.error("========================================\n");

  // If db push fails, DO NOT attempt migrate deploy
  // This is a real error that needs to be fixed (database issue, schema problem, etc.)
  process.exit(1);
}
