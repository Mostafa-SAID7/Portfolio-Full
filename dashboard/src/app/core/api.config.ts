/**
 * The Angular dev server proxies `/api` to the Portfolio API
 * (http://localhost:5039) via proxy.conf.json, so in development requests
 * are same-origin and CORS-free.
 *
 * Versioned namespace (issue #24): every JSON endpoint lives under
 * `/api/v1/...` — the API also serves the legacy unversioned paths as
 * deprecated aliases (see docs/API_VERSIONING.md). Static file serving
 * (`/api/files/...`) and the Vercel `/api/:path*` rewrite are unaffected.
 *
 * In production, deploy the API behind the same domain (reverse proxy /
 * platform rewrite) and keep this relative; if the API lives on another
 * domain, replace it with the absolute URL at build time.
 */
export const API_URL = '/api/v1';
