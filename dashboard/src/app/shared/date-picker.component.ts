import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  forwardRef,
  HostListener,
  Input,
  computed,
  signal,
  inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

export interface CalendarDay {
  day: number;
  month: number; // 0-indexed
  year: number;
  iso: string;
  isCurrentMonth: boolean;
  isToday: boolean;
  isSelected: boolean;
  isDisabled: boolean;
}

let nextDatePickerId = 0;

/**
 * Reusable dark-theme calendar date picker for the dashboard.
 *
 * Implements ControlValueAccessor for seamless integration with reactive
 * forms and [(ngModel)]. Outputs and accepts ISO strings (YYYY-MM-DD).
 */
@Component({
  selector: 'app-date-picker',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  templateUrl: './date-picker.component.html',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => DatePickerComponent),
      multi: true
    }
  ]
})
export class DatePickerComponent implements ControlValueAccessor {
  @Input() label = '';
  @Input() placeholder = 'Select date';
  @Input() min = '';
  @Input() max = '';
  @Input() inputClass = '';

  private readonly host = inject(ElementRef<HTMLElement>);
  readonly inputId = `date-picker-${nextDatePickerId++}`;

  readonly open = signal(false);
  readonly disabled = signal(false);
  readonly value = signal('');
  readonly viewYear = signal(new Date().getFullYear());
  readonly viewMonth = signal(new Date().getMonth()); // 0-11

  readonly weekDays = ['Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa', 'Su'];

  private changeHandler: (value: string) => void = () => undefined;
  private touchedHandler: () => void = () => undefined;

  readonly monthLabel = computed(() => {
    const d = new Date(this.viewYear(), this.viewMonth(), 1);
    return d.toLocaleString('en-US', { month: 'long', year: 'numeric' });
  });

  readonly displayValue = computed(() => {
    const val = this.value();
    if (!val) return '';
    const parts = val.split('-');
    if (parts.length === 3) {
      const year = parseInt(parts[0], 10);
      const month = parseInt(parts[1], 10) - 1;
      const day = parseInt(parts[2], 10);
      const d = new Date(year, month, day);
      if (!isNaN(d.getTime())) {
        return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
      }
    }
    return val;
  });

  readonly calendarDays = computed<CalendarDay[]>(() => {
    const year = this.viewYear();
    const month = this.viewMonth();
    const selectedIso = this.value();

    const today = new Date();
    const todayIso = this.formatIso(today.getFullYear(), today.getMonth() + 1, today.getDate());

    const firstDay = new Date(year, month, 1);
    // Sunday is 0, convert to Monday-first (0 = Monday, ..., 6 = Sunday)
    let startDayOfWeek = firstDay.getDay() - 1;
    if (startDayOfWeek === -1) startDayOfWeek = 6;

    const daysInMonth = new Date(year, month + 1, 0).getDate();
    const daysInPrevMonth = new Date(year, month, 0).getDate();

    const days: CalendarDay[] = [];

    // Previous month padding days
    for (let i = startDayOfWeek - 1; i >= 0; i--) {
      const d = daysInPrevMonth - i;
      const prevMonth = month === 0 ? 11 : month - 1;
      const prevYear = month === 0 ? year - 1 : year;
      const iso = this.formatIso(prevYear, prevMonth + 1, d);
      days.push({
        day: d,
        month: prevMonth,
        year: prevYear,
        iso,
        isCurrentMonth: false,
        isToday: iso === todayIso,
        isSelected: iso === selectedIso,
        isDisabled: this.isDateDisabled(iso)
      });
    }

    // Current month days
    for (let d = 1; d <= daysInMonth; d++) {
      const iso = this.formatIso(year, month + 1, d);
      days.push({
        day: d,
        month,
        year,
        iso,
        isCurrentMonth: true,
        isToday: iso === todayIso,
        isSelected: iso === selectedIso,
        isDisabled: this.isDateDisabled(iso)
      });
    }

    // Next month padding days to complete 6 rows (42 cells total) for steady height
    const remaining = 42 - days.length;
    for (let d = 1; d <= remaining; d++) {
      const nextMonth = month === 11 ? 0 : month + 1;
      const nextYear = month === 11 ? year + 1 : year;
      const iso = this.formatIso(nextYear, nextMonth + 1, d);
      days.push({
        day: d,
        month: nextMonth,
        year: nextYear,
        iso,
        isCurrentMonth: false,
        isToday: iso === todayIso,
        isSelected: iso === selectedIso,
        isDisabled: this.isDateDisabled(iso)
      });
    }

    return days;
  });

  private formatIso(year: number, month: number, day: number): string {
    const mm = month.toString().padStart(2, '0');
    const dd = day.toString().padStart(2, '0');
    return `${year}-${mm}-${dd}`;
  }

  private isDateDisabled(iso: string): boolean {
    if (this.min && iso < this.min) return true;
    if (this.max && iso > this.max) return true;
    return false;
  }

  writeValue(val: string | null): void {
    const clean = val ? val.trim() : '';
    this.value.set(clean);
    if (clean) {
      const parts = clean.split('-');
      if (parts.length >= 2) {
        const y = parseInt(parts[0], 10);
        const m = parseInt(parts[1], 10) - 1;
        if (!isNaN(y) && !isNaN(m) && m >= 0 && m <= 11) {
          this.viewYear.set(y);
          this.viewMonth.set(m);
        }
      }
    }
  }

  registerOnChange(fn: (val: string) => void): void {
    this.changeHandler = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.touchedHandler = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled.set(isDisabled);
    if (isDisabled) this.open.set(false);
  }

  toggle(): void {
    if (this.disabled()) return;
    this.open.update(v => !v);
  }

  prevMonth(): void {
    if (this.viewMonth() === 0) {
      this.viewMonth.set(11);
      this.viewYear.update(y => y - 1);
    } else {
      this.viewMonth.update(m => m - 1);
    }
  }

  nextMonth(): void {
    if (this.viewMonth() === 11) {
      this.viewMonth.set(0);
      this.viewYear.update(y => y + 1);
    } else {
      this.viewMonth.update(m => m + 1);
    }
  }

  selectDay(calDay: CalendarDay): void {
    if (calDay.isDisabled || this.disabled()) return;
    this.value.set(calDay.iso);
    this.viewYear.set(calDay.year);
    this.viewMonth.set(calDay.month);
    this.open.set(false);
    this.changeHandler(calDay.iso);
    this.touchedHandler();
  }

  selectToday(): void {
    const today = new Date();
    const iso = this.formatIso(today.getFullYear(), today.getMonth() + 1, today.getDate());
    if (this.isDateDisabled(iso) || this.disabled()) return;
    this.value.set(iso);
    this.viewYear.set(today.getFullYear());
    this.viewMonth.set(today.getMonth());
    this.open.set(false);
    this.changeHandler(iso);
    this.touchedHandler();
  }

  clear(): void {
    if (this.disabled()) return;
    this.value.set('');
    this.open.set(false);
    this.changeHandler('');
    this.touchedHandler();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.open()) return;
    if (!this.host.nativeElement.contains(event.target as Node)) {
      this.open.set(false);
      this.touchedHandler();
    }
  }

  @HostListener('keydown.escape')
  onEscape(): void {
    if (this.open()) {
      this.open.set(false);
    }
  }
}

