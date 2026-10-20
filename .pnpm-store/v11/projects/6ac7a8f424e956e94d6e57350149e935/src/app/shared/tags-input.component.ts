import { Component, ElementRef, forwardRef, HostListener, Input, signal, inject } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

/**
 * Tag input with suggestions (no Angular Material).
 *
 * The value stays a comma separated string - the same shape the API and the
 * portfolio expect ("Angular, .NET 9, SQL Server"), so nothing downstream
 * changes; only the way it is typed becomes faster: the suggestions come
 * from the live data (the skill names and the technologies the other projects
 * already use), Enter or a comma commits a tag, and a click adds one.
 */
@Component({
  selector: 'app-tags-input',
  standalone: true,
  template: `
    <div class="relative">
      <label [attr.for]="inputId" class="field-label">{{ label }}</label>

      <div
        class="input flex flex-wrap items-center gap-1.5"
        [class]="inputClass"
        (click)="focusInput()"
      >
        @for (tag of tags(); track tag) {
          <span class="inline-flex items-center gap-1 rounded-md bg-accent/10 px-2 py-1 text-xs font-semibold text-accent-text">
            {{ tag }}
            <button
              type="button"
              class="text-accent-text/70 transition hover:text-danger"
              (click)="removeTag(tag, $event)"
              [attr.aria-label]="'Remove ' + tag"
            >
              &times;
            </button>
          </span>
        }
        <input
          [id]="inputId"
          type="text"
          class="min-w-[9rem] flex-1 bg-transparent text-sm text-ink outline-none placeholder:text-faint"
          [placeholder]="tags().length ? '' : placeholder"
          [value]="draft()"
          autocomplete="off"
          role="combobox"
          aria-autocomplete="list"
          [attr.aria-expanded]="open()"
          [attr.aria-controls]="listId"
          (input)="onInput($event)"
          (keydown)="onKeydown($event)"
          (focus)="open.set(true)"
          (blur)="onBlur()"
        />
      </div>

      @if (open() && filteredSuggestions().length) {
        <ul
          [id]="listId"
          role="listbox"
          class="absolute z-50 mt-1 max-h-56 w-full overflow-auto rounded-xl border border-line/15 bg-raised py-1 shadow-card"
        >
          @for (suggestion of filteredSuggestions(); track suggestion) {
            <li
              role="option"
              [attr.aria-selected]="tags().includes(suggestion)"
              class="flex cursor-pointer items-center justify-between gap-2 px-4 py-2 text-sm text-soft transition hover:bg-line/[0.06] hover:text-ink"
              (mouseenter)="active.set(suggestion)"
              (mousedown)="addTag(suggestion)"
            >
              <span class="truncate">{{ suggestion }}</span>
              @if (tags().includes(suggestion)) {
                <span class="badge-muted">added</span>
              }
            </li>
          }
        </ul>
      }

      @if (hint) {
        <p class="mt-2 text-xs text-muted">{{ hint }}</p>
      }
    </div>
  `,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => TagsInputComponent),
      multi: true
    }
  ]
})
export class TagsInputComponent implements ControlValueAccessor {
  @Input() label = '';
  @Input() placeholder = '';
  @Input() hint = '';
  /** Known values offered while typing (from the live API data). */
  @Input() suggestions: readonly string[] = [];
  @Input() inputClass = '';

  private readonly host = inject(ElementRef<HTMLElement>);

  readonly inputId = `tags-input-${TagsInputComponent.nextId++}`;
  readonly listId = `tags-list-${TagsInputComponent.nextId}`;
  readonly open = signal(false);
  readonly active = signal<string | null>(null);
  readonly draft = signal('');

  private static nextId = 0;

  private tagsValue: string[] = [];
  private changeHandler: (value: string) => void = () => undefined;
  private touchedHandler: () => void = () => undefined;

  /** The committed tags. */
  tags(): string[] {
    return this.tagsValue;
  }

  /** Suggestions that match what is being typed and are not added yet. */
  filteredSuggestions(): string[] {
    const needle = this.draft().trim().toLowerCase();
    return this.suggestions
      .filter(s => !this.tagsValue.some(t => t.toLowerCase() === s.toLowerCase()))
      .filter(s => (needle ? s.toLowerCase().includes(needle) : true))
      .slice(0, 8);
  }

  writeValue(value: string | null): void {
    this.tagsValue = splitTags(value);
    this.draft.set('');
  }

  registerOnChange(handler: (value: string) => void): void {
    this.changeHandler = handler;
  }

  registerOnTouched(handler: () => void): void {
    this.touchedHandler = handler;
  }

  focusInput(): void {
    const input = this.host.nativeElement.querySelector('input');
    input?.focus();
    this.open.set(true);
  }

  onInput(event: Event): void {
    this.draft.set((event.target as HTMLInputElement).value);
    this.open.set(true);
  }

  onKeydown(event: KeyboardEvent): void {
    switch (event.key) {
      case 'Enter':
        event.preventDefault();
        this.commitDraft();
        break;
      case ',':
        event.preventDefault();
        this.commitDraft();
        break;
      case 'Backspace':
        if (!this.draft() && this.tagsValue.length) {
          event.preventDefault();
          this.removeTag(this.tagsValue[this.tagsValue.length - 1], event);
        }
        break;
      case 'Escape':
        this.open.set(false);
        this.draft.set('');
        break;
      case 'ArrowDown':
        event.preventDefault();
        this.open.set(true);
        this.active.set(this.filteredSuggestions()[0] ?? null);
        break;
    }
  }

  onBlur(): void {
    // Commit what was typed, then close the list.
    this.commitDraft();
    this.open.set(false);
  }

  addTag(value: string): void {
    const tag = value.trim();
    if (!tag) return;

    if (!this.tagsValue.some(t => t.toLowerCase() === tag.toLowerCase())) {
      this.tagsValue = [...this.tagsValue, tag];
      this.emit();
    }

    this.draft.set('');
    this.active.set(null);
  }

  removeTag(tag: string, event?: Event): void {
    event?.stopPropagation();
    this.tagsValue = this.tagsValue.filter(t => t !== tag);
    this.emit();
  }

  private commitDraft(): void {
    const draft = this.draft().trim();
    if (!draft) return;

    // "React, .NET 9" pasted in one go becomes two tags.
    splitTags(draft).forEach(tag => this.addTag(tag));
    this.draft.set('');
  }

  private emit(): void {
    this.changeHandler(this.tagsValue.join(', '));
    this.touchedHandler();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.open()) return;
    if (!this.host.nativeElement.contains(event.target as Node)) {
      this.open.set(false);
    }
  }
}

/** Splits a comma separated list into clean, unique tags. */
function splitTags(value: string | null | undefined): string[] {
  if (!value) return [];
  return value
    .split(',')
    .map(part => part.trim())
    .filter(part => part.length > 0)
    .filter((part, index, all) => all.findIndex(p => p.toLowerCase() === part.toLowerCase()) === index);
}
