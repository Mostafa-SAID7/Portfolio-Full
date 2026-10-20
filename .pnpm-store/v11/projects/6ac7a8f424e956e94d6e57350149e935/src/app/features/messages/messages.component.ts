import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ContactService } from '../../core/services/contact.service';
import { ContactMessage } from '../../core/models/contact.model';
import { ConfirmDialogService } from '../../shared/confirm-dialog.component';
import { PaginatorComponent } from '../../shared/paginator.component';
import { EmptyStateComponent } from '../../shared/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state.component';

const PAGE_SIZE = 8;

@Component({
  selector: 'app-messages',
  standalone: true,
  imports: [
    CommonModule,
    PaginatorComponent,
    EmptyStateComponent,
    ErrorStateComponent,
  ],
  template: `
    <div class="space-y-6">
      <div class="flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
        <div>
          <p class="eyebrow mb-2">Contact form</p>
          <h2 class="font-display text-3xl font-semibold tracking-[-0.05em] text-ink">Messages</h2>
          <p class="mt-2 text-sm text-soft">
            {{ service.messages().length }} messages — {{ service.unreadCount() }} unread
          </p>
        </div>
        <div class="flex items-center gap-2">
          @for (option of filters; track option.value) {
            <button
              type="button"
              class="segmented"
              [class.active]="filter === option.value"
              (click)="selectFilter(option.value)"
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
        <app-error-state
          title="Failed to load messages"
          [error]="service.error()"
          retryLabel="Retry loading"
          (retry)="load()"
        ></app-error-state>
      } @else if (visibleMessages().length === 0) {
        <app-empty-state
          icon="message"
          [title]="filter === 'unread' ? 'All caught up' : 'No messages yet'"
          [description]="filter === 'unread' ? 'No unread messages in your inbox.' : 'Visitor contact forms submitted from your portfolio will appear here.'"
        ></app-empty-state>
      } @else {
        <div class="space-y-3">
          @for (message of paginatedMessages(); track message.id) {
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
                      @if (message.readAt) { — Read {{ message.readAt | date: 'MMM d' }} }
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

        <app-paginator
          [totalItems]="visibleMessages().length"
          [pageSize]="pageSize"
          [page]="page()"
          (pageChange)="page.set($event)"
        />
      }
    </div>
  `
})
export class MessagesComponent implements OnInit {
  readonly service = inject(ContactService);
  private readonly dialog = inject(ConfirmDialogService);

  readonly pageSize = PAGE_SIZE;
  readonly page = signal(1);

  filter: 'all' | 'unread' = 'all';

  readonly filters = [
    { value: 'all' as const, label: 'All' },
    { value: 'unread' as const, label: 'Unread' }
  ];

  readonly visibleMessages = computed(() => {
    const messages = [...this.service.messages()].sort((a, b) => b.createdAt.localeCompare(a.createdAt));
    return this.filter === 'unread' ? messages.filter(message => !message.isRead) : messages;
  });

  readonly paginatedMessages = computed(() => {
    const start = (this.page() - 1) * this.pageSize;
    return this.visibleMessages().slice(start, start + this.pageSize);
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    // The inbox is where messages are worked on — always fetch the current
    // state instead of the shell's (at most 30 s old) copy.
    this.service.loadMessages({ force: true }).subscribe({ error: () => undefined });
  }

  selectFilter(value: 'all' | 'unread'): void {
    this.filter = value;
    this.page.set(1);
  }

  markRead(message: ContactMessage): void {
    this.service.markAsRead(message.id).subscribe({
      error: err => alert(err?.error?.message ?? 'Failed to update the message.')
    });
  }

  async remove(message: ContactMessage): Promise<void> {
    const confirmed = await this.dialog.confirm({
      title: 'Delete message?',
      message: `The message from ${message.name} will be deleted permanently.`,
      confirmLabel: 'Delete message'
    });
    if (!confirmed) return;

    this.service.deleteMessage(message.id).subscribe({
      error: err => alert(err?.error?.message ?? 'Failed to delete the message.')
    });
  }
}
