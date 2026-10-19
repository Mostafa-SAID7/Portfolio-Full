import { Component, HostListener, Injectable, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface ConfirmDialogOptions {
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  /** Red (destructive) confirm button — the default, since this dialog exists for deletes. */
  danger?: boolean;
}

interface ConfirmDialogRequest extends ConfirmDialogOptions {
  id: number;
}

/**
 * In-app confirmation dialog (replaces browser `confirm()`).
 *
 * Usage: `const ok = await this.dialog.confirm({ title, message });`
 * Mounted once via `<app-confirm-dialog />` in the app shell.
 */
@Injectable({ providedIn: 'root' })
export class ConfirmDialogService {
  readonly request = signal<ConfirmDialogRequest | null>(null);

  private pendingResolve: ((value: boolean) => void) | null = null;

  confirm(options: ConfirmDialogOptions): Promise<boolean> {
    return new Promise<boolean>(resolve => {
      // Opening a new dialog cancels any previous one.
      this.pendingResolve?.(false);
      this.pendingResolve = resolve;
      this.request.set({ ...options, id: Date.now() });
    });
  }

  settle(value: boolean): void {
    const resolve = this.pendingResolve;
    this.pendingResolve = null;
    this.request.set(null);
    resolve?.(value);
  }
}

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (service.request(); as req) {
      <div
        class="fixed inset-0 z-50 flex items-center justify-center p-4"
        role="dialog"
        aria-modal="true"
        [attr.aria-labelledby]="'confirm-title-' + req.id"
        [attr.aria-describedby]="'confirm-message-' + req.id"
      >
        <div class="absolute inset-0 bg-ink/50 backdrop-blur-sm" (click)="service.settle(false)" aria-hidden="true"></div>
        <div class="card relative w-full max-w-md p-6">
          <h3 [id]="'confirm-title-' + req.id" class="font-display text-xl font-semibold tracking-[-0.03em] text-ink">
            {{ req.title }}
          </h3>
          <p [id]="'confirm-message-' + req.id" class="mt-3 text-sm leading-6 text-soft">{{ req.message }}</p>
          <div class="mt-6 flex justify-end gap-3">
            <button type="button" class="btn-ghost" (click)="service.settle(false)">
              {{ req.cancelLabel ?? 'Cancel' }}
            </button>
            <button
              type="button"
              [class]="req.danger === false ? 'btn-primary' : 'btn-danger'"
              autofocus
              (click)="service.settle(true)"
            >
              {{ req.confirmLabel ?? 'Delete' }}
            </button>
          </div>
        </div>
      </div>
    }
  `
})
export class ConfirmDialogComponent {
  readonly service = inject(ConfirmDialogService);

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.service.request()) {
      this.service.settle(false);
    }
  }
}
