import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";

/**
 * Security headers: clickjacking, XSS, MIME sniffing, and optional HSTS.
 * CSP allows same-origin scripts and styles; for nonce-based CSP, add x-nonce and use it in layout.
 * /admin is embedded in Shoplazza admin iframe, so we allow frame-ancestors from *.myshoplaza.com there.
 */
const cspBase = [
  "default-src 'self'",
  // Allow inline/eval in dev so Next.js scripts and HMR work inside the Shoplazza iframe.
  // For stricter production CSP, these can be revisited with nonces.
  "script-src 'self' 'unsafe-inline' 'unsafe-eval'",
  "style-src 'self' 'unsafe-inline'",
  "img-src 'self' blob: data: https:",
  "font-src 'self'",
  "connect-src 'self' https: wss:",
  "object-src 'none'",
  "base-uri 'self'",
  "form-action 'self'",
  "upgrade-insecure-requests",
];

const securityHeadersBase = {
  "X-Content-Type-Options": "nosniff",
  "Referrer-Policy": "strict-origin-when-cross-origin",
  "Permissions-Policy": "camera=(), microphone=(), geolocation=()",
};

/** In-memory rate limit: key -> timestamps in last windowMs. For production, use @upstash/ratelimit + Redis. */
const rateLimitStore = new Map<string, number[]>();
const RATE_LIMIT_WINDOW_MS = 60_000;
const AUTH_LIMIT = 30;
const SETTINGS_LIMIT = 60;

function getClientId(request: NextRequest): string {
  return (
    request.headers.get("x-forwarded-for")?.split(",")[0]?.trim() ||
    request.headers.get("x-real-ip") ||
    "unknown"
  );
}

function isRateLimited(key: string, limit: number): boolean {
  const now = Date.now();
  let timestamps = rateLimitStore.get(key) ?? [];
  timestamps = timestamps.filter((t) => now - t < RATE_LIMIT_WINDOW_MS);
  if (timestamps.length >= limit) return true;
  timestamps.push(now);
  rateLimitStore.set(key, timestamps);
  return false;
}

export function middleware(request: NextRequest) {
  const path = request.nextUrl.pathname;

  // Rate limit auth and settings to mitigate brute-force and abuse
  if (path.startsWith("/api/auth")) {
    const key = `auth:${getClientId(request)}`;
    if (isRateLimited(key, AUTH_LIMIT)) {
      return NextResponse.json({ error: "Too many requests" }, { status: 429 });
    }
  }
  if (path.startsWith("/api/settings") && request.method !== "GET") {
    const key = `settings:${getClientId(request)}`;
    if (isRateLimited(key, SETTINGS_LIMIT)) {
      return NextResponse.json({ error: "Too many requests" }, { status: 429 });
    }
  }

  const response = NextResponse.next();

  // Allow Shoplazza admin to embed our app (iframe); Shoplazza may load root "/" or "/admin"
  const isEmbeddedAdmin = path === "/" || path.startsWith("/admin");
  const csp =
    cspBase.join("; ") +
    "; " +
    (isEmbeddedAdmin
      ? "frame-ancestors 'self' https://*.myshoplaza.com"
      : "frame-ancestors 'none'");

  response.headers.set("Content-Security-Policy", csp);
  // Only set X-Frame-Options when not embedded (CSP frame-ancestors overrides it when both present; omit for /admin so Shoplazza can embed)
  if (!isEmbeddedAdmin) {
    response.headers.set("X-Frame-Options", "DENY");
  }
  Object.entries(securityHeadersBase).forEach(([key, value]) => {
    response.headers.set(key, value);
  });

  // HSTS: only when served over HTTPS (e.g. production)
  const proto = request.headers.get("x-forwarded-proto") ?? request.nextUrl.protocol.replace(":", "");
  if (proto === "https") {
    response.headers.set("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
  }

  return response;
}

export const config = {
  matcher: [
    "/((?!_next/static|_next/image|favicon.ico|checkout-widget\\.js).*)",
  ],
};
