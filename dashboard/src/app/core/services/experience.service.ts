import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, tap, throwError } from 'rxjs';
import { Experience, ExperiencePayload } from '../models/experience.model';
import { API_URL } from '../api.config';

const ENDPOINT = `${API_URL}/Experience`;

@Injectable({ providedIn: 'root' })
export class ExperienceService {
  private readonly http = inject(HttpClient);

  private readonly experienceSignal = signal<Experience[]>([]);
  private readonly loadingSignal = signal(false);
  private readonly errorSignal = signal<string | null>(null);

  readonly experience = this.experienceSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();

  loadExperience(): Observable<Experience[]> {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);

    return this.http.get<Experience[]>(ENDPOINT).pipe(
      tap(items => {
        this.experienceSignal.set(items ?? []);
        this.loadingSignal.set(false);
      }),
      catchError(err => {
        this.errorSignal.set(err?.error?.message ?? err?.message ?? 'Failed to load experience.');
        this.loadingSignal.set(false);
        return throwError(() => err);
      })
    );
  }

  createExperience(payload: ExperiencePayload): Observable<Experience> {
    return this.http.post<Experience>(ENDPOINT, payload).pipe(
      tap(item => this.experienceSignal.update(list => [...list, item])),
      catchError(err => throwError(() => err))
    );
  }

  updateExperience(id: number, payload: ExperiencePayload): Observable<void> {
    return this.http.put<void>(`${ENDPOINT}/${id}`, payload).pipe(
      tap(() =>
        this.experienceSignal.update(list =>
          list.map(item => (item.id === id ? { ...item, ...payload } : item))
        )
      ),
      catchError(err => throwError(() => err))
    );
  }

  deleteExperience(id: number): Observable<void> {
    return this.http.delete<void>(`${ENDPOINT}/${id}`).pipe(
      tap(() => this.experienceSignal.update(list => list.filter(item => item.id !== id))),
      catchError(err => throwError(() => err))
    );
  }
}
