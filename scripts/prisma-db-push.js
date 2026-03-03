#!/usr/bin/env node
/**
 * Push Prisma schema to database.
 * For PostgreSQL (Vercel), uses db push.
 * For SQLite (local dev), uses db push.
 */
const fs = require("fs");
const path = require("path");
const { execFileSync } = require("child_process");

const projectRoot = path.join(__dirname, "..");
const envPath = path.join(projectRoot, ".env.local");

// Load .env.local manually
if (fs.existsSync(envPath)) {
  const envContent = fs.readFileSync(envPath, "utf-8");
  const lines = envContent.split("\n");
  for (const line of lines) {
    if (line.trim() && !line.startsWith("#")) {
      const [key, ...valueParts] = line.split("=");
      const value = valueParts.join("=");
      if (key && value) {
        process.env[key.trim()] = value.trim();
      }
    }
  }
}

const dbUrl = process.env.DATABASE_URL || "file:./dev.db";
const isPostgres =
  dbUrl.startsWith("postgres://") ||
  dbUrl.startsWith("postgresql://") ||
  dbUrl.startsWith("prisma+postgres://");

const schema = isPostgres ? "schema.postgres.prisma" : "schema.prisma";

console.log("[prisma-db-push] DATABASE_URL:", dbUrl ? "SET" : "using fallback file:./dev.db");
console.log("[prisma-db-push] Schema:", schema);

try {
  const prismaPath = path.join(projectRoot, "node_modules", ".bin", "prisma");

  console.log(`[prisma-db-push] Running: prisma db push --skip-generate --accept-data-loss --schema=./prisma/${schema}`);

  // Call prisma via bash since it's a shell script
  execFileSync("bash", [prismaPath, "db", "push", "--skip-generate", "--accept-data-loss", `--schema=./prisma/${schema}`], {
    stdio: "inherit",
    cwd: projectRoot,
    env: process.env,
  });

  console.log("[prisma-db-push] SUCCESS");
} catch (err) {
  console.error("[prisma-db-push] FAILED");
  console.error(err.message);
  process.exit(1);
}
