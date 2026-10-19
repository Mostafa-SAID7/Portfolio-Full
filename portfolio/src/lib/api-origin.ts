/**
 * Absolute origin for PortfolioAPI requests.
 *
 * - Browser: `""` → same-origin `/api/...`, which the Vercel rewrites in
 *   `vercel.json` proxy to the API. Dev mode talks straight to the local API
 *   on :5039 (its CORS policy allows any origin).
 * - SSR (Node): `fetch()` requires an absolute URL, so we resolve the
 *   deployment's own origin from Vercel's runtime environment
 *   (`VERCEL_PROJECT_PRODUCTION_URL` / `VERCEL_URL`, present at build and
 *   request time) and call back through the same public `/api` rewrite.
 *   No hardcoded API domain ever appears in application code.
 *
 * `VITE_API_BASE_URL` (optional) overrides everything, in both environments.
 */
export function apiOrigin(): string {
  const explicit = import.meta.env["VITE_API_BASE_URL"] as string | undefined;
  if (explicit) return explicit.replace(/\/+$/, "");
  if (import.meta.env.DEV) return "http://localhost:5039";
  // Non-browser (SSR): Vercel exposes its own deployment origin.
  if (typeof window === "undefined") {
    const proc = (globalThis as { process?: { env?: Record<string, string | undefined> } }).process;
    const host = proc?.env?.["VERCEL_PROJECT_PRODUCTION_URL"] ?? proc?.env?.["VERCEL_URL"];
    if (host) return `https://${host}`;
    return "";
  }
  return "";
}
