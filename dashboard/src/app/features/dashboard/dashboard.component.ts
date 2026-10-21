import { Component, computed, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
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
  templateUrl: "./dashboard.component.html"
})
export class DashboardComponent implements OnInit {
  readonly projectService = inject(ProjectService);
  readonly skillService = inject(SkillService);
  readonly experienceService = inject(ExperienceService);
  readonly contactService = inject(ContactService);
  private readonly destroyRef = inject(DestroyRef);
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

  readonly sources = [
    { label: 'Projects', service: this.projectService, retry: () => this.loadProjects() },
    { label: 'Skills', service: this.skillService, retry: () => this.loadSkills() },
    { label: 'Experience', service: this.experienceService, retry: () => this.loadExperience() },
    { label: 'Messages', service: this.contactService, retry: () => this.loadMessages() }
  ];
  readonly healthUnavailable = computed(() =>
    this.projectService.loading() || this.projectService.error() !== null ||
    this.skillService.loading() || this.skillService.error() !== null ||
    this.experienceService.loading() || this.experienceService.error() !== null
  );

  readonly projectCount = computed(() => this.projects().length);
  readonly publishedProjects = computed(() => this.projects().filter(p => p.isActive).length);
  readonly featuredCount = computed(() => this.projects().filter(p => p.featured && p.isActive).length);
  readonly skillCount = computed(() => this.skills().length);
  readonly visibleSkills = computed(() => this.skills().filter(s => s.isActive).length);
  readonly skillCategories = computed(
    () => new Set(this.skills().map(s => s.categoryEn || 'Uncategorized')).size
  );
  readonly currentRole = computed(
    () => this.experienceService.error() || this.experienceService.loading() ? 'Unavailable' : this.experience().find(item => item.current && item.isActive)?.companyEn ?? '—'
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
      value: this.projectService.loading() ? '…' : this.projectService.error() ? '—' : this.projectCount(),
      hint: this.projectService.error() ? 'Unavailable' : `${this.featuredCount()} featured`,
      icon: this.trust('<svg viewBox="0 0 24 24" fill="none" width="18" height="18"><path d="M4 7.5a2 2 0 0 1 2-2h4l2 2h6a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2v-10Z" stroke="currentColor" stroke-width="1.7" stroke-linejoin="round"/></svg>')
    },
    {
      label: 'Skills',
      value: this.skillService.loading() ? '…' : this.skillService.error() ? '—' : this.skillCount(),
      hint: this.skillService.error() ? 'Unavailable' : `${this.visibleSkills()} visible`,
      icon: this.trust('<svg viewBox="0 0 24 24" fill="none" width="18" height="18"><path d="m12 4 2.1 4.3 4.7.7-3.4 3.3.8 4.7-4.2-2.2-4.2 2.2.8-4.7-3.4-3.3 4.7-.7L12 4Z" stroke="currentColor" stroke-width="1.7" stroke-linejoin="round"/></svg>')
    },
    {
      label: 'Unread messages',
      value: this.contactService.loading() ? '…' : this.contactService.error() ? '—' : this.unreadCount(),
      hint: this.contactService.error() ? 'Unavailable' : `${this.messages().length} total`,
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
    this.reload(false);
  }

  private loadProjects(): void {
    this.projectService.loadProjects().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ error: () => undefined });
  }

  private loadSkills(): void {
    this.skillService.loadSkills().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ error: () => undefined });
  }

  private loadExperience(): void {
    this.experienceService.loadExperience().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ error: () => undefined });
  }

  private loadMessages(force: boolean): void {
    this.contactService.loadMessages({ force }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ error: () => undefined });
  }

  reload(forceMessages = true): void {
    this.loadProjects();
    this.loadSkills();
    this.loadExperience();
    this.loadMessages(forceMessages);
  }
}
