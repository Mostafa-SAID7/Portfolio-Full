import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/services/auth.service';
import { ThemeService } from './core/services/theme.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="app-shell">
      @if (isAuthenticated()) {
        <div class="flex min-h-screen">
          <aside class="hidden w-[248px] shrink-0 flex-col border-r border-white/[0.07] bg-[#0f1218] px-5 py-6 lg:flex">
            <a routerLink="/dashboard" class="mb-12 flex items-center gap-3 px-2" aria-label="TaskFlow home">
              <span class="flex h-9 w-9 items-center justify-center rounded-xl bg-[#ff6b35] text-[#17110e] shadow-[0_8px_24px_rgba(255,107,53,.25)]">
                <svg class="h-5 w-5" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                  <path d="M6 4.75h8.5L18 8.25v11H6v-14.5Z" stroke="currentColor" stroke-width="1.8"/>
                  <path d="M14 4.75v3.5h4M9 12h6M9 15.5h4" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/>
                </svg>
              </span>
              <span class="font-display text-xl font-bold tracking-[-0.04em] text-[#f4f1ea]">task<span class="text-[#ff6b35]">.</span></span>
            </a>

            <div class="px-2 pb-3 eyebrow">Workspace</div>
            <nav class="space-y-1" aria-label="Main navigation">
              <a routerLink="/dashboard" routerLinkActive="nav-active" [routerLinkActiveOptions]="{exact: true}" class="shell-nav">
                <svg viewBox="0 0 24 24" fill="none" aria-hidden="true"><path d="M4 13.5 12 5l8 8.5M6.5 11.5v7h11v-7M9.5 18.5v-4h5v4" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"/></svg>
                Overview
              </a>
              <a routerLink="/tasks" routerLinkActive="nav-active" class="shell-nav">
                <svg viewBox="0 0 24 24" fill="none" aria-hidden="true"><path d="M7 5.5h10M7 9.5h6M7 13.5h10M7 17.5h6M4.5 4h.01M4.5 8h.01M4.5 12h.01M4.5 16h.01" stroke="currentColor" stroke-width="1.9" stroke-linecap="round"/></svg>
                My tasks
                <span class="ml-auto rounded-full bg-[#ff6b35]/15 px-2 py-0.5 text-[10px] font-bold text-[#ff8b61]">12</span>
              </a>
              <a routerLink="/projects" routerLinkActive="nav-active" class="shell-nav">
                <svg viewBox="0 0 24 24" fill="none" aria-hidden="true"><path d="M4 7.5a2 2 0 0 1 2-2h4l2 2h6a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2v-10Z" stroke="currentColor" stroke-width="1.7" stroke-linejoin="round"/></svg>
                Projects
              </a>
              <a routerLink="/tasks" [queryParams]="{view: 'calendar'}" class="shell-nav">
                <svg viewBox="0 0 24 24" fill="none" aria-hidden="true"><rect x="4" y="5.5" width="16" height="14" rx="2" stroke="currentColor" stroke-width="1.7"/><path d="M8 3.5v4M16 3.5v4M4 10h16" stroke="currentColor" stroke-width="1.7" stroke-linecap="round"/><path d="M8 13h.01M12 13h.01M16 13h.01M8 16h.01M12 16h.01" stroke="currentColor" stroke-width="2.3" stroke-linecap="round"/></svg>
                Calendar
              </a>
            </nav>

            <div class="mt-10 px-2 pb-3 eyebrow">Personal</div>
            <nav class="space-y-1" aria-label="Personal navigation">
              <a routerLink="/projects" class="shell-nav">
                <svg viewBox="0 0 24 24" fill="none" aria-hidden="true"><path d="m12 4 2.1 4.3 4.7.7-3.4 3.3.8 4.7-4.2-2.2-4.2 2.2.8-4.7-3.4-3.3 4.7-.7L12 4Z" stroke="currentColor" stroke-width="1.7" stroke-linejoin="round"/></svg>
                Favorites
              </a>
              <a routerLink="/dashboard" fragment="activity" class="shell-nav">
                <svg viewBox="0 0 24 24" fill="none" aria-hidden="true"><path d="M5 18.5V12M12 18.5V5.5M19 18.5v-9" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/></svg>
                Activity
              </a>
            </nav>

            <div class="mt-auto">
              <div class="mb-5 rounded-2xl border border-[#ff6b35]/20 bg-[#ff6b35]/[0.07] p-4">
                <div class="mb-3 flex items-center justify-between">
                  <span class="eyebrow text-[#ff9d7c]">Weekly focus</span>
                  <span class="text-xs font-semibold text-[#ff9d7c]">72%</span>
                </div>
                <div class="h-1.5 overflow-hidden rounded-full bg-[#ff6b35]/15"><div class="h-full w-[72%] rounded-full bg-[#ff6b35]"></div></div>
                <p class="mt-3 text-xs leading-5 text-[#b6a39c]">You are on a roll. Keep the momentum going.</p>
              </div>
              @if (currentUser(); as user) {
                <div class="flex items-center gap-3 rounded-2xl border border-white/[0.07] bg-white/[0.03] p-3">
                  <div class="flex h-9 w-9 items-center justify-center rounded-full bg-[#9b8cff] text-sm font-bold text-[#201c38]">{{ user.name.charAt(0).toUpperCase() }}</div>
                  <div class="min-w-0 flex-1">
                    <p class="truncate text-sm font-semibold text-[#f4f1ea]">{{ user.name }}</p>
                    <p class="truncate text-xs text-[#8f96a5]">Product designer</p>
                  </div>
                  <button (click)="logout()" type="button" class="text-[#8f96a5] transition hover:text-[#ff8b61]" aria-label="Log out">
                    <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none"><path d="M10 5H6.5A1.5 1.5 0 0 0 5 6.5v11A1.5 1.5 0 0 0 6.5 19H10M14 16l4-4-4-4M18 12H9" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/></svg>
                  </button>
                </div>
              }
            </div>
          </aside>

          <div class="min-w-0 flex-1">
            <header class="flex h-[76px] items-center justify-between border-b border-white/[0.07] bg-[#0b0d12]/90 px-5 backdrop-blur-xl sm:px-8">
              <div class="flex items-center gap-3">
                <button type="button" class="flex h-9 w-9 items-center justify-center rounded-xl border border-white/[0.08] text-[#8f96a5] lg:hidden" aria-label="Open menu">
                  <svg class="h-5 w-5" viewBox="0 0 24 24" fill="none"><path d="M4 7h16M4 12h16M4 17h16" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/></svg>
                </button>
                <div class="hidden items-center gap-2 rounded-xl border border-white/[0.07] bg-white/[0.03] px-3 py-2 sm:flex">
                  <svg class="h-4 w-4 text-[#8f96a5]" viewBox="0 0 24 24" fill="none"><circle cx="11" cy="11" r="6.5" stroke="currentColor" stroke-width="1.7"/><path d="m16 16 4 4" stroke="currentColor" stroke-width="1.7" stroke-linecap="round"/></svg>
                  <input class="w-40 bg-transparent text-xs text-[#f4f1ea] outline-none placeholder:text-[#687080]" placeholder="Search anything" aria-label="Search anything" />
                  <kbd class="rounded-md border border-white/[0.08] px-1.5 py-0.5 text-[10px] text-[#687080]">⌘ K</kbd>
                </div>
              </div>
              <div class="flex items-center gap-3">
                <button type="button" class="relative flex h-9 w-9 items-center justify-center rounded-xl border border-white/[0.08] text-[#8f96a5] transition hover:border-white/20 hover:text-[#f4f1ea]" aria-label="Notifications">
                  <svg class="h-[18px] w-[18px]" viewBox="0 0 24 24" fill="none"><path d="M18 9a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9ZM10 21h4" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"/></svg>
                  <span class="absolute right-2 top-2 h-1.5 w-1.5 rounded-full bg-[#ff6b35]"></span>
                </button>
                <button (click)="toggleTheme()" type="button" class="flex h-9 w-9 items-center justify-center rounded-xl border border-white/[0.08] text-[#8f96a5] transition hover:border-white/20 hover:text-[#f4f1ea]" aria-label="Toggle theme">
                  @if (currentTheme() === 'light') { <span aria-hidden="true">☾</span> } @else { <span aria-hidden="true">☼</span> }
                </button>
                <div class="hidden h-7 w-px bg-white/[0.08] sm:block"></div>
                <span class="hidden text-xs text-[#8f96a5] sm:block">Monday, Sep 7</span>
              </div>
            </header>
            <main class="mx-auto max-w-[1440px] px-5 py-7 sm:px-8 sm:py-9" role="main">
              <router-outlet />
            </main>
          </div>
        </div>
      } @else {
        <router-outlet />
      }
    </div>
  `
})
export class AppComponent {
  private readonly authService = inject(AuthService);
  private readonly themeService = inject(ThemeService);

  readonly isAuthenticated = this.authService.isAuthenticated;
  readonly currentUser = this.authService.currentUser;
  readonly currentTheme = this.themeService.theme;

  toggleTheme(): void {
    this.themeService.toggleTheme();
  }

  logout(): void {
    this.authService.logout();
  }
}