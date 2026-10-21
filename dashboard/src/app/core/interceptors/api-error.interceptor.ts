import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { ToastService } from '../../shared/toast.component';

export interface ApiError {
  message: string;
  errors: Record<string, string[]>;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

export function normalizeApiError(error: HttpErrorResponse): ApiError {
  const body: unknown = error.error;
  const errors: Record<string, string[]> = {};
  if (isRecord(body) && isRecord(body['errors'])) {
    for (const [field, messages] of Object.entries(body['errors'])) {
      if (Array.isArray(messages)) {
        errors[field] = messages.filter((message): message is string => typeof message === 'string');
      }
    }
  }
  if (Array.isArray(body)) {
    for (const failure of body) {
      if (!isRecord(failure) || typeof failure['errorMessage'] !== 'string') continue;
      const field = typeof failure['propertyName'] === 'string' ? failure['propertyName'] : '';
      errors[field] = [...(errors[field] ?? []), failure['errorMessage']];
    }
  }
  const details = isRecord(body)
    ? [body['message'], body['detail'], body['title']].find(
        (value): value is string => typeof value === 'string' && value.trim().length > 0
      )
    : undefined;
  const message = error.status === 0
    ? 'Cannot reach the Portfolio API. Make sure the API is running and try again.'
    : typeof body === 'string' && body.trim()
      ? body
      : details ?? (Object.values(errors).flat().join(' ') || `Request failed (${error.status} ${error.statusText}).`);
  return { message, errors };
}

export const apiErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const toast = inject(ToastService);

  return next(req).pipe(
    catchError((err: unknown) => {
      if (!(err instanceof HttpErrorResponse)) return throwError(() => err);
      const error = normalizeApiError(err);
      const isLoginAttempt = req.url.split('?')[0].endsWith('/auth/login');
      if (err.status === 401 && !isLoginAttempt) {
        auth.handleAuthFailure();
        toast.show('Your session has expired — please sign in again.', 'error');
      } else if (err.status === 403) {
        toast.show(error.message, 'error');
      }
      return throwError(() => new HttpErrorResponse({
        url: err.url ?? undefined,
        status: err.status,
        statusText: err.statusText,
        headers: err.headers,
        error
      }));
    })
  );
};
