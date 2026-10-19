import { Component, forwardRef, Input } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

/** One entry of a select: a plain string is treated as value + label. */
export type SelectOptionValue = string | { value: string; label: string };

let nextFieldId = 0;

/**
 * Reusable labelled dropdown (no Angular Material — the dashboard uses
 * native controls styled with Tailwind).
 *
 * Implements ControlValueAccessor, so it plugs straight into a reactive
 * form:
 *
 *   <app-select-field formControlName="type" label="Type" [options]="typeOptions" />
 *
 * The dashboard previously repeated the same label + select + placeholder +
 * option-loop markup for every dropdown; this component is the single owner
 * of that pattern.
 */
@Component({
  selector: 'app-select-field',
  standalone: true,
  template: `
    <div>
      <label [attr.for]="id" class="field-label">{{ label }}</label>
      <select
        [id]="id"
        [class]="inputClass"
        [attr.aria-label]="label"
        (change)="handleChange($event)"
        (blur)="handleBlur()"
      >
        <option value="" [selected]="value === ''">{{ placeholder }}</option>
        @for (option of normalizedOptions; track option.value) {
          <option [value]="option.value" [selected]="option.value === value">{{ option.label }}</option>
        }
      </select>
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
  /** Extra classes for the <select> element (the dashboard's shared input style by default). */
  @Input() inputClass = 'input';

  /** Unique id so the <label for> always points at this select. */
  readonly id = `select-field-${nextFieldId++}`;

  value = '';

  private changeHandler: (value: string) => void = () => undefined;
  private touchedHandler: () => void = () => undefined;

  /** Options normalised to { value, label } pairs. */
  get normalizedOptions(): { value: string; label: string }[] {
    return this.options.map(option =>
      typeof option === 'string' ? { value: option, label: option } : option
    );
  }

  writeValue(value: string | null): void {
    this.value = value ?? '';
  }

  registerOnChange(handler: (value: string) => void): void {
    this.changeHandler = handler;
  }

  registerOnTouched(handler: () => void): void {
    this.touchedHandler = handler;
  }

  handleChange(event: Event): void {
    this.value = (event.target as HTMLSelectElement).value;
    this.changeHandler(this.value);
  }

  handleBlur(): void {
    this.touchedHandler();
  }
}
