import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

/**
 * Normalizes API failures so every screen can show a human-readable
 * message (services read `error.error.message`), and turns a dead API
 * (status 0 / network error) into an actionable hint instead of a blank
 * "Http failure response" string.
 */
export const apiErrorInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(
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
