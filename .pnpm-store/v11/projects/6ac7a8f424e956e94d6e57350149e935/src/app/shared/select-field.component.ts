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
  templateUrl: "./select-field.component.html",
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
  readonly listId = `${this.inputId}-list`;
  readonly open = signal(false);
  readonly disabled = signal<boolean>(false);
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

  private get selectableOptions(): Option[] {
    const custom = this.customValue;
    return custom ? [...this.visibleOptions, { value: custom, label: custom }] : this.visibleOptions;
  }

  get activeDescendant(): string | null {
    if (!this.open() || this.disabled()) return null;
    const index = this.selectableOptions.findIndex(option => option.value === this.activeValue());
    return index >= 0 ? this.optionId(index) : null;
  }

  optionId(index: number): string {
    return `${this.listId}-option-${index}`;
  }

  /** Tailwind classes for one option row (literal strings so the JIT sees them). */
  optionClass(option: Option): string {
    if (option.value === this.value) {
      return 'flex cursor-pointer items-center justify-between gap-2 bg-accent/10 px-4 py-2.5 text-sm font-semibold text-accent-text transition';
    }
    if (option.value === this.activeValue()) {
      return 'flex cursor-pointer items-center justify-between gap-2 bg-line/[0.06] px-4 py-2.5 text-sm text-ink transition';
    }
    return 'flex cursor-pointer items-center justify-between gap-2 px-4 py-2.5 text-sm text-soft transition hover:bg-line/[0.06] hover:text-ink';
  }

  get customOptionClass(): string {
    const base = 'flex cursor-pointer items-center justify-between gap-2 px-4 py-2.5 text-sm text-accent-text transition hover:bg-line/[0.06]';
    return this.customValue === this.activeValue() ? `${base} bg-line/[0.06]` : base;
  }

  /** Combo-box input: the shared field style plus room for the chevron button. */
  get inputFieldClass(): string {
    return `input pr-10 disabled:cursor-not-allowed disabled:opacity-60 ${this.inputClass}`.trim();
  }

  /** Trigger button: the shared field style, dimmed while nothing is selected. */
  get triggerClass(): string {
    const base = 'input flex items-center justify-between gap-2 text-left disabled:cursor-not-allowed disabled:opacity-60';
    const tone = this.value ? 'text-ink' : 'text-faint';
    return `${base} ${tone} ${this.inputClass}`.trim();
  }

  writeValue(value: string | null): void {
    this.value = value ?? '';
    this.filter.set('');
    this.activeValue.set(null);
  }

  registerOnChange(handler: (value: string) => void): void {
    this.changeHandler = handler;
  }

  registerOnTouched(handler: () => void): void {
    this.touchedHandler = handler;
  }

  setDisabledState(disabled: boolean): void {
    this.disabled.set(disabled);
    if (disabled) this.close();
  }

  onFocus(): void {
    if (!this.disabled()) this.open.set(true);
  }

  onBlur(): void {
    this.close();
    if (!this.disabled()) this.touchedHandler();
  }

  setActive(value: string): void {
    if (!this.disabled()) this.activeValue.set(value);
  }

  toggle(): void {
    if (this.disabled()) return;
    this.open.update(open => !open);
    const options = this.selectableOptions;
    this.activeValue.set(this.open()
      ? options.find(option => option.value === this.value)?.value ?? options[0]?.value ?? null
      : null);
  }

  onInput(event: Event): void {
    if (this.disabled()) return;
    const text = (event.target as HTMLInputElement).value;
    this.value = text;
    this.filter.set(text);
    this.activeValue.set(null);
    this.open.set(true);
    this.changeHandler(text);
  }

  select(value: string): void {
    if (this.disabled()) return;
    this.value = value;
    this.filter.set('');
    this.close();
    this.changeHandler(value);
    this.touchedHandler();
    (this.host.nativeElement.querySelector(`#${this.inputId}`) as HTMLElement | null)?.focus();
    // Focusing the custom input must not reopen a list just selected by pointer.
    this.close();
  }

  onKeydown(event: KeyboardEvent): void {
    if (this.disabled()) return;
    if (event.isComposing || event.keyCode === 229) return;
    const options = this.selectableOptions;

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
          if (active !== null && options.some(option => option.value === active)) this.select(active);
          else if (this.allowCustom && this.customValue) this.select(this.customValue);
        } else {
          this.toggle();
        }
        break;
      case ' ':
        if (!this.allowCustom) {
          event.preventDefault();
          const active = this.activeValue();
          if (this.open() && active !== null && options.some(option => option.value === active)) this.select(active);
          else this.toggle();
        }
        break;
      case 'Escape':
        if (this.open()) {
          event.preventDefault();
          event.stopPropagation();
          this.close();
        }
        break;
      case 'Tab':
        this.close();
        break;
    }
    const id = this.activeDescendant;
    if (id) (this.host.nativeElement.querySelector(`#${id}`) as HTMLElement | null)?.scrollIntoView({ block: 'nearest' });
  }

  private close(): void {
    this.open.set(false);
    this.activeValue.set(null);
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
