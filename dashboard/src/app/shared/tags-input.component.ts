import { Component, ElementRef, forwardRef, HostListener, Input, signal, inject } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

/** Suggestion-assisted tags CVA; the public value remains a comma-separated string. */
@Component({
  selector: 'app-tags-input',
  standalone: true,
  templateUrl: './tags-input.component.html',
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => TagsInputComponent), multi: true }]
})
export class TagsInputComponent implements ControlValueAccessor {
  @Input() label = '';
  @Input() placeholder = '';
  @Input() hint = '';
  @Input() suggestions: readonly string[] = [];
  @Input() inputClass = '';

  private readonly host = inject(ElementRef<HTMLElement>);
  private static nextId = 0;
  readonly inputId = `tags-input-${TagsInputComponent.nextId++}`;
  readonly listId = `${this.inputId}-list`;
  readonly open = signal(false);
  readonly disabled = signal<boolean>(false);
  readonly active = signal<string | null>(null);
  readonly draft = signal('');

  private tagsValue: string[] = [];
  private changeHandler: (value: string) => void = () => undefined;
  private touchedHandler: () => void = () => undefined;

  tags(): string[] { return this.tagsValue; }

  filteredSuggestions(): string[] {
    const needle = this.draft().trim().toLowerCase();
    return this.suggestions
      .filter((s, index, all) => all.findIndex(value => value.toLowerCase() === s.toLowerCase()) === index)
      .filter(s => !this.tagsValue.some(t => t.toLowerCase() === s.toLowerCase()))
      .filter(s => !needle || s.toLowerCase().includes(needle))
      .slice(0, 8);
  }

  optionId(index: number): string { return `${this.listId}-option-${index}`; }

  get activeDescendant(): string | null {
    if (!this.open() || this.disabled()) return null;
    const index = this.filteredSuggestions().indexOf(this.active() ?? '');
    return index >= 0 ? this.optionId(index) : null;
  }

  writeValue(value: string | null): void {
    this.tagsValue = splitTags(value);
    this.draft.set('');
    this.active.set(null);
  }

  registerOnChange(handler: (value: string) => void): void { this.changeHandler = handler; }
  registerOnTouched(handler: () => void): void { this.touchedHandler = handler; }

  setDisabledState(disabled: boolean): void {
    this.disabled.set(disabled);
    if (disabled) this.close();
  }

  onFocus(): void {
    if (!this.disabled()) this.open.set(true);
  }

  focusInput(): void {
    if (this.disabled()) return;
    (this.host.nativeElement.querySelector('input') as HTMLInputElement | null)?.focus();
    this.open.set(true);
  }

  setActive(value: string): void {
    if (!this.disabled()) this.active.set(value);
  }

  onInput(event: Event): void {
    if (this.disabled()) return;
    this.draft.set((event.target as HTMLInputElement).value);
    this.active.set(null);
    this.open.set(true);
  }

  onKeydown(event: KeyboardEvent): void {
    if (this.disabled()) return;
    const suggestions = this.filteredSuggestions();
    switch (event.key) {
      case 'ArrowDown':
      case 'ArrowUp': {
        event.preventDefault();
        this.open.set(true);
        const step = event.key === 'ArrowDown' ? 1 : -1;
        const current = suggestions.indexOf(this.active() ?? '');
        const next = current < 0 ? (step > 0 ? 0 : suggestions.length - 1) :
          (current + step + suggestions.length) % suggestions.length;
        this.active.set(suggestions[next] ?? null);
        break;
      }
      case 'Home':
      case 'End':
        if (this.open() && suggestions.length > 0) {
          event.preventDefault();
          this.active.set(suggestions[event.key === 'Home' ? 0 : suggestions.length - 1] ?? null);
        }
        break;
      case 'Enter': {
        event.preventDefault();
        const active = this.active();
        if (this.open() && active !== null && suggestions.includes(active)) this.addTag(active);
        else this.commitDraft();
        break;
      }
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
        if (this.open()) {
          event.preventDefault();
          event.stopPropagation();
          this.close();
          this.draft.set('');
        }
        break;
      case 'Tab':
        this.close();
        break;
    }
    const id = this.activeDescendant;
    if (id) (this.host.nativeElement.querySelector(`#${id}`) as HTMLElement | null)?.scrollIntoView({ block: 'nearest' });
  }

  onBlur(): void {
    if (!this.disabled()) {
      this.commitDraft();
      this.touchedHandler();
    }
    this.close();
  }

  addTag(value: string): void {
    if (this.disabled()) return;
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
    if (this.disabled()) return;
    this.tagsValue = this.tagsValue.filter(t => t !== tag);
    this.active.set(null);
    this.emit();
  }

  private commitDraft(): void {
    if (this.disabled()) return;
    splitTags(this.draft()).forEach(tag => this.addTag(tag));
    this.draft.set('');
  }

  private emit(): void {
    this.changeHandler(this.tagsValue.join(', '));
    this.touchedHandler();
  }

  private close(): void {
    this.open.set(false);
    this.active.set(null);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (this.open() && !this.host.nativeElement.contains(event.target as Node)) this.close();
  }
}

function splitTags(value: string | null | undefined): string[] {
  if (!value) return [];
  return value.split(',').map(part => part.trim()).filter(part => part.length > 0)
    .filter((part, index, all) => all.findIndex(p => p.toLowerCase() === part.toLowerCase()) === index);
}
