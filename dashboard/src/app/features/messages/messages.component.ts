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

  /**
   * Server-side pagination (issue #132): the API orders and pages in SQL —
   * `service.messages()` is the current page, `service.totalCount()` is the
   * total for the active filter (its X-Total-Count header).
   */
  readonly messages = this.service.messages;
  readonly totalCount = this.service.totalCount;

  /** Header copy: the paged total belongs to the active filter. */
  readonly headerTotal = computed(() =>
    this.filter() === 'unread' ? `${this.totalCount()} unread` : `${this.totalCount()} messages`
  );

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
    this.load();
  }

  load(): void {
    // The server does the ordering, the unread filter and the Skip/Take —
    // the client asks for exactly one page (issue #132).
    this.service
      .loadMessages({
        page: this.page(),
        pageSize: PAGE_SIZE,
        unread: this.filter() === 'unread',
        force: true
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.clampPageToLast(),
        error: () => undefined
      });
  }

  /**
   * Deleting the last message of the last page can strand the view on an
   * empty page — step back to the final page the total now allows.
   */
  private clampPageToLast(): void {
    if (this.service.loading() || this.service.error()) return;
    const last = Math.max(1, Math.ceil(this.totalCount() / PAGE_SIZE));
    if (this.page() > last) {
      this.page.set(last);
      this.persistInUrl();
      this.load();
    }
  }

  selectFilter(value: 'all' | 'unread'): void {
    if (this.filter() === value) return;
    this.filter.set(value);
    this.page.set(1);
    this.persistInUrl();
    this.load();
  }

  markRead(message: ContactMessage): void {
    if (this.busy().has(message.id)) return;
    this.busy.update(ids => new Set(ids).add(message.id));
    this.service.markAsRead(message.id).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.clearBusy(message.id))
    ).subscribe({
      next: () => {
        // In the unread view the row leaves the page once it is read.
        if (this.filter() === 'unread') this.load();
      },
      error: err => this.toast.show(err?.error?.message ?? 'Failed to update the message.', 'error')
    });
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
        // Replying marks the message read — in the unread view it leaves
        // the list, so pull the fresh page (issue #132).
        if (this.filter() === 'unread') this.load();
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
    ).subscribe({
      next: () => {
        // Totals shifted on the server — pull the fresh page so the freed
        // slot is filled and the paginator stays honest (issue #132).
        this.load();
      },
      error: err => this.toast.show(err?.error?.message ?? 'Failed to delete the message.', 'error')
    });
  }

  private clearBusy(id: number): void {
    this.busy.update(ids => {
      const next = new Set(ids);
      next.delete(id);
      return next;
    });
  }
}
