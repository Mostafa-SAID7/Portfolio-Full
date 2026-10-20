import {
  Component,
  ElementRef,
  forwardRef,
  HostListener,
  Input,
  inject,
  signal
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

/** One entry of a select: a plain string is treated as value + label. */
export type SelectOptionValue = string | { value: string; label: string };

interface Option {
  value: string;
  label: string;
}

let nextFieldId = 0;

/**
 * Reusable labelled dropdown for the dashboard.
 *
 * The browser's native `<select>` (and the `<datalist>` popup) is rendered by
 * the operating system: it ignores the dashboard's design tokens, looks out
 * of place in the dark UI and cannot be styled. This component draws the
 * trigger and the popup itself, so the dropdown matches the rest of the app
 * and behaves the same on every platform.
 *
 * Implements ControlValueAccessor, so it plugs straight into a reactive form:
 *
 *   <app-select-field formControlName="type" label="Type" [options]="typeOptions" />
 *
 * `allowCustom` turns the field into a combo box: the admin can type a value
 * that does not exist yet (a new project category) while the list suggests
 * the known ones.
 */
@Component({
  selector: 'app-select-field',
  standalone: true,
  template: `
    <div class="relative">
      <label [attr.for]="inputId" class="field-label">{{ label }}</label>

      @if (allowCustom) {
        <div class="relative">
          <input
            [id]="inputId"
            type="text"
            [class]="inputFieldClass"
            [placeholder]="placeholder"
            [value]="value"
            role="combobox"
            aria-autocomplete="list"
            [attr.aria-expanded]="open()"
            [attr.aria-controls]="listId"
            (input)="onInput($event)"
            (focus)="open.set(true)"
            (keydown)="onKeydown($event)"
          />
          <button
            type="button"
            class="absolute right-1.5 top-1/2 flex h-8 w-8 -translate-y-1/2 items-center justify-center rounded-lg text-muted transition hover:bg-line/[0.06] hover:text-ink"
            (click)="toggle()"
            [attr.aria-label]="'Show options for ' + label"
            tabindex="-1"
          >
            <svg
              class="h-4 w-4 transition-transform"
              [class.rotate-180]="open()"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
              aria-hidden="true"
            >
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="m6 9 6 6 6-6" />
            </svg>
          </button>
        </div>
      } @else {
        <button
          [id]="inputId"
          type="button"
          role="combobox"
          [class]="triggerClass"
          [attr.aria-expanded]="open()"
          [attr.aria-controls]="listId"
          (click)="toggle()"
          (keydown)="onKeydown($event)"
        >
          <span class="truncate">{{ value ? selectedLabel() : placeholder }}</span>
          <svg
            class="h-4 w-4 shrink-0 text-muted transition-transform"
            [class.rotate-180]="open()"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            aria-hidden="true"
          >
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="m6 9 6 6 6-6" />
          </svg>
        </button>
      }

      @if (open()) {
        <ul
          [id]="listId"
          role="listbox"
          [attr.aria-label]="label"
          class="absolute z-50 mt-1 max-h-64 w-full overflow-auto rounded-xl border border-line/15 bg-raised py-1 shadow-card"
        >
          @for (option of visibleOptions; track option.value) {
            <li
              role="option"
              [id]="optionId(option.value)"
              [attr.aria-selected]="option.value === value"
              [class]="optionClass(option)"
              (mouseenter)="activeValue.set(option.value)"
              (click)="select(option.value)"
            >
              <span class="truncate">{{ option.label }}</span>
              @if (option.value === value) {
                <svg class="h-4 w-4 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="m5 13 4 4L19 7" />
                </svg>
              }
            </li>
          }

          @if (allowCustom && customValue) {
            <li
              role="option"
              [class]="customOptionClass"
              (mouseenter)="activeValue.set(customValue)"
              (click)="select(customValue)"
            >
              <span class="truncate">Use “{{ customValue }}”</span>
            </li>
          }

          @if (!visibleOptions.length && !customValue) {
            <li class="px-4 py-2.5 text-sm text-muted">No options</li>
          }
        </ul>
      }
    </div>
  `,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => SelectFieldComponent),
      multi: true
    }
  ]
})
export class SelectFieldComponent implements ControlValueAccessor {
  @Input() label = '';
  @Input() placeholder = '—';
  @Input() options: readonly SelectOptionValue[] = [];
  /** Allow typing a value that is not in the list (e.g. a new category). */
  @Input() allowCustom = false;
  /** Extra classes for the trigger/input element. */
  @Input() inputClass = '';

  private readonly host = inject(ElementRef<HTMLElement>);

  readonly inputId = `select-field-${nextFieldId++}`;
  readonly listId = `select-list-${nextFieldId}`;
  readonly open = signal(false);
  readonly activeValue = signal<string | null>(null);
  /** Filter text while typing in combo-box mode. */
  readonly filter = signal('');

  value = '';

  private changeHandler: (value: string) => void = () => undefined;
  private touchedHandler: () => void = () => undefined;

  /** Options normalised to { value, label } pairs. */
  get normalizedOptions(): Option[] {
    return this.options.map(option =>
      typeof option === 'string' ? { value: option, label: option } : option
    );
  }

  /** Options that match the current filter (combo-box mode only). */
  get visibleOptions(): Option[] {
    const needle = this.filter().trim().toLowerCase();
    if (!needle) {
      return this.normalizedOptions;
    }
    return this.normalizedOptions.filter(
      option =>
        option.label.toLowerCase().includes(needle) ||
        option.value.toLowerCase().includes(needle)
    );
  }

  /** The typed text that is not an existing option (combo-box mode only). */
  get customValue(): string {
    const text = this.filter().trim();
    if (!text || !this.allowCustom) {
      return '';
    }
    const exists = this.normalizedOptions.some(
      option => option.value.toLowerCase() === text.toLowerCase()
    );
    return exists ? '' : text;
  }

  /** Label of the selected option (falls back to the raw value). */
  selectedLabel(): string {
    const match = this.normalizedOptions.find(option => option.value === this.value);
    return match ? match.label : this.value;
  }

  optionId(value: string): string {
    return `${this.listId}-option-${value.replace(/[^a-zA-Z0-9_-]/g, '-')}`;
  }

  /** Tailwind classes for one option row (literal strings so the JIT sees them). */
  optionClass(option: Option): string {    if (option.value === this.value) {
      return 'flex cursor-pointer items-center justify-between gap-2 bg-accent/10 px-4 py-2.5 text-sm font-semibold text-accent-text transition';
    }
    if (option.value === this.activeValue()) {
      return 'flex cursor-pointer items-center justify-between gap-2 bg-line/[0.06] px-4 py-2.5 text-sm text-ink transition';
    }
    return 'flex cursor-pointer items-center justify-between gap-2 px-4 py-2.5 text-sm text-soft transition hover:bg-line/[0.06] hover:text-ink';
  }

  get customOptionClass(): string {
    return 'flex cursor-pointer items-center justify-between gap-2 px-4 py-2.5 text-sm text-accent-text transition hover:bg-line/[0.06]';
  }

  /** Combo-box input: the shared field style plus room for the chevron button. */
  get inputFieldClass(): string {
    return `input pr-10 ${this.inputClass}`.trim();
  }

  /** Trigger button: the shared field style, dimmed while nothing is selected. */
  get triggerClass(): string {
    const base = 'input flex items-center justify-between gap-2 text-left';
    const tone = this.value ? 'text-ink' : 'text-faint';
    return `${base} ${tone} ${this.inputClass}`.trim();
  }

  writeValue(value: string | null): void {
    this.value = value ?? '';
    if (!this.allowCustom) {
      this.filter.set('');
    }
  }

  registerOnChange(handler: (value: string) => void): void {
    this.changeHandler = handler;
  }

  registerOnTouched(handler: () => void): void {
    this.touchedHandler = handler;
  }

  toggle(): void {
    this.open.update(open => !open);
    if (this.open()) {
      this.activeValue.set(this.value || this.visibleOptions[0]?.value || null);
    }
  }

  onInput(event: Event): void {
    const text = (event.target as HTMLInputElement).value;
    this.value = text;
    this.filter.set(text);
    this.open.set(true);
    this.changeHandler(text);
  }

  select(value: string): void {
    this.value = value;
    this.filter.set('');
    this.open.set(false);
    this.activeValue.set(null);
    this.changeHandler(value);
    this.touchedHandler();
  }

  onKeydown(event: KeyboardEvent): void {
    const options = this.visibleOptions;

    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        this.open.set(true);
        this.activeValue.set(this.nextActive(options, 1));
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.open.set(true);
        this.activeValue.set(this.nextActive(options, -1));
        break;
      case 'Home':
        if (this.open()) {
          event.preventDefault();
          this.activeValue.set(options[0]?.value ?? null);
        }
        break;
      case 'End':
        if (this.open()) {
          event.preventDefault();
          this.activeValue.set(options[options.length - 1]?.value ?? null);
        }
        break;
      case 'Enter':
        event.preventDefault();
        if (this.open()) {
          const active = this.activeValue();
          if (active) {
            this.select(active);
          }
        } else {
          this.toggle();
        }
        break;
      case ' ':
        if (!this.allowCustom) {
          event.preventDefault();
          this.toggle();
        }
        break;
      case 'Escape':
        if (this.open()) {
          event.preventDefault();
          this.open.set(false);
          this.activeValue.set(null);
        }
        break;
      case 'Tab':
        this.open.set(false);
        break;
    }
  }

  private nextActive(options: Option[], step: number): string | null {
    if (!options.length) {
      return null;
    }
    const currentIndex = options.findIndex(option => option.value === this.activeValue());
    const nextIndex = currentIndex < 0
      ? (step > 0 ? 0 : options.length - 1)
      : (currentIndex + step + options.length) % options.length;
    return options[nextIndex].value;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.open()) {
      return;
    }
    if (!this.host.nativeElement.contains(event.target as Node)) {
      this.open.set(false);
      this.activeValue.set(null);
    }
  }
}
