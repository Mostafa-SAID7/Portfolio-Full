import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AdminUser } from '../models/user.model';

/**
 * Local admin session.
 *
 * PortfolioAPI has no auth endpoints yet, so signing in simply unlocks the
 * admin workspace and stores the identity locally. When JWT auth lands on
 * the API, replace signIn() with a real POST /api/Auth/login and keep the
 * rest of the surface identical.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly router = inject(Router);
  private readonly storageKey = 'portfolio_admin_session';

  private readonly userSignal = signal<AdminUser | null>(this.loadSession());

  readonly currentUser = this.userSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.userSignal() !== null);

  signIn(name: string, email: string): void {
    const user: AdminUser = {
      id: this.slugify(email),
      name: name.trim(),
      email: email.trim().toLowerCase()
    };
    this.userSignal.set(user);
    localStorage.setItem(this.storageKey, JSON.stringify(user));
    this.router.navigate(['/dashboard']);
  }

  logout(): void {
    this.userSignal.set(null);
    localStorage.removeItem(this.storageKey);
    this.router.navigate(['/auth/login']);
  }

  initials(): string {
    const name = this.userSignal()?.name ?? 'Admin';
    return name
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map(part => part[0].toUpperCase())
      .join('');
  }

  private loadSession(): AdminUser | null {
    try {
      const raw = localStorage.getItem(this.storageKey);
      return raw ? (JSON.parse(raw) as AdminUser) : null;
    } catch {
      return null;
    }
  }

  private slugify(value: string): string {
    return value.trim().toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '') || 'admin';
  }
}
