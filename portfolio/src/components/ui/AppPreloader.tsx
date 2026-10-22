import { useEffect, useRef, useState } from "react";
import { SESSION_KEYS, sessionFlags } from "@/lib/storage";

/** Never flash: once shown, the splash stays up at least this long. */
const MIN_VISIBLE_MS = 100;
/** Hard ceiling — a slow asset must never trap the visitor behind the splash. */
const MAX_VISIBLE_MS = 900;
/** Webfonts must never gate the splash for longer than this. */
const FONTS_TIMEOUT_MS = 200;
const FADE_MS = 150;
/**
 * Safety valve for the pre-paint scroll lock: if the bundle never hydrates
 * (blocked script, crash before the effect), the document must still become
 * scrollable. The component clears the attribute far earlier on the happy path.
 */
const SCROLL_LOCK_CEILING_MS = 4000;

/**
 * Runs before first paint. If this session already booted the app (SPA
 * revisit, back/forward, reload within the tab) the splash is hidden by CSS
 * *before* it can paint, so returning visitors never see it twice. Reduced
 * motion users skip it entirely.
 *
 * The scroll lock, however, is armed on EVERY full load: the page is fully
 * server-rendered, but the webfonts and the hydrated content still settle for
 * a moment. Without the lock the visitor sees a scrollbar first and the whole
 * layout shifts when the content and the fonts land. The lock is released by
 * the component once the document has settled (see below) and, at the very
 * latest, by the safety timeout — a blocked script can never trap the visitor
 * on an unscrollable page.
 */
export const preloaderBootstrapScript = `(function(){try{
var r=document.documentElement;
var seen=false;try{seen=sessionStorage.getItem("${SESSION_KEYS.preloaded}")==="1";}catch(e){}
var rm=window.matchMedia&&window.matchMedia("(prefers-reduced-motion: reduce)").matches;
r.setAttribute("data-preloading","1");
setTimeout(function(){r.removeAttribute("data-preloading");},${SCROLL_LOCK_CEILING_MS});
if(seen||rm){r.setAttribute("data-preloaded","1");return;}
}catch(e){}})();`;

/**
 * First-load splash screen.
 *
 * Rendered during SSR so it is part of the very first paint (no white flash,
 * no layout shift). It dismisses on the earliest of:
 *  - boot animation + webfonts ready, past the minimum visible time
 *  - the safety timeout
 */
export function AppPreloader() {
  const [mounted, setMounted] = useState(true);
  const [leaving, setLeaving] = useState(false);
  const [progress, setProgress] = useState(8);
  const doneRef = useRef(false);

  useEffect(() => {
    const reduced = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    if (sessionFlags.preloaderSeen() || reduced) {
      setMounted(false);
      return;
    }

    document.documentElement.setAttribute("data-preloading", "1");
    const startedAt = performance.now();
    let raf = 0;

    // Ease towards 92% while we wait; the real "ready" signal finishes it.
    const tick = () => {
      setProgress((p) => (p >= 92 ? p : p + (92 - p) * 0.045 + 0.35));
      raf = requestAnimationFrame(tick);
    };
    raf = requestAnimationFrame(tick);

    const finish = () => {
      if (doneRef.current) return;
      doneRef.current = true;
      cancelAnimationFrame(raf);
      setProgress(100);
      const wait = Math.max(0, MIN_VISIBLE_MS - (performance.now() - startedAt));
      window.setTimeout(() => {
        setLeaving(true);
        window.setTimeout(() => {
          setMounted(false);
          sessionFlags.markPreloaderSeen();
        }, FADE_MS);
      }, wait);
    };

    // Fonts only, and never for long: the animation runtime now loads lazily
    // after hydration instead of gating first paint.
    const ready = Promise.race([
      document.fonts?.ready ?? Promise.resolve(),
      new Promise((r) => window.setTimeout(r, FONTS_TIMEOUT_MS)),
    ]);
    void ready.then(finish);
    const safety = window.setTimeout(finish, MAX_VISIBLE_MS);

    return () => {
      cancelAnimationFrame(raf);
      window.clearTimeout(safety);
    };
  }, []);

  /**
   * Release the pre-paint scroll lock once the splash is gone AND the
   * document has settled (the webfonts swapped in). Releasing it earlier
   * makes the scrollbar pop into view while the content is still moving —
   * exactly the flash the lock exists to prevent. The safety timeout and the
   * bootstrap script's ceiling guarantee the page always becomes scrollable.
   */
  useEffect(() => {
    if (mounted) return;
    const root = document.documentElement;
    let released = false;
    const release = () => {
      if (released) return;
      released = true;
      root.removeAttribute("data-preloading");
    };
    const fontsReady = Promise.race([
      document.fonts?.ready ?? Promise.resolve(),
      new Promise((resolve) => window.setTimeout(resolve, FONTS_TIMEOUT_MS)),
    ]);
    void fontsReady.then(release);
    const safety = window.setTimeout(release, MAX_VISIBLE_MS);
    return () => {
      window.clearTimeout(safety);
    };
  }, [mounted]);

  if (!mounted) return null;

  return (
    <div
      className="app-preloader"
      data-leaving={leaving ? "1" : undefined}
      role="status"
      aria-live="polite"
      aria-label="Loading"
    >
      <div className="app-preloader__inner">
        <div className="app-preloader__art">
          <span className="app-preloader__mark">MS</span>
        </div>

        <p className="app-preloader__name">Mostafa Samir</p>
        <p className="app-preloader__role">Marketplace Systems Architect</p>

        <div className="app-preloader__bar">
          <span style={{ width: `${Math.min(100, progress)}%` }} />
        </div>
      </div>
    </div>
  );
}
