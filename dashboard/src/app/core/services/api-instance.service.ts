import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, of, tap } from 'rxjs';
import { API_URL } from '../api.config';

/** Shape of GET /api/v1/health. */
interface ApiHealth {
  status: string;
  database: string;
  /** Which instance answered - the dev API and the production API share one database. */
  instance?: string;
  time: string;
}

/**
 * Tells the admin which API instance the dashboard is talking to.
 *
 * The dev API and the production API share the same database but not the
 * same upload folder, so a cover uploaded through the local dashboard is
 * invisible on the live site (and vice versa). Showing the instance name
 * makes that visible instead of mysterious.
 */
@Injectable({ providedIn: 'root' })
export class ApiInstanceService {
  private readonly http = inject(HttpClient);

  private readonly instanceSignal = signal<string | null>(null);
  readonly instance = this.instanceSignal.asReadonly();

  /** Ask the API which instance it is (once, on start-up). */
  load(): void {
    this.http
      .get<ApiHealth>(`${API_URL}/health`)
      .pipe(
        tap(health => this.instanceSignal.set(health.instance ?? 'unknown')),
        catchError(() => of(null))
      )
      .subscribe();
  }
}
