/**
 * Responsive image helpers.
 *
 * Three kinds of source end up here, and each one needs different treatment:
 *
 * 1. Bundled screenshots in /public, referenced as logical paths
 *    (e.g. "/projects/react/HAVEN.png"). They ship as pre-optimised WebP/AVIF
 *    variants (<base>-640, <base>-1280) which are mapped onto srcSet entries.
 *
 * 2. Backend uploads delivered from the API itself (legacy local storage,
 *    "/api/files/..."). They are resolved against the API origin — the
 *    same-origin /api proxy in production, the local API in development.
 *
 * 3. Cloudinary CDN URLs returned by the API (current storage provider).
 *    The CDN transforms the image at delivery time, so srcSet entries are
 *    built by inserting width-based transformations into the URL; no local
 *    file and no pre-generated variant is involved. The delivery URL is
 *    always consumed as-is — it is never re-derived or hard-coded here, so a
 *    cloud rename or folder change in the backend needs no frontend change.
 */
import { apiOrigin } from "@/lib/api-origin";

/**
 * Default cover for a project that has no image yet. It is used until a cover
 * is uploaded through the dashboard - the API stays the source of truth, so the
 * placeholder never overrides a real image.
 */
export const PLACEHOLDER_COVER = "/images/placeholder.svg";

/** Bases that have -640/-1280 WebP variants on disk. */
const RESPONSIVE_BASES = new Set([
  "/api/Marketing-Mvc",
  "/api/e-commerce-api",
  "/api/market-api",
  "/projects/angular/luxelle-landing",
  "/projects/angular/zyro-electric",
  "/projects/microservices/Inventory",
  "/projects/microservices/Shop",
  "/projects/react/E-Inventory",
  "/projects/react/HAVEN",
  "/projects/react/TownTeam",
  "/projects/react/UNDER-ARMOUR",
  "/projects/react/VELOCITY",
  "/projects/react/Vingo-Roll",
  "/projects/react/linea-jewelry",
  "/projects/react/little-boys",
  "/projects/react/luminabeaut",
  "/projects/react/luminabeauty",
  "/projects/react/ohanna",
]);

interface ResolvedImage {
  src: string;
  srcSet?: string;
  /** AVIF variants (~30% smaller than the WebP set) for <picture> sources. */
  avifSrcSet?: string;
}

const stripExtension = (path: string) => path.replace(/\.(png|jpe?g|webp|avif)$/i, "");

/** Path segment that marks a Cloudinary delivery URL. */
const CLOUDINARY_UPLOAD = "/image/upload/";

/** Width ladder for Cloudinary candidates (cards get 320-768, heroes go wider). */
const CLOUDINARY_WIDTHS = [320, 640, 768, 1024, 1280] as const;

/** True when an absolute URL points at Cloudinary's image delivery endpoint. */
export function isCloudinaryUrl(path: string | undefined): boolean {
  if (!path) return false;
  try {
    return new URL(path).pathname.includes(CLOUDINARY_UPLOAD);
  } catch {
    return false;
  }
}

/**
 * Splits a Cloudinary URL into the parts needed to inject a transformation:
 *   https://res.cloudinary.com/<cloud>/image/upload/  +  v123/portfolio/.../cover.webp
 * The version segment is kept so a replaced cover never serves a cached copy.
 */
function splitCloudinaryUrl(url: string) {
  const parsed = new URL(url);
  const markerIndex = parsed.pathname.indexOf(CLOUDINARY_UPLOAD);
  if (markerIndex < 0) return null;

  const head = `${parsed.origin}${parsed.pathname.slice(0, markerIndex + CLOUDINARY_UPLOAD.length)}`;
  const tail = parsed.pathname.slice(markerIndex + CLOUDINARY_UPLOAD.length);
  return { head, tail };
}

/**
 * Builds a Cloudinary delivery URL with an extra transformation inserted.
 * `f_auto`/`q_auto` let the CDN pick format and quality per browser; `c_limit`
 * never upscales, so a small upload is delivered at its natural size.
 */
function cloudinaryVariant(url: string, transformation: string): string | null {
  const parts = splitCloudinaryUrl(url);
  if (!parts) return null;

  // An already-transformed URL would end up with a second transformation
  // component - leave it alone instead of guessing.
  const firstSegment = parts.tail.split("/")[0] ?? "";
  if (firstSegment.includes(",") || firstSegment.includes("=")) return null;

  return `${parts.head}${transformation}/${parts.tail}`;
}

/** Delivery URLs for one Cloudinary source: responsive ladder + best-format src. */
function resolveCloudinary(path: string): ResolvedImage {
  const src = cloudinaryVariant(path, "f_auto,q_auto") ?? path;

  const srcSet = CLOUDINARY_WIDTHS.map((width) => {
    const candidate = cloudinaryVariant(path, `f_auto,q_auto,c_limit,w_${width}`);
    return candidate ? `${candidate} ${width}w` : null;
  })
    .filter((entry): entry is string => entry !== null)
    .join(", ");

  // f_auto already negotiates AVIF/WebP per browser, so a separate AVIF
  // <source> would only duplicate what the CDN does better.
  return { src, srcSet: srcSet || undefined };
}

export function resolveImage(path: string | undefined): ResolvedImage | null {
  if (!path) return null;

  // Cloudinary CDN URLs: build the responsive ladder from the delivery URL.
  if (isCloudinaryUrl(path)) {
    return resolveCloudinary(path);
  }

  // Absolute URLs and inline data render as-is.
  if (/^(https?:)?\/\//.test(path) || path.startsWith("data:")) {
    return { src: path };
  }

  // Backend-owned upload (`/api/files/...`): resolve against the API origin.
  // Scoped to /api/files specifically — legacy logical covers (/api/Marketing-Mvc etc.)
  // are portfolio public assets and must keep their existing handling.
  if (path.startsWith("/api/files/")) {
    return { src: `${apiOrigin()}${path}` };
  }

  const base = stripExtension(path);
  if (RESPONSIVE_BASES.has(base)) {
    return {
      src: `${base}-1280.webp`,
      srcSet: `${base}-640.webp 640w, ${base}-1280.webp 1280w`,
      avifSrcSet: `${base}-640.avif 640w, ${base}-1280.avif 1280w`,
    };
  }

  return { src: path };
}

/** `sizes` presets matching the layout slots used across the app. */
export const IMAGE_SIZES = {
  /** 3-up card grid capped at a 6xl container. */
  card: "(min-width: 1024px) 384px, (min-width: 640px) 50vw, 100vw",
  /** Narrow thumbnail in the list view. */
  thumb: "(min-width: 640px) 220px, 100vw",
  /** Full-bleed project hero. */
  hero: "100vw",
} as const;
