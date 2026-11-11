import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { WebsiteService } from '../../core/services/website.service';
import { Website, WebsitePayload } from '../../core/models/website.model';
import { ToastService } from '../../shared/toast.component';
import { ConfirmDialogService } from '../../shared/confirm-dialog.component';

function slugify(text: string): string {
  return text
    .toLowerCase()
    .trim()
    .replace(/[^\w\s-]/g, '')
    .replace(/[\s_-]+/g, '-')
    .replace(/^-+|-+$/g, '');
}

@Component({
  selector: 'app-websites',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './websites.component.html'
})
export class WebsitesComponent implements OnInit {
  readonly websiteService = inject(WebsiteService);
  private readonly toast = inject(ToastService);
  private readonly dialog = inject(ConfirmDialogService);
  private readonly destroyRef = inject(DestroyRef);

  readonly toggling = signal<Set<number>>(new Set());
  readonly deleting = signal<Set<number>>(new Set());

  /** Inline edit state: websiteId → dirty payload */
  readonly editing = signal<Map<number, WebsitePayload>>(new Map());

  /** Create new website state */
  readonly showCreateModal = signal(false);
  readonly creating = signal(false);
  readonly newWebsite = signal<WebsitePayload>({
    name: '',
    slug: '',
    description: '',
    baseUrl: '',
    order: 0,
    isActive: true
  });

  readonly websites = computed(() => this.websiteService.websites());

  ngOnInit(): void {
    if (this.websites().length === 0) {
      this.websiteService.loadWebsites()
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({ error: () => undefined });
    }
  }

  openCreate(): void {
    const nextOrder = (this.websites().length > 0)
      ? Math.max(...this.websites().map(w => w.order)) + 1
      : 1;
    this.newWebsite.set({
      name: '',
      slug: '',
      description: '',
      baseUrl: '',
      order: nextOrder,
      isActive: true
    });
    this.showCreateModal.set(true);
  }

  cancelCreate(): void {
    this.showCreateModal.set(false);
  }

  onNewNameChange(name: string): void {
    const current = this.newWebsite();
    const autoSlug = slugify(name);
    this.newWebsite.set({
      ...current,
      name,
      slug: (!current.slug || current.slug === slugify(current.name)) ? autoSlug : current.slug
    });
  }

  updateNewField<K extends keyof WebsitePayload>(field: K, value: WebsitePayload[K]): void {
    this.newWebsite.update(prev => ({ ...prev, [field]: value }));
  }

  saveNewWebsite(): void {
    const payload = this.newWebsite();
    const name = payload.name.trim();
    if (!name) {
      this.toast.show('Website name is required.', 'error');
      return;
    }
    const slug = (payload.slug && payload.slug.trim()) ? slugify(payload.slug) : slugify(name);

    this.creating.set(true);
    this.websiteService.createWebsite({
      ...payload,
      name,
      slug,
      baseUrl: payload.baseUrl?.trim() || null,
      description: payload.description?.trim() || null
    })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.creating.set(false))
      )
      .subscribe({
        next: created => {
          this.showCreateModal.set(false);
          this.toast.show(`Website "${created.name}" created.`, 'info');
        },
        error: err => this.toast.show(err?.error?.message ?? 'Failed to create website.', 'error')
      });
  }

  isEditing(id: number): boolean {
    return this.editing().has(id);
  }

  startEdit(website: Website): void {
    this.editing.update(map => {
      const next = new Map(map);
      next.set(website.id, {
        name: website.name,
        slug: website.slug,
        description: website.description ?? '',
        baseUrl: website.baseUrl ?? '',
        order: website.order,
        isActive: website.isActive
      });
      return next;
    });
  }

  cancelEdit(id: number): void {
    this.editing.update(map => {
      const next = new Map(map);
      next.delete(id);
      return next;
    });
  }

  getDraft(id: number): WebsitePayload | undefined {
    return this.editing().get(id);
  }

  updateDraft(id: number, patch: Partial<WebsitePayload>): void {
    this.editing.update(map => {
      const existing = map.get(id);
      if (!existing) return map;
      const next = new Map(map);
      next.set(id, { ...existing, ...patch });
      return next;
    });
  }

  saveEdit(website: Website): void {
    const draft = this.getDraft(website.id);
    if (!draft) return;
    this.websiteService.updateWebsite(website.id, {
      ...draft,
      baseUrl: draft.baseUrl?.trim() || null,
      description: draft.description?.trim() || null
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.cancelEdit(website.id);
          this.toast.show(`"${draft.name}" updated.`, 'info');
        },
        error: err => this.toast.show(err?.error?.message ?? 'Failed to update website.', 'error')
      });
  }

  toggleActive(website: Website): void {
    if (this.toggling().has(website.id)) return;
    this.toggling.update(s => new Set(s).add(website.id));
    this.websiteService.toggleActive(website.id)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.toggling.update(s => { const n = new Set(s); n.delete(website.id); return n; }))
      )
      .subscribe({
        error: err => this.toast.show(err?.error?.message ?? 'Failed to toggle website.', 'error')
      });
  }

  async remove(website: Website): Promise<void> {
    if (this.deleting().has(website.id)) return;
    const confirmed = await this.dialog.confirm({
      title: 'Delete website?',
      message: `"${website.name}" and all its project assignments will be removed. Projects themselves are not deleted.`,
      confirmLabel: 'Delete website'
    });
    if (!confirmed || this.destroyRef.destroyed) return;
    this.deleting.update(s => new Set(s).add(website.id));
    this.websiteService.deleteWebsite(website.id)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.deleting.update(s => { const n = new Set(s); n.delete(website.id); return n; }))
      )
      .subscribe({
        next: () => this.toast.show(`"${website.name}" deleted.`, 'info'),
        error: err => this.toast.show(err?.error?.message ?? 'Failed to delete website.', 'error')
      });
  }
}
