import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="flex min-h-screen bg-[#0b0d12] text-[#f4f1ea]">
      <div class="relative hidden w-[48%] overflow-hidden border-r border-white/[0.07] lg:block">
        <div class="absolute inset-0 bg-[radial-gradient(circle_at_20%_20%,rgba(255,107,53,.3),transparent_30%),radial-gradient(circle_at_80%_80%,rgba(155,140,255,.17),transparent_35%)]"></div>
        <div class="relative flex h-full flex-col justify-between p-12 xl:p-16">
          <a routerLink="/auth/login" class="flex items-center gap-3">
            <span class="flex h-10 w-10 items-center justify-center rounded-xl bg-[#ff6b35] text-[#17110e]">
              <svg class="h-5 w-5" viewBox="0 0 24 24" fill="none"><path d="M6 4.75h8.5L18 8.25v11H6v-14.5Z" stroke="currentColor" stroke-width="1.8"/><path d="M14 4.75v3.5h4M9 12h6M9 15.5h4" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/></svg>
            </span>
            <span class="font-display text-2xl font-bold tracking-[-0.05em]">task<span class="text-[#ff6b35]">.</span></span>
          </a>
          <div class="max-w-lg">
            <p class="eyebrow mb-5 text-[#ff9d7c]">A calmer way to get things done</p>
            <h1 class="font-display text-6xl font-semibold leading-[0.94] tracking-[-0.08em] xl:text-7xl">Your best work<br><span class="text-[#ff6b35]">starts here.</span></h1>
            <p class="mt-7 max-w-sm text-base leading-7 text-[#8f96a5]">One thoughtful space for your tasks, your team, and the work that matters.</p>
          </div>
          <div class="flex items-center justify-between text-xs text-[#687080]"><span>© 2026 task.</span><span>Built for focus</span></div>
        </div>
      </div>
      <div class="flex flex-1 items-center justify-center px-5 py-12 sm:px-10">
        <div class="w-full max-w-[380px]">
          <div class="mb-10 lg:hidden"><span class="font-display text-2xl font-bold tracking-[-0.05em]">task<span class="text-[#ff6b35]">.</span></span></div>
          <div class="mb-9">
            <p class="eyebrow mb-3 text-[#ff8254]">Welcome back</p>
            <h2 class="font-display text-3xl font-semibold tracking-[-0.06em]">Sign in to your workspace</h2>
            <p class="mt-3 text-sm leading-6 text-[#8f96a5]">Pick up right where you left off.</p>
          </div>
          @if (errorMessage()) {
            <div role="alert" class="mb-5 rounded-xl border border-[#ff6b35]/30 bg-[#ff6b35]/10 p-3 text-sm text-[#ffb38e]">{{ errorMessage() }}</div>
          }
          <form [formGroup]="loginForm" (ngSubmit)="onSubmit()" class="space-y-5">
            <div>
              <label for="email" class="mb-2 block text-xs font-semibold text-[#c8ccd4]">Email address</label>
              <input id="email" type="email" formControlName="email" autocomplete="email" placeholder="you@example.com" class="w-full rounded-xl border border-white/[0.1] bg-white/[0.04] px-4 py-3 text-sm text-[#f4f1ea] outline-none transition placeholder:text-[#687080] focus:border-[#ff6b35] focus:ring-4 focus:ring-[#ff6b35]/10" />
              @if (loginForm.get('email')?.invalid && loginForm.get('email')?.touched) { <p class="mt-2 text-xs text-[#ff8b61]">Enter a valid email address.</p> }
            </div>
            <div>
              <div class="mb-2 flex items-center justify-between"><label for="password" class="block text-xs font-semibold text-[#c8ccd4]">Password</label><button type="button" class="text-xs font-semibold text-[#ff8254]">Forgot password?</button></div>
              <input id="password" type="password" formControlName="password" autocomplete="current-password" placeholder="Enter your password" class="w-full rounded-xl border border-white/[0.1] bg-white/[0.04] px-4 py-3 text-sm text-[#f4f1ea] outline-none transition placeholder:text-[#687080] focus:border-[#ff6b35] focus:ring-4 focus:ring-[#ff6b35]/10" />
              @if (loginForm.get('password')?.invalid && loginForm.get('password')?.touched) { <p class="mt-2 text-xs text-[#ff8b61]">Password must be at least 6 characters.</p> }
            </div>
            <button type="submit" [disabled]="loginForm.invalid || isLoading()" class="orange-glow flex w-full items-center justify-center rounded-xl bg-[#ff6b35] px-4 py-3 text-sm font-bold text-[#1d120d] transition hover:bg-[#ff8254] disabled:cursor-not-allowed disabled:opacity-50">
              @if (isLoading()) { Signing you in... } @else { Continue to task. }
            </button>
          </form>
          <div class="my-7 flex items-center gap-3 text-[10px] uppercase tracking-widest text-[#687080]"><span class="h-px flex-1 bg-white/[0.08]"></span>or<span class="h-px flex-1 bg-white/[0.08]"></span></div>
          <button type="button" (click)="exploreDemo()" class="flex w-full items-center justify-center gap-3 rounded-xl border border-white/[0.1] bg-white/[0.03] px-4 py-3 text-sm font-semibold text-[#c8ccd4] transition hover:bg-white/[0.07]"><span class="font-display text-base font-bold text-[#ff8254]">✦</span> Explore the demo workspace</button>
          <p class="mt-8 text-center text-sm text-[#8f96a5]">New to task.? <a routerLink="/auth/register" class="font-semibold text-[#ff8254] hover:text-[#ffb38e]">Create an account</a></p>
        </div>
      </div>
    </div>
  `
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly loginForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  onSubmit(): void {
    if (this.loginForm.invalid) { this.loginForm.markAllAsTouched(); return; }
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.authService.login(this.loginForm.getRawValue()).subscribe({
      next: () => this.isLoading.set(false),
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.error?.message || 'Unable to sign in right now.');
      }
    });
  }

  exploreDemo(): void {
    this.authService.enterDemoMode();
  }
}