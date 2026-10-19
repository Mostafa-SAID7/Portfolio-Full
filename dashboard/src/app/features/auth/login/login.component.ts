import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
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
              Projects, skills, experience, blog posts and visitor messages — all served live by the Portfolio API.
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
              Enter your name and email to unlock the admin workspace on this device.
            </p>
          </div>

          <form [formGroup]="signInForm" (ngSubmit)="onSubmit()" class="space-y-5" novalidate>
            <div>
              <label for="name" class="field-label">Your name</label>
              <input
                id="name"
                type="text"
                formControlName="name"
                autocomplete="name"
                placeholder="Mostafa Said"
                class="input"
                [attr.aria-invalid]="signInForm.get('name')?.invalid && signInForm.get('name')?.touched ? 'true' : null"
              />
              @if (signInForm.get('name')?.invalid && signInForm.get('name')?.touched) {
                <p class="field-error" role="alert">Name is required.</p>
              }
            </div>
            <div>
              <label for="email" class="field-label">Email address</label>
              <input
                id="email"
                type="email"
                formControlName="email"
                autocomplete="email"
                placeholder="you@example.com"
                class="input"
                [attr.aria-invalid]="signInForm.get('email')?.invalid && signInForm.get('email')?.touched ? 'true' : null"
              />
              @if (signInForm.get('email')?.invalid && signInForm.get('email')?.touched) {
                <p class="field-error" role="alert">Enter a valid email address.</p>
              }
            </div>
            <button type="submit" [disabled]="signInForm.invalid" class="btn-primary w-full">
              Open dashboard
            </button>
          </form>

          <p class="mt-8 rounded-xl border border-line/10 bg-surface p-3 text-xs leading-5 text-muted">
            The Portfolio API does not expose authentication endpoints yet, so this sign-in is a
            local unlock for this browser. Add JWT auth to the API to secure it for everyone.
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

  readonly signInForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]]
  });

  onSubmit(): void {
    if (this.signInForm.invalid) {
      this.signInForm.markAllAsTouched();
      return;
    }
    const { name, email } = this.signInForm.getRawValue();
    this.authService.signIn(name, email);
  }
}
