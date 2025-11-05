import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-loader',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="loader-overlay" *ngIf="show">
      <div class="loader-container">
        <div class="spinner" [class]="sizeClass()"></div>
        <p class="loader-text" *ngIf="message()">{{ message() }}</p>
      </div>
    </div>
  `,
  styleUrls: ['./loader.component.scss']
})
export class LoaderComponent {
  show = input<boolean>(false);
  message = input<string>('');
  size = input<'sm' | 'md' | 'lg'>('md');

  sizeClass(): string {
    return `spinner-${this.size()}`;
  }
}