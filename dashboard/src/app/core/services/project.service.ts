import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, defer, finalize, tap, throwError } from 'rxjs';
import { Project, ProjectPayload } from '../models/project.model';
import { UploadedImage } from '../portfolio-origin';
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
    return defer(() => {
      this.loadingSignal.set(true);
      this.errorSignal.set(null);
      return this.http.get<Project[]>(ENDPOINT).pipe(
        tap(projects => this.projectsSignal.set(projects ?? [])),
        catchError((err: unknown) => {
          this.errorSignal.set(this.message(err, 'Failed to load projects.'));
          return throwError(() => err);
        }),
        finalize(() => this.loadingSignal.set(false))
      );
    });
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

  /**
   * Upload a cover image to the backend store (App_Data/uploads, served at
   * /api/files/*). The returned `url` is a relative path — store it on the
   * project as-is; every frontend resolves it through its own /api proxy.
   */
  uploadCover(file: File, projectTitle: string): Observable<UploadedImage> {
    const form = new FormData();
    form.append('file', file, file.name);
    form.append('projectTitle', projectTitle);
    return this.http.post<UploadedImage>(`${API_URL}/upload/image`, form).pipe(
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

  private message(err: unknown, fallback: string): string {
    if (!(err instanceof HttpErrorResponse)) return fallback;
    const body: unknown = err.error;
    if (typeof body === 'string' && body.trim()) return body;
    if (typeof body === 'object' && body !== null && 'message' in body && typeof body.message === 'string') return body.message;
    return err.message || fallback;
  }
}
