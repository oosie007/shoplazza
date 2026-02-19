#!/usr/bin/env node
/**
 * Run prisma generate with the correct schema:
 * - If DATABASE_URL looks like Postgres (postgres://, postgresql://, prisma+postgres://) → use schema.postgres.prisma
 * - Otherwise (e.g. file:./dev.db for SQLite) → use schema.prisma (local testing)
 */
const path = require("path");
const { execSync } = require("child_process");

const dbUrl = process.env.DATABASE_URL || "";
const usePostgres =
  dbUrl.startsWith("postgres://") ||
  dbUrl.startsWith("postgresql://") ||
  dbUrl.startsWith("prisma+postgres://");

const schemaFile = usePostgres ? "schema.postgres.prisma" : "schema.prisma";
const schemaPath = path.join(__dirname, "..", "prisma", schemaFile);

console.log("[prisma-generate] Using", schemaFile);
execSync(`npx prisma generate --schema=${schemaPath}`, {
  stdio: "inherit",
  cwd: path.join(__dirname, ".."),
});
