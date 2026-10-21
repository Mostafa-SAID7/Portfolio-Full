import { Component, ElementRef, OnDestroy, OnInit, ViewChild, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidatorFn, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { EMPTY, Observable, Subject, Subscription, catchError, finalize, from, switchMap, takeUntil, tap } from 'rxjs';
import { ProjectService } from '../../../core/services/project.service';
import { CategoryService } from '../../../core/services/category.service';
import { SkillService } from '../../../core/services/skill.service';
import { TagsInputComponent } from '../../../shared/tags-input.component';
import { Project, ProjectMetric, ProjectPayload } from '../../../core/models/project.model';
import { isApiHostedImage, resolvePortfolioImage } from '../../../core/portfolio-origin';
import { SelectFieldComponent } from '../../../shared/select-field.component';
import { optimizeProjectImage } from '../../../core/project-image-optimizer';

const MAX_COVER_BYTES = 10 * 1024 * 1024;
const COVER_MIME_TYPES = new Set(['image/jpeg', 'image/png', 'image/gif', 'image/webp', 'image/avif']);
const splitList = (value: string): string[] => value.split(',').map(item => item.trim()).filter(Boolean);
const requiredText: ValidatorFn = control =>
  typeof control.value === 'string' && control.value.trim() ? null : { required: true };
const listLimit = (maximum: number): ValidatorFn => control =>
  splitList(control.value as string).length <= maximum ? null : { listLimit: { maximum } };
const httpUrl: ValidatorFn = control => {
  const value = (control.value as string).trim();
  if (!value) return null;
  try {
    const url = new URL(value);
    return /^https?:\/\//i.test(value) && ['http:', 'https:'].includes(url.protocol) && !!url.hostname
      ? null : { httpUrl: true };
  } catch {
    return { httpUrl: true };
  }
};

type ServerErrors = Record<string, string[]>;
type FormControlName = keyof ProjectFormComponent['projectForm']['controls'];
const SERVER_CONTROL_NAMES: Record<string, FormControlName> = {
  titleen: 'title', titlear: 'titleAr', descriptionen: 'description', descriptionar: 'descriptionAr',
  categoryen: 'category', categoryar: 'categoryAr', clienten: 'client', clientar: 'clientAr',
  typeen: 'type', typear: 'typeAr', databaseen: 'database', databasear: 'databaseAr',
  statusen: 'status', statusar: 'statusAr', badgesen: 'badges', badgesar: 'badgesAr',
  technologies: 'technologies', liveurl: 'liveUrl', githuburl: 'githubUrl', imageurl: 'imageUrl',
  order: 'order', featured: 'featured', isactive: 'isActive'
};

@Component({
  selector: 'app-project-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, SelectFieldComponent, TagsInputComponent],
  templateUrl: './project-form.component.html'
})
export class ProjectFormComponent implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly projectService = inject(ProjectService);
  private readonly categoryService = inject(CategoryService);
  private readonly skillService = inject(SkillService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly stop$ = new Subject<void>();
  private readonly contextChanged$ = new Subject<void>();
  private stopped = false;
  private uploadSubscription?: Subscription;
  private uploadVersion = 0;
  private projectId: number | null = null;
  private loadedGradient: string | null = null;
  private loadedMetricsEn: ProjectMetric[] = [];
  private loadedMetricsAr: ProjectMetric[] = [];
  private loadedImageUrl = '';
  private readonly editMode = signal(false);

  readonly fetching = signal(false);
  readonly saving = signal(false);
  readonly ready = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly serverErrors = signal<ServerErrors>({});
  readonly categoryError = signal<string | null>(null);
  readonly skillError = signal<string | null>(null);
  readonly coverBroken = signal(false);
  readonly uploading = signal(false);
  readonly optimizingCover = signal(false);
  readonly uploadError = signal<string | null>(null);
  readonly dragActive = signal(false);
  readonly slug = signal('');
  readonly inputBlocked = computed(() => !this.ready() || this.fetching() || this.saving());
  readonly categoryOptions = computed(() => this.categoryService.names());
  readonly categoryOptionsAr = computed(() => this.categoryService.categories()
    .filter(category => category.isActive).map(category => category.nameAr));
  readonly technologySuggestions = computed<string[]>(() => {
    const fromSkills = this.skillService.skills().map(skill => skill.NameEn || skill.NameAr);
    const fromProjects = this.projectService.projects().flatMap(project => project.technologies ?? []);
    return [...new Set([...fromSkills, ...fromProjects]
      .filter((name): name is string => typeof name === 'string')
      .map(name => name.trim()).filter(Boolean))].sort((a, b) => a.localeCompare(b));
  });
  readonly typeOptions = ['Team', 'Freelance', 'Self'];
  readonly statusOptions = ['In Progress', 'Completed', 'Deployed'];

  @ViewChild('coverInput') coverInput?: ElementRef<HTMLInputElement>;

  readonly projectForm = this.fb.nonNullable.group({
    title: ['', [requiredText, Validators.maxLength(100)]],
    titleAr: ['', [requiredText, Validators.maxLength(100)]],
    description: ['', [requiredText, Validators.maxLength(500)]],
    descriptionAr: ['', [requiredText, Validators.maxLength(500)]],
    technologies: ['', listLimit(20)],
    liveUrl: ['', httpUrl],
    githubUrl: ['', httpUrl],
    imageUrl: ['', [Validators.maxLength(500), (control: AbstractControl<string>) => {
      const value = control.value.trim();
      if (!value || !httpUrl(control)) return null;
      // Only API uploads and the unchanged legacy cover may be relative paths.
      // Protocol-relative URLs, schemes and backslashes cannot cross this boundary.
      const local = !/^[a-z][a-z\d+.-]*:|^\/\/|[\\\u0000-\u001f]/i.test(value);
      return local && (value === this.loadedImageUrl || value.startsWith('/api/files/'))
        ? null : { httpUrl: true };
    }]],
    order: [0, [Validators.required, Validators.min(0), (control: AbstractControl<number>) =>
      Number.isInteger(control.value) ? null : { integer: true }]],
    featured: [false],
    isActive: [true],
    category: ['', Validators.maxLength(100)],
    categoryAr: ['', Validators.maxLength(100)],
    client: ['', Validators.maxLength(100)],
    clientAr: ['', Validators.maxLength(100)],
    type: ['', Validators.maxLength(50)],
    typeAr: ['', Validators.maxLength(50)],
    database: ['', Validators.maxLength(100)],
    databaseAr: ['', Validators.maxLength(100)],
    status: ['', Validators.maxLength(50)],
    statusAr: ['', Validators.maxLength(50)],
    badges: ['', listLimit(5)],
    badgesAr: ['', listLimit(5)]
  }, { validators: () => {
    if (this.loadedMetricsEn.length > 6 || this.loadedMetricsAr.length > 6) {
      return { presentation: 'The stored project exceeds the limit of 6 metrics per language. Correct it before saving.' };
    }
    return (this.loadedGradient?.length ?? 0) > 200
      ? { presentation: 'The stored gradient exceeds 200 characters. Correct it before saving.' } : null;
  }});

  constructor() {
    this.projectForm.disable({ emitEvent: false });
  }

  get isEditMode(): boolean { return this.editMode(); }

  ngOnInit(): void {
    this.route.paramMap.pipe(
      switchMap(params => {
        this.ready.set(false);
        this.contextChanged$.next();
        this.invalidateUpload();
        this.projectForm.disable({ emitEvent: false });
        this.projectForm.reset();
        this.projectId = null;
        this.slug.set('');
        this.loadedGradient = null;
        this.loadedMetricsEn = [];
        this.loadedMetricsAr = [];
        this.loadedImageUrl = '';
        this.coverBroken.set(false);
        this.errorMessage.set(null);
        this.serverErrors.set({});
        this.uploadError.set(null);
        const idParam = params.get('id');
        const editing = idParam !== null || this.route.snapshot.url.some(segment => segment.path === 'edit');
        this.editMode.set(editing);
        if (!editing) {
          this.ready.set(true);
          this.projectForm.enable({ emitEvent: false });
          return EMPTY;
        }
        if (!idParam || !/^[1-9]\d*$/.test(idParam) || !Number.isSafeInteger(Number(idParam))) {
          this.errorMessage.set('Invalid project ID. Use a positive integer project ID.');
          return EMPTY;
        }
        this.projectId = Number(idParam);
        this.fetching.set(true);
        // Always fetch the full replacement DTO, never treat a cached list entry as an edit load.
        return this.projectService.getProject(this.projectId).pipe(
          tap(project => {
            this.populateForm(project);
            this.ready.set(true);
            this.projectForm.enable({ emitEvent: false });
          }),
          catchError((error: unknown) => {
            this.showFailure(error, 'Failed to load the project.');
            return EMPTY;
          }),
          finalize(() => { if (!this.stopped) this.fetching.set(false); })
        );
      }),
      takeUntil(this.stop$)
    ).subscribe();

    if (this.categoryService.categories().length === 0) {
      this.categoryService.loadCategories().pipe(takeUntil(this.stop$)).subscribe({
        error: (error: unknown) => this.categoryError.set(projectFailure(error, 'Failed to load categories. Custom typing is still available.').message)
      });
    }
    if (this.skillService.skills().length === 0) {
      this.skillService.loadSkills().pipe(takeUntil(this.stop$)).subscribe({
        error: (error: unknown) => this.skillError.set(projectFailure(error, 'Failed to load skill suggestions. Technologies can still be entered manually.').message)
      });
    }
  }

  invalid(control: FormControlName): boolean {
    const field = this.projectForm.controls[control];
    return field.invalid && field.touched;
  }

  fieldError(control: FormControlName): string | null {
    const field = this.projectForm.controls[control];
    if (!this.invalid(control)) return null;
    const errors = field.errors;
    if (errors?.['server']) return (errors['server'] as string[]).join(' ');
    if (errors?.['required']) return 'This field is required.';
    if (errors?.['maxlength']) return `Use at most ${errors['maxlength'].requiredLength} characters.`;
    if (errors?.['listLimit']) return `Use at most ${errors['listLimit'].maximum} entries.`;
    if (errors?.['httpUrl']) return 'Enter a valid HTTP(S) URL.';
    return 'Enter a non-negative whole number.';
  }

  coverPreview(): string | null {
    const control = this.projectForm.controls.imageUrl;
    const value = control.value;
    if (!value || (control.enabled && control.invalid)) return null;
    // Disabled controls still need validation before resolving untrusted stored URLs.
    if (/^[a-z][a-z\d+.-]*:/i.test(value) && !/^https?:\/\//i.test(value)) return null;
    if (/^\/\/|[\\\u0000-\u001f]/.test(value)) return null;
    return resolvePortfolioImage(value);
  }

  onPreviewError(event: Event): void {
    if (this.stopped) return;
    (event.currentTarget as HTMLImageElement).style.display = 'none';
    if (isApiHostedImage(this.projectForm.controls.imageUrl.value)) this.coverBroken.set(true);
  }

  chooseCover(): void {
    if (!this.inputBlocked() && !this.uploading() && !this.stopped) this.coverInput?.nativeElement.click();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (file) this.uploadCover(file);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragActive.set(false);
    const file = event.dataTransfer?.files?.[0];
    if (file) this.uploadCover(file);
  }

  clearCover(): void {
    if (this.inputBlocked() || this.stopped) return;
    this.invalidateUpload();
    this.projectForm.patchValue({ imageUrl: '' });
    this.coverBroken.set(false);
    this.uploadError.set(null);
  }

  private invalidateUpload(): void {
    this.uploadVersion++;
    this.uploadSubscription?.unsubscribe();
    this.uploadSubscription = undefined;
    if (!this.stopped) {
      this.uploading.set(false);
      if (this.ready() && !this.saving()) this.projectForm.controls.imageUrl.enable({ emitEvent: false });
    }
  }

  private uploadCover(file: File): void {
    if (this.inputBlocked() || this.uploading() || this.stopped) return;
    this.uploadError.set(null);
    if (!COVER_MIME_TYPES.has(file.type)) {
      this.uploadError.set('Choose a JPEG, PNG, GIF, WebP or AVIF image.');
      return;
    }
    if (file.size > MAX_COVER_BYTES) {
      this.uploadError.set('That image is larger than 10 MiB — pick a smaller one.');
      return;
    }
    const version = ++this.uploadVersion;
    this.uploading.set(true);
    this.projectForm.controls.imageUrl.disable({ emitEvent: false });
    const projectTitle = this.projectForm.controls.title.value.trim()
      || this.projectForm.controls.titleAr.value.trim()
      || file.name.replace(/\.[^.]+$/, '');
    this.optimizingCover.set(true);
    this.uploadSubscription = from(optimizeProjectImage(file)).pipe(
      switchMap(optimizedFile => {
        this.optimizingCover.set(false);
        return this.projectService.uploadCover(optimizedFile, projectTitle);
      }),
      takeUntil(this.contextChanged$),
      takeUntil(this.stop$),
      finalize(() => {
        if (!this.stopped && version === this.uploadVersion) {
          this.uploading.set(false);
          this.optimizingCover.set(false);
          this.projectForm.controls.imageUrl.enable({ emitEvent: false });
        }
      })
    ).subscribe({
      next: uploaded => {
        if (this.stopped || version !== this.uploadVersion) return;
        this.projectForm.patchValue({ imageUrl: uploaded.url });
        this.coverBroken.set(false);
      },
      error: (error: unknown) => {
        if (!this.stopped && version === this.uploadVersion) {
          this.uploadError.set(error instanceof Error
            ? error.message
            : projectFailure(error, 'Upload failed — please try again.').message);
        }
      }
    });
  }

  onSubmit(): void {
    if (this.stopped || this.inputBlocked() || this.uploading()) return;
    if (this.projectForm.invalid) {
      this.projectForm.markAllAsTouched();
      return;
    }
    const value = this.projectForm.getRawValue();
    const optional = (text: string): string | null => text.trim() || null;
    const payload: ProjectPayload = {
      titleEn: value.title.trim(), titleAr: value.titleAr.trim(),
      descriptionEn: value.description.trim(), descriptionAr: value.descriptionAr.trim(),
      technologies: splitList(value.technologies),
      liveUrl: optional(value.liveUrl), githubUrl: optional(value.githubUrl),
      // Keep server-owned and legacy cover paths unchanged, not preview-resolved.
      imageUrl: optional(value.imageUrl), order: value.order,
      featured: value.featured, isActive: value.isActive,
      categoryEn: optional(value.category), categoryAr: optional(value.categoryAr),
      clientEn: optional(value.client), clientAr: optional(value.clientAr),
      typeEn: optional(value.type), typeAr: optional(value.typeAr),
      databaseEn: optional(value.database), databaseAr: optional(value.databaseAr),
      statusEn: optional(value.status), statusAr: optional(value.statusAr),
      badgesEn: splitList(value.badges), badgesAr: splitList(value.badgesAr),
      gradient: this.loadedGradient,
      metricsEn: this.loadedMetricsEn.map(metric => ({ ...metric })),
      metricsAr: this.loadedMetricsAr.map(metric => ({ ...metric }))
    };
    this.saving.set(true);
    this.errorMessage.set(null);
    this.serverErrors.set({});
    this.projectForm.disable({ emitEvent: false });
    const request: Observable<unknown> = this.isEditMode && this.projectId !== null
      ? this.projectService.updateProject(this.projectId, payload)
      : this.projectService.createProject(payload);
    request.pipe(
      takeUntil(this.contextChanged$),
      takeUntil(this.stop$),
      finalize(() => {
        if (!this.stopped) {
          this.saving.set(false);
          if (this.ready() && this.projectForm.disabled) this.projectForm.enable({ emitEvent: false });
        }
      })
    ).subscribe({
      next: () => {
        if (!this.stopped) {
          this.ready.set(false);
          void this.router.navigate(['/projects']);
        }
      },
      error: (error: unknown) => {
        if (this.stopped) return;
        // Re-enable before mapping server errors: enabling re-runs validators.
        this.projectForm.enable({ emitEvent: false });
        this.showFailure(error, 'Failed to save the project. Please try again.');
      }
    });
  }

  cancel(): void {
    this.stopRequests();
    void this.router.navigate(['/projects']);
  }

  ngOnDestroy(): void { this.stopRequests(); }

  private stopRequests(): void {
    if (this.stopped) return;
    this.stopped = true;
    this.stop$.next();
    this.stop$.complete();
    this.contextChanged$.complete();
    this.invalidateUpload();
  }

  private showFailure(error: unknown, fallback: string): void {
    const failure = projectFailure(error, fallback);
    this.errorMessage.set(failure.message);
    this.serverErrors.set(failure.errors);
    for (const [key, messages] of Object.entries(failure.errors)) {
      const name = SERVER_CONTROL_NAMES[key.replace(/^\$\.|^project\./i, '').toLowerCase()];
      if (name) {
        const control = this.projectForm.controls[name];
        control.setErrors({ ...control.errors, server: messages });
        control.markAsTouched();
      }
    }
  }

  private populateForm(project: Project): void {
    this.loadedGradient = project.gradient ?? null;
    this.loadedMetricsEn = (project.metricsEn ?? []).map(metric => ({ ...metric }));
    this.loadedMetricsAr = (project.metricsAr ?? []).map(metric => ({ ...metric }));
    this.loadedImageUrl = project.imageUrl ?? '';
    this.slug.set(project.slug);
    this.projectForm.patchValue({
      title: project.titleEn, titleAr: project.titleAr,
      description: project.descriptionEn, descriptionAr: project.descriptionAr,
      technologies: (project.technologies ?? []).join(', '),
      liveUrl: project.liveUrl ?? '', githubUrl: project.githubUrl ?? '', imageUrl: project.imageUrl ?? '',
      order: project.order, featured: project.featured, isActive: project.isActive,
      category: project.categoryEn ?? '', categoryAr: project.categoryAr ?? '',
      client: project.clientEn ?? '', clientAr: project.clientAr ?? '',
      type: project.typeEn ?? '', typeAr: project.typeAr ?? '',
      database: project.databaseEn ?? '', databaseAr: project.databaseAr ?? '',
      status: project.statusEn ?? '', statusAr: project.statusAr ?? '',
      badges: (project.badgesEn ?? []).join(', '), badgesAr: (project.badgesAr ?? []).join(', ')
    });
  }
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

/** Accept the interceptor's {message, errors}, ProblemDetails and FluentValidation details. */
function projectFailure(error: unknown, fallback: string): { message: string; errors: ServerErrors } {
  const body: unknown = error instanceof HttpErrorResponse ? error.error : error;
  const errors: ServerErrors = {};
  const details = isRecord(body) ? body['errors'] : body;
  if (isRecord(details)) {
    for (const [field, messages] of Object.entries(details)) {
      const list = Array.isArray(messages) ? messages : [messages];
      const text = list.filter((message): message is string => typeof message === 'string');
      if (text.length) errors[field] = text;
    }
  } else if (Array.isArray(details)) {
    for (const detail of details) {
      if (isRecord(detail) && typeof detail['errorMessage'] === 'string') {
        const field = typeof detail['propertyName'] === 'string' ? detail['propertyName'] : 'Project';
        (errors[field] ??= []).push(detail['errorMessage']);
      }
    }
  }
  const message = isRecord(body) ? body['message'] ?? body['detail'] ?? body['title'] : body;
  return { message: typeof message === 'string' && message.trim() ? message : fallback, errors };
}
