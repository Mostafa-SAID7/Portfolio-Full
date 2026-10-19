import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ProjectService } from '../../../core/services/project.service';
import { Project } from '../../../core/models/project.model';
import { resolvePortfolioImage } from '../../../core/portfolio-origin';

@Component({
  selector: 'app-project-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="mx-auto max-w-4xl space-y-6">
      <a routerLink="/projects" class="text-xs font-semibold text-accent-text hover:underline">← Back to projects</a>

      @if (loading()) {
        <div class="card flex min-h-[240px] items-center justify-center">
          <div class="spinner" role="status" aria-label="Loading project"></div>
        </div>
      } @else if (project(); as p) {
        <div class="flex flex-col justify-between gap-4 sm:flex-row sm:items-start">
          <div>
            <div class="mb-3 flex flex-wrap items-center gap-2">
              @if (p.featured) { <span class="badge-warning">Featured</span> }
              @if (!p.isActive) { <span class="badge-muted">Hidden draft</span> }
              @if (p.category) { <span class="badge-accent">{{ p.category }}</span> }
              @if (p.type) { <span class="badge-muted">{{ p.type }}</span> }
              @if (p.status) { <span class="badge-muted">{{ p.status }}</span> }
              @for (badge of p.badges; track badge) { <span class="badge-muted">{{ badge }}</span> }
            </div>
            <h2 class="font-display text-3xl font-semibold tracking-[-0.05em] text-ink">{{ p.title }}</h2>
            @if (p.client) { <p class="mt-1 text-sm text-muted">Client · {{ p.client }}</p> }
            <p class="mt-2 text-sm text-muted">Added {{ p.createdAt | date: 'MMMM d, y' }}</p>
          </div>
          <div class="flex shrink-0 gap-3">
            <a [routerLink]="['/projects', p.id, 'edit']" class="btn-ghost">Edit</a>
            <button type="button" (click)="remove()" class="btn-danger">Delete</button>
          </div>
        </div>

        @if (p.imageUrl) {
          <div class="card overflow-hidden bg-line/[0.06]" [style.background]="p.gradient || undefined">
            <img
              [src]="coverUrl(p.imageUrl)"
              [alt]="p.title + ' cover'"
              class="w-full object-cover"
              loading="lazy"
              (error)="onCoverError($event)"
            />
          </div>
        }

        <div class="card p-6">
          <h3 class="eyebrow mb-3">About</h3>
          <p class="whitespace-pre-line text-sm leading-7 text-soft">{{ p.description }}</p>
        </div>

        <dl class="grid gap-4 sm:grid-cols-2">
          <div class="card-raised p-5">
            <dt class="eyebrow mb-2">Status</dt>
            <dd class="flex flex-wrap gap-2 text-sm text-ink">
              <span [class]="p.isActive ? 'badge-success' : 'badge-muted'">{{ p.isActive ? 'Visible on site' : 'Hidden' }}</span>
              <span [class]="p.featured ? 'badge-warning' : 'badge-muted'">{{ p.featured ? 'Featured' : 'Standard' }}</span>
              <span class="badge-muted">Order {{ p.order }}</span>
            </dd>
          </div>
          <div class="card-raised p-5">
            <dt class="eyebrow mb-2">Links</dt>
            <dd class="flex flex-wrap gap-4 text-sm">
              @if (p.liveUrl) {
                <a [href]="p.liveUrl" target="_blank" rel="noopener" class="font-semibold text-accent-text hover:underline">Live site ↗</a>
              }
              @if (p.githubUrl) {
                <a [href]="p.githubUrl" target="_blank" rel="noopener" class="font-semibold text-accent-text hover:underline">Repository ↗</a>
              }
              @if (!p.liveUrl && !p.githubUrl) {
                <span class="text-muted">No links yet</span>
              }
            </dd>
          </div>
          <div class="card-raised p-5 sm:col-span-2">
            <dt class="eyebrow mb-3">Portfolio presentation</dt>
            <dd class="grid gap-x-8 gap-y-2 text-sm sm:grid-cols-2">
              <p><span class="text-muted">Category:</span> <span class="font-semibold text-ink">{{ p.category || '—' }}</span></p>
              <p><span class="text-muted">Client:</span> <span class="font-semibold text-ink">{{ p.client || '—' }}</span></p>
              <p><span class="text-muted">Type:</span> <span class="font-semibold text-ink">{{ p.type || '—' }}</span></p>
              <p><span class="text-muted">Database:</span> <span class="font-semibold text-ink">{{ p.database || '—' }}</span></p>
              <p><span class="text-muted">Build status:</span> <span class="font-semibold text-ink">{{ p.status || '—' }}</span></p>
              <p>
                <span class="text-muted">Badges:</span>
                @if (p.badges.length) {
                  <span class="ml-1 font-semibold text-ink">{{ p.badges.join(', ') }}</span>
                } @else {
                  <span class="font-semibold text-ink">—</span>
                }
              </p>
            </dd>
          </div>
        </dl>
      } @else {
        <div class="card empty-state">
          <p class="text-sm font-semibold text-ink">Project not found</p>
          <a routerLink="/projects" class="btn-ghost mt-2">Back to projects</a>
        </div>
      }
    </div>
  `
})
export class ProjectDetailComponent implements OnInit {
  private readonly projectService = inject(ProjectService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  private readonly projectSignal = signal<Project | null>(null);
  private readonly loadingSignal = signal(true);

  readonly project = this.projectSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();

  /** Stored image → loadable URL (covers live in the portfolio's public folder). */
  coverUrl(imageUrl: string | null): string | null {
    return resolvePortfolioImage(imageUrl, 1280);
  }

  /** Hide a broken cover so the gradient background shows through. */
  onCoverError(event: Event): void {
    (event.currentTarget as HTMLImageElement).style.display = 'none';
  }

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!Number.isFinite(id)) {
      this.loadingSignal.set(false);
      return;
    }

    // Prefer the cached list, then fetch fresh data.
    const cached = this.projectService.projects().find(item => item.id === id);
    if (cached) {
      this.projectSignal.set(cached);
      this.loadingSignal.set(false);
    }

    this.projectService.getProject(id).subscribe({
      next: project => {
        this.projectSignal.set(project);
        this.loadingSignal.set(false);
      },
      error: () => this.loadingSignal.set(false)
    });
  }

  remove(): void {
    const project = this.projectSignal();
    if (!project) return;
    if (!confirm(`Delete "${project.title}"? This removes it from the public portfolio.`)) return;

    this.projectService.deleteProject(project.id).subscribe({
      next: () => this.router.navigate(['/projects']),
      error: err => alert(err?.error?.message ?? 'Failed to delete the project.')
    });
  }
}
