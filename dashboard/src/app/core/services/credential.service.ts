import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, defer, finalize, tap, throwError } from 'rxjs';
import { Credential, CredentialPayload } from '../models/credential.model';
import { API_URL } from '../api.config';

const ENDPOINT = `${API_URL}/Credentials`;

/**
 * The career credentials behind the portfolio's Experience / Platforms /
 * Education / Certifications tabs.
 *
 * The API is the single source of truth: a row edited here is what the
 * public site renders, so the list is kept sorted exactly like the site
 * orders it (order, then year descending).
 */
@Injectable({ providedIn: 'root' })
export class CredentialService {
  private readonly http = inject(HttpClient);

  private readonly credentialsSignal = signal<Credential[]>([]);
  private readonly loadingSignal = signal(false);
  private readonly errorSignal = signal<string | null>(null);

  readonly credentials = this.credentialsSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();

  loadCredentials(): Observable<Credential[]> {
    return defer(() => {
      this.loadingSignal.set(true);
      this.errorSignal.set(null);
      return this.http.get<Credential[]>(ENDPOINT).pipe(
        tap(credentials => this.credentialsSignal.set([...(credentials ?? [])].sort(byOrder))),
        catchError(err => {
          this.errorSignal.set(err?.error?.message ?? err?.message ?? 'Failed to load credentials.');
          return throwError(() => err);
        }),
        finalize(() => this.loadingSignal.set(false))
      );
    });
  }

  createCredential(payload: CredentialPayload): Observable<Credential> {
    return this.http.post<Credential>(ENDPOINT, payload).pipe(
      tap(credential => this.credentialsSignal.update(list => [...list, credential].sort(byOrder))),
      catchError(err => throwError(() => err))
    );
  }

  /** Full replacement (the API's update verb is PUT, so every field is sent). */
  updateCredential(id: number, payload: CredentialPayload): Observable<void> {
    return this.http.put<void>(`${ENDPOINT}/${id}`, payload).pipe(
      tap(() => this.credentialsSignal.update(list =>
        list.map(credential => (credential.id === id ? { ...credential, ...payload } : credential)).sort(byOrder)
      )),
      catchError(err => throwError(() => err))
    );
  }

  deleteCredential(id: number): Observable<void> {
    return this.http.delete<void>(`${ENDPOINT}/${id}`).pipe(
      tap(() => this.credentialsSignal.update(list => list.filter(credential => credential.id !== id))),
      catchError(err => throwError(() => err))
    );
  }
}

function byOrder(a: Credential, b: Credential): number {
  return a.order - b.order || b.year.localeCompare(a.year);
}
