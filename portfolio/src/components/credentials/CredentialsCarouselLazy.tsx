import { Suspense, lazy } from "react";

/**
 * The awards carousel pulls in the heaviest animation code on the home page
 * (framer-motion AnimatePresence + the credential visuals). It is rendered
 * inside a <LazyIsland>, so its JS is never needed for first paint — loading it
 * as its own chunk keeps that weight out of the initial route bundle.
 *
 * SSR still streams the full markup (React resolves the dynamic import during
 * the server render), so the section stays in the server HTML.
 */
const Carousel = lazy(() =>
  import("./CredentialsCarousel").then((m) => ({ default: m.CredentialsCarousel })),
);

export function CredentialsCarouselLazy() {
  return (
    <Suspense fallback={<div className="section-y min-h-[60vh]" aria-hidden />}>
      <Carousel />
    </Suspense>
  );
}
