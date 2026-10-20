import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ProjectListStateService } from '../../../core/services/project-list-state.service';
import { ProjectService } from '../../../core/services/project.service';
import { Project } from '../../../core/models/project.model';
import { isApiHostedImage, resolvePortfolioImage } from '../../../core/portfolio-origin';
import { ConfirmDialogService } from '../../../shared/confirm-dialog.component';
import { PaginatorComponent } from '../../../shared/paginator.component';

const PAGE_SIZE = 9;

@Component({
  selector: 'app-project-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, PaginatorComponent],
  template: `
    <div class="space-y-6">
      <div class="flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
        <div>
          <p class="eyebrow mb-2">Showcase</p>
          <h2 class="font-display text-3xl font-semibold tracking-[-0.05em] text-ink">Projects</h2>
          <p class="mt-2 text-sm text-soft">{{ projectCount() }} projects served by the Portfolio API.</p>
        </div>
        <div class="flex items-center gap-3">
          <input
            type="search"
            [(ngModel)]="queryValue"
            placeholder="Search projects..."
            aria-label="Search projects"
            class="input max-w-[220px] py-2.5"
          />
          <a routerLink="/projects/new" class="btn-primary shrink-0"><span class="text-lg leading-none">+</span> New</a>
        </div>
      </div>

      @if (service.loading()) {
        <div class="card flex min-h-[280px] items-center justify-center">
          <div class="spinner" role="status" aria-label="Loading projects"></div>
        </div>
      } @else if (service.error()) {
        <div class="card empty-state">
          <p class="text-sm font-semibold text-ink">Failed to load projects</p>
          <p class="max-w-md text-sm text-soft">{{ service.error() }}</p>
          <button type="button" class="btn-ghost mt-2" (click)="load()">Retry</button>
        </div>
      } @else if (filteredProjects().length === 0) {
        <div class="card empty-state">
              @if (queryValue) {
                <p class="text-sm text-muted">No projects match "{{ queryValue }}".</p>
                      <button type="button" class="btn-ghost mt-2" (click)="setQuery('')">Clear search</button>
          } @else {
            <p class="text-sm text-muted">No projects yet. Create the first one for your portfolio.</p>
            <a routerLink="/projects/new" class="btn-primary mt-2">New project</a>
          }
        </div>
      } @else {
        <div class="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
          @for (project of paginatedProjects(); track project.id) {
            <article class="card-raised flex flex-col overflow-hidden p-0">
              <!-- Cover: API-owned upload (or legacy image) over the project
                   gradient; the letter tile shows through when missing. -->
              <div
                class="relative block h-40 shrink-0 overflow-hidden bg-line/[0.06]"
                [style.background]="project.gradient || undefined"
              >
                <span class="absolute inset-0 flex items-center justify-center font-display text-4xl font-bold text-ink/10">
                  {{ project.title.charAt(0) }}
                </span>
                @if (coverUrl(project.imageUrl); as src) {
                  <img
                    [src]="src"
                    [alt]="project.title + ' cover'"
                    loading="lazy"
                    class="relative h-full w-full object-cover"
                    (error)="onCoverError($event, project)"
                  />
                }
                @if (coverBroken(project)) {
                  <span
                    class="badge-danger absolute bottom-2 right-2 z-10"
                    title="The cover file is referenced by the database but missing on the server. Upload it again (and keep the App_Data/uploads folder when redeploying the API)."
                  >
                    Cover not found — re-upload
                  </span>
                }
              </div>

              <div class="flex flex-1 flex-col p-5">
                <div class="mb-3 flex flex-wrap items-center gap-1.5">
                  @if (project.featured) { <span class="badge-warning">Featured</span> }
                  @if (!project.isActive) { <span class="badge-muted">Hidden</span> }
                  @if (project.category) { <span class="badge-accent">{{ project.category }}</span> }
                  @if (project.type) { <span class="badge-muted">{{ project.type }}</span> }
                  @if (project.status) { <span class="badge-muted">{{ project.status }}</span> }
                  @for (badge of project.badges; track badge) { <span class="badge-muted">{{ badge }}</span> }
                </div>

                <div class="mb-2 flex items-start justify-between gap-3">
                  <div class="block min-w-0">
                    <h3 class="truncate font-display text-lg font-semibold tracking-[-0.02em] text-ink">{{ project.title }}</h3>
                    @if (project.client) {
                      <p class="mt-0.5 truncate text-xs text-muted">Client — {{ project.client }}</p>
                    }
                  </div>
                  <div class="flex shrink-0 gap-1">
                    <a [routerLink]="['/projects', project.id, 'edit']" class="icon-btn" [attr.aria-label]="'Edit ' + project.title" title="Edit">
                      <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M11 5H6a2 2 0 0 0-2 2v11a2 2 0 0 0 2 2h11a2 2 0 0 0 2-2v-5m-1.414-9.414a2 2 0 1 1 2.828 2.828L11.828 15H9v-2.828l8.586-8.586Z"/></svg>
                    </a>
                    <button type="button" class="icon-btn hover:border-danger/40 hover:text-danger" (click)="remove(project)" [attr.aria-label]="'Delete ' + project.title" title="Delete">
                      <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M19 7l-.867 12.142A2 2 0 0 1 16.138 21H7.862a2 2 0 0 1-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 0 0-1-1h-4a1 1 0 0 0-1 1v3M4 7h16"/></svg>
                    </button>
                  </div>
                </div>

                <p class="line-clamp-3 text-sm leading-6 text-soft">{{ project.description }}</p>

                @if (project.technologies.length) {
                  <div class="mt-3 flex flex-wrap gap-1.5">
                    @for (tech of project.technologies.slice(0, 3); track tech) {
                      <span class="badge-muted">{{ tech }}</span>
                    }
                  </div>
                }

                <div class="mt-auto flex items-center justify-between pt-4 text-xs text-muted">
                  <span>{{ project.createdAt | date: 'MMM d, y' }}</span>
                  @if (project.liveUrl) {
                    <a [href]="project.liveUrl" target="_blank" rel="noopener" class="font-semibold text-accent-text hover:underline">
                      Live →
                    </a>
                  } @else if (project.githubUrl) {
                    <a [href]="project.githubUrl" target="_blank" rel="noopener" class="font-semibold text-accent-text hover:underline">
                      Repository →
                    </a>
                  }
                </div>
              </div>
            </article>
          }
        </div>

        <app-paginator
          [totalItems]="filteredProjects().length"
          [pageSize]="pageSize"
          [page]="page()"
              (pageChange)="setPage($event)"
        />
      }
    </div>
  `
})
export class ProjectListComponent implements OnInit {
  readonly service = inject(ProjectService);
  private readonly dialog = inject(ConfirmDialogService);
  private readonly listState = inject(ProjectListStateService);

  readonly pageSize = PAGE_SIZE;
  readonly page = this.listState.page;

  /** Ids of projects whose cover image failed to load (404 on the server). */
  readonly brokenCovers = signal<Set<number>>(new Set<number>());

  /** Two-way binding target for the search box (the state lives in the shared service). */
  get queryValue(): string {
    return this.listState.query();
  }
  set queryValue(value: string) {
    this.setQuery(value);
  }
  setQuery(query: string): void {
    this.listState.setQuery(query);
  }
  setPage(page: number): void {
    this.listState.setPage(page);
  }

  readonly projectCount = computed(() => this.service.projects().length);

  readonly filteredProjects = computed(() => {
    const needle = this.listState.query().trim().toLowerCase();
    const projects = [...this.service.projects()].sort((a, b) => b.createdAt.localeCompare(a.createdAt));
    if (!needle) return projects;
    return projects.filter(
      project =>
        project.title.toLowerCase().includes(needle) ||
        project.description.toLowerCase().includes(needle) ||
        project.technologies.join(' ').toLowerCase().includes(needle) ||
        (project.client ?? '').toLowerCase().includes(needle) ||
        (project.category ?? '').toLowerCase().includes(needle)
    );
  });

  readonly paginatedProjects = computed(() => {
    const start = (this.page() - 1) * this.pageSize;
    return this.filteredProjects().slice(start, start + this.pageSize);
  });

  /** Stored image → loadable URL (backend uploads, legacy paths, absolute URLs). */
  coverUrl(imageUrl: string | null): string | null {
    return resolvePortfolioImage(imageUrl);
  }

  /** Hide a broken cover so the gradient + letter fallback shows through. */
  onCoverError(event: Event, project: Project): void {
    (event.currentTarget as HTMLImageElement).style.display = 'none';
    // Flag it ONLY for an API-hosted upload (/api/files/...): those can be
    // missing on the server (uploaded through a different deployment, or the
    // upload folder was wiped by a redeploy) and the admin can fix that by
    // uploading again. A bundled portfolio cover is served by the portfolio
    // app, which the dashboard cannot reach - a failed preview there says
    // nothing about the live site, so it must not raise an alarm.
    if (isApiHostedImage(project.imageUrl)) {
      this.brokenCovers.update(ids => new Set(ids).add(project.id));
    }
  }

  /** True when the cover image failed to load for this project. */
  coverBroken(project: Project): boolean {
    return this.brokenCovers().has(project.id);
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.service.loadProjects().subscribe({ error: () => undefined });
  }

  async remove(project: Project): Promise<void> {
    const confirmed = await this.dialog.confirm({
      title: 'Delete project?',
      message: `“${project.title}” will be removed from the public portfolio permanently.`,
      confirmLabel: 'Delete project'
    });
    if (!confirmed) return;

    this.service.deleteProject(project.id).subscribe({
      error: err => alert(err?.error?.message ?? 'Failed to delete the project.')
    });
  }
}
