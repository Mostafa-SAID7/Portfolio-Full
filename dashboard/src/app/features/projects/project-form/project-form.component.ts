import { Component, ElementRef, OnInit, ViewChild, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable, firstValueFrom } from 'rxjs';
import { ProjectService } from '../../../core/services/project.service';
import { CategoryService } from '../../../core/services/category.service';
import { SkillService } from '../../../core/services/skill.service';
import { TagsInputComponent } from '../../../shared/tags-input.component';
import { ProjectMetric, ProjectPayload } from '../../../core/models/project.model';
import { isApiHostedImage, resolvePortfolioImage } from '../../../core/portfolio-origin';
import { SelectFieldComponent } from '../../../shared/select-field.component';

const MAX_COVER_BYTES = 10 * 1024 * 1024; // API-side cap is 10MB.

@Component({
  selector: 'app-project-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, SelectFieldComponent, TagsInputComponent],
  template: `
    <div class="mx-auto max-w-3xl">
      <div class="mb-8">
        <a routerLink="/projects" class="text-xs font-semibold text-accent-text hover:underline">← Back to projects</a>
        <p class="eyebrow mb-2 mt-3">{{ isEditMode ? 'Editing' : 'New entry' }}</p>
        <h2 class="font-display text-3xl font-semibold tracking-[-0.05em] text-ink">
          {{ isEditMode ? 'Edit project' : 'Create project' }}
        </h2>
        <p class="mt-2 text-sm text-soft">Saved directly to the Portfolio API — visible on the public site instantly.</p>
      </div>

      <form [formGroup]="projectForm" (ngSubmit)="onSubmit()" class="card space-y-6 p-6" novalidate>
        @if (errorMessage()) {
          <div role="alert" class="rounded-xl border border-danger/30 bg-danger/10 p-3 text-sm text-danger">
            {{ errorMessage() }}
          </div>
        }

        <div>
          <label for="title" class="field-label">Title <span class="text-danger">*</span></label>
          <input
            id="title"
            type="text"
            formControlName="title"
            class="input"
            placeholder="E-Inventory platform"
            [attr.aria-invalid]="invalid('title')"
          />
          @if (invalid('title')) { <p class="field-error" role="alert">Title is required (max 100 characters).</p> }
        </div>

        <div>
          <label for="description" class="field-label">Description <span class="text-danger">*</span></label>
          <textarea
            id="description"
            formControlName="description"
            rows="4"
            class="input resize-y"
            placeholder="What the project is, who it was for, the outcome..."
            [attr.aria-invalid]="invalid('description')"
          ></textarea>
          @if (invalid('description')) { <p class="field-error" role="alert">Description is required (max 500 characters).</p> }
        </div>

        <div>
          <app-tags-input
            formControlName="technologies"
            label="Technologies"
            placeholder="Angular, .NET 9, SQL Server"
            [suggestions]="technologySuggestions()"
            hint="Press Enter or a comma to add a tag - the suggestions come from your skills and the other projects."
          />
        </div>

        <div class="border-t border-line/10 pt-5">
          <p class="eyebrow mb-1">Portfolio presentation</p>
          <p class="mb-4 text-xs text-muted">
            These fields drive the category filters, client and status chips on the public portfolio.
          </p>
          <div class="grid gap-5 sm:grid-cols-2">
            <div>
              <app-select-field
                formControlName="category"
                label="Category"
                [options]="categoryOptions()"
                [allowCustom]="true"
                placeholder="Clothing"
              />
            </div>
            <div>
              <label for="client" class="field-label">Client</label>
              <input id="client" type="text" formControlName="client" class="input" placeholder="Under Armour" />
            </div>
            <div>
              <app-select-field formControlName="type" label="Type" [options]="typeOptions" />
            </div>
            <div>
              <label for="database" class="field-label">Database</label>
              <input id="database" type="text" formControlName="database" class="input" placeholder="PostgreSQL" />
            </div>
            <div>
              <app-select-field formControlName="status" label="Build status" [options]="statusOptions" />
            </div>
            <div>
              <label for="badges" class="field-label">Badges</label>
              <input id="badges" type="text" formControlName="badges" class="input" placeholder="Done, Deployed" />
              <p class="mt-2 text-xs text-muted">Separate with commas — small chips on the project card.</p>
            </div>
          </div>
        </div>

        <div class="grid gap-5 sm:grid-cols-2">
          <div>
            <label for="liveUrl" class="field-label">Live URL</label>
            <input id="liveUrl" type="url" formControlName="liveUrl" class="input" placeholder="https://..." />
          </div>
          <div>
            <label for="githubUrl" class="field-label">GitHub URL</label>
            <input id="githubUrl" type="url" formControlName="githubUrl" class="input" placeholder="https://github.com/..." />
          </div>
        </div>

        <!-- ---------------- Cover image ---------------- -->
        <div>
          <label class="field-label">Cover image</label>

          @if (coverPreview(); as preview) {
            <div class="relative overflow-hidden rounded-xl border border-line/10 bg-line/[0.06]">
              <img
                [src]="preview"
                [alt]="(projectForm.getRawValue().title || 'Project') + ' cover preview'"
                class="h-48 w-full object-cover"
                (error)="onPreviewError($event)"
              />
              <button
                type="button"
                class="icon-btn absolute right-2 top-2 bg-surface/90"
                (click)="clearCover()"
                aria-label="Remove cover image"
                title="Remove cover"
              >
                <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M6 6l12 12M18 6 6 18"/></svg>
              </button>
            </div>
            @if (coverBroken()) {
              <p class="mt-2 rounded-lg border border-danger/30 bg-danger/10 px-3 py-2 text-xs text-danger">
                The cover file is referenced by the database but missing on the server — upload it again.
                (Common cause: it was uploaded through a different deployment, or the API's
                <code>App_Data/uploads</code> folder was replaced during a redeploy.)
              </p>
            }
          }

          <!-- The upload control is ALWAYS available: a project that still points at a
               bundled cover (e.g. /projects/angular/zyro-electric.png) must be
               replaceable, otherwise the admin can never change it. -->
          <div
            class="mt-3 flex cursor-pointer flex-col items-center justify-center gap-2 rounded-xl border border-dashed border-line/20 bg-line/[0.03] px-6 py-8 text-center transition"
            [class.border-accent/50]="dragActive()"
            role="button"
            tabindex="0"
            aria-label="Upload cover image"
            (click)="coverInput.click()"
            (keydown.enter)="coverInput.click()"
            (keydown.space)="$event.preventDefault(); coverInput.click()"
            (dragover)="$event.preventDefault(); dragActive.set(true)"
            (dragleave)="dragActive.set(false)"
            (drop)="onDrop($event)"
          >
            <svg class="h-8 w-8 text-muted" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M4 16v1a3 3 0 0 0 3 3h10a3 3 0 0 0 3-3v-1m-5-4-4-4m0 0-4 4m4-4v12"/></svg>
            <span class="text-sm font-semibold text-ink">
              {{ coverPreview() ? 'Drop a new image here to replace the current cover' : 'Drop an image here, or click to browse' }}
            </span>
            <span class="text-xs text-muted">JPG, PNG, WebP, GIF or AVIF · up to 10MB</span>
          </div>
          <input
            #coverInput
            type="file"
            accept="image/jpeg,image/png,image/webp,image/gif,image/avif"
            class="hidden"
            (change)="onFileSelected($event)"
          />

          @if (uploading()) {
            <p class="mt-2 flex items-center gap-2 text-xs text-muted">
              <span class="h-3.5 w-3.5 animate-spin rounded-full border-2 border-accent/30 border-t-accent"></span>
              Uploading to the Portfolio API…
            </p>
          }
          @if (uploadError()) {
            <p class="mt-2 text-xs text-danger" role="alert">{{ uploadError() }}</p>
          }
          @if (projectForm.getRawValue().imageUrl; as stored) {
            <p class="mt-2 truncate text-xs text-muted">Stored as: {{ stored }}</p>
          }

          <details class="mt-3">
            <summary class="cursor-pointer text-xs font-semibold text-accent-text">Or use an external image URL instead</summary>
            <input id="imageUrl" type="url" formControlName="imageUrl" class="input mt-2" placeholder="https://..." />
          </details>
        </div>

        <div class="grid gap-5 sm:grid-cols-2">
          <div>
            <label for="order" class="field-label">Display order</label>
            <input id="order" type="number" formControlName="order" class="input" min="0" />
            <p class="mt-2 text-xs text-muted">Lower numbers appear first on the portfolio.</p>
          </div>
          <div class="flex flex-col justify-center gap-3 pt-1">
            <label class="flex cursor-pointer items-center gap-3 rounded-xl border border-line/10 bg-line/[0.03] p-4">
              <input type="checkbox" formControlName="featured" class="h-4 w-4 accent-[rgb(var(--accent))]" />
              <span>
                <span class="block text-sm font-semibold text-ink">Featured</span>
                <span class="block text-xs text-muted">Shown in the highlighted section.</span>
              </span>
            </label>
            <label class="flex cursor-pointer items-center gap-3 rounded-xl border border-line/10 bg-line/[0.03] p-4">
              <input type="checkbox" formControlName="isActive" class="h-4 w-4 accent-[rgb(var(--accent))]" />
              <span>
                <span class="block text-sm font-semibold text-ink">Visible on site</span>
                <span class="block text-xs text-muted">Uncheck to keep it as a hidden draft.</span>
              </span>
            </label>
          </div>
        </div>

        <div class="flex justify-end gap-3 border-t border-line/10 pt-5">
          <button type="button" (click)="cancel()" class="btn-ghost">Cancel</button>
          <button type="submit" [disabled]="projectForm.invalid || isLoading()" class="btn-primary" [attr.aria-busy]="isLoading()">
            @if (isLoading()) {
              <span class="h-4 w-4 animate-spin rounded-full border-2 border-on-accent/30 border-t-on-accent"></span>
              Saving…
            } @else {
              {{ isEditMode ? 'Update project' : 'Create project' }}
            }
          </button>
        </div>
      </form>
    </div>
  `
})
export class ProjectFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly projectService = inject(ProjectService);
  private readonly categoryService = inject(CategoryService);
  private readonly skillService = inject(SkillService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly isLoading = signal(false);
  /** True when the stored cover file could not be loaded (missing on the server). */
  readonly coverBroken = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly isEditMode = this.route.snapshot.url.some(segment => segment.path === 'edit');

  /** Cover upload state. */
  readonly uploading = signal(false);
  readonly uploadError = signal<string | null>(null);
  readonly dragActive = signal(false);

  @ViewChild('coverInput') coverInput?: ElementRef<HTMLInputElement>;

  private projectId: number | null = null;

  /** Presentation data kept out of the form UI but round-tripped on save. */
  private loadedGradient: string | null = null;
  private loadedMetrics: ProjectMetric[] = [];

  /** Fallback list when the API has no categories yet (first run). */
  private static readonly FALLBACK_CATEGORIES = [
    'Clothing',
    'Beauty',
    'Jewelry',
    'Furniture',
    'Dashboard',
    'Web3',
    'Sports',
    'Tech Accessories',
    'Backend',
    'Other'
  ];

  /** Category names offered in the dropdown - managed on the Content page. */
  readonly categoryOptions = computed<string[]>(() => {
    const fromApi = this.categoryService.names();
    return fromApi.length ? fromApi : [...ProjectFormComponent.FALLBACK_CATEGORIES];
  });

  /**
   * Technology suggestions for the tag input: the skill names (the same
   * vocabulary the portfolio shows as tech chips) plus the technologies the
   * other projects already use.
   */
  readonly technologySuggestions = computed<string[]>(() => {
    const fromSkills = this.skillService.skills().map(skill => skill.name);
    const fromProjects = this.projectService
      .projects()
      .flatMap(project => project.technologies ?? []);

    return [...new Set(
      [...fromSkills, ...fromProjects]
        .map(name => name.trim())
        .filter(name => name.length > 0)
    )].sort((a, b) => a.localeCompare(b));
  });
  readonly typeOptions = ['Team', 'Freelance', 'Self'];
  readonly statusOptions = ['In Progress', 'Completed', 'Deployed'];

  readonly projectForm = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', [Validators.required, Validators.maxLength(500)]],
    technologies: [''],
    liveUrl: [''],
    githubUrl: [''],
    imageUrl: [''],
    order: [0, [Validators.min(0), Validators.max(9999)]],
    featured: [false],
    isActive: [true],
    category: [''],
    client: [''],
    type: [''],
    database: [''],
    status: [''],
    badges: ['']
  });

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (this.isEditMode && idParam) {
      this.projectId = Number(idParam);
      this.loadProject(this.projectId);
    }

    // The category list comes from the API (managed on the Content page), so a
    // category added there shows up here without touching the frontend.
    if (this.categoryService.categories().length === 0) {
      this.categoryService.loadCategories().subscribe({ error: () => undefined });
    }

    // The skills are the vocabulary behind the technology chips, so they are
    // the suggestions for the technology tags.
    if (this.skillService.skills().length === 0) {
      this.skillService.loadSkills().subscribe({ error: () => undefined });
    }
  }

  invalid(control: string): boolean {
    const field = this.projectForm.get(control);
    return !!field && field.invalid && field.touched;
  }

  /** Resolved preview of the current cover (uploaded or external URL). */
  coverPreview(): string | null {
    const imageUrl = this.projectForm.getRawValue().imageUrl;
    // No cover yet -> null, so the form keeps showing the drop zone (the
    // placeholder image is for the LIST, not for the upload control).
    if (!imageUrl) return null;
    return resolvePortfolioImage(imageUrl);
  }

  /**
   * Hide a broken external image so the fallback surface shows through — and
   * say so: the file is referenced by the database but missing on the server
   * (uploaded through another deployment, or the upload folder was wiped by
   * a redeploy). Silently hiding it left no clue what to fix.
   */
  onPreviewError(event: Event): void {
    (event.currentTarget as HTMLImageElement).style.display = 'none';
    // Only an API-hosted upload can be missing in a way the admin can fix
    // (upload it again). A bundled portfolio cover is served by the portfolio
    // app, which the dashboard cannot reach, so a failed preview there says
    // nothing about the live site and must not raise an alarm.
    if (isApiHostedImage(this.projectForm.getRawValue().imageUrl)) {
      this.coverBroken.set(true);
    }
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = ''; // allow re-selecting the same file after a removal
    if (file) {
      void this.uploadCover(file);
    }
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragActive.set(false);
    const file = event.dataTransfer?.files?.[0];
    if (file) {
      void this.uploadCover(file);
    }
  }

  clearCover(): void {
    this.projectForm.patchValue({ imageUrl: '' });
    this.coverBroken.set(false);
  }

  /**
   * Upload the picked file to the backend store; the API answers with a
   * relative /api/files/... path which becomes the project's imageUrl.
   */
  private async uploadCover(file: File): Promise<void> {
    this.uploadError.set(null);

    if (!file.type.startsWith('image/')) {
      this.uploadError.set('Choose an image file (JPG, PNG, WebP, GIF or AVIF).');
      return;
    }
    if (file.size > MAX_COVER_BYTES) {
      this.uploadError.set('That image is larger than 10MB — pick a smaller one.');
      return;
    }

    this.uploading.set(true);
    try {
      const uploaded = await firstValueFrom(this.projectService.uploadCover(file));
      this.projectForm.patchValue({ imageUrl: uploaded.url });
      // A fresh upload replaces the missing file — clear the warning.
      this.coverBroken.set(false);
    } catch (err: any) {
      this.uploadError.set(
        err?.error?.message ?? 'Upload failed — make sure the Portfolio API is running and try again.'
      );
    } finally {
      this.uploading.set(false);
    }
  }

  onSubmit(): void {
    if (this.projectForm.invalid) {
      this.projectForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const value = this.projectForm.getRawValue();
    const payload: ProjectPayload = {
      title: value.title.trim(),
      description: value.description.trim(),
      technologies: value.technologies
        .split(',')
        .map(item => item.trim())
        .filter(Boolean),
      liveUrl: value.liveUrl.trim() || null,
      githubUrl: value.githubUrl.trim() || null,
      imageUrl: value.imageUrl.trim() || null,
      order: value.order,
      featured: value.featured,
      isActive: value.isActive,
      category: value.category.trim() || null,
      client: value.client.trim() || null,
      type: value.type || null,
      database: value.database.trim() || null,
      status: value.status || null,
      badges: value.badges
        .split(',')
        .map(item => item.trim())
        .filter(Boolean),
      gradient: this.loadedGradient,
      metrics: this.loadedMetrics
    };

    const request: Observable<unknown> =
      this.isEditMode && this.projectId !== null
        ? this.projectService.updateProject(this.projectId, payload)
        : this.projectService.createProject(payload);

    request.subscribe({
      next: () => {
        this.isLoading.set(false);
        this.router.navigate(['/projects']);
      },
      error: err => {
        this.isLoading.set(false);
        this.errorMessage.set(err?.error?.message ?? 'Failed to save the project. Please try again.');
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/projects']);
  }

  private loadProject(id: number): void {
    const cached = this.projectService.projects().find(project => project.id === id);
    if (cached) {
      this.populateForm(cached);
      return;
    }

    this.isLoading.set(true);
    this.projectService.getProject(id).subscribe({
      next: project => {
        this.isLoading.set(false);
        this.populateForm(project);
      },
      error: err => {
        this.isLoading.set(false);
        this.errorMessage.set(err?.error?.message ?? 'Failed to load the project.');
      }
    });
  }

  private populateForm(project: {
    title: string;
    description: string;
    technologies: string[];
    liveUrl: string | null;
    githubUrl: string | null;
    imageUrl: string | null;
    order: number;
    featured: boolean;
    isActive: boolean;
    category?: string | null;
    client?: string | null;
    type?: string | null;
    database?: string | null;
    status?: string | null;
    badges?: string[];
    gradient?: string | null;
    metrics?: ProjectMetric[];
  }): void {
    this.loadedGradient = project.gradient ?? null;
    this.loadedMetrics = project.metrics ?? [];
    this.projectForm.patchValue({
      title: project.title,
      description: project.description,
      technologies: project.technologies.join(', '),
      liveUrl: project.liveUrl ?? '',
      githubUrl: project.githubUrl ?? '',
      imageUrl: project.imageUrl ?? '',
      order: project.order,
      featured: project.featured,
      isActive: project.isActive,
      category: project.category ?? '',
      client: project.client ?? '',
      type: project.type ?? '',
      database: project.database ?? '',
      status: project.status ?? '',
      badges: (project.badges ?? []).join(', ')
    });
  }
}
