import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NgForm } from '@angular/forms';
import { By } from '@angular/platform-browser';
import { Subject, of, tap, throwError } from 'rxjs';
import { Category } from '../../core/models/category.model';
import { Experience, ExperiencePayload } from '../../core/models/experience.model';
import { Skill } from '../../core/models/skill.model';
import { CategoryService } from '../../core/services/category.service';
import { ExperienceService } from '../../core/services/experience.service';
import { SkillService } from '../../core/services/skill.service';
import { ConfirmDialogService } from '../../shared/confirm-dialog.component';
import { ToastService } from '../../shared/toast.component';
import { ContentComponent } from './content.component';
import { ExperienceFormComponent } from './experience-form.component';

const category: Category = { id: 1, nameEn: 'Robotics', nameAr: 'الروبوتات', order: 7, isActive: false, projectCount: 2 };
const skill: Skill = {
  id: 1, nameEn: 'Angular', nameAr: 'أنجولار', categoryEn: 'Frontend', categoryAr: 'واجهات', proficiency: 80,
  iconUrl: '/angular.svg', descriptionEn: 'English description', descriptionAr: 'الوصف العربي',
  order: 9, isActive: false, createdAt: '2024-01-01T00:00:00', updatedAt: null
};
const experience: Experience = {
  id: 1, companyEn: 'Company', companyAr: 'شركة', positionEn: 'Engineer', positionAr: 'مهندس',
  descriptionEn: 'Built APIs', descriptionAr: 'تطوير واجهات برمجية', locationEn: 'Remote', locationAr: 'عن بعد',
  startDate: '2024-03-01T00:00:00+14:00', endDate: '2024-04-01T00:00:00-12:00',
  technologies: ['Angular', '.NET'], current: false, order: 8, isActive: false,
  createdAt: '2024-01-01T00:00:00', updatedAt: null
};

describe('ContentComponent', () => {
  let fixture: ComponentFixture<ContentComponent>;
  let component: ContentComponent;
  let skills: ReturnType<typeof skillStub>;
  let categories: ReturnType<typeof categoryStub>;
  let experiences: ReturnType<typeof experienceStub>;
  let dialog: { confirm: jasmine.Spy<ConfirmDialogService['confirm']> };
  let toast: { show: jasmine.Spy<ToastService['show']> };

  function skillStub() {
    return {
      skills: signal<Skill[]>([]), loading: signal(false), error: signal<string | null>(null),
      loadSkills: jasmine.createSpy<SkillService['loadSkills']>('loadSkills').and.returnValue(of([])),
      createSkill: jasmine.createSpy<SkillService['createSkill']>('createSkill').and.returnValue(of(skill)),
      updateSkill: jasmine.createSpy<SkillService['updateSkill']>('updateSkill').and.returnValue(of(undefined)),
      deleteSkill: jasmine.createSpy<SkillService['deleteSkill']>('deleteSkill').and.returnValue(of(undefined))
    };
  }
  function categoryStub() {
    return {
      categories: signal<Category[]>([]), loading: signal(false), error: signal<string | null>(null),
      loadCategories: jasmine.createSpy<CategoryService['loadCategories']>('loadCategories').and.returnValue(of([])),
      createCategory: jasmine.createSpy<CategoryService['createCategory']>('createCategory').and.returnValue(of(category)),
      updateCategory: jasmine.createSpy<CategoryService['updateCategory']>('updateCategory').and.returnValue(of(undefined)),
      deleteCategory: jasmine.createSpy<CategoryService['deleteCategory']>('deleteCategory').and.returnValue(of(undefined))
    };
  }
  function experienceStub() {
    return {
      experience: signal<Experience[]>([]), loading: signal(false), error: signal<string | null>(null),
      loadExperience: jasmine.createSpy<ExperienceService['loadExperience']>('loadExperience').and.returnValue(of([])),
      createExperience: jasmine.createSpy<ExperienceService['createExperience']>('createExperience').and.returnValue(of(experience)),
      updateExperience: jasmine.createSpy<ExperienceService['updateExperience']>('updateExperience').and.returnValue(of(undefined)),
      deleteExperience: jasmine.createSpy<ExperienceService['deleteExperience']>('deleteExperience').and.returnValue(of(undefined))
    };
  }

  beforeEach(async () => {
    skills = skillStub();
    categories = categoryStub();
    experiences = experienceStub();
    dialog = { confirm: jasmine.createSpy<ConfirmDialogService['confirm']>('confirm').and.returnValue(Promise.resolve(false)) };
    toast = { show: jasmine.createSpy<ToastService['show']>('show') };
    await TestBed.configureTestingModule({
      imports: [ContentComponent],
      providers: [
        { provide: SkillService, useValue: skills }, { provide: CategoryService, useValue: categories },
        { provide: ExperienceService, useValue: experiences }, { provide: ConfirmDialogService, useValue: dialog },
        { provide: ToastService, useValue: toast }
      ]
    }).compileComponents();
    fixture = TestBed.createComponent(ContentComponent);
    component = fixture.componentInstance;
    await render();
    skills.loadSkills.calls.reset();
    categories.loadCategories.calls.reset();
    experiences.loadExperience.calls.reset();
  });

  async function render(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }
  function form(selector = 'form'): NgForm {
    return fixture.debugElement.query(By.css(selector)).injector.get(NgForm);
  }
  function experienceForm(): ExperienceFormComponent {
    return fixture.debugElement.query(By.directive(ExperienceFormComponent)).componentInstance as ExperienceFormComponent;
  }

  it('creates and renames categories with independent translations and preserved flags/order', async () => {
    component.selectTab('categories');
    component.newCategoryName = ' Robotics ';
    component.newCategoryNameAr = ' الروبوتات ';
    await render();
    component.addCategory(form());
    expect(categories.createCategory).toHaveBeenCalledOnceWith({ nameEn: 'Robotics', nameAr: 'الروبوتات', order: 1, isActive: true });
    categories.categories.set([category]);
    component.startEditingCategory(category);
    expect(component.categoryDraftNameAr).toBe(category.nameAr);
    component.categoryDraftName = 'Automation';
    component.categoryDraftNameAr = 'الأتمتة';
    await render();
    component.saveCategory(category, fixture.debugElement.queryAll(By.directive(NgForm))[1].injector.get(NgForm));
    expect(categories.updateCategory).toHaveBeenCalledOnceWith(1, { nameEn: 'Automation', nameAr: 'الأتمتة' });
  });

  it('does not submit category names with a missing translation or more than 100 characters', async () => {
    component.selectTab('categories');
    component.newCategoryName = 'English only';
    await render();
    component.addCategory(form());
    component.newCategoryNameAr = 'ع'.repeat(101);
    await render();
    component.addCategory(form());
    expect(categories.createCategory).not.toHaveBeenCalled();
    expect(component.categoryFormError).toContain('100');
    expect(form().controls['categoryNameAr'].errors?.['maxlength']).toBeTruthy();
  });

  it('creates a complete bilingual skill without language/category fallback', async () => {
    component.newSkillName = ' Angular ';
    component.newSkillNameAr = ' أنجولار ';
    component.newSkillCategory = ' Frontend ';
    component.newSkillCategoryAr = ' واجهات ';
    await render();
    component.addSkill(form());
    expect(skills.createSkill).toHaveBeenCalledOnceWith({
      nameEn: 'Angular', nameAr: 'أنجولار', categoryEn: 'Frontend', categoryAr: 'واجهات', proficiency: 70,
      iconUrl: null, descriptionEn: null, descriptionAr: null, order: 1, isActive: true
    });
  });

  it('enforces the actual SkillValidator limits and rejects missing Arabic categories', async () => {
    component.newSkillName = 'a'.repeat(51);
    component.newSkillNameAr = 'مهارة';
    component.newSkillCategory = 'Frontend';
    component.newSkillCategoryAr = 'واجهات';
    await render();
    component.addSkill(form());
    expect(form().controls['skillNameEn'].errors?.['maxlength']).toBeTruthy();
    component.newSkillName = 'Angular';
    component.newSkillCategoryAr = ' ';
    await render();
    component.addSkill(form());
    expect(skills.createSkill).not.toHaveBeenCalled();
    expect(component.skillFormError).toContain('Arabic');
  });

  it('preserves both skill descriptions and visibility/order when editing', async () => {
    skills.skills.set([skill]);
    component.startEditSkill(skill);
    component.editDraft!.nameEn = 'Updated';
    await render();
    component.saveEditSkill(fixture.debugElement.queryAll(By.directive(NgForm))[1].injector.get(NgForm));
    expect(skills.updateSkill).toHaveBeenCalledOnceWith(1, {
      NameEn: 'Updated', NameAr: skill.NameAr, CategoryEn: skill.CategoryEn, CategoryAr: skill.CategoryAr,
      Proficiency: 80, IconUrl: '/angular.svg', DescriptionEn: skill.DescriptionEn,
      DescriptionAr: skill.DescriptionAr, Order: 9, IsActive: false
    });
  });

  it('retains both descriptions on skill visibility changes', () => {
    component.toggleSkill(skill);
    expect(skills.updateSkill).toHaveBeenCalledOnceWith(1, {
      NameEn: skill.NameEn, NameAr: skill.NameAr, CategoryEn: skill.CategoryEn, CategoryAr: skill.CategoryAr,
      Proficiency: 80, IconUrl: skill.IconUrl, DescriptionEn: skill.DescriptionEn,
      DescriptionAr: skill.DescriptionAr, Order: 9, IsActive: true
    });
  });

  it('round-trips complete bilingual experience and preserves calendar dates during edit', async () => {
    component.selectTab('experience');
    component.startExperienceEdit(experience);
    expect(component.expDraft!.startDate).toBe('2024-03-01');
    expect(component.expDraft!.endDate).toBe('2024-04-01');
    await render();
    experienceForm().submit(form());
    const payload: ExperiencePayload = {
      companyEn: experience.companyEn, companyAr: experience.companyAr,
      positionEn: experience.positionEn, positionAr: experience.positionAr,
      descriptionEn: experience.descriptionEn, descriptionAr: experience.descriptionAr,
      locationEn: experience.locationEn, locationAr: experience.locationAr,
      startDate: '2024-03-01T00:00:00', endDate: '2024-04-01T00:00:00',
      technologies: ['Angular', '.NET'], current: false, order: 8, isActive: false
    };
    expect(experiences.updateExperience).toHaveBeenCalledOnceWith(1, payload);
    expect(experiences.createExperience).not.toHaveBeenCalled();
  });

  it('creates experience through the same validated bilingual form', async () => {
    experiences.experience.set([
      { ...experience, id: 10, order: 1 },
      { ...experience, id: 11, order: 2 }
    ]);
    component.selectTab('experience');
    component.startExperienceCreate();
    Object.assign(component.expDraft!, {
      companyEn: 'Company', companyAr: 'شركة', positionEn: 'Engineer', positionAr: 'مهندس',
      descriptionEn: 'Built APIs', descriptionAr: 'تطوير', locationEn: 'Remote', locationAr: 'عن بعد',
      startDate: '2024-02-29', current: true, technologies: 'Angular, .NET'
    });
    await render();
    experienceForm().submit(form());
    expect(experiences.createExperience).toHaveBeenCalledOnceWith({
      companyEn: 'Company', companyAr: 'شركة', positionEn: 'Engineer', positionAr: 'مهندس',
      descriptionEn: 'Built APIs', descriptionAr: 'تطوير', locationEn: 'Remote', locationAr: 'عن بعد',
      startDate: '2024-02-29T00:00:00', endDate: null, technologies: ['Angular', '.NET'], current: true, order: 3, isActive: true
    });
  });

  for (const source of ['skills', 'categories', 'experience'] as const) {
    it(`renders mutually exclusive loading/error/empty states and retries only ${source}`, async () => {
      const state = source === 'skills' ? skills : source === 'categories' ? categories : experiences;
      component.selectTab(source);
      state.loading.set(true);
      state.error.set('Unavailable');
      await render();
      expect(fixture.debugElement.query(By.css('[role="status"]'))).toBeTruthy();
      expect(fixture.debugElement.query(By.css('app-empty-state'))).toBeNull();
      expect(fixture.debugElement.query(By.css('app-error-state'))).toBeNull();
      state.loading.set(false);
      await render();
      const error = fixture.debugElement.query(By.css('app-error-state'));
      expect(error).toBeTruthy();
      expect(fixture.debugElement.query(By.css('app-empty-state'))).toBeNull();
      error.query(By.css('button')).nativeElement.click();
      expect(skills.loadSkills.calls.count()).toBe(source === 'skills' ? 1 : 0);
      expect(categories.loadCategories.calls.count()).toBe(source === 'categories' ? 1 : 0);
      expect(experiences.loadExperience.calls.count()).toBe(source === 'experience' ? 1 : 0);
      state.error.set(null);
      await render();
      expect(fixture.debugElement.query(By.css('app-empty-state'))).toBeTruthy();
    });
  }

  it('uses ordinary labeled toggle buttons instead of incomplete ARIA tabs', () => {
    const group = fixture.debugElement.query(By.css('[role="group"]'));
    expect(group.attributes['aria-label']).toBe('Content type');
    expect(group.queryAll(By.css('[aria-pressed]')).length).toBe(3);
    expect(fixture.debugElement.query(By.css('[role="tab"]'))).toBeNull();
    group.queryAll(By.css('button'))[2].nativeElement.click();
    fixture.detectChanges();
    expect(component.tab()).toBe('categories');
    expect(group.queryAll(By.css('button'))[2].attributes['aria-pressed']).toBe('true');
  });

  it('cancels category, skill, and experience deletion without requests', async () => {
    await component.deleteCategory(category);
    await component.deleteSkill(skill);
    await component.deleteExperience(1, 'Engineer');
    expect(dialog.confirm.calls.count()).toBe(3);
    expect(categories.deleteCategory).not.toHaveBeenCalled();
    expect(skills.deleteSkill).not.toHaveBeenCalled();
    expect(experiences.deleteExperience).not.toHaveBeenCalled();
    expect(component.categoriesBusy()).toBeFalse();
  });

  it('reserves the category while confirming and only deletes after confirmation', async () => {
    let resolve!: (value: boolean) => void;
    dialog.confirm.and.returnValue(new Promise<boolean>(settle => { resolve = settle; }));
    const pending = component.deleteCategory(category);
    component.toggleCategory(category);
    await component.deleteCategory(category);
    expect(component.categoriesBusy()).toBeTrue();
    expect(dialog.confirm.calls.count()).toBe(1);
    expect(categories.updateCategory).not.toHaveBeenCalled();
    resolve(true);
    await pending;
    expect(categories.deleteCategory).toHaveBeenCalledOnceWith(1);
    expect(component.categoriesBusy()).toBeFalse();
  });

  it('sequences a category swap, blocks concurrent writes, and reloads truth on partial failure', async () => {
    const neighbour: Category = { id: 2, nameEn: 'Web', nameAr: 'ويب', order: 12, isActive: true, projectCount: 0 };
    categories.categories.set([category, neighbour]);
    const first = new Subject<void>();
    const authoritative = [{ ...category, order: 12 }, neighbour];
    categories.updateCategory.and.returnValues(first, throwError(() => new HttpErrorResponse({ status: 409, error: { message: 'Swap failed' } })));
    categories.loadCategories.and.returnValue(of(authoritative).pipe(tap(list => categories.categories.set(list))));
    component.selectTab('categories');
    component.moveCategory(category, 1);
    expect(categories.updateCategory).toHaveBeenCalledOnceWith(1, { order: 12 });
    component.moveCategory(category, 1);
    component.toggleCategory(neighbour);
    await render();
    const move = fixture.debugElement.query(By.css('[title="Move down"]')).nativeElement as HTMLButtonElement;
    expect(move.disabled).toBeTrue();
    expect(categories.updateCategory.calls.count()).toBe(1);
    first.next();
    expect(categories.updateCategory.calls.argsFor(1)).toEqual([2, { order: 7 }]);
    expect(categories.loadCategories).toHaveBeenCalledTimes(1);
    expect(categories.categories()).toEqual(authoritative);
    expect(component.categoriesBusy()).toBeFalse();
    expect(component.categoryFormError).toBe('Swap failed');
    expect(toast.show).toHaveBeenCalledWith('Swap failed', 'error');
    expect(skills.loadSkills).not.toHaveBeenCalled();
    expect(experiences.loadExperience).not.toHaveBeenCalled();
  });

  it('shows a load error, not empty data, when authoritative reorder reload also fails', async () => {
    categories.categories.set([category, { ...category, id: 2, order: 12 }]);
    categories.updateCategory.and.returnValue(throwError(() => new Error('Reorder failed')));
    categories.loadCategories.and.callFake(() => {
      categories.error.set('Reload failed');
      return throwError(() => new Error('Reload failed'));
    });
    component.selectTab('categories');
    component.moveCategory(category, 1);
    await render();
    expect(fixture.debugElement.query(By.css('app-error-state'))).toBeTruthy();
    expect(fixture.debugElement.query(By.css('app-empty-state'))).toBeNull();
    expect(component.categoriesBusy()).toBeFalse();
  });

  it('blocks duplicate skill submissions and reports typed domain errors inline and by toast', async () => {
    const pending = new Subject<Skill>();
    skills.createSkill.and.returnValue(pending);
    component.newSkillName = 'Angular'; component.newSkillNameAr = 'أنجولار';
    component.newSkillCategory = 'Frontend'; component.newSkillCategoryAr = 'واجهات';
    await render();
    component.addSkill(form());
    component.addSkill(form());
    expect(skills.createSkill).toHaveBeenCalledTimes(1);
    expect(component.skillsBusy()).toBeTrue();
    pending.error(new HttpErrorResponse({ status: 400, error: { errors: { NameAr: ['Arabic name rejected.'] } } }));
    expect(component.skillsBusy()).toBeFalse();
    expect(component.skillFormError).toBe('Arabic name rejected.');
    expect(toast.show).toHaveBeenCalledWith('Arabic name rejected.', 'error');
  });

  it('clamps group and experience pages when totals shrink, including to zero', () => {
    skills.skills.set(Array.from({ length: 9 }, (_, index) => ({ ...skill, id: index + 1, categoryEn: `Group ${index}` })));
    experiences.experience.set(Array.from({ length: 13 }, (_, index) => ({ ...experience, id: index + 1 })));
    component.skillsPage.set(3); component.experiencePage.set(3);
    expect(component.pagedSkillGroups().length).toBe(1);
    expect(component.pagedExperience().length).toBe(1);
    skills.skills.set(skills.skills().slice(0, 4));
    experiences.experience.set(experiences.experience().slice(0, 6));
    expect(component.skillsPage()).toBe(1);
    expect(component.experiencePage()).toBe(1);
    expect(component.pagedSkillGroups().length).toBe(4);
    expect(component.pagedExperience().length).toBe(6);
    skills.skills.set([]); experiences.experience.set([]);
    expect(component.skillsPage()).toBe(1);
    expect(component.experiencePage()).toBe(1);
  });

  it('cancels component mutation subscriptions and releases busy state on destruction', () => {
    const pending = new Subject<void>();
    skills.updateSkill.and.returnValue(pending);
    component.toggleSkill(skill);
    expect(pending.observed).toBeTrue();
    fixture.destroy();
    expect(pending.observed).toBeFalse();
    expect(component.skillsBusy()).toBeFalse();
  });
});
