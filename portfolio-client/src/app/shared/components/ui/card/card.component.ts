import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="card" [class]="cardClass()">
      <div class="card-header" *ngIf="header()">
        <ng-content select="[card-header]"></ng-content>
      </div>
      <div class="card-body">
        <ng-content></ng-content>
      </div>
      <div class="card-footer" *ngIf="footer()">
        <ng-content select="[card-footer]"></ng-content>
      </div>
    </div>
  `,
  styleUrls: ['./card.component.scss']
})
export class CardComponent {
  variant = input<'default' | 'elevated' | 'outlined'>('default');
  header = input<boolean>(false);
  footer = input<boolean>(false);

  cardClass(): string {
    return `card-${this.variant()}`;
  }
}