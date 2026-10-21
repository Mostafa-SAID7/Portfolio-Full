import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink],
  templateUrl: "./login.component.html"
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

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
    if (this.submitting) return;
    if (!this.signInForm.controls.username.value.trim()) this.signInForm.controls.username.setErrors({ required: true });
    if (this.signInForm.invalid) {
      this.signInForm.markAllAsTouched();
      return;
    }

    const { username, password } = this.signInForm.getRawValue();
    this.submitting = true;
    this.submitError = null;

    this.authService.signIn(username.trim(), password).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.submitting = false)
    ).subscribe({
      error: err => {
        this.submitError = err?.error?.message ?? 'Sign-in failed. Check your credentials and try again.';
      }
    });
  }
}
