// src/app/features/auth/register/register.component.ts
import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="min-h-screen flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
      <div class="max-w-md w-full space-y-8">
        <div>
          <h2 class="mt-6 text-center text-3xl font-extrabold text-gray-900 dark:text-white">
            Create your account
          </h2>
          <p class="mt-2 text-center text-sm text-gray-600 dark:text-gray-400">
            Or
            <a routerLink="/auth/login" class="font-medium text-blue-600 hover:text-blue-500 dark:text-blue-400 focus:outline-none focus:underline">
              sign in to existing account
            </a>
          </p>
        </div>
        
        <form [formGroup]="registerForm" (ngSubmit)="onSubmit()" class="mt-8 space-y-6">
          @if (errorMessage()) {
            <div 
              role="alert" 
              class="rounded-md bg-red-50 dark:bg-red-900/20 p-4 border border-red-200 dark:border-red-800"
              aria-live="polite"
            >
              <p class="text-sm text-red-800 dark:text-red-300">{{ errorMessage() }}</p>
            </div>
          }

          <div class="space-y-4">
            <div>
              <label for="name" class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Full name
              </label>
              <input
                id="name"
                type="text"
                formControlName="name"
                required
                autocomplete="name"
                class="appearance-none rounded-lg relative block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 placeholder-gray-500 dark:placeholder-gray-400 text-gray-900 dark:text-white bg-white dark:bg-gray-800 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent sm:text-sm transition-colors"
                [class.border-red-500]="registerForm.get('name')?.invalid && registerForm.get('name')?.touched"
                placeholder="John Doe"
                [attr.aria-invalid]="registerForm.get('name')?.invalid && registerForm.get('name')?.touched"
                [attr.aria-describedby]="registerForm.get('name')?.invalid && registerForm.get('name')?.touched ? 'name-error' : null"
              />
              @if (registerForm.get('name')?.invalid && registerForm.get('name')?.touched) {
                <p id="name-error" class="mt-1 text-sm text-red-600 dark:text-red-400" role="alert">
                  Name is required
                </p>
              }
            </div>

            <div>
              <label for="email" class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Email address
              </label>
              <input
                id="email"
                type="email"
                formControlName="email"
                required
                autocomplete="email"
                class="appearance-none rounded-lg relative block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 placeholder-gray-500 dark:placeholder-gray-400 text-gray-900 dark:text-white bg-white dark:bg-gray-800 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent sm:text-sm transition-colors"
                [class.border-red-500]="registerForm.get('email')?.invalid && registerForm.get('email')?.touched"
                placeholder="you@example.com"
                [attr.aria-invalid]="registerForm.get('email')?.invalid && registerForm.get('email')?.touched"
                [attr.aria-describedby]="registerForm.get('email')?.invalid && registerForm.get('email')?.touched ? 'email-error' : null"
              />
              @if (registerForm.get('email')?.invalid && registerForm.get('email')?.touched) {
                <p id="email-error" class="mt-1 text-sm text-red-600 dark:text-red-400" role="alert">
                  Please enter a valid email address
                </p>
              }
            </div>

            <div>
              <label for="password" class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                Password
              </label>
              <input
                id="password"
                type="password"
                formControlName="password"
                required
                autocomplete="new-password"
                class="appearance-none rounded-lg relative block w-full px-3 py-2 border border-gray-300 dark:border-gray-600 placeholder-gray-500 dark:placeholder-gray-400 text-gray-900 dark:text-white bg-white dark:bg-gray-800 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent sm:text-sm transition-colors"
                [class.border-red-500]="registerForm.get('password')?.invalid && registerForm.get('password')?.touched"
                placeholder="Minimum 6 characters"
                [attr.aria-invalid]="registerForm.get('password')?.invalid && registerForm.get('password')?.touched"
                [attr.aria-describedby]="registerForm.get('password')?.invalid && registerForm.get('password')?.touched ? 'password-error' : null"
              />
              @if (registerForm.get('password')?.invalid && registerForm.get('password')?.touched) {
                <p id="password-error" class="mt-1 text-sm text-red-600 dark:text-red-400" role="alert">
                  Password must be at least 6 characters
                </p>
              }
            </div>
          </div>

          <button
            type="submit"
            [disabled]="registerForm.invalid || isLoading()"
            class="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-lg text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            [attr.aria-busy]="isLoading()"
          >
            @if (isLoading()) {
              <span class="flex items-center">
                <svg class="animate-spin -ml-1 mr-3 h-5 w-5 text-white" fill="none" viewBox="0 0 24 24" aria-hidden="true">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                Creating account...
              </span>
            } @else {
              Create account
            }
          </button>
        </form>
      </div>
    </div>
  `
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly registerForm = this.fb.nonNullable.group({
    name: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  onSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const data = this.registerForm.getRawValue();

    this.authService.register(data).subscribe({
      next: () => {
        this.isLoading.set(false);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(err.error?.message || 'Failed to create account. Please try again.');
      }
    });
  }
}