import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { finalize } from 'rxjs';
import { ContactService } from '../../core/services/contact.service';
import { ContactMessage } from '../../core/models/contact.model';
import { ConfirmDialogService } from '../../shared/confirm-dialog.component';
import { PaginatorComponent } from '../../shared/paginator.component';
import { EmptyStateComponent } from '../../shared/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state.component';
import { ToastService } from '../../shared/toast.component';
import { ActivatedRoute, Router } from '@angular/router';

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
    // Restore the view from the URL (?filter=unread&page=2) so a reload or a
    // shared link reopens the same inbox position.
    const params = this.route.snapshot.queryParamMap;
    const savedPage = Number(params.get('page'));
    if (params.get('filter') === 'unread') this.filter.set('unread');
    if (Number.isFinite(savedPage) && savedPage > 1) this.page.set(Math.floor(savedPage));
    this.load();
  }

  /** Mirror filter/page into the URL (replace: no history flood). */
  private persistInUrl(): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        filter: this.filter() !== 'all' ? this.filter() : null,
        page: this.page() > 1 ? this.page() : null,
      },
      queryParamsHandling: 'merge',
      replaceUrl: true,
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

  async remove(message: ContactMessage): Promise<void> {
    if (this.busy().has(message.id)) return;
    const confirmed = await this.dialog.confirm({
      title: 'Delete message?',
      message: `The message from ${message.name} will be deleted permanently.`,
      confirmLabel: 'Delete message'
    });
    if (!confirmed || this.destroyRef.destroyed || this.busy().has(message.id)) return;
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
