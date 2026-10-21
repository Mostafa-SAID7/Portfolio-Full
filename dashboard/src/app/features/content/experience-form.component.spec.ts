import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NgForm } from '@angular/forms';
import { By } from '@angular/platform-browser';
import { ExperienceDraft, ExperienceFormComponent } from './experience-form.component';

function validDraft(): ExperienceDraft {
  return {
    companyEn: ' Company ', companyAr: ' شركة ', positionEn: ' Engineer ', positionAr: ' مهندس ',
    descriptionEn: ' Built APIs ', descriptionAr: ' تطوير واجهات برمجية ', locationEn: ' Remote ', locationAr: ' عن بعد ',
    startDate: '2024-02-29', endDate: '2024-03-01', technologies: ' Angular, .NET, ',
    current: false, order: 4, isActive: false
  };
}

describe('ExperienceFormComponent', () => {
  let fixture: ComponentFixture<ExperienceFormComponent>;
  let component: ExperienceFormComponent;
  let save: jasmine.Spy;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [ExperienceFormComponent] }).compileComponents();
    fixture = TestBed.createComponent(ExperienceFormComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('draft', validDraft());
    save = spyOn(component.save, 'emit');
    await render();
  });

  async function render(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }
  function form(): NgForm {
    return fixture.debugElement.query(By.directive(NgForm)).injector.get(NgForm);
  }

  it('emits every bilingual field, flags/order, technologies, and exact local dates', () => {
    component.submit(form());
    expect(save).toHaveBeenCalledOnceWith({
      companyEn: 'Company', companyAr: 'شركة', positionEn: 'Engineer', positionAr: 'مهندس',
      descriptionEn: 'Built APIs', descriptionAr: 'تطوير واجهات برمجية', locationEn: 'Remote', locationAr: 'عن بعد',
      startDate: '2024-02-29T00:00:00', endDate: '2024-03-01T00:00:00',
      technologies: ['Angular', '.NET'], current: false, order: 4, isActive: false
    });
  });

  for (const field of ['companyEn', 'companyAr', 'positionEn', 'positionAr', 'descriptionEn', 'descriptionAr'] as const) {
    it(`requires ${field} independently and never mirrors another translation`, async () => {
      component.draft[field] = '';
      await render();
      expect(form().controls[field].errors?.['required']).toBeTrue();
      component.submit(form());
      expect(save).not.toHaveBeenCalled();
      expect(component.validationError()).toContain('required');
      expect(component.draft[field]).toBe('');
    });
  }

  for (const [field, max] of [
    ['companyEn', 100], ['companyAr', 100], ['positionEn', 100], ['positionAr', 100],
    ['descriptionEn', 1000], ['descriptionAr', 1000], ['locationEn', 100], ['locationAr', 100]
  ] as const) {
    it(`applies the server maxlength to ${field}`, async () => {
      component.draft[field] = 'x'.repeat(max + 1);
      await render();
      expect(form().controls[field].errors?.['maxlength']).toBeTruthy();
      component.submit(form());
      expect(save).not.toHaveBeenCalled();
    });
  }

  it('makes a current role end date null regardless of a previously selected date', async () => {
    component.draft.current = true;
    await render();
    const end = fixture.debugElement.query(By.css('#exp-end')).nativeElement as HTMLInputElement;
    expect(end.disabled).toBeTrue();
    component.submit(form());
    expect(save.calls.mostRecent().args[0].endDate).toBeNull();
    expect(component.draft.endDate).toBe('2024-03-01');
  });

  it('requires an end date when switching a current role back to past', async () => {
    component.draft.current = true;
    component.draft.endDate = '';
    await render();
    expect(component.validationError()).toBeNull();
    component.draft.current = false;
    await render();
    expect(form().controls['endDate'].errors?.['required']).toBeTrue();
    component.submit(form());
    expect(save).not.toHaveBeenCalled();
    expect(component.validationError()).toContain('past role');
  });

  it('rejects an end before the start, but permits the same calendar day', async () => {
    component.draft.endDate = '2024-02-28';
    await render();
    component.submit(form());
    expect(save).not.toHaveBeenCalled();
    expect(component.validationError()).toContain('on or after');
    component.draft.endDate = component.draft.startDate;
    await render();
    component.submit(form());
    expect(save).toHaveBeenCalledTimes(1);
  });

  it('rejects nonexistent calendar dates without timezone conversion', async () => {
    component.draft.startDate = '2023-02-29';
    await render();
    component.submit(form());
    expect(save).not.toHaveBeenCalled();
    expect(component.validationError()).toContain('start date');
    component.draft.startDate = '2024-04-31';
    expect(component.validationError()).toContain('start date');
    component.draft.startDate = '2024-02-29';
    expect(component.validationError()).toBeNull();
  });

  it('rejects more than 20 technologies rather than silently truncating', async () => {
    component.draft.technologies = Array.from({ length: 21 }, (_, index) => `Tech ${index}`).join(',');
    await render();
    component.submit(form());
    expect(save).not.toHaveBeenCalled();
    expect(component.validationError()).toContain('20');
    component.draft.technologies = Array.from({ length: 20 }, (_, index) => `Tech ${index}`).join(',');
    await render();
    component.submit(form());
    expect(save.calls.mostRecent().args[0].technologies.length).toBe(20);
  });

  it('uses null for independently empty optional locations', async () => {
    component.draft.locationEn = ' ';
    component.draft.locationAr = 'عن بعد';
    await render();
    component.submit(form());
    expect(save.calls.mostRecent().args[0].locationEn).toBeNull();
    expect(save.calls.mostRecent().args[0].locationAr).toBe('عن بعد');
  });

  it('prevents busy submissions and disables the form and cancel action', async () => {
    fixture.componentRef.setInput('busy', true);
    await render();
    component.submit(form());
    expect(save).not.toHaveBeenCalled();
    expect((fixture.debugElement.query(By.css('fieldset')).nativeElement as HTMLFieldSetElement).disabled).toBeTrue();
    expect((fixture.debugElement.query(By.css('button[type="button"]')).nativeElement as HTMLButtonElement).disabled).toBeTrue();
  });

  it('keeps server domain errors visible in the form', async () => {
    fixture.componentRef.setInput('error', 'Arabic description rejected.');
    await render();
    expect(fixture.debugElement.query(By.css('[role="alert"]')).nativeElement.textContent).toContain('Arabic description rejected.');
  });
});
