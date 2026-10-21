import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { By } from '@angular/platform-browser';
import { SelectFieldComponent } from './select-field.component';

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, SelectFieldComponent],
  template: '<app-select-field label="Category" [formControl]="control" [options]="options" [allowCustom]="custom" />'
})
class SelectHost {
  readonly control = new FormControl('', { nonNullable: true });
  options = ['C++', 'C#', 'عربي', 'فارسي'];
  custom = false;
}

describe('SelectFieldComponent', () => {
  let fixture: ComponentFixture<SelectHost>;
  let field: SelectFieldComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [SelectHost] }).compileComponents();
    fixture = TestBed.createComponent(SelectHost);
    fixture.detectChanges();
    field = fixture.debugElement.query(By.directive(SelectFieldComponent)).componentInstance;
  });

  function trigger(): HTMLInputElement | HTMLButtonElement {
    return fixture.nativeElement.querySelector('[role="combobox"]');
  }

  function key(value: string): void {
    trigger().dispatchEvent(new KeyboardEvent('keydown', { key: value, bubbles: true, cancelable: true }));
    fixture.detectChanges();
  }

  it('traverses options, exposes the active descendant, and selects with Enter', () => {
    key('ArrowDown');
    const options = Array.from(fixture.nativeElement.querySelectorAll('[role="option"]')) as HTMLElement[];
    expect(new Set(options.map(option => option.id)).size).toBe(4);
    expect(trigger().getAttribute('aria-activedescendant')).toBe(options[0].id);
    key('ArrowDown');
    expect(trigger().getAttribute('aria-activedescendant')).toBe(options[1].id);
    key('End');
    expect(field.activeValue()).toBe('فارسي');
    key('Home');
    expect(field.activeValue()).toBe('C++');
    key('ArrowUp');
    expect(field.activeValue()).toBe('فارسي');
    key('Enter');
    expect(fixture.componentInstance.control.value).toBe('فارسي');
    expect(field.open()).toBeFalse();
    expect(trigger().getAttribute('aria-activedescendant')).toBeNull();
  });

  it('includes the custom option in keyboard traversal and gives it an indexed ID', () => {
    fixture.componentInstance.custom = true;
    fixture.detectChanges();
    const input = trigger() as HTMLInputElement;
    input.value = 'C';
    input.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();
    key('ArrowDown');
    key('ArrowDown');
    key('ArrowDown');
    const options = fixture.nativeElement.querySelectorAll('[role="option"]') as NodeListOf<HTMLElement>;
    expect(options.length).toBe(3);
    expect(field.activeValue()).toBe('C');
    expect(input.getAttribute('aria-activedescendant')).toBe(options[2].id);
    expect(options[2].classList.contains('bg-line/[0.06]')).toBeTrue();
    key('Enter');
    expect(fixture.componentInstance.control.value).toBe('C');
  });

  it('marks an unchanged control touched on blur and closes on Escape/Tab', () => {
    key('ArrowDown');
    key('Escape');
    expect(field.open()).toBeFalse();
    key('ArrowDown');
    key('Tab');
    expect(field.open()).toBeFalse();
    trigger().dispatchEvent(new Event('blur'));
    expect(fixture.componentInstance.control.touched).toBeTrue();
    expect(fixture.componentInstance.control.value).toBe('');
  });

  it('honors reactive disabled state and guards manually dispatched events', () => {
    fixture.componentInstance.custom = true;
    fixture.detectChanges();
    fixture.componentInstance.control.setValue('C++');
    key('ArrowDown');
    fixture.componentInstance.control.disable();
    fixture.detectChanges();
    expect(trigger().disabled).toBeTrue();
    expect((fixture.nativeElement.querySelector('button') as HTMLButtonElement).disabled).toBeTrue();
    expect(field.open()).toBeFalse();
    const changes = jasmine.createSpy('change');
    field.registerOnChange(changes);
    (trigger() as HTMLInputElement).value = 'changed';
    trigger().dispatchEvent(new Event('input'));
    key('ArrowDown');
    field.toggle();
    field.select('C#');
    field.onFocus();
    field.onBlur();
    expect(field.value).toBe('C++');
    expect(field.open()).toBeFalse();
    expect(changes).not.toHaveBeenCalled();
    expect(fixture.componentInstance.control.touched).toBeFalse();
    fixture.componentInstance.control.enable();
    fixture.detectChanges();
    expect(trigger().disabled).toBeFalse();
    key('ArrowDown');
    expect(field.open()).toBeTrue();
  });

  it('does not reuse option IDs across component instances', () => {
    const other = TestBed.createComponent(SelectHost);
    other.detectChanges();
    const second = other.debugElement.query(By.directive(SelectFieldComponent)).componentInstance as SelectFieldComponent;
    expect(field.optionId(0)).not.toBe(second.optionId(0));
    other.destroy();
  });
});
