import { Injectable, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

/**
 * Every persisted browser-storage key the dashboard uses, in ONE place.
 * All namespaced under `portfolio_admin_` so they can never collide with
 * anything else sharing this origin.
 */
export const STORAGE_KEYS = {
  /** JWT-backed admin session ({@link AuthService}). */
  session: 'portfolio_admin_session',
  /** light/dark UI preference ({@link ThemeService}). */
  theme: 'portfolio_admin_theme',
  /** Desktop icon-rail state ({@link AppComponent}). */
  sidebarCollapsed: 'portfolio_admin_sidebar_collapsed',
} as const;

/**
 * Single injectable door to localStorage for the whole dashboard.
 *
 * Everything that used to scatter `localStorage.get/set/remove` (auth, theme,
 * sidebar) goes through here, so storage rules live in one file: SSR safety
 * (no browser on the server) and privacy-mode resilience (a blocked store
 * degrades to in-memory behaviour, never an exception).
 */
@Injectable({ providedIn: 'root' })
export class BrowserStorage {
  private readonly platformId = inject(PLATFORM_ID);

  /** Value or null when unavailable. */
  get(key: string): string | null {
    if (!this.available()) return null;
    try {
      return localStorage.getItem(key);
    } catch {
      return null;
    }
  }

  /** Silently no-ops when storage is blocked (the caller's in-memory state still works). */
  set(key: string, value: string): void {
    if (!this.available()) return;
    try {
      localStorage.setItem(key, value);
    } catch {
      /* storage unavailable — state simply won't persist */
    }
  }

  remove(key: string): void {
    if (!this.available()) return;
    try {
      localStorage.removeItem(key);
    } catch {
      /* storage unavailable */
    }
  }

  private available(): boolean {
    return isPlatformBrowser(this.platformId) && typeof localStorage !== 'undefined';
  }
}
