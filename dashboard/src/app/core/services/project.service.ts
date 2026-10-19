import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, of, tap, throwError } from 'rxjs';
import { Project, ProjectPayload } from '../models/project.model';
import { API_URL } from '../api.config';

const ENDPOINT = `${API_URL}/Projects`;

@Injectable({ providedIn: 'root' })
export class ProjectService {
  private readonly http = inject(HttpClient);

  private readonly projectsSignal = signal<Project[]>([]);
  private readonly loadingSignal = signal(false);
  private readonly errorSignal = signal<string | null>(null);

  readonly projects = this.projectsSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();

  readonly featured = computed(() => this.projectsSignal().filter(project => project.featured));

  loadProjects(): Observable<Project[]> {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);

    return this.http.get<Project[]>(ENDPOINT).pipe(
      tap(projects => {
        this.projectsSignal.set(projects ?? []);
        this.loadingSignal.set(false);
      }),
      catchError(err => {
        this.errorSignal.set(this.message(err, 'Failed to load projects.'));
        this.loadingSignal.set(false);
        return throwError(() => err);
      })
    );
  }

  getProject(id: number): Observable<Project> {
    return this.http.get<Project>(`${ENDPOINT}/${id}`).pipe(
      catchError(err => throwError(() => err))
    );
  }

  createProject(payload: ProjectPayload): Observable<Project> {
    return this.http.post<Project>(ENDPOINT, payload).pipe(
      tap(project => this.projectsSignal.update(list => [project, ...list])),
      catchError(err => throwError(() => err))
    );
  }

  /** Backend update endpoint is PUT and expects the full entity. */
  updateProject(id: number, payload: ProjectPayload): Observable<void> {
    return this.http.put<void>(`${ENDPOINT}/${id}`, { ...payload, id }).pipe(
      tap(() => this.patchLocal(id, payload)),
      catchError(err => throwError(() => err))
    );
  }

  deleteProject(id: number): Observable<void> {
    return this.http.delete<void>(`${ENDPOINT}/${id}`).pipe(
      tap(() => this.projectsSignal.update(list => list.filter(project => project.id !== id))),
      catchError(err => throwError(() => err))
    );
  }

  /** Refresh a single entry after a PUT (the API returns 204 No Content). */
  refreshProject(id: number): void {
    this.getProject(id).subscribe({
      next: updated => this.patchLocal(id, updated),
      error: () => undefined
    });
  }

  private patchLocal(id: number, changes: Partial<Project>): void {
    this.projectsSignal.update(list =>
      list.map(project => (project.id === id ? { ...project, ...changes, id } : project))
    );
  }

  private message(err: any, fallback: string): string {
    if (typeof err?.error === 'string' && err.error.trim()) return err.error;
    return err?.error?.message ?? err?.message ?? fallback;
  }
}
