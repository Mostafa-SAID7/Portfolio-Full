import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';

/**
 * Full-page 404 for unknown URLs (the previous `path: '**'` silently
 * redirected home, hiding the fact that the address was wrong).
 */
@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './not-found.component.html'
})
export class NotFoundComponent {
  private readonly router = inject(Router);

  /** Client-side "Back" that never leaves the admin shell. */
  goBack(): void {
    void this.router.navigate(['/dashboard']);
  }
}
