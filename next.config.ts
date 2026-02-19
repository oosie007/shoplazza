import type { NextConfig } from "next";
import path from "path";
import { fileURLToPath } from "url";

// Pin project root so Turbopack/PostCSS resolve tailwindcss from this app (fixes "Can't resolve 'tailwindcss' in '.../dev'" when Cursor workspace root is parent)
const getProjectRoot = (): string => {
  if (typeof __dirname !== "undefined") return __dirname;
  try {
    return path.dirname(fileURLToPath(import.meta.url));
  } catch {
    const cwd = process.cwd();
    return cwd.endsWith("shoplaza") ? cwd : path.join(cwd, "shoplaza");
  }
};

// Use CSP frame-ancestors only (no X-Frame-Options) so Shoplazza can embed / and /admin; middleware sets frame-ancestors per path.
const securityHeaders = [
  { key: "X-Content-Type-Options", value: "nosniff" },
  { key: "Referrer-Policy", value: "strict-origin-when-cross-origin" },
  { key: "Permissions-Policy", value: "camera=(), microphone=(), geolocation=()" },
];

// Headers for / and /admin: allow Shoplazza to embed. Parent frame may be *.myshoplaza.com or partners/angora.
const frameAncestorsCsp = {
  key: "Content-Security-Policy",
  value:
    "frame-ancestors 'self' https://*.myshoplaza.com https://*.shoplazza.com https://partners.shoplazza.com https://*.shoplaza.com",
};

const projectRoot = getProjectRoot();

const nextConfig: NextConfig = {
  turbopack: {
    root: projectRoot,
    // Force tailwindcss to resolve from this app when PostCSS runs (e.g. from parent workspace root)
    resolveAlias: {
      tailwindcss: path.join(projectRoot, "node_modules", "tailwindcss"),
    },
  },
  // Allow dev requests from ngrok when app is opened via tunnel or embedded in Shoplazza admin iframe
  allowedDevOrigins: [
    "cb35-102-204-245-19.ngrok-free.app",
    "*.ngrok-free.app",
    "*.ngrok.io",
    "oostest.myshoplaza.com",
  ],
  async headers() {
    return [
      {
        source: "/",
        headers: [...securityHeaders, frameAncestorsCsp],
      },
      {
        source: "/admin",
        headers: [...securityHeaders, frameAncestorsCsp],
      },
      {
        source: "/admin/(.*)",
        headers: [...securityHeaders, frameAncestorsCsp],
      },
      { source: "/(.*)", headers: securityHeaders },
    ];
  },
};

export default nextConfig;
