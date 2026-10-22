import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { AdminSession, AdminUser } from '../models/user.model';
import { API_URL } from '../api.config';
import { BrowserStorage, STORAGE_KEYS } from './storage.service';

interface LoginResponse {
  token: string;
  expiresAt: string;
  user: { id: number; name: string; email: string };
}

/**
 * JWT-backed admin session (issues #12/#1/#6).
 *
 * `signIn()` performs a real POST /api/auth/login against the API, which
 * validates the server-configured Auth:Username / Auth:Password and returns
 * a signed JWT (503 with instructions when auth is not configured server-side).
 * The session is persisted and the token attached to every API request by
 * `authInterceptor`; a 401 anywhere clears it (see api-error.interceptor).
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly router = inject(Router);
  private readonly http = inject(HttpClient);
  private readonly storage = inject(BrowserStorage);

  private readonly sessionSignal = signal<AdminSession | null>(this.restoreSession());

  readonly currentUser = computed<AdminUser | null>(() => this.sessionSignal()?.user ?? null);
  readonly isAuthenticated = computed(() => this.sessionSignal() !== null);
  /** Bearer token for outgoing API requests, or null while signed out. */
  readonly token = computed(() => this.sessionSignal()?.token ?? null);

  /**
   * Real backend sign-in. Errors (401 invalid credentials, 503 unconfigured,
   * 429 rate limited, network) propagate to the login form untouched —
   * the API's `message` is rendered verbatim.
   */
  signIn(username: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${API_URL}/auth/login`, { username, password }).pipe(
      tap(res => {
        const session: AdminSession = {
          token: res.token,
          expiresAt: res.expiresAt,
          user: {
            id: String(res.user?.id ?? ''),
            name: (res.user?.name ?? '').trim() || username.trim(),
            email: res.user?.email ?? ''
          }
        };
        this.sessionSignal.set(session);
        this.storage.set(STORAGE_KEYS.session, JSON.stringify(session));
        this.router.navigate(['/dashboard']);
      })
    );
  }

  logout(): void {
    this.sessionSignal.set(null);
    this.storage.remove(STORAGE_KEYS.session);
    this.router.navigate(['/auth/login']);
  }

  /**
   * Called by the HTTP layer when the API answers 401 — the token expired or
   * was revoked. Drops the session (the router guard then keeps the user on
   * the login screen) without double-handling login-page failures.
   */
  handleAuthFailure(): void {
    if (this.sessionSignal() === null) {
      return; // Already signed out (e.g., the failed request WAS the login attempt).
    }
    this.logout();
  }

  initials(): string {
    const name = this.userSignalName();
    return name
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map(part => part[0].toUpperCase())
      .join('');
  }

  private userSignalName(): string {
    return this.sessionSignal()?.user?.name ?? 'Admin';
  }

  private restoreSession(): AdminSession | null {
    const raw = this.storage.get(STORAGE_KEYS.session);
    if (!raw) return null;
    try {
      const session = JSON.parse(raw) as AdminSession;
      if (!session?.token || !session.user) return null;

      // Expired tokens are worthless — drop them instead of firing doomed requests.
      if (session.expiresAt && Date.parse(session.expiresAt) <= Date.now()) {
        this.storage.remove(STORAGE_KEYS.session);
        return null;
      }

      return session;
    } catch {
      // Corrupted entry (partial write, old build) — treat as signed out.
      this.storage.remove(STORAGE_KEYS.session);
      return null;
    }
  }
}
