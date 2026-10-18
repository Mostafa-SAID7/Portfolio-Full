/**
 * Centralised HTTP caching policy.
 *
 * The app is a static-ish portfolio rendered on the edge, so the whole caching
 * story is decided by response headers. Without them every asset falls back to
 * the browser's heuristic freshness (usually a revalidation round trip per
 * navigation) and shared caches/CDNs cannot store anything at all.
 *
 * Rules (most specific first):
 *  - server-function calls: `private, no-store` (never shared-cacheable).
 *  - any response carrying `Set-Cookie`: `private, no-store` — a shared cache
 *    must never hand one visitor's cookie to another.
 *  - `/assets/*`, `/_build/*`: content-hashed by Vite → immutable for a year.
 *  - static media by path (lottie/wasm/images/projects/api/fonts/icons) and
 *    by content type (image/font/video/audio/wasm): 1 day + a *bounded* SWR
 *    window, since the filenames are stable but the files can be replaced on
 *    a deploy — an unbounded SWR would let a year-old file be served.
 *  - crawler/config files and the CV (robots.txt, manifest, /cv/*): short TTL,
 *    they are edited far more often than they are re-hashed.
 *  - HTML documents: `no-cache` (store, but always revalidate) so a deploy is
 *    picked up immediately while still allowing 304s.
 *  - everything else: left untouched.
 */

const YEAR = 31_536_000;
const DAY = 86_400;
const HOUR = 3_600;
const WEEK = 604_800;

const IMMUTABLE = `public, max-age=${YEAR}, immutable`;
/** Stable filenames, replaceable on deploy: fresh for a day, bounded SWR. */
const MEDIA = `public, max-age=${DAY}, stale-while-revalidate=${WEEK}`;
/** Frequently edited, non-hashed text/documents. */
const SHORT = `public, max-age=${HOUR}, stale-while-revalidate=${DAY}`;
const DOCUMENT = "public, no-cache, must-revalidate";
const PRIVATE_DOCUMENT = "private, no-cache, must-revalidate";
const NO_STORE = "private, no-store";

const HASHED_PREFIXES = ["/assets/", "/_build/", "/_serverFn/assets/"];

/**
 * `/api/` here is a *static image* folder in `public/`, not a server route
 * namespace; real endpoints live under `/api/public/*` and are excluded below.
 */
const MEDIA_PREFIXES = [
  "/lottie/",
  "/wasm/",
  "/images/",
  "/projects/",
  "/api/",
  "/fonts/",
];

const MEDIA_FILES = new Set([
  "/favicon.ico",
  "/favicon.png",
  "/favicon.svg",
  "/apple-touch-icon.png",
]);

/** Short-TTL, non-hashed files that are edited between deploys. */
const SHORT_PREFIXES = ["/cv/"];
const SHORT_FILES = new Set([
  "/robots.txt",
  "/manifest.json",
  "/site.webmanifest",
  "/sitemap.xml",
]);

/** Content types that are always static bytes, whatever the URL looks like. */
const MEDIA_CONTENT_TYPES = ["image/", "font/", "video/", "audio/", "application/font"];

/**
 * Framework defaults that are safe to upgrade. Anything else means a handler
 * made an explicit decision, and that decision wins.
 */
const UPGRADABLE_DEFAULTS = new Set([
  "no-cache",
  "public, no-cache",
  "max-age=0",
  "public, max-age=0",
  "public, max-age=0, must-revalidate",
  "no-cache, must-revalidate",
]);

function isServerFnPath(pathname: string): boolean {
  return pathname.startsWith("/_serverFn/") && !pathname.startsWith("/_serverFn/assets/");
}

function isApiRoute(pathname: string): boolean {
  return pathname.startsWith("/api/public/") || pathname === "/api" || pathname === "/api/public";
}

function isMedia(pathname: string, contentType: string | null): boolean {
  if (MEDIA_PREFIXES.some((p) => pathname.startsWith(p))) return true;
  if (MEDIA_FILES.has(pathname)) return true;
  if (contentType && MEDIA_CONTENT_TYPES.some((t) => contentType.startsWith(t))) return true;
  if (contentType?.startsWith("application/wasm")) return true;
  return false;
}

function isShortLived(pathname: string): boolean {
  return SHORT_FILES.has(pathname) || SHORT_PREFIXES.some((p) => pathname.startsWith(p));
}

export function cacheControlFor(
  pathname: string,
  contentType: string | null,
  opts: { credentialed?: boolean } = {},
): string | null {
  if (isServerFnPath(pathname) || isApiRoute(pathname)) return NO_STORE;

  if (HASHED_PREFIXES.some((p) => pathname.startsWith(p))) return IMMUTABLE;
  if (isShortLived(pathname)) return SHORT;
  if (isMedia(pathname, contentType)) return MEDIA;

  if (contentType?.includes("text/html")) {
    return opts.credentialed ? PRIVATE_DOCUMENT : DOCUMENT;
  }

  return null;
}

/** Append a value to `Vary` without clobbering what the framework already set. */
function addVary(headers: Headers, value: string) {
  const existing = headers.get("vary");
  if (!existing) {
    headers.set("Vary", value);
    return;
  }
  const parts = existing.split(",").map((p) => p.trim().toLowerCase());
  if (parts.includes("*") || parts.includes(value.toLowerCase())) return;
  headers.set("Vary", `${existing}, ${value}`);
}

/**
 * Apply the policy without ever overriding a handler that already made an
 * explicit decision (e.g. `sitemap.xml`).
 */
export function withCacheHeaders(request: Request, response: Response): Response {
  const isRead = request.method === "GET" || request.method === "HEAD";
  if (response.status >= 400) return response;

  let pathname: string;
  try {
    pathname = new URL(request.url).pathname;
  } catch {
    return response;
  }

  // A response that sets a cookie is visitor-specific by definition, and an
  // unsafe method's response must not be reused — both are hard `no-store`.
  const setsCookie = response.headers.has("set-cookie");
  if (!isRead || setsCookie) {
    if (!setsCookie && !isServerFnPath(pathname)) return response;
    return applyHeaders(request, response, (headers) => headers.set("Cache-Control", NO_STORE));
  }

  const credentialed = request.headers.has("authorization") || request.headers.has("cookie");
  const value = cacheControlFor(pathname, response.headers.get("content-type"), { credentialed });
  if (!value) return response;

  // The static-asset layer only ever emits a framework default such as
  // `no-cache`, which forces a revalidation round trip per asset per
  // navigation; those are upgraded. Anything more specific is a deliberate
  // handler decision and is left alone.
  const existing = response.headers.get("cache-control");
  if (existing && !UPGRADABLE_DEFAULTS.has(existing.trim().toLowerCase())) return response;

  return applyHeaders(request, response, (headers) => {
    headers.set("Cache-Control", value);
    addVary(headers, "Accept-Encoding");
    // The document policy (public vs private) is derived from the request's
    // credentials, so a shared cache must key on them — otherwise one
    // visitor's variant can be replayed to another.
    if (value === DOCUMENT || value === PRIVATE_DOCUMENT) {
      addVary(headers, "Cookie");
    }
  });
}

/**
 * Mutate the response headers in place when the runtime allows it. Re-wrapping
 * a streamed SSR response (`new Response(response.body, ...)`) tears off the
 * original stream, which cancels the in-flight React render — in dev that
 * surfaces as `Error: aborted` / "render was aborted by the server". Only fall
 * back to a copy when the headers are genuinely immutable.
 */
function applyHeaders(
  request: Request,
  response: Response,
  mutate: (headers: Headers) => void,
): Response {
  try {
    mutate(response.headers);
    return response;
  } catch {
    const headers = new Headers(response.headers);
    mutate(headers);
    // 204/304/205 and HEAD must not carry a body.
    const bodyless =
      request.method === "HEAD" ||
      response.status === 204 ||
      response.status === 205 ||
      response.status === 304;
    return new Response(bodyless ? null : response.body, {
      status: response.status,
      statusText: response.statusText,
      headers,
    });
  }
}

