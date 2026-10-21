import { afterRenderEffect, Component, ElementRef, HostListener, computed, inject, isDevMode, signal, PLATFORM_ID, DestroyRef, viewChild } from '@angular/core';
import { CommonModule, DOCUMENT, isPlatformBrowser } from '@angular/common';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { NavigationEnd, NavigationStart, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter, interval, map, startWith } from 'rxjs';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { AuthService } from './core/services/auth.service';
import { ContactService } from './core/services/contact.service';
import { ApiInstanceService } from './core/services/api-instance.service';
import { ThemeService } from './core/services/theme.service';
import { ConfirmDialogComponent, ConfirmDialogService } from './shared/confirm-dialog.component';
import { ToastComponent } from './shared/toast.component';

interface NavItem {
  link: string;
  label: string;
  icon: SafeHtml;
  exact?: boolean;
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, ConfirmDialogComponent, ToastComponent],
  templateUrl: './app.html'
})
export class AppComponent {
  private readonly authService = inject(AuthService);
  private readonly contactService = inject(ContactService);
  readonly apiInstance = inject(ApiInstanceService);
  private readonly themeService = inject(ThemeService);
  private readonly router = inject(Router);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly destroyRef = inject(DestroyRef);
  private readonly document = inject(DOCUMENT);
  private readonly confirmDialog = inject(ConfirmDialogService);
  private readonly sidebar = viewChild<ElementRef<HTMLElement>>('sidebar');
  private readonly drawerTrigger = viewChild<ElementRef<HTMLButtonElement>>('drawerTrigger');
  private previousFocus: HTMLElement | null = null;
  private previousBodyOverflow: string | null = null;
  private drawerWasActive = false;

  /** Angular's default sanitizer strips <svg> from innerHTML — mark the hardcoded icon strings trusted. */
  private trust(icon: string): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(icon);
  }

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
      icon: this.trust('<svg viewBox="0 0 24 24" fill="none" width="18" height="18"><path d="M4 13.5 12 5l8 8.5M6.5 11.5v7h11v-7M9.5 18.5v-4h5v4" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"/></svg>')
    },
    {
      link: '/projects',
      label: 'Projects',
      icon: this.trust('<svg viewBox="0 0 24 24" fill="none" width="18" height="18"><path d="M4 7.5a2 2 0 0 1 2-2h4l2 2h6a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2v-10Z" stroke="currentColor" stroke-width="1.7" stroke-linejoin="round"/></svg>')
    },
    {
      link: '/messages',
      label: 'Messages',
      icon: this.trust('<svg viewBox="0 0 24 24" fill="none" width="18" height="18"><path d="M4 6.5A2.5 2.5 0 0 1 6.5 4h11A2.5 2.5 0 0 1 20 6.5v8a2.5 2.5 0 0 1-2.5 2.5H9l-4 3.5v-3.5A2.5 2.5 0 0 1 4 14.5v-8Z" stroke="currentColor" stroke-width="1.7" stroke-linejoin="round"/></svg>')
    },
    {
      link: '/content',
      label: 'Content',
      icon: this.trust('<svg viewBox="0 0 24 24" fill="none" width="18" height="18"><path d="M6 4.75h8.5L18 8.25v11H6v-14.5Z" stroke="currentColor" stroke-width="1.7"/><path d="M14 4.75v3.5h4M9 12h6M9 15.5h4" stroke="currentColor" stroke-width="1.7" stroke-linecap="round"/></svg>')
    }
  ];

  /**
   * Reactive page title derived from router NavigationEnd events.
   * Using toSignal on the router events stream instead of reading
   * this.router.url (a plain string) so the signal actually updates
   * on every navigation (fix for DASHBOARD-H1).
   */
  readonly pageTitle = toSignal(
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd),
      map((event: NavigationEnd) => this.pageTitleFromUrl(event.urlAfterRedirects)),
      startWith(this.pageTitleFromUrl(this.router.url)),
    ),
    { initialValue: 'Overview' },
  );

  /** Synchronous helper used for the startWith seed. */
  private pageTitleFromUrl(url: string): string {
    const path = url.split('?')[0];
    if (path.startsWith('/projects')) return 'Projects';
    if (path.startsWith('/messages')) return 'Messages';
    if (path.startsWith('/content')) return 'Content';
    return 'Overview';
  }

  private readonly clockTick = toSignal(interval(60_000).pipe(map(tick => tick + 1)), { initialValue: 0 });
  readonly todayLabel = computed(() => {
    this.clockTick();
    return new Intl.DateTimeFormat('en-US', { weekday: 'long', month: 'short', day: 'numeric' }).format(new Date());
  });

  constructor() {
    // Show which API instance the dashboard is talking to.
    this.apiInstance.load();

    // Cancel a pending destructive action as soon as its originating route leaves.
    this.router.events.pipe(
      filter(event => event instanceof NavigationStart),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(() => {
      this.closeDrawer();
      this.confirmDialog.settle(false);
    });

    // Focus only after inert bindings and the conditional shell have rendered.
    afterRenderEffect(() => {
      const authenticated = this.isAuthenticated();
      if (!authenticated && this.drawerOpen()) this.closeDrawer();
      const active = authenticated && this.drawerOpen() && !this.isDesktop();
      const sidebar = this.sidebar()?.nativeElement;
      this.syncBodyScroll();
      if (active && !this.drawerWasActive && sidebar) {
        this.previousFocus ??= this.drawerTrigger()?.nativeElement ?? null;
        this.drawerFocusTargets()[0]?.focus();
      } else if (!active && this.drawerWasActive) {
        this.restoreDrawerFocus();
      }
      this.drawerWasActive = active;
    });

    this.destroyRef.onDestroy(() => {
      this.drawerOpen.set(false);
      this.syncBodyScroll();
      this.restoreDrawerFocus();
      this.confirmDialog.settle(false);
    });

    // Track the lg breakpoint so the drawer/overlay only apply on small screens.
    if (isPlatformBrowser(this.platformId)) {
      const mql = window.matchMedia('(min-width: 1024px)');
      this.isDesktop.set(mql.matches);
      const onChange = (e: MediaQueryListEvent) => {
        this.isDesktop.set(e.matches);
        if (e.matches) this.closeDrawer();
      };
      mql.addEventListener('change', onChange);

      // Remove the listener when the component is destroyed to prevent a
      // memory leak (fix for DASHBOARD-L1).
      this.destroyRef.onDestroy(() => {
        mql.removeEventListener('change', onChange);
      });
    }

    // Keep the unread badge warm even before visiting /messages.
    if (this.isAuthenticated()) {
      this.contactService.loadMessages().subscribe({ error: () => undefined });
    }
  }

  /** Initials signal delegated directly — no extra method call per CD cycle (fix for DASHBOARD-M1). */
  readonly initials = computed(() => this.authService.initials());

  toggleTheme(): void {
    this.themeService.toggleTheme();
  }

  logout(): void {
    this.closeDrawer();
    this.confirmDialog.settle(false);
    this.authService.logout();
  }

  toggleDrawer(): void {
    if (!this.isAuthenticated() || this.isDesktop()) return;
    if (this.drawerOpen()) {
      this.closeDrawer();
      return;
    }
    this.previousFocus = this.document.activeElement instanceof HTMLElement ? this.document.activeElement : null;
    this.drawerOpen.set(true);
    this.syncBodyScroll();
  }

  closeDrawer(): void {
    this.drawerOpen.set(false);
    this.syncBodyScroll();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    // The native dialog owns Escape while it is in the top layer.
    if (!this.confirmDialog.request()) this.closeDrawer();
  }

  @HostListener('document:keydown', ['$event'])
  onDrawerKeydown(event: KeyboardEvent): void {
    if (event.key !== 'Tab' || !this.drawerOpen() || this.isDesktop() || this.confirmDialog.request()) return;
    const targets = this.drawerFocusTargets();
    const first = targets[0];
    const last = targets[targets.length - 1];
    if (!first) {
      event.preventDefault();
      this.sidebar()?.nativeElement.focus();
    } else if (!this.sidebar()?.nativeElement.contains(this.document.activeElement) ||
        (event.shiftKey && this.document.activeElement === first) ||
        (!event.shiftKey && this.document.activeElement === last)) {
      event.preventDefault();
      (event.shiftKey ? last : first).focus();
    }
  }

  @HostListener('document:focusin', ['$event'])
  onDrawerFocus(event: FocusEvent): void {
    if (!this.drawerOpen() || this.isDesktop() || this.confirmDialog.request()) return;
    const sidebar = this.sidebar()?.nativeElement;
    if (sidebar && !sidebar.contains(event.target as Node)) this.drawerFocusTargets()[0]?.focus();
  }

  private drawerFocusTargets(): HTMLElement[] {
    const sidebar = this.sidebar()?.nativeElement;
    return sidebar ? Array.from(sidebar.querySelectorAll<HTMLElement>(
      'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]'
    )).filter(element => element.tabIndex >= 0 && !element.closest('[inert]') && element.getClientRects().length > 0) : [];
  }

  private restoreDrawerFocus(): void {
    // On desktop the navigation stays visible; do not focus its hidden trigger.
    if (!this.isDesktop() && this.previousFocus?.isConnected && !this.previousFocus.closest('[inert]')) {
      this.previousFocus.focus();
    }
    this.previousFocus = null;
  }

  private syncBodyScroll(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    const body = this.document.body;
    if (this.isAuthenticated() && this.drawerOpen() && !this.isDesktop()) {
      this.previousBodyOverflow ??= body.style.overflow;
      body.style.overflow = 'hidden';
    } else if (this.previousBodyOverflow !== null) {
      body.style.overflow = this.previousBodyOverflow;
      this.previousBodyOverflow = null;
    }
  }
}
