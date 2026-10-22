import { TestBed } from '@angular/core/testing';
import { BrowserStorage, STORAGE_KEYS } from './storage.service';

/** Every persisted key flows through this one service (auth, theme, sidebar). */
describe('BrowserStorage', () => {
  let storage: BrowserStorage;

  beforeEach(() => {
    localStorage.clear();
    storage = TestBed.inject(BrowserStorage);
  });

  afterEach(() => localStorage.clear());

  it('round-trips values through the namespaced keys', () => {
    storage.set(STORAGE_KEYS.theme, 'dark');
    expect(storage.get(STORAGE_KEYS.theme)).toBe('dark');
    expect(localStorage.getItem(STORAGE_KEYS.theme)).toBe('dark');

    storage.remove(STORAGE_KEYS.theme);
    expect(storage.get(STORAGE_KEYS.theme)).toBeNull();
  });

  it('returns null for keys that were never written', () => {
    expect(storage.get('never_written')).toBeNull();
  });
});
