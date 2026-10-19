import { Component, OnInit, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ContactService } from '../../core/services/contact.service';
import { ContactMessage } from '../../core/models/contact.model';

@Component({
  selector: 'app-messages',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="space-y-6">
      <div class="flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
        <div>
          <p class="eyebrow mb-2">Contact form</p>
          <h2 class="font-display text-3xl font-semibold tracking-[-0.05em] text-ink">Messages</h2>
          <p class="mt-2 text-sm text-soft">
            {{ service.messages().length }} messages · {{ service.unreadCount() }} unread
          </p>
        </div>
        <div class="flex items-center gap-2">
          @for (option of filters; track option.value) {
            <button
              type="button"
              class="segmented"
              [class.active]="filter === option.value"
              (click)="filter = option.value"
            >
              {{ option.label }}
            </button>
          }
        </div>
      </div>

      @if (service.loading()) {
        <div class="card flex min-h-[280px] items-center justify-center">
          <div class="spinner" role="status" aria-label="Loading messages"></div>
        </div>
      } @else if (service.error()) {
        <div class="card empty-state">
          <p class="text-sm font-semibold text-ink">Failed to load messages</p>
          <p class="max-w-md text-sm text-soft">{{ service.error() }}</p>
          <button type="button" class="btn-ghost mt-2" (click)="load()">Retry</button>
        </div>
      } @else if (visibleMessages().length === 0) {
        <div class="card empty-state">
          <p class="text-sm text-muted">
            @if (filter === 'unread') { No unread messages — you are all caught up. }
            @else { No messages yet. Visitors' contact-form submissions appear here. }
          </p>
        </div>
      } @else {
        <div class="space-y-3">
          @for (message of visibleMessages(); track message.id) {
            <article class="card-raised p-5">
              <div class="flex flex-col gap-4 sm:flex-row sm:items-start">
                <div class="flex min-w-0 flex-1 items-start gap-3">
                  <span class="mt-1.5 h-2 w-2 shrink-0 rounded-full" [class]="message.isRead ? 'bg-faint/50' : 'bg-accent'"></span>
                  <div class="min-w-0">
                    <div class="flex flex-wrap items-center gap-2">
                      <h3 class="text-sm font-bold text-ink">{{ message.name }}</h3>
                      @if (!message.isRead) { <span class="badge-accent">Unread</span> }
                      <a [href]="'mailto:' + message.email" class="text-xs font-semibold text-accent-text hover:underline">{{ message.email }}</a>
                    </div>
                    @if (message.subject) {
                      <p class="mt-1 text-sm font-semibold text-soft">{{ message.subject }}</p>
                    }
                    <p class="mt-2 whitespace-pre-line text-sm leading-6 text-soft">{{ message.message }}</p>
                    <p class="mt-3 text-xs text-muted">
                      {{ message.createdAt | date: 'MMM d, y, h:mm a' }}
                      @if (message.readAt) { · Read {{ message.readAt | date: 'MMM d' }} }
                    </p>
                  </div>
                </div>
                <div class="flex shrink-0 gap-2 pl-5 sm:pl-0">
                  @if (!message.isRead) {
                    <button type="button" class="btn-ghost" (click)="markRead(message)">Mark read</button>
                  }
                  <button type="button" class="btn-danger" (click)="remove(message)">Delete</button>
                </div>
              </div>
            </article>
          }
        </div>
      }
    </div>
  `
})
export class MessagesComponent implements OnInit {
  readonly service = inject(ContactService);

  filter: 'all' | 'unread' = 'all';

  readonly filters = [
    { value: 'all' as const, label: 'All' },
    { value: 'unread' as const, label: 'Unread' }
  ];

  readonly visibleMessages = computed(() => {
    const messages = [...this.service.messages()].sort((a, b) => b.createdAt.localeCompare(a.createdAt));
    return this.filter === 'unread' ? messages.filter(message => !message.isRead) : messages;
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.service.loadMessages().subscribe({ error: () => undefined });
  }

  markRead(message: ContactMessage): void {
    this.service.markAsRead(message.id).subscribe({
      error: err => alert(err?.error?.message ?? 'Failed to update the message.')
    });
  }

  remove(message: ContactMessage): void {
    if (!confirm(`Delete the message from ${message.name}?`)) return;
    this.service.deleteMessage(message.id).subscribe({
      error: err => alert(err?.error?.message ?? 'Failed to delete the message.')
    });
  }
}
