import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, tap, throwError } from 'rxjs';
import { Skill, SkillPayload } from '../models/skill.model';
import { API_URL } from '../api.config';

const ENDPOINT = `${API_URL}/Skills`;

@Injectable({ providedIn: 'root' })
export class SkillService {
  private readonly http = inject(HttpClient);

  private readonly skillsSignal = signal<Skill[]>([]);
  private readonly loadingSignal = signal(false);
  private readonly errorSignal = signal<string | null>(null);

  readonly skills = this.skillsSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();

  loadSkills(): Observable<Skill[]> {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);

    return this.http.get<Skill[]>(ENDPOINT).pipe(
      tap(skills => {
        this.skillsSignal.set(skills ?? []);
        this.loadingSignal.set(false);
      }),
      catchError(err => {
        this.errorSignal.set(err?.error?.message ?? err?.message ?? 'Failed to load skills.');
        this.loadingSignal.set(false);
        return throwError(() => err);
      })
    );
  }

  createSkill(payload: SkillPayload): Observable<Skill> {
    return this.http.post<Skill>(ENDPOINT, payload).pipe(
      tap(skill => this.skillsSignal.update(list => [...list, skill])),
      catchError(err => throwError(() => err))
    );
  }

  updateSkill(id: number, payload: SkillPayload): Observable<void> {
    return this.http.put<void>(`${ENDPOINT}/${id}`, payload).pipe(
      tap(() =>
        this.skillsSignal.update(list =>
          list.map(skill => (skill.id === id ? { ...skill, ...payload } : skill))
        )
      ),
      catchError(err => throwError(() => err))
    );
  }

  toggleVisibility(skill: Skill): Observable<void> {
    // Send an explicit payload (never the raw model): the API binds
    // UpdateSkillDto and ignores server-managed fields anyway.
    return this.updateSkill(skill.id, {
      name: skill.name,
      category: skill.category,
      proficiency: skill.proficiency,
      iconUrl: skill.iconUrl,
      description: skill.description,
      order: skill.order,
      isActive: !skill.isActive
    });
  }

  deleteSkill(id: number): Observable<void> {
    return this.http.delete<void>(`${ENDPOINT}/${id}`).pipe(
      tap(() => this.skillsSignal.update(list => list.filter(skill => skill.id !== id))),
      catchError(err => throwError(() => err))
    );
  }
}
