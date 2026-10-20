import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="flex min-h-screen bg-canvas text-ink">
      <!-- Brand panel (desktop only) -->
      <div class="relative hidden w-[46%] overflow-hidden border-r border-line/10 lg:block">
        <div class="absolute inset-0 bg-[radial-gradient(circle_at_25%_15%,rgb(var(--accent)/0.22),transparent_45%),radial-gradient(circle_at_80%_85%,rgb(var(--violet)/0.14),transparent_45%)]"></div>
        <div class="relative flex h-full flex-col justify-between p-12 xl:p-16">
          <a routerLink="/auth/login" class="flex items-center gap-3">
            <span class="flex h-10 w-10 items-center justify-center rounded-xl bg-accent text-on-accent">
              <svg class="h-5 w-5" viewBox="0 0 24 24" fill="none" aria-hidden="true"><path d="M4 13.5 12 5l8 8.5M6.5 11.5v7h11v-7M9.5 18.5v-4h5v4" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/></svg>
            </span>
            <span class="font-display text-2xl font-bold tracking-[-0.05em]">Portfolio<span class="text-accent">.</span>admin</span>
          </a>
          <div class="max-w-lg">
            <p class="eyebrow mb-5 text-accent-text">Content control center</p>
            <h1 class="font-display text-6xl font-semibold leading-[0.94] tracking-[-0.06em] xl:text-7xl">
              Run your portfolio<br><span class="text-accent">from one place.</span>
            </h1>
            <p class="mt-7 max-w-sm text-base leading-7 text-soft">
              Projects, skills, experience and visitor messages — all served live by the Portfolio API.
            </p>
          </div>
          <div class="flex items-center justify-between text-xs text-muted"><span>© {{ year }}</span><span>Portfolio API · v1</span></div>
        </div>
      </div>

      <!-- Sign-in panel -->
      <div class="flex flex-1 items-center justify-center px-5 py-12 sm:px-10">
        <div class="w-full max-w-[380px]">
          <div class="mb-10 lg:hidden">
            <span class="font-display text-2xl font-bold tracking-[-0.05em]">Portfolio<span class="text-accent">.</span>admin</span>
          </div>
          <div class="mb-9">
            <p class="eyebrow mb-3 text-accent-text">Welcome back</p>
            <h2 class="font-display text-3xl font-semibold tracking-[-0.05em]">Open the dashboard</h2>
            <p class="mt-3 text-sm leading-6 text-soft">
              Sign in with your admin credentials — they are verified by the Portfolio API and a JWT is issued for this session.
            </p>
          </div>

          <form [formGroup]="signInForm" (ngSubmit)="onSubmit()" class="space-y-5" novalidate ngNativeValidate>
            <div>
              <label for="username" class="field-label">Username</label>
              <input
                id="username"
                type="text"
                formControlName="username"
                autocomplete="username"
                placeholder="admin"
                class="input"
                [attr.aria-invalid]="signInForm.get('username')?.invalid && signInForm.get('username')?.touched ? 'true' : null"
              />
              @if (signInForm.get('username')?.invalid && signInForm.get('username')?.touched) {
                <p class="field-error" role="alert">Username is required.</p>
              }
            </div>
            <div>
              <label for="password" class="field-label">Password</label>
              <div class="relative">
                <input
                  id="password"
                  [type]="showPassword ? 'text' : 'password'"
                  formControlName="password"
                  autocomplete="current-password"
                  placeholder="••••••••"
                  class="input pr-11"
                  [attr.aria-invalid]="signInForm.get('password')?.invalid && signInForm.get('password')?.touched ? 'true' : null"
                />
                <button
                  type="button"
                  class="absolute right-1.5 top-1/2 flex h-8 w-8 -translate-y-1/2 items-center justify-center rounded-lg text-muted transition hover:bg-line/[0.06] hover:text-ink"
                  (click)="togglePasswordVisibility()"
                  [attr.aria-label]="showPassword ? 'Hide password' : 'Show password'"
                  [attr.aria-pressed]="showPassword"
                  title="{{ showPassword ? 'Hide password' : 'Show password' }}"
                >
                  @if (showPassword) {
                    <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M3 3l18 18M10.6 10.6a2 2 0 0 0 2.8 2.8M9.4 5.3A9.5 9.5 0 0 1 12 5c5 0 9 4.5 9 7 0 .9-.4 1.8-1.1 2.7M6.2 6.2C4.4 7.4 3 9.4 3 12c0 2.5 4 7 9 7 1.6 0 3.1-.5 4.3-1.3"/></svg>
                  } @else {
                    <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M3 12s3.6-7 9-7 9 7 9 7-3.6 7-9 7-9-7-9-7Z"/><circle cx="12" cy="12" r="2.8"/></svg>
                  }
                </button>
              </div>
              @if (signInForm.get('password')?.invalid && signInForm.get('password')?.touched) {
                <p class="field-error" role="alert">Password is required.</p>
              }
            </div>

            @if (submitError) {
              <div class="rounded-xl border border-danger/30 bg-danger/10 p-3 text-sm text-danger" role="alert">
                {{ submitError }}
              </div>
            }

            <button type="submit" [disabled]="signInForm.invalid || submitting" class="btn-primary w-full">
              {{ submitting ? 'Signing in…' : 'Sign in' }}
            </button>
          </form>

          <p class="mt-8 rounded-xl border border-line/10 bg-surface p-3 text-xs leading-5 text-muted">
            Credentials are configured on the API server (Auth:Username / Auth:Password). Sign-in is rate
            limited, and every admin write request requires this session's token.
          </p>
        </div>
      </div>
    </div>
  `
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);

  readonly year = new Date().getFullYear();

  submitting = false;
  submitError: string | null = null;
  /** Whether the password is currently revealed (eye toggle). */
  showPassword = false;

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  readonly signInForm = this.fb.nonNullable.group({
    username: ['', [Validators.required]],
    password: ['', [Validators.required]]
  });

  onSubmit(): void {
    if (this.signInForm.invalid) {
      this.signInForm.markAllAsTouched();
      return;
    }

    const { username, password } = this.signInForm.getRawValue();
    this.submitting = true;
    this.submitError = null;

    this.authService.signIn(username.trim(), password).subscribe({
      next: () => {
        this.submitting = false;
      },
      error: err => {
        this.submitting = false;
        this.submitError =
          err?.error?.message ?? 'Sign-in failed. Check your credentials and try again.';
      }
    });
  }
}
