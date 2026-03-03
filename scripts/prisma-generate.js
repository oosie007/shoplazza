#!/usr/bin/env node
/**
 * Generate Prisma client with the correct schema.
 * Uses schema.postgres.prisma for PostgreSQL, schema.prisma for SQLite.
 *
 * This wrapper loads .env.local before calling Prisma to ensure DATABASE_URL is set.
 */
const fs = require("fs");
const path = require("path");
const { spawn } = require("child_process");

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

console.log("[prisma-generate] DATABASE_URL:", dbUrl ? "SET" : "using fallback file:./dev.db");
console.log("[prisma-generate] Schema:", schema);

try {
  // Use the prisma CLI directly via node
  const prismaCliPath = path.join(projectRoot, "node_modules", "prisma", "build", "index.js");

  console.log(`[prisma-generate] Running: prisma generate --schema=./prisma/${schema}`);

  const prisma = spawn("node", [prismaCliPath, "generate", `--schema=./prisma/${schema}`], {
    stdio: "inherit",
    cwd: projectRoot,
    env: process.env,
  });

  prisma.on("exit", (code) => {
    if (code === 0) {
      console.log("[prisma-generate] SUCCESS");
      process.exit(0);
    } else {
      console.error("[prisma-generate] FAILED");
      process.exit(code || 1);
    }
  });

  prisma.on("error", (err) => {
    console.error("[prisma-generate] FAILED");
    console.error(err.message);
    process.exit(1);
  });
} catch (err) {
  console.error("[prisma-generate] FAILED");
  console.error(err.message);
  process.exit(1);
}
