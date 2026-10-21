import { Component, Injectable, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

type ToastKind = 'info' | 'error';

interface ToastState {
  text: string;
  kind: ToastKind;
}

/**
 * Shared transient notification surface (issue #20): global events that are
 * not owned by any single screen — session expiry, permission denials — are
 * surfaced here instead of each service inventing its own alert(). Domain
 * validation messages stay inline in their forms.
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly stateSignal = signal<ToastState | null>(null);
  private timer: ReturnType<typeof setTimeout> | null = null;

  readonly state = this.stateSignal.asReadonly();

  show(text: string, kind: ToastKind = 'info'): void {
    this.stateSignal.set({ text, kind });
    if (this.timer !== null) clearTimeout(this.timer);
    this.timer = setTimeout(() => {
      this.stateSignal.set(null);
      this.timer = null;
    }, 5000);
  }
}

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [CommonModule],
  templateUrl: "./toast.component.html"
})
export class ToastComponent {
  readonly toast = inject(ToastService);
}
