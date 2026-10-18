// src/app/core/services/auth.service.ts
import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, of, tap } from 'rxjs';
import { User, AuthResponse, LoginCredentials, RegisterData } from '../models/user.model';
import { API_URL } from '../api.config';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly tokenKey = 'taskflow_token';
  private readonly userKey = 'taskflow_user';

  private readonly currentUserSignal = signal<User | null>(this.loadUserFromStorage());
  private readonly tokenSignal = signal<string | null>(this.loadTokenFromStorage());

  readonly currentUser = this.currentUserSignal.asReadonly();
  readonly token = this.tokenSignal.asReadonly();
  readonly isAuthenticated = computed(() => !!this.currentUserSignal() && !!this.tokenSignal());
  readonly isAdmin = computed(() => this.currentUserSignal()?.role === 'admin');
  readonly isDemoSession = computed(() => this.tokenSignal() === 'taskflow-demo-session');

  private loadUserFromStorage(): User | null {
    const userData = localStorage.getItem(this.userKey);
    return userData ? JSON.parse(userData) : null;
  }

  private loadTokenFromStorage(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  login(credentials: LoginCredentials): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${API_URL}/auth/login`, credentials).pipe(
      tap(response => this.handleAuthSuccess(response))
    );
  }

  register(data: RegisterData): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${API_URL}/auth/register`, data).pipe(
      tap(response => this.handleAuthSuccess(response))
    );
  }

  logout(): void {
    this.currentUserSignal.set(null);
    this.tokenSignal.set(null);
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.userKey);
    this.router.navigate(['/auth/login']);
  }

  enterDemoMode(): void {
    this.handleAuthSuccess({
      token: 'taskflow-demo-session',
      user: {
        id: 'demo-user',
        email: 'alex@task.local',
        name: 'Alex Morgan',
        role: 'member'
      }
    });
  }

  refreshUser(): Observable<User> {
    if (this.isDemoSession()) {
      const user = this.currentUserSignal();
      const demoUser: User = {
        id: 'demo-user',
        email: 'alex@task.local',
        name: 'Alex Morgan',
        role: 'member'
      };
      return of(user ?? demoUser);
    }

    return this.http.get<User>(`${API_URL}/auth/me`).pipe(
      tap(user => {
        this.currentUserSignal.set(user);
        localStorage.setItem(this.userKey, JSON.stringify(user));
      })
    );
  }

  private handleAuthSuccess(response: AuthResponse): void {
    this.tokenSignal.set(response.token);
    this.currentUserSignal.set(response.user);
    localStorage.setItem(this.tokenKey, response.token);
    localStorage.setItem(this.userKey, JSON.stringify(response.user));
    this.router.navigate(['/dashboard']);
  }
}