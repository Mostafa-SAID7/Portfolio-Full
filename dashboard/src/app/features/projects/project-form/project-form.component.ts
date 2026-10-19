import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { ProjectService } from '../../../core/services/project.service';
import { ProjectMetric, ProjectPayload } from '../../../core/models/project.model';

@Component({
  selector: 'app-project-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
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
          <label for="technologies" class="field-label">Technologies</label>
          <input id="technologies" type="text" formControlName="technologies" class="input" placeholder="Angular, .NET 9, SQL Server" />
          <p class="mt-2 text-xs text-muted">Separate with commas — they become tech chips on the portfolio.</p>
        </div>

        <div class="border-t border-line/10 pt-5">
          <p class="eyebrow mb-1">Portfolio presentation</p>
          <p class="mb-4 text-xs text-muted">
            These fields drive the category filters, client and status chips on the public portfolio.
          </p>
          <div class="grid gap-5 sm:grid-cols-2">
            <div>
              <label for="category" class="field-label">Category</label>
              <input id="category" type="text" formControlName="category" class="input" list="category-options" placeholder="Clothing" />
              <datalist id="category-options">
                @for (option of categoryOptions; track option) { <option [value]="option"></option> }
              </datalist>
            </div>
            <div>
              <label for="client" class="field-label">Client</label>
              <input id="client" type="text" formControlName="client" class="input" placeholder="Under Armour" />
            </div>
            <div>
              <label for="type" class="field-label">Type</label>
              <select id="type" formControlName="type" class="input">
                <option value="">—</option>
                @for (option of typeOptions; track option) { <option [value]="option">{{ option }}</option> }
              </select>
            </div>
            <div>
              <label for="database" class="field-label">Database</label>
              <input id="database" type="text" formControlName="database" class="input" placeholder="PostgreSQL" />
            </div>
            <div>
              <label for="status" class="field-label">Build status</label>
              <select id="status" formControlName="status" class="input">
                <option value="">—</option>
                @for (option of statusOptions; track option) { <option [value]="option">{{ option }}</option> }
              </select>
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

        <div>
          <label for="imageUrl" class="field-label">Cover image URL</label>
          <input id="imageUrl" type="url" formControlName="imageUrl" class="input" placeholder="/api/my-project-1280.webp" />
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
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly isEditMode = this.route.snapshot.url.some(segment => segment.path === 'edit');

  private projectId: number | null = null;

  /** Presentation data kept out of the form UI but round-tripped on save. */
  private loadedGradient: string | null = null;
  private loadedMetrics: ProjectMetric[] = [];

  readonly categoryOptions = [
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
  }

  invalid(control: string): boolean {
    const field = this.projectForm.get(control);
    return !!field && field.invalid && field.touched;
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
