import { flushSync } from "react-dom";
import { createContext, useContext, useEffect, useRef, useState, type ReactNode } from "react";
import { LOCAL_KEYS, LEGACY_LOCAL_KEYS, localStore, readMigrated } from "@/lib/storage";

type Theme = "dark" | "light";

/**
 * Paint colors MUST match the --background CSS variable in styles.css for
 * each theme — this is what the browser shows before the first paint, so a
 * mismatch causes a visible color flash on reload.
 * Dark (matches dashboard canvas)  → #14100d
 * Light (matches dashboard canvas) → #faf6f1
 */
const PAINT: Record<Theme, string> = {
  dark: "#14100d",
  light: "#faf6f1",
};

interface ThemeContextValue {
  theme: Theme;
  setTheme: (theme: Theme) => void;
  toggleTheme: (origin?: { x: number; y: number }) => void;
}

const ThemeContext = createContext<ThemeContextValue | undefined>(undefined);

/**
 * Inlined in <head> (see __root.tsx) so it executes before the first paint:
 * the stored theme is applied to <html> in the very first frame — no coral/dark
 * flash for light-mode visitors on reload.
 */
export const themeBootstrapScript = `(function(){try{
var s=localStorage.getItem("${LOCAL_KEYS.theme}")||localStorage.getItem("${LEGACY_LOCAL_KEYS.theme}");
if(s!=="light"&&s!=="dark"){s=window.matchMedia&&window.matchMedia("(prefers-color-scheme: light)").matches?"light":"dark";}
else{ /* an explicit user choice: migrate the bare-"theme" entry forward */
try{localStorage.setItem("${LOCAL_KEYS.theme}",s);localStorage.removeItem("${LEGACY_LOCAL_KEYS.theme}");}catch(e){}}
var r=document.documentElement;
r.classList.toggle("light",s==="light");
r.style.colorScheme=s==="light"?"light":"dark";
r.style.backgroundColor=s==="light"?"${PAINT.light}":"${PAINT.dark}";
r.setAttribute("data-theme",s);
}catch(e){}})();`;

function applyTheme(theme: Theme) {
  const root = document.documentElement;
  root.classList.toggle("light", theme === "light");
  root.style.colorScheme = theme === "light" ? "light" : "dark";
  root.style.backgroundColor = PAINT[theme];
  root.setAttribute("data-theme", theme);
}

/** What the bootstrap script already painted — the single source of truth on the client. */
function readAppliedTheme(): Theme {
  if (typeof document === "undefined") return "dark";
  const attr = document.documentElement.getAttribute("data-theme");
  if (attr === "light" || attr === "dark") return attr;
  return document.documentElement.classList.contains("light") ? "light" : "dark";
}

function readStoredTheme(): Theme | null {
  // readMigrated rewrites an old bare-"theme" entry into the namespaced key
  // once, so returning visitors keep their explicit choice across the rename.
  const v = readMigrated("theme", LEGACY_LOCAL_KEYS.theme);
  return v === "light" || v === "dark" ? v : null;
}

export function ThemeProvider({ children }: { children: ReactNode }) {
  // SSR renders the default theme; the bootstrap script already painted the
  // correct one, and this state syncs to it right after hydration.
  const [theme, setThemeState] = useState<Theme>("dark");
  const [hydrated, setHydrated] = useState(false);
  // Only an explicit user choice is persisted, so visitors who never toggled
  // keep following their OS preference.
  const explicitRef = useRef(false);

  useEffect(() => {
    const stored = readStoredTheme();
    explicitRef.current = stored !== null;
    setThemeState(stored ?? readAppliedTheme());
    setHydrated(true);
  }, []);

  useEffect(() => {
    if (!hydrated) return;
    applyTheme(theme);
    if (!explicitRef.current) return;
    localStore.set(LOCAL_KEYS.theme, theme);
    localStore.remove(LEGACY_LOCAL_KEYS.theme);
  }, [theme, hydrated]);

  // Follow the OS only while the visitor has never made an explicit choice.
  useEffect(() => {
    if (typeof window === "undefined" || !window.matchMedia) return;
    const mq = window.matchMedia("(prefers-color-scheme: light)");
    const onChange = (e: MediaQueryListEvent) => {
      if (!explicitRef.current) setThemeState(e.matches ? "light" : "dark");
    };
    mq.addEventListener("change", onChange);
    return () => mq.removeEventListener("change", onChange);
  }, []);

  // Keep tabs/windows of the same site in sync.
  useEffect(() => {
    const onStorage = (e: StorageEvent) => {
      if (e.key !== LOCAL_KEYS.theme) return;
      if (e.newValue === "light" || e.newValue === "dark") {
        explicitRef.current = true;
        setThemeState(e.newValue);
      } else if (e.newValue === null) {
        explicitRef.current = false;
      }
    };
    window.addEventListener("storage", onStorage);
    return () => window.removeEventListener("storage", onStorage);
  }, []);

  const setTheme = (next: Theme) => {
    explicitRef.current = true;
    setThemeState(next);
  };

  /**
   * Toggles with a circular reveal growing from the clicked control
   * (View Transitions API). Falls back to an instant swap where unsupported
   * or when the visitor asked for reduced motion.
   */
  const toggleTheme = (origin?: { x: number; y: number }) => {
    const next: Theme = theme === "dark" ? "light" : "dark";
    const doc =
      typeof document !== "undefined"
        ? (document as Document & {
            startViewTransition?: (cb: () => void) => {
              ready: Promise<void>;
              finished: Promise<void>;
            };
          })
        : null;

    const reduced =
      typeof window !== "undefined" &&
      window.matchMedia?.("(prefers-reduced-motion: reduce)").matches;

    if (!doc?.startViewTransition || reduced || !origin) {
      setTheme(next);
      return;
    }

    const root = doc.documentElement;
    root.classList.add("theme-transitioning");

    const transition = doc.startViewTransition(() => {
      flushSync(() => setTheme(next));
    });

    transition.ready
      .then(() => {
        const { x, y } = origin;
        const radius = Math.hypot(
          Math.max(x, window.innerWidth - x),
          Math.max(y, window.innerHeight - y),
        );
        // Duration scales a little with how far the circle has to travel, so
        // small viewports don't feel sluggish and large ones don't feel abrupt.
        const duration = Math.round(Math.min(420, Math.max(260, radius * 0.28)));
        root.animate(
          {
            clipPath: [`circle(0px at ${x}px ${y}px)`, `circle(${radius}px at ${x}px ${y}px)`],
          },
          {
            duration,
            easing: "cubic-bezier(0.32, 0.72, 0, 1)",
            fill: "forwards",
            pseudoElement: "::view-transition-new(root)",
          },
        );
      })
      .catch(() => root.classList.remove("theme-transitioning"));

    transition.finished.catch(() => {}).finally(() => root.classList.remove("theme-transitioning"));
  };

  return (
    <ThemeContext.Provider value={{ theme, setTheme, toggleTheme }}>
      {children}
    </ThemeContext.Provider>
  );
}

export function useTheme() {
  const ctx = useContext(ThemeContext);
  if (!ctx) throw new Error("useTheme must be used within ThemeProvider");
  return ctx;
}
