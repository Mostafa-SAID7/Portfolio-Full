import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { ToastService } from '../../../shared/toast.component';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ProjectListStateService } from '../../../core/services/project-list-state.service';
import { replaceUrlParams, readUrlInt } from '../../../core/url-state';
import { ProjectService } from '../../../core/services/project.service';
import { WebsiteService } from '../../../core/services/website.service';
import { Project } from '../../../core/models/project.model';
import { isApiHostedImage, resolvePortfolioImage } from '../../../core/portfolio-origin';
import { ConfirmDialogService } from '../../../shared/confirm-dialog.component';
import { PaginatorComponent } from '../../../shared/paginator.component';
import { EmptyStateComponent } from '../../../shared/empty-state.component';
import { ErrorStateComponent } from '../../../shared/error-state.component';
import { SelectFieldComponent, SelectOptionValue } from '../../../shared/select-field.component';

const PAGE_SIZE = 9;

const DEFAULT_WEBSITES: { slug: string; name: string }[] = [
  { slug: 'marketplace', name: 'Marketplace' },
  { slug: 'banking', name: 'Fintech / Banking' },
  { slug: 'edutech', name: 'EduTech' },
  { slug: 'real-estate', name: 'Real Estate' },
  { slug: 'healthcare', name: 'Healthcare' },
  { slug: 'restaurants', name: 'Restaurants' },
  { slug: 'platforms', name: 'Platforms' },
  { slug: 'entertainment', name: 'Entertainment / Booking' },
  { slug: 'asp-net', name: 'ASP.NET Projects' }
];

@Component({
  selector: 'app-project-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    PaginatorComponent,
    EmptyStateComponent,
    ErrorStateComponent,
    SelectFieldComponent,
  ],
  templateUrl: "./project-list.component.html"
})
export class ProjectListComponent implements OnInit {
  readonly service = inject(ProjectService);
  readonly websiteService = inject(WebsiteService);
  private readonly dialog = inject(ConfirmDialogService);
  private readonly listState = inject(ProjectListStateService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  readonly deleting = signal<Set<number>>(new Set());

  /** Currently selected website slug for filtering (empty = all websites). */
  readonly selectedWebsiteSlug = signal<string>('');

  createProject(): void {
    this.router.navigate(['/projects/new']);
  }

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
    this.listState.setPage(1);
    this.persistInUrl();
  }
  setPage(page: number): void {
    this.listState.setPage(page);
    this.persistInUrl();
  }

  setWebsiteFilter(slug: string): void {
    this.selectedWebsiteSlug.set(slug);
    this.listState.setPage(1);
    this.persistInUrl();
  }

  /** Options formatted for the reusable SelectFieldComponent */
  readonly websiteOptions = computed<SelectOptionValue[]>(() => {
    const map = new Map<string, string>();
    for (const w of this.websiteService.websites()) {
      map.set(w.slug, w.name);
    }
    if (map.size === 0) {
      for (const d of DEFAULT_WEBSITES) {
        map.set(d.slug, d.name);
      }
    }
    for (const p of this.service.projects()) {
      if (p.websites) {
        for (const w of p.websites) {
          if (!map.has(w.websiteSlug)) {
            map.set(w.websiteSlug, w.websiteName);
          }
        }
      }
    }
    const list: SelectOptionValue[] = [{ value: '', label: 'All websites' }];
    for (const [slug, name] of map.entries()) {
      list.push({ value: slug, label: name });
    }
    return list;
  });

  /** Mirror the shared list state into the URL (see ProjectListStateService). */
  private persistInUrl(): void {
    replaceUrlParams(this.route, this.router, {
      q: this.listState.query().trim() || null,
      page: this.listState.page() > 1 ? this.listState.page() : null,
      website: this.selectedWebsiteSlug() || null,
    });
  }

  readonly projectCount = computed(() => this.service.projects().length);
  readonly hasActiveQuery = computed(() => this.listState.query().trim().length > 0 || this.selectedWebsiteSlug().length > 0);

  readonly filteredProjects = computed(() => {
    const needle = this.listState.query().trim().toLowerCase();
    const websiteSlug = this.selectedWebsiteSlug();
    const projects = [...this.service.projects()].sort((a, b) => (b.createdAt || '').localeCompare(a.createdAt || ''));
    return projects.filter(project => {
      // Website filter
      if (websiteSlug && !project.websites?.some(w => w.websiteSlug === websiteSlug)) return false;
      // Text search filter
      if (!needle) return true;
      return (
        (project.titleEn || '').toLowerCase().includes(needle) ||
        (project.descriptionEn || '').toLowerCase().includes(needle) ||
        (project.technologies || []).join(' ').toLowerCase().includes(needle) ||
        (project.clientEn ?? '').toLowerCase().includes(needle) ||
        (project.categoryEn ?? '').toLowerCase().includes(needle)
      );
    });
  });

  readonly currentPage = computed(() => Math.min(Math.max(1, this.page()), Math.max(1, Math.ceil(this.filteredProjects().length / PAGE_SIZE))));
  readonly paginatedProjects = computed(() => {
    const start = (this.currentPage() - 1) * PAGE_SIZE;
    return this.filteredProjects().slice(start, start + PAGE_SIZE);
  });

  /** Stored image → loadable URL (backend uploads, legacy paths, absolute URLs). */
  coverUrl(imageUrl: string | null): string | null {
    return resolvePortfolioImage(imageUrl);
  }

  /** Hide a broken cover so the gradient + letter fallback shows through. */
  onCoverError(event: Event, project: Project): void {
    (event.currentTarget as HTMLImageElement).style.display = 'none';
    if (isApiHostedImage(project.imageUrl)) {
      this.brokenCovers.update(ids => new Set(ids).add(project.id));
    }
  }

  /** True when the cover image failed to load for this project. */
  coverBroken(project: Project): boolean {
    return this.brokenCovers().has(project.id);
  }

  coverGradient(value: string | null): string | null {
    if (!value) return null;
    if (/^(?:linear|radial|conic)-gradient\(/i.test(value)) return value;
    const colors: Record<string, string> = {
      'blue-500': '#3b82f6', 'purple-500': '#a855f7', 'indigo-500': '#6366f1',
      'cyan-500': '#06b6d4', 'teal-500': '#14b8a6', 'emerald-500': '#10b981',
      'green-500': '#22c55e', 'lime-500': '#84cc16', 'amber-500': '#f59e0b',
      'orange-500': '#f97316', 'rose-500': '#f43f5e', 'pink-500': '#ec4899',
      'red-500': '#ef4444', 'sky-500': '#0ea5e9', 'violet-500': '#8b5cf6',
      'fuchsia-500': '#d946ef', 'yellow-500': '#eab308'
    };
    const match = value.match(/^from-([a-z]+-\d+)\s+to-([a-z]+-\d+)$/);
    return match && colors[match[1]] && colors[match[2]]
      ? `linear-gradient(to right, ${colors[match[1]]}, ${colors[match[2]]})`
      : null;
  }

  ngOnInit(): void {
    const params = this.route.snapshot.queryParamMap;
    const savedPage = readUrlInt(params, 'page');
    const savedQuery = params.get('q');
    const savedWebsite = params.get('website');
    if (savedPage !== null || savedQuery !== null) {
      this.listState.restore(savedPage ?? 1, savedQuery ?? '');
    }
    if (savedWebsite) {
      this.selectedWebsiteSlug.set(savedWebsite);
    }
    // Load websites for the filter dropdown.
    if (this.websiteService.websites().length === 0) {
      this.websiteService.loadWebsites().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ error: () => undefined });
    }
    this.load();
  }

  load(): void {
    this.brokenCovers.set(new Set());
    this.service.loadProjects().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ error: () => undefined });
  }

  async remove(project: Project): Promise<void> {
    if (this.deleting().has(project.id)) return;
    const confirmed = await this.dialog.confirm({
      title: 'Delete project?',
      message: `"${project.titleEn}" will be removed from the public portfolio permanently.`,
      confirmLabel: 'Delete project'
    });
    if (!confirmed || this.destroyRef.destroyed || this.deleting().has(project.id)) return;
    this.deleting.update(ids => new Set(ids).add(project.id));
    this.service.deleteProject(project.id).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        this.deleting.update(ids => {
          const next = new Set(ids);
          next.delete(project.id);
          return next;
        });
        this.setPage(this.currentPage());
      })
    ).subscribe({ error: err => this.toast.show(err?.error?.message ?? 'Failed to delete the project.', 'error') });
  }
}
