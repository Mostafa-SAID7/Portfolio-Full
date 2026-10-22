import { TestBed } from '@angular/core/testing';
import { ProjectListStateService } from './project-list-state.service';

/**
 * The list keeps search/page across navigations (root service) and seeds
 * itself from the URL (?q=&page=) so a reload reopens the exact position
 * (issue: returning to page 2 of /projects restored page 1).
 */
describe('ProjectListStateService', () => {
  let service: ProjectListStateService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ProjectListStateService);
  });

  it('starts at page 1 with no query', () => {
    expect(service.page()).toBe(1);
    expect(service.query()).toBe('');
  });

  it('restores a saved position without the setters side effects', () => {
    service.restore(3, 'laravel');
    expect(service.page()).toBe(3);
    expect(service.query()).toBe('laravel');

    // restore must NOT reset the page the way setQuery does
    service.restore(2, '');
    expect(service.page()).toBe(2);
    expect(service.query()).toBe('');
  });

  it('clamps the restored page to at least 1', () => {
    service.restore(0, '');
    expect(service.page()).toBe(1);
    service.restore(-4, '');
    expect(service.page()).toBe(1);
  });

  it('a new search still resets to the first page', () => {
    service.setPage(5);
    service.setQuery('react');
    expect(service.page()).toBe(1);
    expect(service.query()).toBe('react');
  });
});
