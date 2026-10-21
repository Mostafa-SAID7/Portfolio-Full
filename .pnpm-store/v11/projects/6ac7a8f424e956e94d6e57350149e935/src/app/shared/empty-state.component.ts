import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [CommonModule],
  templateUrl: "./empty-state.component.html"
})
export class EmptyStateComponent {
  @Input() icon: 'folder' | 'search' | 'message' | 'tag' | 'box' = 'folder';
  @Input() title = '';
  @Input() description = '';
  @Input() actionLabel: string | null = null;
  @Output() action = new EventEmitter<void>();
}
