import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { By } from '@angular/platform-browser';
import { TagsInputComponent } from './tags-input.component';

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, TagsInputComponent],
  template: '<app-tags-input label="Technologies" [formControl]="control" [suggestions]="suggestions" />'
})
class TagsHost {
  readonly control = new FormControl('', { nonNullable: true });
  suggestions = ['React', 'React Native', '.NET', 'SQL'];
}

describe('TagsInputComponent', () => {
  let fixture: ComponentFixture<TagsHost>;
  let field: TagsInputComponent;
  let input: HTMLInputElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [TagsHost] }).compileComponents();
    fixture = TestBed.createComponent(TagsHost);
    fixture.detectChanges();
    field = fixture.debugElement.query(By.directive(TagsInputComponent)).componentInstance;
    input = fixture.nativeElement.querySelector('input');
  });

  function key(value: string): void {
    input.dispatchEvent(new KeyboardEvent('keydown', { key: value, bubbles: true, cancelable: true }));
    fixture.detectChanges();
  }

  function type(value: string): void {
    input.value = value;
    input.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();
  }

  it('selects the active suggestion rather than the partial draft on Enter', () => {
    type('Re');
    key('ArrowDown');
    key('ArrowDown');
    expect(field.active()).toBe('React Native');
    const active = fixture.nativeElement.querySelector(`#${input.getAttribute('aria-activedescendant')}`) as HTMLElement;
    expect(active.textContent).toContain('React Native');
    expect(active.classList.contains('bg-line/[0.06]')).toBeTrue();
    key('Enter');
    expect(fixture.componentInstance.control.value).toBe('React Native');
    expect(field.draft()).toBe('');
    expect(input.getAttribute('aria-activedescendant')).toBeNull();
  });

  it('supports ArrowUp/Down, Home/End and wrapping', () => {
    key('ArrowUp');
    expect(field.active()).toBe('SQL');
    key('ArrowDown');
    expect(field.active()).toBe('React');
    key('End');
    expect(field.active()).toBe('SQL');
    key('Home');
    expect(field.active()).toBe('React');
    key('Enter');
    expect(field.tags()).toEqual(['React']);
    key('ArrowDown');
    expect(field.active()).toBe('React Native');
    key('Escape');
    expect(field.open()).toBeFalse();
  });

  it('commits comma-separated drafts and marks even an unchanged blur touched', () => {
    input.dispatchEvent(new Event('blur'));
    expect(fixture.componentInstance.control.touched).toBeTrue();
    type(' Angular, .NET, angular ');
    key(',');
    expect(fixture.componentInstance.control.value).toBe('Angular, .NET');
    key('Backspace');
    expect(fixture.componentInstance.control.value).toBe('Angular');
    type('Vue');
    input.dispatchEvent(new Event('blur'));
    fixture.detectChanges();
    expect(fixture.componentInstance.control.value).toBe('Angular, Vue');
    expect(field.open()).toBeFalse();
  });

  it('disables input/removal buttons and guards every mutation path', () => {
    fixture.componentInstance.control.setValue('React');
    type('Vue');
    fixture.componentInstance.control.disable();
    fixture.detectChanges();
    expect(input.disabled).toBeTrue();
    expect((fixture.nativeElement.querySelector('button') as HTMLButtonElement).disabled).toBeTrue();
    expect(field.open()).toBeFalse();
    const changes = jasmine.createSpy('change');
    field.registerOnChange(changes);
    type('changed');
    key('ArrowDown');
    key('Enter');
    key('Backspace');
    field.addTag('Angular');
    field.removeTag('React');
    field.focusInput();
    field.onFocus();
    field.onBlur();
    expect(field.tags()).toEqual(['React']);
    expect(field.draft()).toBe('Vue');
    expect(field.open()).toBeFalse();
    expect(changes).not.toHaveBeenCalled();
    expect(fixture.componentInstance.control.touched).toBeFalse();
    fixture.componentInstance.control.enable();
    fixture.detectChanges();
    expect(input.disabled).toBeFalse();
    key('Enter');
    expect(field.tags()).toEqual(['React', 'Vue']);
  });

  it('uses distinct indexed IDs for punctuation, non-Latin text, and separate instances', () => {
    fixture.componentInstance.suggestions = ['C++', 'C#', 'عربي', 'فارسي'];
    fixture.detectChanges();
    key('ArrowDown');
    const options = Array.from(fixture.nativeElement.querySelectorAll('[role="option"]')) as HTMLElement[];
    expect(new Set(options.map(option => option.id)).size).toBe(4);
    expect(fixture.nativeElement.querySelector('input').getAttribute('aria-activedescendant')).toBe(options[0].id);
    const other = TestBed.createComponent(TagsHost);
    other.detectChanges();
    const second = other.debugElement.query(By.directive(TagsInputComponent)).componentInstance as TagsInputComponent;
    expect(field.optionId(0)).not.toBe(second.optionId(0));
    other.destroy();
  });
});

