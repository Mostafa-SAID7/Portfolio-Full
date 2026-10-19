/**
 * Absolute origin for PortfolioAPI requests.
 *
 * Resolution order (identical semantics on client and SSR):
 *   1. `VITE_API_BASE_URL` — optional explicit override. Read from
 *      `process.env` first on the server (runtime) and from `import.meta.env`
 *      (build-time) everywhere. Normalized the same way on both paths:
 *      trimmed, trailing slashes stripped, `https://` prefixed when the
 *      scheme is missing, and `""`/whitespace treated as "not configured".
 *      No hardcoded API domain ever appears in application code.
 *   2. Dev mode → the local API on :5039 (its CORS policy allows any origin).
 *   3. SSR (Node): `fetch()` requires an absolute URL, so we resolve the
 *      deployment's own origin from Vercel's runtime environment
 *      (`VERCEL_PROJECT_PRODUCTION_URL` / `VERCEL_URL`, present at build and
 *      request time) and call back through the same public `/api` rewrite —
 *      preview deployments therefore hit the same production API.
 *   4. SSR with nothing configured → log ONE warning and return `""`
 *      (same-origin), which makes every SSR fetch go through the `/api`
 *      rewrite from `vercel.json`. The fallback is explicit and observable,
 *      never silent.
 *
 * - Browser: `""` → same-origin `/api/...`, which the Vercel rewrites in
 *   `vercel.json` proxy to the API (the normal production setup).
 */

/** Trim, drop trailing slashes and guarantee an http(s) scheme. */
function normalizeOrigin(raw: string | undefined): string | null {
  const trimmed = raw?.trim().replace(/\/+$/, "") ?? "";
  // Empty string (or whitespace) means "not configured".
  if (!trimmed) return null;
  if (/^https?:\/\//i.test(trimmed)) return trimmed;
  if (trimmed.startsWith("//")) return `https:${trimmed}`;
  return `https://${trimmed}`;
}

/** `VITE_API_BASE_URL` from the runtime env (SSR) or the build env (both). */
function explicitOrigin(): string | null {
  if (typeof window === "undefined") {
    const proc = (globalThis as { process?: { env?: Record<string, string | undefined> } })
      .process;
    const runtime = normalizeOrigin(proc?.env?.["VITE_API_BASE_URL"]);
    if (runtime) return runtime;
  }
  return normalizeOrigin(import.meta.env["VITE_API_BASE_URL"] as string | undefined);
}

// The SSR fallback must be observable but not spam the logs: warn once per
// server process, then stay quiet.
let warnedMissingServerOrigin = false;

export function apiOrigin(): string {
  const explicit = explicitOrigin();
  if (explicit) return explicit;
  if (import.meta.env.DEV) return "http://localhost:5039";

  // Non-browser (SSR): Vercel exposes its own deployment origin, so preview
  // deployments resolve to the same production API by default.
  if (typeof window === "undefined") {
    const proc = (globalThis as { process?: { env?: Record<string, string | undefined> } })
      .process;
    const host =
      proc?.env?.["VERCEL_PROJECT_PRODUCTION_URL"] ?? proc?.env?.["VERCEL_URL"];
    const fromVercel = normalizeOrigin(host ? `https://${host}` : undefined);
    if (fromVercel) return fromVercel;

    if (!warnedMissingServerOrigin) {
      warnedMissingServerOrigin = true;
      console.warn(
        '[api-origin] no VERCEL_*/VITE_API_BASE_URL configured — SSR will use same-origin "" (rewrite-dependent)',
      );
    }
    return "";
  }

  // Browser: same-origin `/api/...` behind the Vercel rewrite.
  return "";
}
