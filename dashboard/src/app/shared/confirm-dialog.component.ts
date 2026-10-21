import { afterRenderEffect, Component, DestroyRef, ElementRef, Injectable, inject, signal, viewChild } from '@angular/core';
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
  private nextId = 0;

  confirm(options: ConfirmDialogOptions): Promise<boolean> {
    return new Promise<boolean>(resolve => {
      // Opening a new dialog cancels any previous one.
      this.pendingResolve?.(false);
      this.pendingResolve = resolve;
      this.request.set({ ...options, id: this.nextId++ });
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
  templateUrl: './confirm-dialog.component.html'
})
export class ConfirmDialogComponent {
  readonly service = inject(ConfirmDialogService);
  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');
  private readonly cancelButton = viewChild<ElementRef<HTMLButtonElement>>('cancelButton');
  private previousFocus: HTMLElement | null = null;

  constructor() {
    // Runs after the conditional content and query are ready, never during SSR.
    afterRenderEffect(() => {
      const request = this.service.request();
      const dialog = this.dialog().nativeElement;
      if (request) {
        if (!dialog.open) {
          this.previousFocus = dialog.ownerDocument.activeElement instanceof HTMLElement
            ? dialog.ownerDocument.activeElement : null;
          dialog.showModal();
        }
        this.cancelButton()?.nativeElement.focus();
      } else {
        this.closeAndRestoreFocus();
      }
    });
    inject(DestroyRef).onDestroy(() => {
      this.service.settle(false);
      this.closeAndRestoreFocus();
    });
  }

  onCancel(event: Event): void {
    event.preventDefault();
    this.service.settle(false);
  }

  onClose(): void {
    // Ignore a queued close event if a subsequent request already reopened it.
    if (!this.dialog().nativeElement.open && this.service.request()) this.service.settle(false);
  }

  onBackdropClick(event: MouseEvent): void {
    const dialog = this.dialog().nativeElement;
    if (event.target !== dialog) return;
    const rect = dialog.getBoundingClientRect();
    if (event.clientX < rect.left || event.clientX > rect.right ||
        event.clientY < rect.top || event.clientY > rect.bottom) this.service.settle(false);
  }

  private closeAndRestoreFocus(): void {
    const dialog = this.dialog().nativeElement;
    if (dialog.open) dialog.close();
    if (this.previousFocus?.isConnected && !this.previousFocus.closest('[inert]')) this.previousFocus.focus();
    this.previousFocus = null;
  }
}
