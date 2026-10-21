import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, defer, finalize, tap, throwError } from 'rxjs';
import { Category, CategoryPayload } from '../models/category.model';
import { API_URL } from '../api.config';

const ENDPOINT = `${API_URL}/Categories`;

/**
 * The project categories: the list behind the dashboard's category dropdown
 * and the filter chips on the public portfolio. A category typed in the
 * project form is registered by the API when the project is saved, so it
 * becomes manageable right after.
 */
@Injectable({ providedIn: 'root' })
export class CategoryService {
  private readonly http = inject(HttpClient);

  private readonly categoriesSignal = signal<Category[]>([]);
  private readonly loadingSignal = signal(false);
  private readonly errorSignal = signal<string | null>(null);

  readonly categories = this.categoriesSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();

  /** The names offered in the project form's category dropdown. */
  readonly names = signal<string[]>([]);

  loadCategories(): Observable<Category[]> {
    return defer(() => {
      this.loadingSignal.set(true);
      this.errorSignal.set(null);
      return this.http.get<Category[]>(ENDPOINT).pipe(
        tap(categories => {
          const list = categories ?? [];
          this.categoriesSignal.set(list);
          this.names.set(list.filter(c => c.isActive).map(c => c.nameEn));
        }),
        catchError(err => {
          this.errorSignal.set(err?.error?.message ?? err?.message ?? 'Failed to load categories.');
          return throwError(() => err);
        }),
        finalize(() => this.loadingSignal.set(false))
      );
    });
  }

  createCategory(payload: CategoryPayload): Observable<Category> {
    return this.http.post<Category>(ENDPOINT, payload).pipe(
      tap(category => {
        this.categoriesSignal.update(list => [...list, category].sort(byOrder));
        this.names.set(this.categoriesSignal().filter(c => c.isActive).map(c => c.nameEn));
      }),
      catchError(err => throwError(() => err))
    );
  }

  updateCategory(id: number, payload: CategoryPayload): Observable<void> {
    return this.http.put<void>(`${ENDPOINT}/${id}`, payload).pipe(
      tap(() => {
        this.categoriesSignal.update(list =>
          list.map(category => (category.id === id ? { ...category, ...payload } : category)).sort(byOrder)
        );
        this.names.set(this.categoriesSignal().filter(c => c.isActive).map(c => c.nameEn));
      }),
      catchError(err => throwError(() => err))
    );
  }

  deleteCategory(id: number): Observable<void> {
    return this.http.delete<void>(`${ENDPOINT}/${id}`).pipe(
      tap(() => {
        this.categoriesSignal.update(list => list.filter(category => category.id !== id));
        this.names.set(this.categoriesSignal().filter(c => c.isActive).map(c => c.nameEn));
      }),
      catchError(err => throwError(() => err))
    );
  }
}

function byOrder(a: Category, b: Category): number {
  return a.order - b.order || a.nameEn.localeCompare(b.nameEn);
}
