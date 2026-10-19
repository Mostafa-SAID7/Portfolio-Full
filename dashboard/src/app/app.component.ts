import { Component, HostListener, computed, inject, isDevMode, signal, PLATFORM_ID } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { AuthService } from './core/services/auth.service';
import { ContactService } from './core/services/contact.service';
import { ThemeService } from './core/services/theme.service';
import { ConfirmDialogComponent } from './shared/confirm-dialog.component';
import { ToastComponent } from './shared/toast.component';

interface NavItem {
  link: string;
  label: string;
  icon: string;
  exact?: boolean;
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, ConfirmDialogComponent, ToastComponent],
  template: `
    <div class="app-shell">
      @if (isAuthenticated()) {
        <!-- Mobile scrim: only active below the lg breakpoint. -->
        <div
          class="sidebar-overlay"
          [class.is-open]="drawerOpen() && !isDesktop()"
          (click)="closeDrawer()"
          aria-hidden="true"
        ></div>

        <!-- Sidebar: fixed full-height column on every breakpoint.
             Desktop (lg+): always visible, page scrolls beside it.
             Mobile: off-canvas drawer toggled by the header button. -->
        <aside
          id="app-sidebar"
          class="sidebar"
          [class.is-open]="drawerOpen() && !isDesktop()"
          [attr.aria-hidden]="isDesktop() ? null : !drawerOpen()"
          aria-label="Sidebar"
        >
          <div class="flex items-center gap-3 px-5 pb-5 pt-6">
            <a routerLink="/dashboard" class="flex items-center gap-3" (click)="closeDrawer()" aria-label="Portfolio admin home">
              <span class="flex h-9 w-9 items-center justify-center rounded-xl bg-accent text-on-accent shadow-accent-glow">
                <svg class="h-5 w-5" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                  <path d="M4 13.5 12 5l8 8.5M6.5 11.5v7h11v-7M9.5 18.5v-4h5v4" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/>
                </svg>
              </span>
              <span class="font-display text-lg font-bold tracking-[-0.03em] text-ink">Portfolio<span class="text-accent">.</span>admin</span>
            </a>
          </div>

          <!-- Scrollable nav region: keeps the footer pinned while short
               viewports scroll the links independently. -->
          <div class="min-h-0 flex-1 overflow-y-auto px-3">
            <p class="eyebrow px-3 pb-2 pt-1">Manage</p>
            <nav class="space-y-1" aria-label="Main navigation">
              @for (item of navItems; track item.link) {
                <a
                  [routerLink]="item.link"
                  routerLinkActive="nav-active"
                  [routerLinkActiveOptions]="{ exact: !!item.exact }"
                  class="nav-link"
                  (click)="closeDrawer()"
                >
                  <span class="flex h-[18px] w-[18px] shrink-0 items-center justify-center" [innerHTML]="item.icon" aria-hidden="true"></span>
                  {{ item.label }}
                  @if (item.link === '/messages' && unreadCount() > 0) {
                    <span class="nav-badge">{{ unreadCount() }}</span>
                  }
                </a>
              }
            </nav>

            <p class="eyebrow px-3 pb-2 pt-8">Site</p>
            <a [href]="portfolioUrl" target="_blank" rel="noopener" class="nav-link" (click)="closeDrawer()">
              <span class="flex h-[18px] w-[18px] shrink-0 items-center justify-center" aria-hidden="true">
                <svg viewBox="0 0 24 24" fill="none"><circle cx="12" cy="12" r="8" stroke="currentColor" stroke-width="1.7"/><path d="M4 12h16M12 4c2.5 2.2 2.5 13.8 0 16-2.5-2.2-2.5-13.8 0-16Z" stroke="currentColor" stroke-width="1.7"/></svg>
              </span>
              View portfolio
              <span class="ml-auto text-faint" aria-hidden="true">↗</span>
            </a>
          </div>

          <!-- Pinned footer: identity + logout, always visible. -->
          <div class="border-t border-line/10 p-4">
            @if (currentUser(); as user) {
              <div class="flex items-center gap-3">
                <span class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-violet/20 text-sm font-bold text-violet">
                  {{ initials() }}
                </span>
                <div class="min-w-0 flex-1">
                  <p class="truncate text-sm font-semibold text-ink">{{ user.name }}</p>
                  <p class="truncate text-xs text-muted">{{ user.email }}</p>
                </div>
                <button type="button" (click)="logout()" class="icon-btn" aria-label="Sign out" title="Sign out">
                  <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" aria-hidden="true"><path d="M10 5H6.5A1.5 1.5 0 0 0 5 6.5v11A1.5 1.5 0 0 0 6.5 19H10M14 16l4-4-4-4M18 12H9" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/></svg>
                </button>
              </div>
            }
          </div>
        </aside>

        <!-- Main column -->
        <div class="min-w-0 lg:pl-[268px]">
          <header class="sticky top-0 z-20 flex h-16 items-center gap-3 border-b border-line/10 bg-canvas/85 px-4 backdrop-blur-xl sm:px-7">
            <button
              type="button"
              class="icon-btn lg:hidden"
              (click)="toggleDrawer()"
              [attr.aria-expanded]="drawerOpen()"
              aria-controls="app-sidebar"
              [attr.aria-label]="drawerOpen() ? 'Close navigation menu' : 'Open navigation menu'"
            >
              <svg class="h-5 w-5" viewBox="0 0 24 24" fill="none" aria-hidden="true"><path d="M4 7h16M4 12h16M4 17h16" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/></svg>
            </button>

            <h1 class="font-display text-base font-semibold tracking-[-0.02em] text-ink sm:text-lg">{{ pageTitle() }}</h1>

            <div class="ml-auto flex items-center gap-3">
              <span class="hidden text-xs text-muted md:block">{{ todayLabel() }}</span>
              <div class="hidden h-6 w-px bg-line/10 md:block"></div>
              <button type="button" (click)="toggleTheme()" class="icon-btn" [attr.aria-label]="currentTheme() === 'light' ? 'Switch to dark theme' : 'Switch to light theme'">
                @if (currentTheme() === 'light') {
                  <svg class="h-[18px] w-[18px]" viewBox="0 0 24 24" fill="none" aria-hidden="true"><path d="M20 14.5A8 8 0 0 1 9.5 4 8 8 0 1 0 20 14.5Z" stroke="currentColor" stroke-width="1.7" stroke-linejoin="round"/></svg>
                } @else {
                  <svg class="h-[18px] w-[18px]" viewBox="0 0 24 24" fill="none" aria-hidden="true"><circle cx="12" cy="12" r="4" stroke="currentColor" stroke-width="1.7"/><path d="M12 2.5v2.2M12 19.3v2.2M2.5 12h2.2M19.3 12h2.2M5 5l1.6 1.6M17.4 17.4 19 19M19 5l-1.6 1.6M6.6 17.4 5 19" stroke="currentColor" stroke-width="1.7" stroke-linecap="round"/></svg>
                }
              </button>
            </div>
          </header>

          <main class="mx-auto w-full max-w-[1400px] px-4 py-6 sm:px-7 sm:py-8" role="main">
            <router-outlet />
          </main>
        </div>

        <!-- Global confirm dialog (delete confirmations etc.). -->
        <app-confirm-dialog />
      } @else {
        <router-outlet />
      }

      <!-- Global toast: session expiry / permission errors (issue #20). -->
      <app-toast />
    </div>
  `
})
export class AppComponent {
  private readonly authService = inject(AuthService);
  private readonly contactService = inject(ContactService);
  private readonly themeService = inject(ThemeService);
  private readonly router = inject(Router);
  private readonly platformId = inject(PLATFORM_ID);
  private mediaQueryList?: MediaQueryList;

  readonly isAuthenticated = this.authService.isAuthenticated;
  readonly currentUser = this.authService.currentUser;
  readonly currentTheme = this.themeService.theme;
  readonly unreadCount = this.contactService.unreadCount;

  /** "View portfolio" target: the main frontend (dev server in dev, same origin in prod). */
  readonly portfolioUrl = isDevMode() ? 'http://localhost:5173' : '/';

  readonly drawerOpen = signal(false);
  /** Breakpoint flag: true when the viewport is at the lg (desktop) width. */
  readonly isDesktop = signal(false);

  readonly navItems: NavItem[] = [
    {
      link: '/dashboard',
      label: 'Overview',
      exact: true,
      icon: '<svg viewBox="0 0 24 24" fill="none" width="18" height="18"><path d="M4 13.5 12 5l8 8.5M6.5 11.5v7h11v-7M9.5 18.5v-4h5v4" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"/></svg>'
    },
    {
      link: '/projects',
      label: 'Projects',
      icon: '<svg viewBox="0 0 24 24" fill="none" width="18" height="18"><path d="M4 7.5a2 2 0 0 1 2-2h4l2 2h6a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2v-10Z" stroke="currentColor" stroke-width="1.7" stroke-linejoin="round"/></svg>'
    },
    {
      link: '/messages',
      label: 'Messages',
      icon: '<svg viewBox="0 0 24 24" fill="none" width="18" height="18"><path d="M4 6.5A2.5 2.5 0 0 1 6.5 4h11A2.5 2.5 0 0 1 20 6.5v8a2.5 2.5 0 0 1-2.5 2.5H9l-4 3.5v-3.5A2.5 2.5 0 0 1 4 14.5v-8Z" stroke="currentColor" stroke-width="1.7" stroke-linejoin="round"/></svg>'
    },
    {
      link: '/content',
      label: 'Content',
      icon: '<svg viewBox="0 0 24 24" fill="none" width="18" height="18"><path d="M6 4.75h8.5L18 8.25v11H6v-14.5Z" stroke="currentColor" stroke-width="1.7"/><path d="M14 4.75v3.5h4M9 12h6M9 15.5h4" stroke="currentColor" stroke-width="1.7" stroke-linecap="round"/></svg>'
    }
  ];

  readonly pageTitle = computed(() => {
    const url = this.router.url.split('?')[0];
    if (url.startsWith('/projects')) return 'Projects';
    if (url.startsWith('/messages')) return 'Messages';
    if (url.startsWith('/content')) return 'Content';
    return 'Overview';
  });

  readonly todayLabel = computed(() =>
    new Intl.DateTimeFormat('en-US', { weekday: 'long', month: 'short', day: 'numeric' }).format(new Date())
  );

  constructor() {
    // Close the mobile drawer on navigation.
    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe(() => this.closeDrawer());

    // Track the lg breakpoint so the drawer/overlay only apply on small screens.
    if (isPlatformBrowser(this.platformId)) {
      const mql = window.matchMedia('(min-width: 1024px)');
      this.isDesktop.set(mql.matches);
      const onChange = (e: MediaQueryListEvent) => {
        this.isDesktop.set(e.matches);
        // Leaving mobile: release the scroll lock and reset the drawer.
        if (e.matches && this.drawerOpen()) {
          this.drawerOpen.set(false);
          this.syncBodyScroll();
        }
      };
      mql.addEventListener('change', onChange);
      this.mediaQueryList = mql;
    }

    // Keep the unread badge warm even before visiting /messages.
    if (this.isAuthenticated()) {
      this.contactService.loadMessages().subscribe({ error: () => undefined });
    }
  }

  initials(): string {
    return this.authService.initials();
  }

  toggleTheme(): void {
    this.themeService.toggleTheme();
  }

  logout(): void {
    this.authService.logout();
  }

  toggleDrawer(): void {
    this.drawerOpen.update(open => !open);
    this.syncBodyScroll();
  }

  closeDrawer(): void {
    if (this.drawerOpen()) {
      this.drawerOpen.set(false);
      this.syncBodyScroll();
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closeDrawer();
  }

  private syncBodyScroll(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    // Only lock scroll for the mobile drawer; never on desktop.
    document.body.style.overflow = this.drawerOpen() && !this.isDesktop() ? 'hidden' : '';
  }
}
