import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, catchError, finalize, map, of, shareReplay, tap, throwError } from 'rxjs';
import { ContactMessage, ContactReply } from '../models/contact.model';
import { API_URL } from '../api.config';

const ENDPOINT = `${API_URL}/Contact`;

/** Mirrors ContactController's server-side clamp (issue #132). */
export const MAX_PAGE_SIZE = 100;

export interface InboxPageOptions {
  page?: number;
  pageSize?: number;
  /** Server-side unread-only filter (GET /Contact?unread=true). */
  unread?: boolean;
  force?: boolean;
}

@Injectable({ providedIn: 'root' })
export class ContactService {
  private readonly http = inject(HttpClient);

  private readonly messagesSignal = signal<ContactMessage[]>([]);
  /** Total rows matching the last loaded filter (API X-Total-Count header). */
  private readonly totalCountSignal = signal(0);
  /** Unread badge counter — served by the dedicated /unread endpoint. */
  private readonly unreadCountSignal = signal(0);
  private readonly loadingSignal = signal(false);
  private readonly errorSignal = signal<string | null>(null);

  /** In-flight request, shared by concurrent callers (see loadMessages). */
  private inFlight?: Observable<ContactMessage[]>;
  /** Timestamp of the last successful load (freshness window). */
  private lastLoadedAt = 0;
  /** Parameters the cached page was loaded with (fresh only within same view). */
  private lastKey = '';

  /** Separate in-flight/freshness state for the unread badge counter. */
  private unreadInFlight?: Observable<number>;
  private unreadLoadedAt = 0;

  /**
   * How long a loaded inbox page is served from memory before it is fetched
   * again. The app shell loads the unread badge and the dashboard/messages
   * pages ask for their own page — without this window both requests would
   * hit the API on every page load.
   */
  private static readonly FRESH_MS = 30_000;

  /** Default server page size — mirrors ContactController.DefaultPageSize. */
  static readonly DEFAULT_PAGE_SIZE = 25;

  readonly messages = this.messagesSignal.asReadonly();
  readonly totalCount = this.totalCountSignal.asReadonly();
  readonly unreadCount = this.unreadCountSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();

  /**
   * Loads ONE page of the inbox from the server (issue #132).
   *
   * - ordering and the unread filter are done by the API in SQL — the client
   *   no longer downloads the whole table and paginates in memory;
   * - concurrent callers share ONE request;
   * - a recently loaded page is served from memory (see FRESH_MS) so the
   *   shell and the pages do not both hit the API on every page load;
   * - `force: true` always refetches (used by the manual reload).
   */
  loadMessages(options: InboxPageOptions = {}): Observable<ContactMessage[]> {
    const page = Math.max(1, options.page ?? 1);
    const pageSize = Math.min(
      MAX_PAGE_SIZE,
      Math.max(1, options.pageSize ?? ContactService.DEFAULT_PAGE_SIZE)
    );
    const unread = options.unread ?? false;
    const key = `${page}|${pageSize}|${unread}`;

    if (this.inFlight) {
      return this.inFlight;
    }

    const fresh = key === this.lastKey && Date.now() - this.lastLoadedAt < ContactService.FRESH_MS;
    if (!options.force && fresh) {
      return of(this.messagesSignal());
    }

    this.loadingSignal.set(true);
    this.errorSignal.set(null);

    const params = new HttpParams()
      .set('page', String(page))
      .set('pageSize', String(pageSize))
      .set('unread', unread ? 'true' : 'false');

    const request = this.http
      .get<ContactMessage[]>(ENDPOINT, { params, observe: 'response' })
      .pipe(
        map(response => {
          const items = response.body ?? [];
          const total = Number(response.headers.get('X-Total-Count'));
          // Fall back to the page length when the header is missing
          // (proxies sometimes strip custom headers).
          this.totalCountSignal.set(Number.isFinite(total) ? total : items.length);
          return items;
        }),
        tap(items => {
          this.messagesSignal.set(items);
          this.lastLoadedAt = Date.now();
          this.lastKey = key;
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

  /**
   * Refreshes the unread badge counter. The inbox is paged now, so the count
   * CANNOT be derived from the loaded page anymore — the /unread endpoint
   * (admin-only, naturally bounded by the still-unopened messages) is the
   * source of truth.
   */
  loadUnreadCount(options: { force?: boolean } = {}): Observable<number> {
    if (this.unreadInFlight) {
      return this.unreadInFlight;
    }
    if (!options.force && Date.now() - this.unreadLoadedAt < ContactService.FRESH_MS) {
      return of(this.unreadCountSignal());
    }

    const request = this.http.get<ContactMessage[]>(`${ENDPOINT}/unread`).pipe(
      map(messages => messages?.length ?? 0),
      tap(count => {
        this.unreadCountSignal.set(count);
        this.unreadLoadedAt = Date.now();
      }),
      catchError(err => throwError(() => err)),
      finalize(() => {
        this.unreadInFlight = undefined;
      }),
      shareReplay({ bufferSize: 1, refCount: false })
    );

    this.unreadInFlight = request;
    return request;
  }

  markAsRead(id: number): Observable<void> {
    return this.http.put<void>(`${ENDPOINT}/${id}/read`, {}).pipe(
      tap(() => {
        const wasUnread = this.messagesSignal().some(message => message.id === id && !message.isRead);
        this.patchLocal(id, { isRead: true, readAt: new Date().toISOString() });
        if (wasUnread) this.decrementUnread();
      }),
      catchError(err => throwError(() => err))
    );
  }

  /** Replies already delivered for one message (newest first). */
  getReplies(id: number): Observable<ContactReply[]> {
    return this.http.get<ContactReply[]>(`${ENDPOINT}/${id}/replies`);
  }

  /**
   * Sends the answer through the API. A 503 from the mail layer means the text
   * was NOT delivered and nothing was stored — the caller must not pretend
   * otherwise; the inbox is only patched when the request succeeded.
   */
  replyToMessage(id: number, body: string): Observable<ContactReply> {
    return this.http.post<ContactReply>(`${ENDPOINT}/${id}/reply`, { body }).pipe(
      tap(reply => {
        const wasUnread = this.messagesSignal().some(message => message.id === id && !message.isRead);
        this.patchLocal(id, { isRead: true, readAt: reply.sentAt });
        if (wasUnread) this.decrementUnread();
      })
    );
  }

  deleteMessage(id: number): Observable<void> {
    return this.http.delete<void>(`${ENDPOINT}/${id}`).pipe(
      tap(() => {
        const deleted = this.messagesSignal().find(message => message.id === id);
        if (deleted && !deleted.isRead) this.decrementUnread();
        this.totalCountSignal.update(total => Math.max(0, total - 1));
        this.messagesSignal.update(list => list.filter(message => message.id !== id));
      }),
      catchError(err => throwError(() => err))
    );
  }

  private patchLocal(id: number, changes: Partial<ContactMessage>): void {
    this.messagesSignal.update(list =>
      list.map(message => (message.id === id ? { ...message, ...changes } : message))
    );
  }

  /** Keeps the sidebar badge in sync between server refetches. */
  private decrementUnread(): void {
    this.unreadCountSignal.update(count => Math.max(0, count - 1));
  }
}
