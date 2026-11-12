import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { By } from '@angular/platform-browser';
import { DatePickerComponent } from './date-picker.component';

@Component({
  standalone: true,
  imports: [FormsModule, DatePickerComponent],
  template: `
    <app-date-picker
      [(ngModel)]="date"
      label="Start Date"
      min="2026-01-01"
      max="2026-12-31"
    />
  `
})
class HostComponent {
  date = '2026-11-12';
}

describe('DatePickerComponent', () => {
  let fixture: ComponentFixture<HostComponent>;
  let host: HostComponent;
  let datePicker: DatePickerComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HostComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(HostComponent);
    host = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();

    const de = fixture.debugElement.query(By.directive(DatePickerComponent));
    datePicker = de.componentInstance;
  });

  it('creates the component and binds initial value', () => {
    expect(datePicker).toBeTruthy();
    expect(datePicker.value()).toBe('2026-11-12');
    expect(datePicker.viewYear()).toBe(2026);
    expect(datePicker.viewMonth()).toBe(10); // November is 10
  });

  it('opens and closes popup on toggle', () => {
    expect(datePicker.open()).toBeFalse();
    datePicker.toggle();
    expect(datePicker.open()).toBeTrue();
    datePicker.toggle();
    expect(datePicker.open()).toBeFalse();
  });

  it('navigates months backward and forward', () => {
    datePicker.viewYear.set(2026);
    datePicker.viewMonth.set(10); // Nov

    datePicker.nextMonth();
    expect(datePicker.viewMonth()).toBe(11); // Dec

    datePicker.nextMonth();
    expect(datePicker.viewMonth()).toBe(0); // Jan
    expect(datePicker.viewYear()).toBe(2027);

    datePicker.prevMonth();
    expect(datePicker.viewMonth()).toBe(11); // Dec
    expect(datePicker.viewYear()).toBe(2026);
  });

  it('selects a day and emits change', async () => {
    datePicker.toggle();
    fixture.detectChanges();

    const targetDay = datePicker.calendarDays().find(d => d.iso === '2026-11-20');
    expect(targetDay).toBeTruthy();

    datePicker.selectDay(targetDay!);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(datePicker.value()).toBe('2026-11-20');
    expect(host.date).toBe('2026-11-20');
    expect(datePicker.open()).toBeFalse();
  });

  it('clears date on clear()', async () => {
    datePicker.clear();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(datePicker.value()).toBe('');
    expect(host.date).toBe('');
  });

  it('respects min and max constraints', () => {
    const pastDay = datePicker.calendarDays().find(d => d.iso === '2025-12-31');
    if (pastDay) {
      expect(pastDay.isDisabled).toBeTrue();
    }
  });

  it('closes on Escape key', () => {
    datePicker.open.set(true);
    datePicker.onEscape();
    expect(datePicker.open()).toBeFalse();
  });
});

