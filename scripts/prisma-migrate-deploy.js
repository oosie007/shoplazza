#!/usr/bin/env node
/**
 * Sync Prisma schema to database using 'db push' ONLY.
 * For PostgreSQL (Vercel), syncs schema.postgres.prisma directly.
 * For SQLite (local dev), skips this entirely.
 *
 * NEVER uses 'migrate deploy' - only 'db push' for schema sync.
 */
const path = require("path");
const { execSync } = require("child_process");

const dbUrl = process.env.DATABASE_URL || "";

console.log("[prisma-migrate-deploy] START");
console.log("[prisma-migrate-deploy] DATABASE_URL:", dbUrl ? "SET (length: " + dbUrl.length + ")" : "NOT SET");

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
  console.log("[prisma-migrate-deploy] Executing: npx prisma db push --skip-generate --schema=./prisma/schema.postgres.prisma");
  execSync("npx prisma db push --skip-generate --schema=./prisma/schema.postgres.prisma", {
    stdio: "inherit",
    cwd: process.cwd(),
  });
  console.log("[prisma-migrate-deploy] SUCCESS - db push completed");
  process.exit(0);
} catch (err) {
  console.error("[prisma-migrate-deploy] FAILED - db push exited with code:", err.status);
  console.error("[prisma-migrate-deploy] This may be a database connectivity or schema issue");
  console.error("[prisma-migrate-deploy] Full error:", err.message);

  // If db push fails, it's likely a real error (no database, wrong schema, etc.)
  // We should NOT try migrate deploy - just fail cleanly
  console.error("[prisma-migrate-deploy] Exiting with error code 1");
  process.exit(1);
}
