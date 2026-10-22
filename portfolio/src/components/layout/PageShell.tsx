import { type ReactNode, type Ref, lazy, Suspense } from "react";
import { Navbar } from "@/components/layout/Navbar";
import { cn } from "@/lib/utils";

// Footer is below the fold — its module evaluation and icon imports stay out
// of the hydration critical path, but the promise is KICKED OFF at module
// scope so it is resolved by the time React mounts the Suspense boundary: a
// resolved promise renders synchronously, which removes the "boundary
// hydrated after the language state settled" race entirely (it was the one
// source of hydration-mismatch warnings on first Arabic paint). SSR resolves
// the same promise (in-process, near-instant) so the footer still streams.
const footerModulePromise: Promise<typeof import("@/components/layout/Footer")> =
  import("@/components/layout/Footer");
const Footer = lazy(() => footerModulePromise.then((m) => ({ default: m.Footer })));

interface PageShellProps {
  children: ReactNode;
  /** Wrap the content in a centered container. Disable for full-bleed pages. */
  contained?: boolean;
  /** Apply the standard top/bottom page padding. Disable for hero-first pages. */
  padded?: boolean;
  /** Extra classes for the <main> landmark. */
  className?: string;
  /** Optional ref for pages with pointer-driven main content. */
  mainRef?: Ref<HTMLElement>;
  /** Skip the outer horizontal clipping wrapper. */
  disableOverflowX?: boolean;
}

/**
 * The single page chrome: navbar, main landmark and footer.
 * Routes render content only — never their own navbar/footer.
 */
export function PageShell({
  children,
  contained = true,
  padded = true,
  className,
  mainRef,
  disableOverflowX = false,
}: PageShellProps) {
  return (
    <div
      className={cn(
        "flex min-h-screen select-none flex-col bg-background text-foreground",
        // `clip`, not `hidden`: `hidden` would put this wrapper into
        // scroll-container mode and let a second scrollbar appear beside the
        // page one, and it is also why sticky children had to opt out below.
        !disableOverflowX && "overflow-x-clip",
      )}
    >
      <Navbar />
      <main
        ref={mainRef}
        className={cn(
          "flex-1",
          padded && "nav-offset pb-16",
          contained && "container-page page-gutter",
          className,
        )}
      >
        {children}
      </main>
      <Suspense fallback={null}>
        <Footer />
      </Suspense>
    </div>
  );
}
