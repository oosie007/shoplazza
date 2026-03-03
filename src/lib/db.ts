import { PrismaClient } from "@/generated/prisma/client";

declare global {
  // eslint-disable-next-line no-var
  var prisma: PrismaClient | undefined;
}

function createPrisma(): PrismaClient {
  const url = process.env.DATABASE_URL;
  const isPostgres =
    url?.startsWith("postgres://") ||
    url?.startsWith("postgresql://") ||
    url?.startsWith("prisma+postgres://");
  if (isPostgres && url) {
    // Postgres schema uses engineType = "client"; adapter required (no Rust binary on Vercel).
    const { PrismaPg } = require("@prisma/adapter-pg");
    return new PrismaClient({ adapter: new PrismaPg({ connectionString: url }) });
  }
  return new PrismaClient();
}

export const prisma = global.prisma ?? createPrisma();

if (process.env.NODE_ENV !== "production") global.prisma = prisma;

