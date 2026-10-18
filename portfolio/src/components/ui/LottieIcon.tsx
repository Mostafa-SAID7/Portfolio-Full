import { useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import type { DotLottie } from "@lottiefiles/dotlottie-react";
import {
  canLoadLottiePlayer,
  getCachedLottie,
  loadLottieData,
  loadLottiePlayer,
  prefetchLottie,
} from "@/lib/lottie-cache";

/**
 * Performance-first Lottie wrapper.
 *
 * - The player bundle *and* the animation bytes are fetched only when the
 *   element gets close to the viewport (IntersectionObserver).
 * - Both are memoized module-level, so revisiting a route (or rendering the
 *   same icon twice) reuses the already-parsed bytes with zero extra requests.
 * - Playback pauses when offscreen or when the tab is hidden.
 * - `prefers-reduced-motion` short-circuits everything: static fallback only,
 *   nothing is downloaded.
 * - SSR-safe: nothing loads during render.
 */
export function LottieIcon({
  src,
  loop = true,
  className,
  fallback = null,
  speed = 1,
  playOnce = false,
  rootMargin = "0px",
  eager = false,
  activateOn = "visible",
}: {
  /** URL of the animation .lottie/JSON, e.g. `import url from "@/assets/x.lottie?url"` */
  src: string;
  loop?: boolean;
  className?: string;
  /** Static visual shown before load, and permanently when motion is reduced. */
  fallback?: ReactNode;
  speed?: number;
  /** Play a single time when first revealed (no loop, no replay). */
  playOnce?: boolean;
  /** How early to start fetching relative to the viewport. */
  rootMargin?: string;
  /** Skip the viewport gate (use for above-the-fold / hover-triggered icons). */
  eager?: boolean;
  /**
   * `"visible"` loads the runtime when the element scrolls into view.
   * `"interaction"` keeps the static poster on first visit and only downloads
   * the ~1.2 MB WASM runtime after deliberate pointer, touch, or keyboard focus.
   * Hover alone never starts a download.
   */
  activateOn?: "visible" | "interaction";
}) {
  const hostRef = useRef<HTMLDivElement | null>(null);
  const playerRef = useRef<DotLottie | null>(null);
  const playedRef = useRef(false);
  const startedRef = useRef(false);
  const [Player, setPlayer] = useState<
    typeof import("@lottiefiles/dotlottie-react").DotLottieReact | null
  >(null);
  const [data, setData] = useState<ArrayBuffer | null>(() => getCachedLottie(src) ?? null);

  useEffect(() => {
    setData(getCachedLottie(src) ?? null);
  }, [src]);

  useEffect(() => {
    const host = hostRef.current;
    if (!host) return;
    if (!canLoadLottiePlayer()) return;

    let cancelled = false;
    let idleHandle = 0;

    const start = () => {
      if (startedRef.current) return;
      startedRef.current = true;
      // Defer the ~1.2 MB runtime to idle time so it never competes with the
      // page's own critical resources.
      const run = () =>
        void Promise.all([loadLottiePlayer(), loadLottieData(src)])
          .then(([lib, buf]) => {
            if (cancelled) return;
            setPlayer(() => lib.DotLottieReact);
            setData(buf);
          })
          .catch(() => {
            startedRef.current = false;
          });
      const idle = (
        window as unknown as {
          requestIdleCallback?: (cb: () => void, o?: { timeout: number }) => number;
        }
      ).requestIdleCallback;
      idleHandle = idle ? idle(run, { timeout: 2000 }) : window.setTimeout(run, 200);
    };

    if (eager) start();

    const onIntent = () => start();
    if (activateOn === "interaction") {
      host.addEventListener("pointerdown", onIntent, { once: true });
      host.addEventListener("focusin", onIntent, { once: true });
    }

    const io = new IntersectionObserver(
      (entries) => {
        const visible = entries.some((e) => e.isIntersecting);
        if (visible && activateOn === "visible") start();
        const p = playerRef.current;
        if (!p) return;
        if (visible) {
          if (playOnce && playedRef.current) return;
          playedRef.current = true;
          p.play();
        } else {
          p.pause();
        }
      },
      { rootMargin },
    );
    io.observe(host);

    const onVisibility = () => {
      const p = playerRef.current;
      if (!p) return;
      if (document.hidden) p.pause();
      else p.play();
    };
    document.addEventListener("visibilitychange", onVisibility);

    return () => {
      cancelled = true;
      if (idleHandle) window.clearTimeout(idleHandle);
      io.disconnect();
      host.removeEventListener("pointerdown", onIntent);
      host.removeEventListener("focusin", onIntent);
      document.removeEventListener("visibilitychange", onVisibility);
    };
  }, [src, playOnce, rootMargin, eager, activateOn]);

  // dotLottie can take ownership of the buffer, so hand it a copy and keep the
  // cached original intact for the next mount. The copy is memoized on `data`
  // so its identity stays stable across unrelated re-renders — the player's
  // `useEffect(() => load({data}), [data])` would otherwise reload and restart
  // the animation every time a parent re-renders (theme toggle, i18n, …).
  const playerData = useMemo(() => (data ? data.slice(0) : null), [data]);

  return (
    <div ref={hostRef} className={className} aria-hidden="true">
      {Player && playerData ? (
        <Player
          data={playerData}
          loop={playOnce ? false : loop}
          autoplay
          speed={speed}
          dotLottieRefCallback={(d) => {
            playerRef.current = d;
          }}
          className="size-full"
          renderConfig={{ autoResize: true, freezeOnOffscreen: true }}
        />
      ) : (
        fallback
      )}
    </div>
  );
}

export { prefetchLottie };
