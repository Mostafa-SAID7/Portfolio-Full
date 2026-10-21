import { isDevMode } from '@angular/core';

/** Response of POST api/upload/image (backend-owned media storage). */
export interface UploadedImage {
  url: string;
  fileName: string;
  size: number;
  /** "local" or "cloudinary" — stored on the project so it can be replaced later. */
  provider?: string | null;
  /** Cloudinary public id (null for local files). Persist with the project. */
  publicId?: string | null;
}

/**
 * True when a stored imageUrl points at the Cloudinary CDN. Such URLs are
 * delivered by Cloudinary itself — no API proxy, no portfolio origin.
 */
export function isCloudinaryImage(imageUrl: string | null | undefined): boolean {
  if (!imageUrl) return false;
  try {
    return new URL(imageUrl).pathname.includes('/image/upload/');
  } catch {
    return false;
  }
}

/**
 * Origin that serves the public portfolio. Project cover images are stored as
 * portfolio-relative logical paths (e.g. `/projects/react/TownTeam.png` — they
 * live in the portfolio app's public folder), so the dashboard resolves them
 * against the portfolio origin instead of its own.
 *
 * Dev: the portfolio dev server. Prod: same origin (reverse proxy serves both).
 */
export const PORTFOLIO_ORIGIN = isDevMode() ? 'http://localhost:5173' : '';

/**
 * Logical bases that ship as pre-optimised -640/-1280 WebP variants on disk.
 * Mirrors `portfolio/src/lib/image.ts` — the stored path is a logical name,
 * the real file always carries a size suffix.
 */
const RESPONSIVE_BASES = new Set([
  '/api/Marketing-Mvc',
  '/api/e-commerce-api',
  '/api/market-api',
  '/projects/angular/luxelle-landing',
  '/projects/angular/zyro-electric',
  '/projects/microservices/Inventory',
  '/projects/microservices/Shop',
  '/projects/react/E-Inventory',
  '/projects/react/HAVEN',
  '/projects/react/TownTeam',
  '/projects/react/UNDER-ARMOUR',
  '/projects/react/VELOCITY',
  '/projects/react/Vingo-Roll',
  '/projects/react/linea-jewelry',
  '/projects/react/little-boys',
  '/projects/react/luminabeaut',
  '/projects/react/luminabeauty',
  '/projects/react/ohanna'
]);

const stripExtension = (path: string) => path.replace(/\.(png|jpe?g|webp|avif)$/i, '');

/**
 * True for a cover that is HOSTED BY THE API (`/api/files/...`).
 *
 * Only those can be "missing on the server" in a way the admin can fix by
 * uploading again. A bundled portfolio cover (e.g. `/projects/angular/zyro-electric.png`)
 * is served by the portfolio app itself - the dashboard cannot even fetch it
 * (different app in production), so a failed preview there says nothing about
 * the live site and must not raise an alarm. Cloudinary covers are served by
 * the CDN and are equally out of the dashboard's control.
 */
export function isApiHostedImage(imageUrl: string | null | undefined): boolean {
  return !!imageUrl && imageUrl.startsWith('/api/files/') && !isCloudinaryImage(imageUrl);
}

/**
 * Default cover for a project that has no image yet. It is used until a cover
 * is uploaded through the dashboard - the API stays the source of truth, so the
 * placeholder never overrides a real image.
 */
export const PLACEHOLDER_COVER = '/placeholder.svg';

/**
 * Resolve a stored imageUrl (absolute URL or portfolio-relative path) to a
 * loadable URL. Responsive images pick the requested WebP variant
 * (`640` for cards/thumbnails, `1280` for detail heroes).
 */
export function resolvePortfolioImage(
  imageUrl: string | null | undefined,
  variant: 640 | 1280 = 640
): string | null {
  // No cover yet -> the shared placeholder image.
  if (!imageUrl) return PLACEHOLDER_COVER;

  // Cloudinary URLs are absolute and self-contained: the CDN does the
  // resizing, so the stored URL is consumed exactly as the API returned it.
  if (isCloudinaryImage(imageUrl)) return imageUrl;
  if (/^(https?:)?\/\//.test(imageUrl) || imageUrl.startsWith('data:')) return imageUrl;

  // Backend-owned uploads (`/api/files/...`) are served by the Portfolio API.
  // Reach them through the dashboard's own /api proxy (dev proxy.conf.json,
  // prod Vercel rewrite) — same-origin, HTTPS, no hardcoded API host.
  // Scoped to /api/files so legacy logical covers (/api/Marketing-Mvc etc.)
  // still resolve against the portfolio origin below.
  if (imageUrl.startsWith('/api/files/')) return imageUrl;

  const path = imageUrl.startsWith('/') ? imageUrl : `/${imageUrl}`;
  const base = stripExtension(path);
  const file = RESPONSIVE_BASES.has(base) ? `${base}-${variant}.webp` : path;
  return `${PORTFOLIO_ORIGIN}${file}`;
}
