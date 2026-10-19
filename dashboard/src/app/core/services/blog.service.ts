import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, tap, throwError } from 'rxjs';
import { BlogPost } from '../models/blog.model';
import { API_URL } from '../api.config';

const ENDPOINT = `${API_URL}/Blog`;

@Injectable({ providedIn: 'root' })
export class BlogService {
  private readonly http = inject(HttpClient);

  private readonly postsSignal = signal<BlogPost[]>([]);
  private readonly loadingSignal = signal(false);
  private readonly errorSignal = signal<string | null>(null);

  readonly posts = this.postsSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();

  loadPosts(): Observable<BlogPost[]> {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);

    return this.http.get<BlogPost[]>(ENDPOINT).pipe(
      tap(posts => {
        this.postsSignal.set(posts ?? []);
        this.loadingSignal.set(false);
      }),
      catchError(err => {
        this.errorSignal.set(err?.error?.message ?? err?.message ?? 'Failed to load posts.');
        this.loadingSignal.set(false);
        return throwError(() => err);
      })
    );
  }

  /** PUT api/Blog/{id} expects the full entity. */
  updatePost(post: BlogPost, changes: Partial<BlogPost>): Observable<void> {
    const payload = { ...post, ...changes };
    return this.http.put<void>(`${ENDPOINT}/${post.id}`, payload).pipe(
      tap(() =>
        this.postsSignal.update(list =>
          list.map(item => (item.id === post.id ? payload : item))
        )
      ),
      catchError(err => throwError(() => err))
    );
  }

  togglePublished(post: BlogPost): Observable<void> {
    const now = new Date().toISOString();
    return this.updatePost(post, {
      published: !post.published,
      publishedAt: !post.published && !post.publishedAt ? now : post.publishedAt
    });
  }

  deletePost(id: number): Observable<void> {
    return this.http.delete<void>(`${ENDPOINT}/${id}`).pipe(
      tap(() => this.postsSignal.update(list => list.filter(post => post.id !== id))),
      catchError(err => throwError(() => err))
    );
  }
}
