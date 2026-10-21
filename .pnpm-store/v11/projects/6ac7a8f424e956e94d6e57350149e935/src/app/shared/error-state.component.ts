import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-error-state',
  standalone: true,
  imports: [CommonModule],
  templateUrl: "./error-state.component.html"
})
export class ErrorStateComponent {
  @Input() title = 'Failed to load data';
  @Input() error: string | null = null;
  @Input() retryLabel = 'Try again';
  @Output() retry = new EventEmitter<void>();
}
