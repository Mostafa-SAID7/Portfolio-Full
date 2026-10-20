import { Injectable, signal } from '@angular/core';

/**
 * Keeps the projects list state (search query + page) alive across
 * navigations.
 *
 * The list component is destroyed when the admin opens a project and comes
 * back, so its local state used to reset to page 1 - "Back" from the edit
 * form lost the position. The state lives here (root-provided) instead, so the
 * browser's Back button returns to the exact page and search.
 */
@Injectable({ providedIn: 'root' })
export class ProjectListStateService {
  private readonly pageSignal = signal(1);
  private readonly querySignal = signal('');

  readonly page = this.pageSignal.asReadonly();
  readonly query = this.querySignal.asReadonly();

  setPage(page: number): void {
    this.pageSignal.set(page);
  }

  setQuery(query: string): void {
    this.querySignal.set(query);
    // A new search always starts at the first page.
    this.pageSignal.set(1);
  }

  /** Called when the list is (re)loaded from the API. */
  reset(): void {
    this.pageSignal.set(1);
    this.querySignal.set('');
  }
}
