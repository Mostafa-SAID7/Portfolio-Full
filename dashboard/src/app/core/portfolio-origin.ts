import { isDevMode } from '@angular/core';

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
 * Resolve a stored imageUrl (absolute URL or portfolio-relative path) to a
 * loadable URL. Responsive images pick the requested WebP variant
 * (`640` for cards/thumbnails, `1280` for detail heroes).
 */
export function resolvePortfolioImage(
  imageUrl: string | null | undefined,
  variant: 640 | 1280 = 640
): string | null {
  if (!imageUrl) return null;
  if (/^(https?:)?\/\//.test(imageUrl) || imageUrl.startsWith('data:')) return imageUrl;

  const path = imageUrl.startsWith('/') ? imageUrl : `/${imageUrl}`;
  const base = stripExtension(path);
  const file = RESPONSIVE_BASES.has(base) ? `${base}-${variant}.webp` : path;
  return `${PORTFOLIO_ORIGIN}${file}`;
}
