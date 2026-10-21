import { HttpErrorResponse } from '@angular/common/http';
import { signal, WritableSignal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, ParamMap, Router, UrlSegment, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject, Subject, of, throwError } from 'rxjs';
import { Category } from '../../../core/models/category.model';
import { Project, ProjectPayload } from '../../../core/models/project.model';
import { Skill } from '../../../core/models/skill.model';
import { UploadedImage } from '../../../core/portfolio-origin';
import { CategoryService } from '../../../core/services/category.service';
import { ProjectService } from '../../../core/services/project.service';
import { SkillService } from '../../../core/services/skill.service';
import { ProjectFormComponent } from './project-form.component';

function projectFixture(id = 1): Project {
  return {
    id, slug: `stable-project-${id}`,
    titleEn: 'Inventory', titleAr: 'المخزون',
    descriptionEn: 'English description', descriptionAr: 'وصف عربي مستقل',
    imageUrl: '/projects/react/E-Inventory.png', technologies: ['Angular', '.NET'],
    liveUrl: 'https://example.com/live', githubUrl: 'https://github.com/example/project',
    order: 17, featured: true, isActive: false,
    categoryEn: 'Dashboard', categoryAr: 'لوحة التحكم',
    clientEn: 'Client', clientAr: 'العميل', typeEn: 'Team', typeAr: 'فريق',
    databaseEn: 'SQL', databaseAr: 'قاعدة البيانات', statusEn: 'Completed', statusAr: 'مكتمل',
    badgesEn: ['Deployed', 'Done'], badgesAr: ['منشور', 'منجز'],
    metricsEn: [{ labelEn: 'Users', labelAr: 'مستخدمون', value: '100' }],
    metricsAr: [{ labelEn: 'Arabic users', labelAr: 'المستخدمون', value: '١٠٠' }],
    gradient: 'from-blue-500 to-purple-500', createdAt: '2026-10-01T00:00:00Z', updatedAt: null
  };
}

const uploadedImage: UploadedImage = { url: '/api/files/new-cover.png', fileName: 'new-cover.png', size: 1 };

describe('ProjectFormComponent', () => {
  let fixture: ComponentFixture<ProjectFormComponent>;
  let component: ProjectFormComponent;
  let params: BehaviorSubject<ParamMap>;
  let snapshot: { url: UrlSegment[] };
  let projects: jasmine.SpyObj<ProjectService>;
  let projectState: WritableSignal<Project[]>;
  let categories: jasmine.SpyObj<CategoryService>;
  let skills: jasmine.SpyObj<SkillService>;
  let navigate: jasmine.Spy;

  beforeEach(async () => {
    params = new BehaviorSubject(convertToParamMap({}));
    snapshot = { url: [] };
    projects = jasmine.createSpyObj<ProjectService>('ProjectService',
      ['getProject', 'createProject', 'updateProject', 'uploadCover'], { projects: signal<Project[]>([]) });
    projectState = signal<Project[]>([]);
    Object.defineProperty(projects, 'projects', { value: projectState });
    projects.getProject.and.returnValue(of(projectFixture()));
    projects.createProject.and.returnValue(of(projectFixture()));
    projects.updateProject.and.returnValue(of(undefined));
    projects.uploadCover.and.returnValue(of(uploadedImage));
    categories = jasmine.createSpyObj<CategoryService>('CategoryService', ['loadCategories'],
      { categories: signal<Category[]>([]), names: signal<string[]>([]) });
    categories.loadCategories.and.returnValue(of([]));
    skills = jasmine.createSpyObj<SkillService>('SkillService', ['loadSkills'], { skills: signal<Skill[]>([]) });
    skills.loadSkills.and.returnValue(of([]));
    await TestBed.configureTestingModule({
      imports: [ProjectFormComponent],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { paramMap: params.asObservable(), snapshot } },
        { provide: ProjectService, useValue: projects },
        { provide: CategoryService, useValue: categories },
        { provide: SkillService, useValue: skills }
      ]
    }).compileComponents();
    navigate = spyOn(TestBed.inject(Router), 'navigate').and.resolveTo(true);
    fixture = TestBed.createComponent(ProjectFormComponent);
    component = fixture.componentInstance;
  });

  function startCreate(): void { fixture.detectChanges(); }
  function startEdit(id = '1'): void {
    snapshot.url = [new UrlSegment(id, {}), new UrlSegment('edit', {})];
    params.next(convertToParamMap({ id }));
    fixture.detectChanges();
  }
  function fillRequired(): void {
    component.projectForm.patchValue({
      title: 'English title', titleAr: 'عنوان عربي',
      description: 'English description', descriptionAr: 'وصف عربي'
    });
  }
  function selectFile(file: File): void {
    // Test the public file-selection boundary without an HTTP client, auth or tokens.
    component.onFileSelected({ target: { files: [file], value: file.name } } as unknown as Event);
  }
  function file(type = 'image/png', size = 1): File {
    return new File([new Uint8Array(size)], 'cover', { type });
  }
  function savedPayload(): ProjectPayload { return projects.updateProject.calls.mostRecent().args[1]; }

  it('round-trips independent Arabic fields, metrics, gradient, stored cover, order and flags', () => {
    const project = projectFixture();
    projects.getProject.and.returnValue(of(project));
    startEdit();
    component.projectForm.controls.title.setValue('Renamed English title');
    component.onSubmit();
    expect(projects.updateProject).toHaveBeenCalledWith(1, jasmine.objectContaining({
      titleEn: 'Renamed English title', titleAr: project.titleAr,
      descriptionEn: project.descriptionEn, descriptionAr: project.descriptionAr,
      categoryEn: project.categoryEn, categoryAr: project.categoryAr,
      clientEn: project.clientEn, clientAr: project.clientAr,
      typeEn: project.typeEn, typeAr: project.typeAr,
      databaseEn: project.databaseEn, databaseAr: project.databaseAr,
      statusEn: project.statusEn, statusAr: project.statusAr,
      badgesEn: project.badgesEn, badgesAr: project.badgesAr,
      metricsEn: project.metricsEn, metricsAr: project.metricsAr, gradient: project.gradient,
      imageUrl: project.imageUrl, order: 17, featured: true, isActive: false
    }));
    expect(savedPayload().metricsEn).not.toBe(savedPayload().metricsAr);
    expect(savedPayload().metricsEn).not.toBe(project.metricsEn);
    expect(component.slug()).toBe(project.slug);
    expect(savedPayload()).not.toEqual(jasmine.objectContaining({ slug: project.slug }));
  });

  it('does not invent optional Arabic translations from English on create', () => {
    startCreate();
    fillRequired();
    component.projectForm.patchValue({ category: 'Custom category', badges: 'Done' });
    component.onSubmit();
    expect(projects.createProject).toHaveBeenCalledWith(jasmine.objectContaining({
      titleAr: 'عنوان عربي', categoryEn: 'Custom category', categoryAr: null, badgesEn: ['Done'], badgesAr: [],
      metricsEn: [], metricsAr: [], gradient: null
    }));
  });

  it('requires both titles and descriptions and rejects whitespace-only translations', () => {
    startCreate();
    fillRequired();
    for (const name of ['title', 'titleAr', 'description', 'descriptionAr'] as const) {
      const control = component.projectForm.controls[name];
      const original = control.value;
      control.setValue('   ');
      component.onSubmit();
      expect(control.hasError('required')).withContext(name).toBeTrue();
      expect(control.touched).toBeTrue();
      control.setValue(original);
    }
    expect(projects.createProject).not.toHaveBeenCalled();
  });

  it('applies bilingual required and optional backend length limits', () => {
    startCreate();
    fillRequired();
    for (const name of ['title', 'titleAr', 'category', 'categoryAr', 'client', 'clientAr', 'database', 'databaseAr'] as const) {
      component.projectForm.controls[name].setValue('x'.repeat(100));
      expect(component.projectForm.controls[name].valid).withContext(name).toBeTrue();
      component.projectForm.controls[name].setValue('x'.repeat(101));
      expect(component.projectForm.controls[name].hasError('maxlength')).withContext(name).toBeTrue();
    }
    for (const name of ['description', 'descriptionAr'] as const) {
      component.projectForm.controls[name].setValue('x'.repeat(500));
      expect(component.projectForm.controls[name].valid).toBeTrue();
      component.projectForm.controls[name].setValue('x'.repeat(501));
      expect(component.projectForm.controls[name].hasError('maxlength')).toBeTrue();
    }
    for (const name of ['type', 'typeAr', 'status', 'statusAr'] as const) {
      component.projectForm.controls[name].setValue('x'.repeat(50));
      expect(component.projectForm.controls[name].valid).toBeTrue();
      component.projectForm.controls[name].setValue('x'.repeat(51));
      expect(component.projectForm.controls[name].hasError('maxlength')).toBeTrue();
    }
  });

  it('renders required Arabic inputs with RTL and uses an adjacent template', () => {
    startCreate();
    const host = fixture.nativeElement as HTMLElement;
    expect(host.querySelector('#titleAr')?.getAttribute('dir')).toBe('rtl');
    expect(host.querySelector('#descriptionAr')?.getAttribute('dir')).toBe('rtl');
    expect(host.querySelector('label[for="titleAr"]')?.textContent).toContain('Arabic');
    expect(host.textContent).not.toContain('instantly');
  });

  it('blocks editing, uploads and saves while fetching even if a cached project exists', () => {
    const pending = new Subject<Project>();
    projects.getProject.and.returnValue(pending);
    projectState.set([projectFixture()]);
    startEdit();
    fillRequired();
    component.onSubmit();
    selectFile(file());
    expect(projects.getProject).toHaveBeenCalledWith(1);
    expect(component.fetching()).toBeTrue();
    expect(component.saving()).toBeFalse();
    expect(component.projectForm.disabled).toBeTrue();
    expect((fixture.nativeElement as HTMLElement).querySelector('fieldset')?.hasAttribute('inert')).toBeTrue();
    expect(projects.updateProject).not.toHaveBeenCalled();
    expect(projects.createProject).not.toHaveBeenCalled();
    expect(projects.uploadCover).not.toHaveBeenCalled();
    pending.next(projectFixture());
    pending.complete();
    expect(component.ready()).toBeTrue();
    expect(component.fetching()).toBeFalse();
    expect(component.projectForm.enabled).toBeTrue();
  });

  it('keeps editing and save blocked after a fetch failure', () => {
    projects.getProject.and.returnValue(throwError(() => new HttpErrorResponse({ status: 404, error: { message: 'Project not found' } })));
    startEdit();
    fillRequired();
    component.onSubmit();
    expect(component.fetching()).toBeFalse();
    expect(component.ready()).toBeFalse();
    expect(component.projectForm.disabled).toBeTrue();
    expect(component.errorMessage()).toBe('Project not found');
    expect(projects.updateProject).not.toHaveBeenCalled();
    expect(projects.createProject).not.toHaveBeenCalled();
    expect(navigate).not.toHaveBeenCalled();
  });

  it('rejects non-positive, non-integer and unsafe route IDs without contacting the API', () => {
    startEdit('0');
    for (const id of ['-1', '1.2', '1e2', 'NaN', 'Infinity', '12junk', '01', ' 1', '9007199254740992']) {
      params.next(convertToParamMap({ id }));
      component.onSubmit();
      expect(component.ready()).withContext(id).toBeFalse();
      expect(component.errorMessage()).toContain('Invalid project ID');
    }
    params.next(convertToParamMap({}));
    expect(component.ready()).toBeFalse();
    expect(projects.getProject).not.toHaveBeenCalled();
    expect(projects.createProject).not.toHaveBeenCalled();
    expect(projects.updateProject).not.toHaveBeenCalled();
  });

  it('switches reused route IDs, unsubscribes stale loads and only saves the latest project', () => {
    const first = new Subject<Project>();
    const second = new Subject<Project>();
    projects.getProject.and.callFake(id => id === 1 ? first : second);
    startEdit();
    params.next(convertToParamMap({ id: '2' }));
    expect(first.observed).toBeFalse();
    first.next(projectFixture(1));
    expect(component.ready()).toBeFalse();
    expect(component.slug()).toBe('');
    const latest = projectFixture(2);
    latest.titleAr = 'المشروع الثاني';
    second.next(latest);
    second.complete();
    component.onSubmit();
    expect(projects.updateProject).toHaveBeenCalledWith(2, jasmine.objectContaining({ titleAr: 'المشروع الثاني' }));
    expect(projects.getProject.calls.allArgs()).toEqual([[1], [2]]);
  });

  it('recovers on a later valid ID after a load error', () => {
    projects.getProject.and.callFake(id => id === 1 ? throwError(() => new Error('failure')) : of(projectFixture(id)));
    startEdit();
    expect(component.ready()).toBeFalse();
    params.next(convertToParamMap({ id: '2' }));
    expect(component.ready()).toBeTrue();
    expect(component.errorMessage()).toBeNull();
    expect(component.projectForm.controls.titleAr.value).toBe('المخزون');
  });

  it('uses only API category options and explicitly displays lookup failures', () => {
    categories.loadCategories.and.returnValue(throwError(() => new HttpErrorResponse({ error: { message: 'Category lookup unavailable' } })));
    skills.loadSkills.and.returnValue(throwError(() => new HttpErrorResponse({ error: {} })));
    startCreate();
    expect(component.categoryOptions()).toEqual([]);
    expect(component.categoryOptionsAr()).toEqual([]);
    expect(component.categoryError()).toBe('Category lookup unavailable');
    expect(component.skillError()).toContain('Failed to load skill suggestions');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Category lookup unavailable');
    expect(component.projectForm.enabled).toBeTrue();
  });

  for (const mime of ['image/jpeg', 'image/png', 'image/gif', 'image/webp', 'image/avif']) {
    it(`accepts backend-supported ${mime} uploads`, () => {
      startCreate();
      const image = file(mime);
      selectFile(image);
      expect(projects.uploadCover).toHaveBeenCalledOnceWith(image);
      expect(component.projectForm.controls.imageUrl.value).toBe(uploadedImage.url);
      expect(component.uploading()).toBeFalse();
      expect(component.projectForm.controls.imageUrl.enabled).toBeTrue();
    });
  }

  it('rejects SVG and every non-allowlisted MIME rather than accepting all image types', () => {
    startCreate();
    for (const mime of ['image/svg+xml', 'image/bmp', 'image/tiff', 'image/x-icon', 'image/jpg', 'text/plain', 'application/pdf', '']) {
      selectFile(file(mime));
      expect(component.uploadError()).withContext(mime).toContain('JPEG, PNG, GIF, WebP or AVIF');
    }
    expect(projects.uploadCover).not.toHaveBeenCalled();
  });

  it('accepts exactly 10 MiB and rejects one byte over the backend size cap', () => {
    startCreate();
    selectFile(file('image/png', 10 * 1024 * 1024));
    expect(projects.uploadCover).toHaveBeenCalledTimes(1);
    selectFile(file('image/png', 10 * 1024 * 1024 + 1));
    expect(projects.uploadCover).toHaveBeenCalledTimes(1);
    expect(component.uploadError()).toContain('10 MiB');
  });

  it('validates dropped images through the same MIME boundary', () => {
    startCreate();
    component.onDrop({ preventDefault: () => undefined, dataTransfer: { files: [file('image/svg+xml')] } } as unknown as DragEvent);
    expect(projects.uploadCover).not.toHaveBeenCalled();
    expect(component.uploadError()).toBeTruthy();
  });

  it('prevents duplicate upload and save races while an upload is pending', () => {
    const pending = new Subject<UploadedImage>();
    projects.uploadCover.and.returnValue(pending);
    startCreate();
    fillRequired();
    selectFile(file());
    selectFile(file('image/jpeg'));
    component.onSubmit();
    expect(projects.uploadCover).toHaveBeenCalledTimes(1);
    expect(projects.createProject).not.toHaveBeenCalled();
    expect(component.projectForm.controls.imageUrl.disabled).toBeTrue();
    pending.next(uploadedImage);
    pending.complete();
    component.onSubmit();
    expect(projects.createProject).toHaveBeenCalledWith(jasmine.objectContaining({ imageUrl: uploadedImage.url }));
  });

  it('clears and unsubscribes a pending upload so a late response cannot resurrect the cover', () => {
    const pending = new Subject<UploadedImage>();
    projects.uploadCover.and.returnValue(pending);
    startEdit();
    selectFile(file());
    component.clearCover();
    expect(pending.observed).toBeFalse();
    pending.next(uploadedImage);
    pending.complete();
    expect(component.projectForm.controls.imageUrl.value).toBe('');
    expect(component.uploading()).toBeFalse();
    expect(component.projectForm.controls.imageUrl.enabled).toBeTrue();
    component.onSubmit();
    expect(savedPayload().imageUrl).toBeNull();
  });

  it('does not let a replaced upload response overwrite a later upload', () => {
    const first = new Subject<UploadedImage>();
    const second = new Subject<UploadedImage>();
    projects.uploadCover.and.returnValues(first, second);
    startCreate();
    selectFile(file());
    component.clearCover();
    selectFile(file('image/jpeg'));
    first.next({ ...uploadedImage, url: '/api/files/old.png' });
    expect(component.uploading()).toBeTrue();
    second.next(uploadedImage);
    second.complete();
    expect(component.projectForm.controls.imageUrl.value).toBe(uploadedImage.url);
  });

  it('retains the previous cover after an upload error and allows retry', () => {
    projects.uploadCover.and.returnValue(throwError(() => new HttpErrorResponse({ error: { message: 'Upload rejected' } })));
    startEdit();
    selectFile(file());
    expect(component.uploadError()).toBe('Upload rejected');
    expect(component.uploading()).toBeFalse();
    expect(component.projectForm.controls.imageUrl.value).toBe(projectFixture().imageUrl!);
    expect(component.projectForm.controls.imageUrl.enabled).toBeTrue();
  });

  it('prevents uploads, clear and duplicate saves while saving', () => {
    const pending = new Subject<void>();
    projects.updateProject.and.returnValue(pending);
    startEdit();
    component.onSubmit();
    component.onSubmit();
    selectFile(file());
    component.clearCover();
    expect(component.saving()).toBeTrue();
    expect(component.fetching()).toBeFalse();
    expect(projects.updateProject).toHaveBeenCalledTimes(1);
    expect(projects.uploadCover).not.toHaveBeenCalled();
    expect(component.projectForm.controls.imageUrl.value).toBe(projectFixture().imageUrl!);
    pending.next();
    pending.complete();
    expect(navigate).toHaveBeenCalledOnceWith(['/projects']);
    expect(component.saving()).toBeFalse();
  });

  it('retains and maps normalized server field errors after save finalization', () => {
    projects.updateProject.and.returnValue(throwError(() => new HttpErrorResponse({
      status: 400, error: { message: 'Validation failed', errors: { TitleAr: ['Arabic title rejected'], MetricsAr: ['Metric error'] } }
    })));
    startEdit();
    component.onSubmit();
    expect(component.saving()).toBeFalse();
    expect(component.projectForm.enabled).toBeTrue();
    expect(component.errorMessage()).toBe('Validation failed');
    expect(component.serverErrors()['MetricsAr']).toEqual(['Metric error']);
    expect(component.projectForm.controls.titleAr.hasError('server')).toBeTrue();
    expect(component.fieldError('titleAr')).toBe('Arabic title rejected');
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('MetricsAr: Metric error');
    component.projectForm.controls.titleAr.setValue('عنوان مصحح');
    expect(component.projectForm.controls.titleAr.valid).toBeTrue();
    expect(navigate).not.toHaveBeenCalled();
  });

  it('retains FluentValidation arrays without discarding the field names', () => {
    projects.createProject.and.returnValue(throwError(() => new HttpErrorResponse({ status: 400,
      error: [{ propertyName: 'DescriptionAr', errorMessage: 'Description rejected' }]
    })));
    startCreate();
    fillRequired();
    component.onSubmit();
    expect(component.serverErrors()['DescriptionAr']).toEqual(['Description rejected']);
    expect(component.fieldError('descriptionAr')).toBe('Description rejected');
  });

  for (const action of ['cancel', 'destroy'] as const) {
    it(`cancels uploads on ${action} without late form mutation or navigation`, () => {
      const pending = new Subject<UploadedImage>();
      projects.uploadCover.and.returnValue(pending);
      startEdit();
      selectFile(file());
      const stored = component.projectForm.controls.imageUrl.value;
      if (action === 'cancel') component.cancel(); else fixture.destroy();
      const navigationCount = navigate.calls.count();
      expect(pending.observed).toBeFalse();
      pending.next(uploadedImage);
      pending.complete();
      component.onSubmit();
      expect(component.projectForm.controls.imageUrl.value).toBe(stored);
      expect(navigate.calls.count()).toBe(navigationCount);
      expect(projects.updateProject).not.toHaveBeenCalled();
    });

    it(`cancels saves on ${action} without navigating on a late success`, () => {
      const pending = new Subject<void>();
      projects.updateProject.and.returnValue(pending);
      startEdit();
      component.onSubmit();
      if (action === 'cancel') component.cancel(); else fixture.destroy();
      const navigationCount = navigate.calls.count();
      expect(pending.observed).toBeFalse();
      pending.next();
      pending.complete();
      expect(navigate.calls.count()).toBe(navigationCount);
    });
  }

  it('cancels route loads and lookup subscriptions on destruction', () => {
    const pending = new Subject<Project>();
    const lookup = new Subject<Category[]>();
    projects.getProject.and.returnValue(pending);
    categories.loadCategories.and.returnValue(lookup);
    startEdit();
    fixture.destroy();
    expect(pending.observed).toBeFalse();
    expect(lookup.observed).toBeFalse();
    params.next(convertToParamMap({ id: '2' }));
    pending.next(projectFixture());
    expect(projects.getProject).toHaveBeenCalledTimes(1);
    expect(component.slug()).toBe('');
  });

  it('cancels a pending upload when the route changes', () => {
    const pending = new Subject<UploadedImage>();
    projects.uploadCover.and.returnValue(pending);
    projects.getProject.and.callFake(id => of(projectFixture(id)));
    startEdit();
    selectFile(file());
    params.next(convertToParamMap({ id: '2' }));
    expect(pending.observed).toBeFalse();
    pending.next(uploadedImage);
    expect(component.slug()).toBe('stable-project-2');
    expect(component.projectForm.controls.imageUrl.value).toBe(projectFixture(2).imageUrl!);
    expect(component.uploading()).toBeFalse();
  });

  it('cancels a pending save when the route changes without late navigation', () => {
    const pending = new Subject<void>();
    projects.updateProject.and.returnValue(pending);
    projects.getProject.and.callFake(id => of(projectFixture(id)));
    startEdit();
    component.onSubmit();
    params.next(convertToParamMap({ id: '2' }));
    expect(pending.observed).toBeFalse();
    pending.next();
    expect(navigate).not.toHaveBeenCalled();
    expect(component.slug()).toBe('stable-project-2');
    expect(component.saving()).toBeFalse();
    expect(component.projectForm.enabled).toBeTrue();
  });

  it('accepts HTTP(S) links but rejects executable, non-HTTP and protocol-relative URLs', () => {
    startCreate();
    for (const name of ['liveUrl', 'githubUrl', 'imageUrl'] as const) {
      const control = component.projectForm.controls[name];
      for (const value of ['https://example.com/path', 'http://localhost:5173/path', '']) {
        control.setValue(value);
        expect(control.valid).withContext(`${name}: ${value}`).toBeTrue();
      }
      for (const value of ['javascript:alert(1)', 'data:image/svg+xml,<svg/>', 'ftp://example.com', 'mailto:user@example.com', '//example.com/image.png', 'https://', 'not-a-url']) {
        control.setValue(value);
        expect(control.hasError('httpUrl')).withContext(`${name}: ${value}`).toBeTrue();
      }
    }
    expect(component.coverPreview()).toBeNull();
    component.projectForm.controls.imageUrl.setValue('x'.repeat(501));
    expect(component.projectForm.controls.imageUrl.hasError('maxlength')).toBeTrue();
  });

  it('preserves a legacy relative cover and accepts backend upload paths without resolving them in the payload', () => {
    startEdit();
    expect(component.projectForm.controls.imageUrl.valid).toBeTrue();
    component.projectForm.controls.imageUrl.setValue('/api/files/existing.webp');
    expect(component.projectForm.controls.imageUrl.valid).toBeTrue();
    component.onSubmit();
    expect(savedPayload().imageUrl).toBe('/api/files/existing.webp');
  });

  it('enforces technology and independent badge array counts at their backend boundaries', () => {
    startCreate();
    for (const [name, limit] of [['technologies', 20], ['badges', 5], ['badgesAr', 5]] as const) {
      const control = component.projectForm.controls[name];
      control.setValue(Array.from({ length: limit }, (_, index) => `item-${index}`).join(', '));
      expect(control.valid).toBeTrue();
      control.setValue(`${control.value}, extra`);
      expect(control.hasError('listLimit')).withContext(name).toBeTrue();
    }
  });

  it('blocks saves of over-limit or mismatched stored metrics rather than silently replacing them', () => {
    const overLimit = projectFixture();
    overLimit.metricsEn = Array.from({ length: 7 }, () => ({ labelEn: 'Metric', labelAr: 'مقياس', value: '1' }));
    overLimit.metricsAr = [...overLimit.metricsEn];
    projects.getProject.and.returnValue(of(overLimit));
    startEdit();
    component.onSubmit();
    expect(component.projectForm.errors?.['presentation']).toContain('6 metrics');
    const mismatched = projectFixture(2);
    mismatched.metricsAr = [];
    projects.getProject.and.returnValue(of(mismatched));
    params.next(convertToParamMap({ id: '2' }));
    component.onSubmit();
    expect(component.projectForm.errors?.['presentation']).toContain('same number');
    expect(projects.updateProject).not.toHaveBeenCalled();
  });

  it('blocks a stored gradient longer than the backend limit without truncating it', () => {
    const project = projectFixture();
    project.gradient = 'x'.repeat(201);
    projects.getProject.and.returnValue(of(project));
    startEdit();
    component.onSubmit();
    expect(component.projectForm.errors?.['presentation']).toContain('200 characters');
    expect(projects.updateProject).not.toHaveBeenCalled();
  });
});
