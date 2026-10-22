import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { finalize } from 'rxjs';
import { ContactService } from '../../core/services/contact.service';
import { ContactMessage, ContactReply, REPLY_BODY_MAX_LENGTH, replySubjectOf } from '../../core/models/contact.model';
import { ConfirmDialogService } from '../../shared/confirm-dialog.component';
import { PaginatorComponent } from '../../shared/paginator.component';
import { EmptyStateComponent } from '../../shared/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state.component';
import { ToastService } from '../../shared/toast.component';
import { ActivatedRoute, Router } from '@angular/router';
import { readUrlInt, replaceUrlParams } from '../../core/url-state';

const PAGE_SIZE = 8;

@Component({
  selector: 'app-messages',
  standalone: true,
  imports: [CommonModule, PaginatorComponent, EmptyStateComponent, ErrorStateComponent],
  templateUrl: './messages.component.html'
})
export class MessagesComponent implements OnInit {
  readonly service = inject(ContactService);
  private readonly dialog = inject(ConfirmDialogService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly pageSize = PAGE_SIZE;
  readonly page = signal(1);
  readonly filter = signal<'all' | 'unread'>('all');
  readonly busy = signal<Set<number>>(new Set());

  readonly maxReplyLength = REPLY_BODY_MAX_LENGTH;
  /** One composer at a time — the id of the message being answered. */
  readonly openReplyFor = signal<number | null>(null);
  readonly replyDraft = signal('');
  readonly replies = signal<Record<number, ContactReply[]>>({});
  readonly repliesLoadingFor = signal<number | null>(null);
  readonly replyError = signal<string | null>(null);
  readonly sendingReply = signal(false);
  readonly replyHistoryMissing = signal(false);
  readonly replyDelivered = signal(false);

  readonly canSendReply = computed(
    () => this.replyDraft().trim().length > 0 && !this.sendingReply()
  );
  readonly remainingReplyCharacters = computed(
    () => REPLY_BODY_MAX_LENGTH - this.replyDraft().length
  );

  readonly filters = [
    { value: 'all' as const, label: 'All' },
    { value: 'unread' as const, label: 'Unread' }
  ];

  readonly visibleMessages = computed(() => {
    const messages = [...this.service.messages()].sort((a, b) => b.createdAt.localeCompare(a.createdAt));
    return this.filter() === 'unread' ? messages.filter(message => !message.isRead) : messages;
  });
  readonly currentPage = computed(() => Math.min(Math.max(1, this.page()), Math.max(1, Math.ceil(this.visibleMessages().length / PAGE_SIZE))));
  readonly paginatedMessages = computed(() => {
    const start = (this.currentPage() - 1) * PAGE_SIZE;
    return this.visibleMessages().slice(start, start + PAGE_SIZE);
  });

  ngOnInit(): void {
    // Restore the view from the URL (?filter=unread&page=2&reply=5) so a reload
    // or a shared link reopens the same inbox position.
    const params = this.route.snapshot.queryParamMap;
    const savedPage = readUrlInt(params, 'page');
    const savedReply = readUrlInt(params, 'reply');
    if (params.get('filter') === 'unread') this.filter.set('unread');
    if (savedPage !== null) this.page.set(savedPage);
    if (savedReply !== null) this.openReply(savedReply);
    this.load();
  }

  /** Mirror the inbox position into the URL (replace: no history flood). */
  private persistInUrl(): void {
    replaceUrlParams(this.route, this.router, {
      filter: this.filter() !== 'all' ? this.filter() : null,
      page: this.page() > 1 ? this.page() : null,
      reply: this.openReplyFor(),
    });
  }

  onPageChange(page: number): void {
    this.page.set(page);
    this.persistInUrl();
  }

  load(): void {
    this.service.loadMessages({ force: true }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ error: () => undefined });
  }

  selectFilter(value: 'all' | 'unread'): void {
    this.filter.set(value);
    this.page.set(1);
    this.persistInUrl();
  }

  markRead(message: ContactMessage): void {
    if (this.busy().has(message.id)) return;
    this.busy.update(ids => new Set(ids).add(message.id));
    this.service.markAsRead(message.id).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.clearBusy(message.id))
    ).subscribe({ error: err => this.toast.show(err?.error?.message ?? 'Failed to update the message.', 'error') });
  }

  /** Open (or, when already open, close) the composer for one message. */
  toggleReply(message: ContactMessage): void {
    if (this.openReplyFor() === message.id) {
      this.closeReply();
      return;
    }
    this.openReply(message.id);
  }

  closeReply(): void {
    this.openReplyFor.set(null);
    this.replyDraft.set('');
    this.replyError.set(null);
    this.replyDelivered.set(false);
    this.persistInUrl();
  }

  repliesFor(id: number): ContactReply[] {
    return this.replies()[id] ?? [];
  }

  onDraftInput(event: Event): void {
    this.replyDraft.set((event.target as HTMLTextAreaElement).value);
    this.replyDelivered.set(false);
  }

  /**
   * Hand the same draft to the admin's own mail client. The API is the primary
   * path, but this link works even while the server's SMTP settings are
   * placeholders — so being unable to send never means being unable to answer.
   */
  mailtoHref(message: ContactMessage): string {
    const query = [`subject=${encodeURIComponent(replySubjectOf(message.subject))}`];
    const body = this.replyDraft().trim();
    if (body) query.push(`body=${encodeURIComponent(body)}`);
    return `mailto:${message.email}?${query.join('&')}`;
  }

  sendReply(message: ContactMessage): void {
    if (!this.canSendReply()) return;
    this.sendingReply.set(true);
    this.replyError.set(null);
    this.service.replyToMessage(message.id, this.replyDraft().trim()).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.sendingReply.set(false))
    ).subscribe({
      next: reply => {
        this.replies.update(map => ({ ...map, [reply.contactId]: [reply, ...(map[reply.contactId] ?? [])] }));
        this.replyDraft.set('');
        this.replyDelivered.set(true);
      },
      error: err => this.replyError.set(err?.error?.message ?? 'The reply could not be sent.')
    });
  }

  private openReply(id: number): void {
    this.openReplyFor.set(id);
    this.replyDraft.set('');
    this.replyError.set(null);
    this.replyDelivered.set(false);
    this.replyHistoryMissing.set(false);
    this.fetchReplies(id);
    this.persistInUrl();
  }

  private fetchReplies(id: number): void {
    if (this.replies()[id] || this.repliesLoadingFor() === id) return;
    this.repliesLoadingFor.set(id);
    this.service.getReplies(id).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.repliesLoadingFor.set(null))
    ).subscribe({
      next: replies => this.replies.update(map => ({ ...map, [id]: replies })),
      // A missing history never blocks composing — the visitor can still be answered.
      error: () => this.replyHistoryMissing.set(true)
    });
  }

  async remove(message: ContactMessage): Promise<void> {
    if (this.busy().has(message.id)) return;
    const confirmed = await this.dialog.confirm({
      title: 'Delete message?',
      message: `The message from ${message.name} will be deleted permanently.`,
      confirmLabel: 'Delete message'
    });
    if (!confirmed || this.destroyRef.destroyed || this.busy().has(message.id)) return;
    if (this.openReplyFor() === message.id) this.closeReply();
    this.busy.update(ids => new Set(ids).add(message.id));
    this.service.deleteMessage(message.id).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.clearBusy(message.id))
    ).subscribe({ error: err => this.toast.show(err?.error?.message ?? 'Failed to delete the message.', 'error') });
  }

  private clearBusy(id: number): void {
    this.busy.update(ids => {
      const next = new Set(ids);
      next.delete(id);
      return next;
    });
    this.page.set(this.currentPage());
  }
}
