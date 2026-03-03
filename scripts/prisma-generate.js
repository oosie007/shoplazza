#!/usr/bin/env node
/**
 * Generate Prisma client with the correct schema.
 * Uses schema.postgres.prisma for PostgreSQL, schema.prisma for SQLite.
 */
const { execSync } = require("child_process");

const dbUrl = process.env.DATABASE_URL || "file:./dev.db";
const isPostgres = 
  dbUrl.startsWith("postgres://") ||
  dbUrl.startsWith("postgresql://") ||
  dbUrl.startsWith("prisma+postgres://");

const schema = isPostgres ? "schema.postgres.prisma" : "schema.prisma";

console.log("[prisma-generate] DATABASE_URL:", dbUrl ? "SET" : "using fallback file:./dev.db");
console.log("[prisma-generate] Schema:", schema);

try {
  execSync(`npx prisma generate --schema=./prisma/${schema}`, {
    stdio: "inherit",
    env: { ...process.env, DATABASE_URL: process.env.DATABASE_URL || "file:./dev.db" },
  });
  console.log("[prisma-generate] SUCCESS");
} catch (err) {
  console.error("[prisma-generate] FAILED");
  console.error(err.message);
  process.exit(1);
}
