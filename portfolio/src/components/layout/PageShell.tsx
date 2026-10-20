import { type ReactNode, type Ref, lazy, Suspense } from "react";
import { Navbar } from "@/components/layout/Navbar";
import { cn } from "@/lib/utils";

// Footer is below the fold — defer its module evaluation and icon imports
// out of the hydration critical path entirely.
const Footer = lazy(() =>
  import("@/components/layout/Footer").then((m) => ({ default: m.Footer }))
);

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
  /** Disable the outer overflow-x-hidden wrapper. Useful when children rely on sticky positioning. */
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
        !disableOverflowX && "overflow-x-hidden",
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
