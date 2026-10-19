import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { ToastService } from '../../shared/toast.component';

/**
 * Normalizes API failures so every screen can show a human-readable
 * message (services read `error.error.message`), turns a dead API
 * (status 0 / network error) into an actionable hint instead of a blank
 * "Http failure response" string, and handles global auth outcomes
 * (issue #20): a 401 anywhere clears the session and sends the user to the
 * login screen with a toast; a 403 surfaces the permission denial.
 *
 * Services are injected at interceptor entry (injection context) — the
 * catchError callback itself runs outside it.
 */
export const apiErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const toast = inject(ToastService);

  return next(req).pipe(
    catchError((err: unknown) => {
      if (err instanceof HttpErrorResponse) {
        let message: string;

        if (err.status === 0) {
          message =
            'Cannot reach the Portfolio API. Make sure it is running (dotnet run, http://localhost:5039) and reload.';
        } else if (typeof err.error === 'string' && err.error.trim()) {
          message = err.error;
        } else if (Array.isArray(err.error) && err.error.length && err.error[0]?.errorMessage) {
          // ASP.NET model validation errors come back as arrays.
          message = err.error.map((e: { errorMessage: string }) => e.errorMessage).join(' ');
        } else {
          message = err.error?.message ?? `Request failed (${err.status} ${err.statusText}).`;
        }

        // --- Global auth outcomes (issue #20) -------------------------------
        const isLoginAttempt = err.url?.includes('/auth/login') ?? false;
        if (err.status === 401 && !isLoginAttempt) {
          // Token expired/revoked: drop the session (guard redirects to login)
          // and explain why — instead of failing silently on every screen.
          auth.handleAuthFailure();
          toast.show('Your session has expired — please sign in again.', 'error');
        } else if (err.status === 403) {
          toast.show(message || 'You do not have permission to perform this action.', 'error');
        }

        const normalized = new HttpErrorResponse({
          url: err.url ?? undefined,
          status: err.status,
          statusText: err.statusText,
          error: { message }
        });
        return throwError(() => normalized);
      }

      return throwError(() => err);
    })
  );
};
