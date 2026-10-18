/**
 * Single source of truth for site-wide identity used by SEO, sitemap and
 * structured data. Change it here only.
 */
export const SITE = {
  name: "Mostafa Samir",
  role: "Senior Full Stack Engineer",
  titleSuffix: "Mostafa Samir | Senior Full Stack Engineer",
  /** Absolute origin, no trailing slash. Override with VITE_SITE_URL if needed. */
  url:
    (import.meta.env["VITE_SITE_URL"] as string | undefined)?.replace(/\/$/, "") ??
    "https://cozy-darkness.lovable.app",
  twitter: "@Lovable",
} as const;

/** Build an absolute URL when an origin is configured, else keep it relative. */
export function absoluteUrl(path: string): string {
  const normalized = path.startsWith("/") ? path : `/${path}`;
  return `${SITE.url}${normalized}`;
}
