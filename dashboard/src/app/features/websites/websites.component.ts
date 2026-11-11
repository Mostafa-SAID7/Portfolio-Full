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

  readonly websites = computed(() => this.websiteService.websites());

  ngOnInit(): void {
    if (this.websites().length === 0) {
      this.websiteService.loadWebsites()
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({ error: () => undefined });
    }
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
    this.websiteService.updateWebsite(website.id, draft)
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

