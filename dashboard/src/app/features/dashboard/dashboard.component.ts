import { Component, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { Router, RouterLink } from '@angular/router';
import { ProjectService } from '../../core/services/project.service';
import { SkillService } from '../../core/services/skill.service';
import { ExperienceService } from '../../core/services/experience.service';
import { ContactService } from '../../core/services/contact.service';
import { AuthService } from '../../core/services/auth.service';
import { EmptyStateComponent } from '../../shared/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, EmptyStateComponent, ErrorStateComponent],
  template: `
    <div class="space-y-7">
      <!-- Header -->
      <section class="flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
        <div>
          <p class="eyebrow mb-2">{{ todayLabel }}</p>
          <h2 class="font-display text-3xl font-semibold tracking-[-0.05em] text-ink sm:text-4xl">
            Welcome back<span class="text-accent">,</span>
            <span class="text-muted">{{ firstName() }}.</span>
          </h2>
        </div>
        <div class="flex items-center gap-3">
          <a routerLink="/messages" class="btn-ghost">
            Inbox
            @if (unreadCount() > 0) {
              <span class="badge-accent">{{ unreadCount() }}</span>
            }
          </a>
          <a routerLink="/projects/new" class="btn-primary"><span class="text-lg leading-none">+</span> New project</a>
        </div>
      </section>

      @if (loading()) {
        <div class="card flex min-h-[280px] items-center justify-center">
          <div class="spinner" role="status" aria-label="Loading dashboard"></div>
        </div>
      } @else if (loadError()) {
        <app-error-state
          title="Could not reach the Portfolio API"
          [error]="loadError()"
          retryLabel="Retry loading"
          (retry)="reload()"
        ></app-error-state>
      } @else {
        <!-- Stat cards -->
        <section class="grid grid-cols-2 gap-3 lg:grid-cols-4" aria-label="Portfolio statistics">
          @for (stat of stats(); track stat.label) {
            <div class="card-raised p-4 sm:p-5">
              <div class="mb-4 flex items-center justify-between">
                <span class="flex h-9 w-9 items-center justify-center rounded-lg bg-accent/10 text-accent-text" [innerHTML]="stat.icon"></span>
                <span class="text-[11px] font-semibold text-muted">{{ stat.hint }}</span>
              </div>
              <p class="font-display text-3xl font-semibold tracking-[-0.04em] text-ink">{{ stat.value }}</p>
              <p class="mt-1 text-xs text-soft">{{ stat.label }}</p>
            </div>
          }
        </section>

        <!-- Health + content snapshot -->
        <section class="grid gap-5 xl:grid-cols-[1.2fr_0.8fr]">
          <div class="card relative overflow-hidden p-6 sm:p-8">
            <div class="absolute -right-24 -top-24 h-72 w-72 rounded-full bg-accent/10 blur-3xl"></div>
            <div class="relative z-10 flex flex-col gap-6 sm:flex-row sm:items-center">
              <div class="flex h-32 w-32 shrink-0 items-center justify-center rounded-full border-[10px] border-accent/15">
                <div class="text-center">
                  <strong class="font-display text-3xl font-semibold text-ink">{{ healthScore() }}%</strong>
                  <span class="block text-[10px] font-bold uppercase tracking-widest text-muted">complete</span>
                </div>
              </div>
              <div>
                <p class="eyebrow mb-2 text-accent-text">Portfolio health</p>
                <h3 class="font-display text-2xl font-semibold tracking-[-0.04em] text-ink">
                  {{ healthMessage() }}
                </h3>
                <ul class="mt-4 space-y-1.5">
                  @for (item of healthChecks(); track item.label) {
                    <li class="flex items-center gap-2 text-xs">
                      <span [class]="item.ok ? 'text-success' : 'text-faint'" aria-hidden="true">{{ item.ok ? '✓' : '○' }}</span>
                      <span [class]="item.ok ? 'text-soft' : 'text-muted'">{{ item.label }}</span>
                    </li>
                  }
                </ul>
              </div>
            </div>
          </div>

          <div class="card p-6">
            <p class="eyebrow mb-2">Content</p>
            <h3 class="font-display mb-5 text-xl font-semibold tracking-[-0.03em] text-ink">Snapshot</h3>
            <dl class="space-y-4">
              <div class="flex items-center justify-between border-b border-line/10 pb-3">
                <dt class="text-sm text-soft">Visible skills</dt>
                <dd class="text-sm font-bold text-ink">{{ visibleSkills() }} / {{ skillCount() }}</dd>
              </div>
              <div class="flex items-center justify-between border-b border-line/10 pb-3">
                <dt class="text-sm text-soft">Skill categories</dt>
                <dd class="text-sm font-bold text-ink">{{ skillCategories() }}</dd>
              </div>
              <div class="flex items-center justify-between border-b border-line/10 pb-3">
                <dt class="text-sm text-soft">Current role</dt>
                <dd class="max-w-[55%] truncate text-right text-sm font-bold text-ink">{{ currentRole() }}</dd>
              </div>
              <div class="flex items-center justify-between">
                <dt class="text-sm text-soft">Featured projects</dt>
                <dd class="text-sm font-bold text-ink">{{ featuredCount() }} / {{ projectCount() }}</dd>
              </div>
            </dl>
            <a routerLink="/content" class="btn-ghost mt-6 w-full">Manage content</a>
          </div>
        </section>

        <!-- Messages + projects -->
        <section class="grid gap-5 xl:grid-cols-[1fr_1fr]">
          <div class="card overflow-hidden">
            <div class="flex items-center justify-between border-b border-line/10 px-6 py-5">
              <div>
                <p class="eyebrow mb-1">Inbox</p>
                <h3 class="font-display text-xl font-semibold tracking-[-0.03em] text-ink">Latest messages</h3>
              </div>
              <a routerLink="/messages" class="text-xs font-semibold text-accent-text transition hover:underline">
                View inbox <span aria-hidden="true">↗</span>
              </a>
            </div>
            @if (recentMessages().length === 0) {
              <div class="p-6">
                <app-empty-state
                  icon="message"
                  title="No messages yet"
                  description="Visitor contact forms submitted from your portfolio will appear here."
                ></app-empty-state>
              </div>
            } @else {
              <div class="divide-y divide-line/10">
                @for (message of recentMessages(); track message.id) {
                  <a routerLink="/messages" class="flex items-start gap-4 px-6 py-4 transition hover:bg-line/[0.03]">
                    <span class="mt-1 h-2 w-2 shrink-0 rounded-full" [class]="message.isRead ? 'bg-faint/50' : 'bg-accent'"></span>
                    <div class="min-w-0 flex-1">
                      <div class="flex flex-wrap items-center gap-2">
                        <h4 class="truncate text-sm font-semibold text-ink">{{ message.name }}</h4>
                        @if (!message.isRead) { <span class="badge-accent">Unread</span> }
                      </div>
                      <p class="mt-0.5 truncate text-xs text-soft">{{ message.subject || message.message }}</p>
                    </div>
                    <span class="shrink-0 text-xs text-muted">{{ message.createdAt | date:'MMM d' }}</span>
                  </a>
                }
              </div>
            }
          </div>

          <div class="card overflow-hidden">
            <div class="flex items-center justify-between border-b border-line/10 px-6 py-5">
              <div>
                <p class="eyebrow mb-1">Showcase</p>
                <h3 class="font-display text-xl font-semibold tracking-[-0.03em] text-ink">Recent projects</h3>
              </div>
              <a routerLink="/projects" class="text-xs font-semibold text-accent-text transition hover:underline">
                All projects <span aria-hidden="true">↗</span>
              </a>
            </div>
            @if (recentProjects().length === 0) {
              <div class="p-6">
                <app-empty-state
                  icon="folder"
                  title="No projects yet"
                  description="Add your first project to populate the showcase."
                  actionLabel="New project"
                  (action)="createNewProject()"
                ></app-empty-state>
              </div>
            } @else {
              <div class="divide-y divide-line/10">
                @for (project of recentProjects(); track project.id) {
                  <a [routerLink]="['/projects', project.id, 'edit']" class="block px-6 py-4 transition hover:bg-line/[0.03]">
                    <div class="flex items-center justify-between gap-3">
                      <h4 class="truncate text-sm font-semibold text-ink">{{ project.titleEn }}</h4>
                      @if (project.featured) { <span class="badge-warning shrink-0">Featured</span> }
                    </div>
                    <p class="mt-0.5 line-clamp-1 text-xs text-soft">{{ project.descriptionEn }}</p>
                    <div class="mt-2 flex flex-wrap gap-1.5">
                      @for (tech of (project.technologies || []).slice(0, 4); track tech) {
                        <span class="badge-muted">{{ tech }}</span>
                      }
                    </div>
                  </a>
                }
              </div>
            }
          </div>
        </section>
      }
    </div>
  `
})
export class DashboardComponent implements OnInit {
  private readonly projectService = inject(ProjectService);
  private readonly skillService = inject(SkillService);
  private readonly experienceService = inject(ExperienceService);
  private readonly contactService = inject(ContactService);
  private readonly authService = inject(AuthService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly router = inject(Router);

  createNewProject(): void {
    this.router.navigate(['/projects/new']);
  }

  /** Angular's default sanitizer strips <svg> from innerHTML — mark the hardcoded icon strings trusted. */
  private trust(icon: string): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(icon);
  }

  readonly projects = this.projectService.projects;
  readonly skills = this.skillService.skills;
  readonly experience = this.experienceService.experience;
  readonly messages = this.contactService.messages;

  readonly todayLabel = new Intl.DateTimeFormat('en-US', {
    weekday: 'long',
    month: 'long',
    day: 'numeric',
    year: 'numeric'
  }).format(new Date());

  readonly firstName = computed(() => this.authService.currentUser()?.name?.split(' ')[0] ?? 'there');
  readonly unreadCount = this.contactService.unreadCount;

  readonly loading = computed(
    () =>
      this.projectService.loading() ||
      this.skillService.loading() ||
      this.experienceService.loading() ||
      this.contactService.loading()
  );

  readonly loadError = computed(
    () =>
      this.projectService.error() ??
      this.skillService.error() ??
      this.experienceService.error() ??
      this.contactService.error()
  );

  readonly projectCount = computed(() => this.projects().length);
  readonly featuredCount = computed(() => this.projects().filter(p => p.featured).length);
  readonly skillCount = computed(() => this.skills().length);
  readonly visibleSkills = computed(() => this.skills().filter(s => s.isActive).length);
  readonly skillCategories = computed(
    () => new Set(this.skills().map(s => s.category || 'Uncategorized')).size
  );
  readonly currentRole = computed(
    () => this.experience().find(item => item.current)?.company ?? '—'
  );

  readonly recentMessages = computed(() =>
    [...this.messages()]
      .sort((a, b) => Number(a.isRead) - Number(b.isRead) || b.createdAt.localeCompare(a.createdAt))
      .slice(0, 4)
  );

  readonly recentProjects = computed(() =>
    [...this.projects()].sort((a, b) => b.createdAt.localeCompare(a.createdAt)).slice(0, 4)
  );

  readonly stats = computed(() => [
    {
      label: 'Projects',
      value: this.projectCount(),
      hint: `${this.featuredCount()} featured`,
      icon: this.trust('<svg viewBox="0 0 24 24" fill="none" width="18" height="18"><path d="M4 7.5a2 2 0 0 1 2-2h4l2 2h6a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2v-10Z" stroke="currentColor" stroke-width="1.7" stroke-linejoin="round"/></svg>')
    },
    {
      label: 'Skills',
      value: this.skillCount(),
      hint: `${this.visibleSkills()} visible`,
      icon: this.trust('<svg viewBox="0 0 24 24" fill="none" width="18" height="18"><path d="m12 4 2.1 4.3 4.7.7-3.4 3.3.8 4.7-4.2-2.2-4.2 2.2.8-4.7-3.4-3.3 4.7-.7L12 4Z" stroke="currentColor" stroke-width="1.7" stroke-linejoin="round"/></svg>')
    },
    {
      label: 'Unread messages',
      value: this.unreadCount(),
      hint: `${this.messages().length} total`,
      icon: this.trust('<svg viewBox="0 0 24 24" fill="none" width="18" height="18"><path d="M4 6.5A2.5 2.5 0 0 1 6.5 4h11A2.5 2.5 0 0 1 20 6.5v8a2.5 2.5 0 0 1-2.5 2.5H9l-4 3.5v-3.5A2.5 2.5 0 0 1 4 14.5v-8Z" stroke="currentColor" stroke-width="1.7" stroke-linejoin="round"/></svg>')
    }
  ]);

  readonly healthChecks = computed(() => [
    { label: 'At least one project', ok: this.projectCount() > 0 },
    { label: 'A featured project is highlighted', ok: this.featuredCount() > 0 },
    { label: 'Skills are listed and visible', ok: this.visibleSkills() > 0 },
    { label: 'Work experience is filled in', ok: this.experience().length > 0 }
  ]);

  readonly healthScore = computed(() => {
    const checks = this.healthChecks();
    return Math.round((checks.filter(check => check.ok).length / checks.length) * 100);
  });

  readonly healthMessage = computed(() => {
    const score = this.healthScore();
    if (score === 100) return 'Your portfolio is fully fleshed out.';
    if (score >= 60) return 'Strong base — a few gaps left.';
    if (score > 0) return 'Getting started. Fill the gaps below.';
    return 'Nothing published yet. Add your first content.';
  });

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.projectService.loadProjects().subscribe({ error: () => undefined });
    this.skillService.loadSkills().subscribe({ error: () => undefined });
    this.experienceService.loadExperience().subscribe({ error: () => undefined });
    this.contactService.loadMessages().subscribe({ error: () => undefined });
  }
}
