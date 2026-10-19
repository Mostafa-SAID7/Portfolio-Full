import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, tap, throwError } from 'rxjs';
import { ContactMessage } from '../models/contact.model';
import { API_URL } from '../api.config';

const ENDPOINT = `${API_URL}/Contact`;

@Injectable({ providedIn: 'root' })
export class ContactService {
  private readonly http = inject(HttpClient);

  private readonly messagesSignal = signal<ContactMessage[]>([]);
  private readonly loadingSignal = signal(false);
  private readonly errorSignal = signal<string | null>(null);

  readonly messages = this.messagesSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();

  readonly unreadCount = computed(
    () => this.messagesSignal().filter(message => !message.isRead).length
  );

  loadMessages(): Observable<ContactMessage[]> {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);

    return this.http.get<ContactMessage[]>(ENDPOINT).pipe(
      tap(messages => {
        this.messagesSignal.set(messages ?? []);
        this.loadingSignal.set(false);
      }),
      catchError(err => {
        this.errorSignal.set(err?.error?.message ?? err?.message ?? 'Failed to load messages.');
        this.loadingSignal.set(false);
        return throwError(() => err);
      })
    );
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
