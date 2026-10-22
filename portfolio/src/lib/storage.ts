/**
 * Single door to browser storage for the portfolio app.
 *
 * Conventions shared with the dashboard
 * (see dashboard/src/app/core/services/storage.service.ts): every key is
 * namespaced under `portfolio_` so nothing generic can collide, and every
 * accessor is try/catch-safe — private-mode or blocked storage degrades to
 * the caller's default value, never an exception.
 *
 * Layers:
 *  - localStorage: durable preferences only (language, theme)
 *  - sessionStorage: per-tab session flags (splash seen, welcome modal seen)
 *
 * Legacy note: language/theme were previously stored under the bare keys
 * "lang"/"theme".  `readMigrated` reads the namespaced key first, falls back
 * to the legacy one and rewrites the value forward exactly once, so existing
 * visitors keep their preferences across the rename.
 */

const NAMESPACE = "portfolio";

/** Every localStorage key this app writes. New code must use these. */
export const LOCAL_KEYS = {
  lang: `${NAMESPACE}_lang`,
  theme: `${NAMESPACE}_theme`,
} as const;

/** Pre-namespace keys — read-only fallbacks inside `readMigrated` only. */
export const LEGACY_LOCAL_KEYS = {
  lang: "lang",
  theme: "theme",
} as const;

/** Every sessionStorage key this app writes. */
export const SESSION_KEYS = {
  preloaded: `${NAMESPACE}_preloaded`,
  welcomeSeen: `${NAMESPACE}_welcome_seen`,
} as const;

/** Namespaced localStorage accessors. */
export const localStore = {
  get(key: string): string | null {
    try {
      return localStorage.getItem(key);
    } catch {
      return null;
    }
  },
  set(key: string, value: string): void {
    try {
      localStorage.setItem(key, value);
    } catch {
      /* storage blocked — preference applies for this session only */
    }
  },
  remove(key: string): void {
    try {
      localStorage.removeItem(key);
    } catch {
      /* storage blocked */
    }
  },
};

/** Namespaced sessionStorage accessors. */
export const sessionStore = {
  get(key: string): string | null {
    try {
      return sessionStorage.getItem(key);
    } catch {
      return null;
    }
  },
  set(key: string, value: string): void {
    try {
      sessionStorage.setItem(key, value);
    } catch {
      /* storage blocked */
    }
  },
  remove(key: string): void {
    try {
      sessionStorage.removeItem(key);
    } catch {
      /* storage blocked */
    }
  },
};

/**
 * Reads a namespaced preference, falling back (once) to its pre-namespace
 * legacy key and migrating the found value forward.
 */
export function readMigrated(key: keyof typeof LOCAL_KEYS, legacy: string): string | null {
  const fresh = localStore.get(LOCAL_KEYS[key]);
  if (fresh !== null) return fresh;
  const old = localStore.get(legacy);
  if (old === null) return null;
  localStore.set(LOCAL_KEYS[key], old);
  localStore.remove(legacy);
  return old;
}

/**
 * Per-tab session flags shared by the pre-hydration scripts and the React
 * tree (single source for both — no parallel implementations).
 */
export const sessionFlags = {
  preloaderSeen(): boolean {
    return sessionStore.get(SESSION_KEYS.preloaded) === "1";
  },
  markPreloaderSeen(): void {
    sessionStore.set(SESSION_KEYS.preloaded, "1");
  },
  welcomeSeen(): boolean {
    return sessionStore.get(SESSION_KEYS.welcomeSeen) === "1";
  },
  markWelcomeSeen(): void {
    sessionStore.set(SESSION_KEYS.welcomeSeen, "1");
  },
};
