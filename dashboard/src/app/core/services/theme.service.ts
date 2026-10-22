import { Injectable, signal, effect, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { BrowserStorage, STORAGE_KEYS } from './storage.service';

export type Theme = 'light' | 'dark';

/**
 * Applies `light`/`dark` on <html>. Both palettes are defined in
 * src/styles.css, so the toggle genuinely switches colors (the old app
 * applied the class but had no light palette, so nothing changed).
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly storage = inject(BrowserStorage);

  private readonly themeSignal = signal<Theme>(this.loadTheme());

  readonly theme = this.themeSignal.asReadonly();

  constructor() {
    if (isPlatformBrowser(this.platformId)) {
      effect(() => {
        const theme = this.themeSignal();
        this.applyTheme(theme);
        this.storage.set(STORAGE_KEYS.theme, theme);
      });
    }
  }

  toggleTheme(): void {
    this.themeSignal.update(current => (current === 'light' ? 'dark' : 'light'));
  }

  private loadTheme(): Theme {
    if (!isPlatformBrowser(this.platformId)) {
      return 'dark';
    }

    const saved = this.storage.get(STORAGE_KEYS.theme);
    if (saved === 'light' || saved === 'dark') {
      return saved;
    }

    return window.matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark';
  }

  private applyTheme(theme: Theme): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    const root = document.documentElement;
    root.classList.remove('light', 'dark');
    root.classList.add(theme);
  }
}
