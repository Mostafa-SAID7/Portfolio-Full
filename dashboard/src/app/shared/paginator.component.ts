import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

/**
 * Lightweight pagination controls (no Angular Material) matching the
 * dashboard's own styling. Hidden when everything fits on one page.
 *
 * Parent owns the page state: `[page]` in, `(pageChange)` out.
 */
@Component({
  selector: 'app-paginator',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (totalPages > 1) {
      <nav class="flex flex-col items-center justify-between gap-3 pt-3 sm:flex-row" aria-label="Pagination">
        <p class="text-xs text-muted">Showing {{ from }}–{{ to }} of {{ totalItems }}</p>
        <div class="flex items-center gap-1">
          <button
            type="button"
            class="icon-btn"
            [disabled]="page <= 1"
            (click)="go(page - 1)"
            aria-label="Previous page"
          >
            <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M15 19l-7-7 7-7"/></svg>
          </button>
          @for (entry of pageEntries; track entry) {
            @if (entry === '…') {
              <span class="px-1 text-xs text-muted" aria-hidden="true">…</span>
            } @else {
              <button
                type="button"
                class="min-h-[32px] min-w-[32px] rounded-lg px-2 text-xs font-semibold transition"
                [class]="entry === page ? 'bg-accent text-on-accent' : 'text-muted hover:bg-line/[0.06] hover:text-ink'"
                [attr.aria-current]="entry === page ? 'page' : null"
                [attr.aria-label]="'Page ' + entry"
                (click)="go(entry)"
              >
                {{ entry }}
              </button>
            }
          }
          <button
            type="button"
            class="icon-btn"
            [disabled]="page >= totalPages"
            (click)="go(page + 1)"
            aria-label="Next page"
          >
            <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M9 5l7 7-7 7"/></svg>
          </button>
        </div>
      </nav>
    }
  `
})
export class PaginatorComponent {
  @Input() totalItems = 0;
  @Input() pageSize = 9;
  @Input() page = 1;
  @Output() pageChange = new EventEmitter<number>();

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalItems / this.pageSize));
  }

  get from(): number {
    return this.totalItems === 0 ? 0 : (this.page - 1) * this.pageSize + 1;
  }

  get to(): number {
    return Math.min(this.totalItems, this.page * this.pageSize);
  }

  /** Windowed page list: 1 … current-1 current current+1 … last. */
  get pageEntries(): (number | '…')[] {
    const total = this.totalPages;
    const current = Math.min(Math.max(1, this.page), total);
    const wanted = [1, total, current - 1, current, current + 1].filter(p => p >= 1 && p <= total);
    const sorted = [...new Set(wanted)].sort((a, b) => a - b);

    const entries: (number | '…')[] = [];
    let previous = 0;
    for (const value of sorted) {
      if (previous && value - previous > 1) {
        entries.push('…');
      }
      entries.push(value);
      previous = value;
    }
    return entries;
  }

  go(page: number): void {
    const next = Math.min(this.totalPages, Math.max(1, page));
    if (next !== this.page) {
      this.pageChange.emit(next);
    }
  }
}
