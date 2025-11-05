import { Component, input, output, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

@Component({
  selector: 'app-input',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="input-wrapper">
      <label *ngIf="label()" class="input-label">{{ label() }}</label>
      <div class="input-container">
        <input
          [type]="type()"
          [placeholder]="placeholder()"
          [value]="_value"
          [disabled]="disabled()"
          [required]="required()"
          (input)="onInput($event)"
          (blur)="onBlur()"
          class="input-field"
        />
        <ng-content select="[input-icon]"></ng-content>
      </div>
      <div *ngIf="error()" class="input-error">{{ error() }}</div>
    </div>
  `,
  styleUrls: ['./input.component.scss'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => InputComponent),
      multi: true
    }
  ]
})
export class InputComponent implements ControlValueAccessor {
  type = input<'text' | 'email' | 'password' | 'number'>('text');
  placeholder = input<string>('');
  label = input<string>('');
  disabled = input<boolean>(false);
  required = input<boolean>(false);
  error = input<string>('');

  valueChange = output<string>();

  _value = '';
  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  get value(): string {
    return this._value;
  }

  set value(val: string) {
    this._value = val;
    this.onChange(val);
    this.valueChange.emit(val);
  }

  writeValue(value: string): void {
    this._value = value || '';
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    // Implementation for disabling the component
  }

  onInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.value = target.value;
  }

  onBlur(): void {
    this.onTouched();
  }
}