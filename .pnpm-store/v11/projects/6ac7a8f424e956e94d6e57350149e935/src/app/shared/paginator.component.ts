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
  templateUrl: "./paginator.component.html"
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
