import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, defer, finalize, tap, throwError } from 'rxjs';
import { AnalyticsReport } from '../models/analytics.model';
import { API_URL } from '../api.config';

const ENDPOINT = `${API_URL}/Analytics`;

/**
 * GA4 reports for the private dashboard.
 *
 * The API is the single source of truth: it holds the service account
 * credentials, validates the date range, caches the result and returns a
 * focused DTO. This service only translates the HTTP response into the
 * shape the dashboard renders — it never parses GA4's raw payloads.
 */
@Injectable({ providedIn: 'root' })
export class AnalyticsService {
  private readonly http = inject(HttpClient);

  private readonly reportSignal = signal<AnalyticsReport | null>(null);
  private readonly loadingSignal = signal(false);
  private readonly errorSignal = signal<string | null>(null);
  private readonly notConfiguredSignal = signal(false);

  readonly report = this.reportSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();
  readonly notConfigured = this.notConfiguredSignal.asReadonly();

  /** Format a Date as the yyyy-MM-dd string the API expects. */
  static iso(date: Date): string {
    return date.toISOString().slice(0, 10);
  }

  /** The default window: the last 28 days, matching GA4's own default view. */
  static defaultRange(): { start: string; end: string } {
    const today = new Date();
    const start = new Date(today);
    start.setDate(start.getDate() - 27);
    return { start: AnalyticsService.iso(start), end: AnalyticsService.iso(today) };
  }

  loadReport(start: string, end: string): Observable<AnalyticsReport> {
    return defer(() => {
      this.loadingSignal.set(true);
      this.errorSignal.set(null);
      this.notConfiguredSignal.set(false);
      return this.http.get<AnalyticsReport>(`${ENDPOINT}/report`, { params: { start, end } }).pipe(
        tap(report => {
          this.reportSignal.set(report);
          this.notConfiguredSignal.set(report.notConfigured ?? false);
        }),
        catchError((err: unknown) => {
          const body: unknown = err instanceof HttpErrorResponse ? err.error : err;
          const message = typeof body === 'object' && body !== null && 'message' in body
            ? String((body as { message: unknown }).message)
            : err instanceof Error ? err.message : 'Failed to load analytics.';
          if (err instanceof HttpErrorResponse && err.status === 501) {
            this.notConfiguredSignal.set(true);
            this.errorSignal.set(message);
          } else {
            this.errorSignal.set(message);
          }
          return throwError(() => err);
        }),
        finalize(() => this.loadingSignal.set(false))
      );
    });
  }
}
