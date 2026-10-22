/**
 * Shared, module-level Lottie cache.
 *
 * Two things are memoized for the whole session (so they survive route changes
 * and component unmounts):
 *  1. the dotLottie player bundle import
 *  2. the fetched animation bytes, keyed by src
 *
 * Everything is promise-cached, so N components asking for the same asset at
 * the same time produce exactly one network request.
 */

type PlayerModule = typeof import("@lottiefiles/dotlottie-react");

let playerPromise: Promise<PlayerModule> | null = null;

/** Self-hosted runtime: avoids a cross-origin DNS+TLS round trip to a CDN. */
const WASM_URL = "/wasm/dotlottie-player.wasm";

let wasmPreloadInjected = false;

/**
 * Put the 1.2 MB runtime into the browser's high-priority fetch queue as
 * soon as a Lottie exists on the page.
 *
 * The runtime used to be fetched from an idle callback, which meant the
 * artwork simply was not there for the first second or two after it scrolled
 * into view. Preloading it in parallel with the player bundle makes the
 * animation appear as soon as its (tiny) bytes are parsed.
 */
export function preloadLottieRuntime(): void {
  if (wasmPreloadInjected || typeof document === "undefined") return;
  wasmPreloadInjected = true;
  try {
    const link = document.createElement("link");
    link.rel = "preload";
    link.as = "fetch";
    link.href = WASM_URL;
    link.crossOrigin = "anonymous";
    document.head.appendChild(link);
  } catch {
    /* preload is an optimisation — the runtime still loads on demand */
  }
}

/**
 * The dotLottie runtime is a ~1.2 MB WebAssembly download for purely
 * decorative artwork. Skip it entirely when the user asked for less motion,
 * is on a metered/slow connection, or on a low-memory device — the static
 * fallback is shown instead.
 */
export function canLoadLottiePlayer(): boolean {
  if (typeof window === "undefined") return false;
  if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) return false;
  const nav = navigator as Navigator & {
    connection?: { saveData?: boolean; effectiveType?: string };
    deviceMemory?: number;
  };
  if (nav.connection?.saveData) return false;
  const type = nav.connection?.effectiveType;
  if (type && type !== "4g") return false;
  if (typeof nav.deviceMemory === "number" && nav.deviceMemory > 0 && nav.deviceMemory < 4)
    return false;
  return true;
}

export function loadLottiePlayer(): Promise<PlayerModule> {
  if (!playerPromise) {
    // Start the heavy download right away — in parallel with the JS bundle.
    preloadLottieRuntime();
    playerPromise = import("@lottiefiles/dotlottie-react").then((mod) => {
      mod.setWasmUrl(WASM_URL);
      // Start fetching + compiling the ~1.2 MB WASM immediately, in parallel
      // with the React component mounting, so the first animation doesn't
      // wait for it sequentially (import → mount → fetch → compile → play).
      //
      // `DotLottie.preload()` is the library's own supported way to do this
      // (it calls the shared WASM manager's `load()`), but the React package
      // only re-exports it in its *types* — at runtime `DotLottie` is not on
      // the module — so import it from the underlying web package, which is
      // the same singleton the player already uses.
      void import("@lottiefiles/dotlottie-web")
        .then((web) => web.DotLottie.preload())
        .catch(() => {});
      return mod;
    });
  }
  return playerPromise;
}

const dataCache = new Map<string, Promise<ArrayBuffer>>();
const resolved = new Map<string, ArrayBuffer>();

/**
 * Decoded animation bytes are kept in memory for instant re-mounts, but the
 * map must stay bounded: a long session that visits every route would
 * otherwise pin every `.lottie` buffer for the lifetime of the tab.
 * Simple LRU — re-reading an entry refreshes its position.
 */
const MAX_RESOLVED = 8;

function touch(src: string, buf: ArrayBuffer) {
  resolved.delete(src);
  resolved.set(src, buf);
  while (resolved.size > MAX_RESOLVED) {
    const oldest = resolved.keys().next().value;
    if (oldest === undefined) break;
    resolved.delete(oldest);
    // The in-flight promise cache is dropped too, so a later mount re-fetches
    // from the HTTP cache (cheap) instead of holding the bytes forever.
    dataCache.delete(oldest);
  }
}

export function getCachedLottie(src: string): ArrayBuffer | undefined {
  const buf = resolved.get(src);
  if (buf) touch(src, buf);
  return buf;
}

export function loadLottieData(src: string): Promise<ArrayBuffer> {
  let p = dataCache.get(src);
  if (!p) {
    // `force-cache` would pin a stale animation forever after a redeploy.
    // The server now sends an explicit `max-age` + `stale-while-revalidate`
    // policy for `/lottie/*`, so the default HTTP cache does the right thing:
    // no network at all while fresh, a cheap 304 afterwards.
    p = fetch(src)
      .then((r) => {
        if (!r.ok) throw new Error(`Failed to load lottie: ${src}`);
        return r.arrayBuffer();
      })
      .then((buf) => {
        touch(src, buf);
        return buf;
      })
      .catch((err) => {
        dataCache.delete(src);
        throw err;
      });
    dataCache.set(src, p);
  }
  return p;
}

/** Warm both the player bundle and the animation bytes ahead of first paint. */
export function prefetchLottie(src: string) {
  if (typeof window === "undefined") return;
  if (!canLoadLottiePlayer()) return;
  // Only the (small) animation bytes are warmed on intent. The heavy player
  // runtime is deferred until an animation is actually on screen.
  void loadLottieData(src).catch(() => {});
}
