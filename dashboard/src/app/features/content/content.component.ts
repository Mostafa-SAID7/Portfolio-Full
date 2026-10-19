import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SkillService } from '../../core/services/skill.service';
import { ExperienceService } from '../../core/services/experience.service';
import { BlogService } from '../../core/services/blog.service';
import { Skill } from '../../core/models/skill.model';
import { BlogPost } from '../../core/models/blog.model';

type ContentTab = 'skills' | 'experience' | 'blog';

@Component({
  selector: 'app-content',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="space-y-6">
      <div class="flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
        <div>
          <p class="eyebrow mb-2">Site content</p>
          <h2 class="font-display text-3xl font-semibold tracking-[-0.05em] text-ink">Content</h2>
          <p class="mt-2 text-sm text-soft">Skills, experience and blog posts managed through the Portfolio API.</p>
        </div>
        <div class="flex items-center gap-1 rounded-xl border border-line/10 bg-surface p-1" role="tablist" aria-label="Content type">
          @for (item of tabs; track item.value) {
            <button
              type="button"
              role="tab"
              class="segmented"
              [class.active]="tab() === item.value"
              [attr.aria-selected]="tab() === item.value"
              (click)="tab.set(item.value)"
            >
              {{ item.label }}
            </button>
          }
        </div>
      </div>

      @if (tab() === 'skills') {
        <!-- ---------------- Skills ---------------- -->
        @if (skillService.loading()) {
          <div class="card flex min-h-[200px] items-center justify-center"><div class="spinner" role="status" aria-label="Loading skills"></div></div>
        } @else if (skillService.error()) {
          <div class="card empty-state">
            <p class="text-sm font-semibold text-ink">Failed to load skills</p>
            <p class="max-w-md text-sm text-soft">{{ skillService.error() }}</p>
            <button type="button" class="btn-ghost mt-2" (click)="loadAll()">Retry</button>
          </div>
        } @else {
          <form class="card flex flex-col gap-3 p-4 sm:flex-row sm:items-end" (ngSubmit)="addSkill()">
            <div class="flex-1">
              <label for="skillName" class="field-label">Add a skill</label>
              <input id="skillName" type="text" class="input" [(ngModel)]="newSkillName" name="skillName" placeholder="e.g. Angular" />
            </div>
            <div class="sm:w-52">
              <label for="skillCategory" class="field-label">Category</label>
              <input id="skillCategory" type="text" class="input" [(ngModel)]="newSkillCategory" name="skillCategory" placeholder="Frontend" />
            </div>
            <button type="submit" class="btn-primary sm:mb-0.5" [disabled]="!newSkillName.trim()">Add</button>
          </form>

          @if (skillsByCategory().length === 0) {
            <div class="card empty-state"><p class="text-sm text-muted">No skills yet — add your first one above.</p></div>
          } @else {
            @for (group of skillsByCategory(); track group.category) {
              <section class="card overflow-hidden">
                <div class="border-b border-line/10 px-6 py-4">
                  <h3 class="font-display text-lg font-semibold text-ink">{{ group.category }}</h3>
                </div>
                <ul class="divide-y divide-line/10">
                  @for (skill of group.skills; track skill.id) {
                    <li class="flex items-center gap-4 px-6 py-3.5">
                      <div class="min-w-0 flex-1">
                        <p class="text-sm font-semibold" [class]="skill.isActive ? 'text-ink' : 'text-muted line-through'">{{ skill.name }}</p>
                        <div class="mt-1.5 h-1.5 w-40 overflow-hidden rounded-full bg-line/10">
                          <div class="h-full rounded-full bg-accent" [style.width.%]="skill.proficiency"></div>
                        </div>
                      </div>
                      <span class="hidden text-xs text-muted sm:block">{{ skill.proficiency }}%</span>
                      <button type="button" class="btn-ghost" (click)="toggleSkill(skill)">
                        {{ skill.isActive ? 'Hide' : 'Show' }}
                      </button>
                      <button type="button" class="icon-btn hover:border-danger/40 hover:text-danger" (click)="deleteSkill(skill)" [attr.aria-label]="'Delete ' + skill.name">
                        <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M19 7l-.867 12.142A2 2 0 0 1 16.138 21H7.862a2 2 0 0 1-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 0 0-1-1h-4a1 1 0 0 0-1 1v3M4 7h16"/></svg>
                      </button>
                    </li>
                  }
                </ul>
              </section>
            }
          }
        }
      } @else if (tab() === 'experience') {
        <!-- ---------------- Experience ---------------- -->
        @if (experienceService.loading()) {
          <div class="card flex min-h-[200px] items-center justify-center"><div class="spinner" role="status" aria-label="Loading experience"></div></div>
        } @else if (experienceService.error()) {
          <div class="card empty-state">
            <p class="text-sm font-semibold text-ink">Failed to load experience</p>
            <p class="max-w-md text-sm text-soft">{{ experienceService.error() }}</p>
            <button type="button" class="btn-ghost mt-2" (click)="loadAll()">Retry</button>
          </div>
        } @else if (experienceService.experience().length === 0) {
          <div class="card empty-state"><p class="text-sm text-muted">No experience entries yet.</p></div>
        } @else {
          <div class="space-y-3">
            @for (item of experienceService.experience(); track item.id) {
              <article class="card-raised flex flex-col gap-3 p-5 sm:flex-row sm:items-start">
                <div class="min-w-0 flex-1">
                  <div class="flex flex-wrap items-center gap-2">
                    <h3 class="text-sm font-bold text-ink">{{ item.position }}</h3>
                    @if (item.current) { <span class="badge-success">Current</span> }
                  </div>
                  <p class="mt-0.5 text-sm text-accent-text">{{ item.company }}@if (item.location) { · {{ item.location }} }</p>
                  <p class="mt-1 text-xs text-muted">
                    {{ item.startDate | date: 'MMM y' }} – {{ item.current ? 'Present' : ((item.endDate | date: 'MMM y') || '—') }}
                  </p>
                  @if (item.description) {
                    <p class="mt-2 line-clamp-2 text-sm leading-6 text-soft">{{ item.description }}</p>
                  }
                  @if (item.technologies.length) {
                    <div class="mt-2 flex flex-wrap gap-1.5">
                      @for (tech of item.technologies.slice(0, 5); track tech) { <span class="badge-muted">{{ tech }}</span> }
                    </div>
                  }
                </div>
                <button type="button" class="btn-danger shrink-0" (click)="deleteExperience(item.id, item.position)">Delete</button>
              </article>
            }
          </div>
        }
      } @else {
        <!-- ---------------- Blog ---------------- -->
        @if (blogService.loading()) {
          <div class="card flex min-h-[200px] items-center justify-center"><div class="spinner" role="status" aria-label="Loading posts"></div></div>
        } @else if (blogService.error()) {
          <div class="card empty-state">
            <p class="text-sm font-semibold text-ink">Failed to load posts</p>
            <p class="max-w-md text-sm text-soft">{{ blogService.error() }}</p>
            <button type="button" class="btn-ghost mt-2" (click)="loadAll()">Retry</button>
          </div>
        } @else if (blogService.posts().length === 0) {
          <div class="card empty-state"><p class="text-sm text-muted">No blog posts yet.</p></div>
        } @else {
          <div class="card overflow-hidden">
            <ul class="divide-y divide-line/10">
              @for (post of blogService.posts(); track post.id) {
                <li class="flex flex-col gap-3 px-6 py-4 sm:flex-row sm:items-center">
                  <div class="min-w-0 flex-1">
                    <div class="flex flex-wrap items-center gap-2">
                      <h3 class="truncate text-sm font-bold text-ink">{{ post.title }}</h3>
                      <span [class]="post.published ? 'badge-success' : 'badge-muted'">
                        {{ post.published ? 'Published' : 'Draft' }}
                      </span>
                    </div>
                    <p class="mt-1 truncate text-xs text-muted">
                      /{{ post.slug }} · {{ post.readingTime }} min read · {{ post.viewCount }} views
                      @if (post.publishedAt) { · {{ post.publishedAt | date: 'MMM d, y' }} }
                    </p>
                  </div>
                  <div class="flex shrink-0 gap-2">
                    <button type="button" class="btn-ghost" (click)="togglePublished(post)">
                      {{ post.published ? 'Unpublish' : 'Publish' }}
                    </button>
                    <button type="button" class="btn-danger" (click)="deletePost(post)">Delete</button>
                  </div>
                </li>
              }
            </ul>
          </div>
        }
      }
    </div>
  `
})
export class ContentComponent implements OnInit {
  readonly skillService = inject(SkillService);
  readonly experienceService = inject(ExperienceService);
  readonly blogService = inject(BlogService);

  readonly tab = signal<ContentTab>('skills');

  readonly tabs: { value: ContentTab; label: string }[] = [
    { value: 'skills', label: 'Skills' },
    { value: 'experience', label: 'Experience' },
    { value: 'blog', label: 'Blog' }
  ];

  newSkillName = '';
  newSkillCategory = '';

  readonly skillsByCategory = computed(() => {
    const groups = new Map<string, Skill[]>();
    for (const skill of this.skillService.skills()) {
      const category = skill.category?.trim() || 'Uncategorized';
      if (!groups.has(category)) groups.set(category, []);
      groups.get(category)!.push(skill);
    }
    return [...groups.entries()]
      .map(([category, skills]) => ({ category, skills }))
      .sort((a, b) => a.category.localeCompare(b.category));
  });

  ngOnInit(): void {
    this.loadAll();
  }

  loadAll(): void {
    this.skillService.loadSkills().subscribe({ error: () => undefined });
    this.experienceService.loadExperience().subscribe({ error: () => undefined });
    this.blogService.loadPosts().subscribe({ error: () => undefined });
  }

  addSkill(): void {
    const name = this.newSkillName.trim();
    if (!name) return;

    this.skillService
      .createSkill({
        name,
        category: this.newSkillCategory.trim() || 'General',
        proficiency: 70,
        isActive: true
      })
      .subscribe({
        next: () => {
          this.newSkillName = '';
          this.newSkillCategory = '';
        },
        error: err => alert(err?.error?.message ?? 'Failed to add the skill.')
      });
  }

  toggleSkill(skill: Skill): void {
    this.skillService.toggleVisibility(skill).subscribe({
      error: err => alert(err?.error?.message ?? 'Failed to update the skill.')
    });
  }

  deleteSkill(skill: Skill): void {
    if (!confirm(`Delete the skill "${skill.name}"?`)) return;
    this.skillService.deleteSkill(skill.id).subscribe({
      error: err => alert(err?.error?.message ?? 'Failed to delete the skill.')
    });
  }

  deleteExperience(id: number, title: string): void {
    if (!confirm(`Delete the experience "${title}"?`)) return;
    this.experienceService.deleteExperience(id).subscribe({
      error: err => alert(err?.error?.message ?? 'Failed to delete the experience.')
    });
  }

  togglePublished(post: BlogPost): void {
    this.blogService.togglePublished(post).subscribe({
      error: err => alert(err?.error?.message ?? 'Failed to update the post.')
    });
  }

  deletePost(post: BlogPost): void {
    if (!confirm(`Delete the post "${post.title}"?`)) return;
    this.blogService.deletePost(post.id).subscribe({
      error: err => alert(err?.error?.message ?? 'Failed to delete the post.')
    });
  }
}
