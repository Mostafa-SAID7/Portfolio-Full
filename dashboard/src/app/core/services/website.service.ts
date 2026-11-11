import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, defer, finalize, tap, throwError } from 'rxjs';
import { Website, WebsitePayload, ProjectWebsiteAssignment, ProjectWebsiteAssignmentPayload } from '../models/website.model';
import { API_URL } from '../api.config';

const ENDPOINT = `${API_URL}/Websites`;

function byOrder(a: Website, b: Website): number {
  if (a.order !== b.order) return a.order - b.order;
  return a.name.localeCompare(b.name);
}

@Injectable({ providedIn: 'root' })
export class WebsiteService {
  private readonly http = inject(HttpClient);

  private readonly websitesSignal = signal<Website[]>([]);
  private readonly loadingSignal = signal(false);
  private readonly errorSignal = signal<string | null>(null);

  readonly websites = this.websitesSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();

  loadWebsites(): Observable<Website[]> {
    return defer(() => {
      this.loadingSignal.set(true);
      this.errorSignal.set(null);
      return this.http.get<Website[]>(ENDPOINT).pipe(
        tap(websites => {
          const list = (websites ?? []).sort(byOrder);
          this.websitesSignal.set(list);
        }),
        catchError(err => {
          this.errorSignal.set(err?.error?.message ?? err?.message ?? 'Failed to load websites.');
          return throwError(() => err);
        }),
        finalize(() => this.loadingSignal.set(false))
      );
    });
  }

  getActiveWebsites(): Observable<Website[]> {
    return this.http.get<Website[]>(`${ENDPOINT}/active`).pipe(
      catchError(err => throwError(() => err))
    );
  }

  createWebsite(payload: WebsitePayload): Observable<Website> {
    return this.http.post<Website>(ENDPOINT, payload).pipe(
      tap(created => {
        this.websitesSignal.update(list => [...list, created].sort(byOrder));
      }),
      catchError(err => throwError(() => err))
    );
  }

  updateWebsite(id: number, payload: WebsitePayload): Observable<Website> {
    return this.http.put<Website>(`${ENDPOINT}/${id}`, payload).pipe(
      tap(updated => {
        this.websitesSignal.update(list =>
          list.map(w => (w.id === id ? updated : w)).sort(byOrder)
        );
      }),
      catchError(err => throwError(() => err))
    );
  }

  deleteWebsite(id: number): Observable<void> {
    return this.http.delete<void>(`${ENDPOINT}/${id}`).pipe(
      tap(() => {
        this.websitesSignal.update(list => list.filter(w => w.id !== id));
      }),
      catchError(err => throwError(() => err))
    );
  }

  toggleActive(id: number): Observable<{ isActive: boolean }> {
    return this.http.patch<{ isActive: boolean }>(`${ENDPOINT}/${id}/toggle-active`, {}).pipe(
      tap(res => {
        this.websitesSignal.update(list =>
          list.map(w => (w.id === id ? { ...w, isActive: res.isActive } : w))
        );
      }),
      catchError(err => throwError(() => err))
    );
  }

  getProjectWebsites(projectId: number): Observable<ProjectWebsiteAssignment[]> {
    return this.http.get<ProjectWebsiteAssignment[]>(`${API_URL}/Projects/${projectId}/websites`);
  }

  updateProjectWebsites(projectId: number, assignments: ProjectWebsiteAssignmentPayload[]): Observable<void> {
    return this.http.put<void>(`${API_URL}/Projects/${projectId}/websites`, { websites: assignments });
  }
}

