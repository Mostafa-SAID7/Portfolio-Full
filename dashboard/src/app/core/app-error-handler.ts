import { ErrorHandler, Injectable } from '@angular/core';

/**
 * App-wide error boundary: every exception Angular cannot attribute to a
 * specific caller (renderer, template, subscription crashes) lands here
 * instead of dying silently. HTTP and form errors are handled upstream
 * (api-error.interceptor / components), so what reaches this handler is
 * always unexpected — logged once, clearly labelled, never thrown onward
 * (an interrupted change-detection run must not cascade).
 */
@Injectable({ providedIn: 'root' })
export class AppErrorHandler implements ErrorHandler {
  handleError(error: unknown): void {
    console.groupCollapsed(
      '%c[Portfolio Admin] unexpected error',
      'color: #f97316; font-weight: 600',
    );
    console.error(error);
    console.groupEnd();
  }
}
