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
console.log("[prisma-migrate-deploy] DATABASE_URL:", dbUrl ? "SET" : "NOT SET");

// Only run for PostgreSQL (Vercel)
const isPostgres = 
  dbUrl.startsWith("postgres://") ||
  dbUrl.startsWith("postgresql://") ||
  dbUrl.startsWith("prisma+postgres://");

console.log("[prisma-migrate-deploy] isPostgres:", isPostgres);

if (!isPostgres) {
  console.log("[prisma-migrate-deploy] Skipping - not PostgreSQL");
  process.exit(0);
}

console.log("[prisma-migrate-deploy] Running: npx prisma db push");

try {
  execSync("npx prisma db push --schema=./prisma/schema.postgres.prisma", {
    stdio: "inherit",
    cwd: process.cwd(),
  });
  console.log("[prisma-migrate-deploy] SUCCESS - db push completed");
  process.exit(0);
} catch (err) {
  console.error("[prisma-migrate-deploy] FAILED - db push error");
  console.error(err.message);
  process.exit(1);
}
