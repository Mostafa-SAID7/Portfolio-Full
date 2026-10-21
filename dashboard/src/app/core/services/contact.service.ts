import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, finalize, of, shareReplay, tap, throwError } from 'rxjs';
import { ContactMessage } from '../models/contact.model';
import { API_URL } from '../api.config';

const ENDPOINT = `${API_URL}/Contact`;

@Injectable({ providedIn: 'root' })
export class ContactService {
  private readonly http = inject(HttpClient);

  private readonly messagesSignal = signal<ContactMessage[]>([]);
  private readonly loadingSignal = signal(false);
  private readonly errorSignal = signal<string | null>(null);

  /** In-flight request, shared by concurrent callers (see loadMessages). */
  private inFlight?: Observable<ContactMessage[]>;
  /** Timestamp of the last successful load (freshness window). */
  private lastLoadedAt = 0;

  /**
   * How long a loaded inbox is served from memory before it is fetched again.
   * The app shell loads the inbox for the unread badge and the dashboard also
   * asks for it — without this window both requests hit the API on every page
   * load (the shell's request finishes before the dashboard component is
   * created, so sharing the in-flight request alone was not enough).
   */
  private static readonly FRESH_MS = 30_000;

  readonly messages = this.messagesSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();

  readonly unreadCount = computed(
    () => this.messagesSignal().filter(message => !message.isRead).length
  );

  /**
   * Loads the inbox.
   *
   * - concurrent callers share ONE request;
   * - a recently loaded inbox is served from memory (see FRESH_MS) so the
   *   shell and the dashboard do not both hit the API on every page load;
   * - `force: true` always refetches (used by the manual reload).
   */
  loadMessages(options: { force?: boolean } = {}): Observable<ContactMessage[]> {
    if (this.inFlight) {
      return this.inFlight;
    }

    const fresh = Date.now() - this.lastLoadedAt < ContactService.FRESH_MS;
    if (!options.force && fresh) {
      return of(this.messagesSignal());
    }

    this.loadingSignal.set(true);
    this.errorSignal.set(null);

    const request = this.http.get<ContactMessage[]>(ENDPOINT).pipe(
      tap(messages => {
        this.messagesSignal.set(messages ?? []);
        this.lastLoadedAt = Date.now();
        this.loadingSignal.set(false);
      }),
      catchError(err => {
        this.errorSignal.set(err?.error?.message ?? err?.message ?? 'Failed to load messages.');
        this.loadingSignal.set(false);
        return throwError(() => err);
      }),
      finalize(() => {
        this.inFlight = undefined;
      }),
      shareReplay({ bufferSize: 1, refCount: false })
    );

    this.inFlight = request;
    return request;
  }

  markAsRead(id: number): Observable<void> {
    return this.http.put<void>(`${ENDPOINT}/${id}/read`, {}).pipe(
      tap(() => this.patchLocal(id, { isRead: true, readAt: new Date().toISOString() })),
      catchError(err => throwError(() => err))
    );
  }

  deleteMessage(id: number): Observable<void> {
    return this.http.delete<void>(`${ENDPOINT}/${id}`).pipe(
      tap(() => this.messagesSignal.update(list => list.filter(message => message.id !== id))),
      catchError(err => throwError(() => err))
    );
  }

  private patchLocal(id: number, changes: Partial<ContactMessage>): void {
    this.messagesSignal.update(list =>
      list.map(message => (message.id === id ? { ...message, ...changes } : message))
    );
  }
}
