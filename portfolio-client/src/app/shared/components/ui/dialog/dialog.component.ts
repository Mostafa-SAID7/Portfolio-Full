import { Component, input, output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DOCUMENT } from '@angular/common';

@Component({
  selector: 'app-dialog',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="dialog-overlay" *ngIf="isOpen()" (click)="onOverlayClick($event)">
      <div class="dialog-content" [class]="sizeClass()">
        <div class="dialog-header">
          <h2 class="dialog-title">{{ title() }}</h2>
          <button class="dialog-close" (click)="close()">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="18" y1="6" x2="6" y2="18"></line>
              <line x1="6" y1="6" x2="18" y2="18"></line>
            </svg>
          </button>
        </div>
        <div class="dialog-body">
          <ng-content></ng-content>
        </div>
        <div class="dialog-footer" *ngIf="showFooter()">
          <ng-content select="[dialog-footer]"></ng-content>
        </div>
      </div>
    </div>
  `,
  styleUrls: ['./dialog.component.scss']
})
export class DialogComponent {
  private document = inject(DOCUMENT);

  isOpen = input<boolean>(false);
  title = input<string>('');
  size = input<'sm' | 'md' | 'lg' | 'xl'>('md');
  showFooter = input<boolean>(false);

  onClose = output<void>();

  close(): void {
    this.onClose.emit();
  }

  onOverlayClick(event: Event): void {
    if (event.target === event.currentTarget) {
      this.close();
    }
  }

  sizeClass(): string {
    return `dialog-${this.size()}`;
  }
}