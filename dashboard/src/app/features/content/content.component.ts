import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';
import { SkillService } from '../../core/services/skill.service';
import { ExperienceService } from '../../core/services/experience.service';
import { Skill } from '../../core/models/skill.model';
import { Experience, ExperiencePayload } from '../../core/models/experience.model';
import { ConfirmDialogService } from '../../shared/confirm-dialog.component';
import { PaginatorComponent } from '../../shared/paginator.component';

type ContentTab = 'skills' | 'experience';

const SKILL_GROUPS_PER_PAGE = 4;
const EXPERIENCE_PER_PAGE = 6;

@Component({
  selector: 'app-content',
  standalone: true,
  imports: [CommonModule, FormsModule, PaginatorComponent],
  template: `
    <div class="space-y-6">
      <div class="flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
        <div>
          <p class="eyebrow mb-2">Site content</p>
          <h2 class="font-display text-3xl font-semibold tracking-[-0.05em] text-ink">Content</h2>
          <p class="mt-2 text-sm text-soft">Skills and experience managed through the Portfolio API.</p>
        </div>
        <div class="flex items-center gap-1 rounded-xl border border-line/10 bg-surface p-1" role="tablist" aria-label="Content type">
          @for (item of tabs; track item.value) {
            <button
              type="button"
              role="tab"
              class="segmented"
              [class.active]="tab() === item.value"
              [attr.aria-selected]="tab() === item.value"
              (click)="selectTab(item.value)"
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
            <div class="sm:w-32">
              <label for="skillProficiency" class="field-label">Proficiency %</label>
              <input id="skillProficiency" type="number" min="0" max="100" class="input" [(ngModel)]="newSkillProficiency" name="skillProficiency" />
            </div>
            <button type="submit" class="btn-primary sm:mb-0.5" [disabled]="!newSkillName.trim()">Add</button>
          </form>

          @if (skillsByCategory().length === 0) {
            <div class="card empty-state"><p class="text-sm text-muted">No skills yet — add your first one above.</p></div>
          } @else {
            @for (group of pagedSkillGroups(); track group.category) {
              <section class="card overflow-hidden">
                <div class="border-b border-line/10 px-6 py-4">
                  <h3 class="font-display text-lg font-semibold text-ink">{{ group.category }}</h3>
                </div>
                <ul class="divide-y divide-line/10">
                  @for (skill of group.skills; track skill.id) {
                    <li class="flex items-center gap-4 px-6 py-3.5">
                      @if (editingSkillId === skill.id && editDraft) {
                        <!-- Inline editor: all content fields the API accepts. -->
                        <div class="min-w-0 flex-1 space-y-3">
                          <div class="grid gap-3 sm:grid-cols-2">
                            <div>
                              <label class="field-label" [for]="'edit-name-' + skill.id">Name</label>
                              <input [id]="'edit-name-' + skill.id" type="text" class="input" [(ngModel)]="editDraft.name" />
                            </div>
                            <div>
                              <label class="field-label" [for]="'edit-cat-' + skill.id">Category</label>
                              <input [id]="'edit-cat-' + skill.id" type="text" class="input" [(ngModel)]="editDraft.category" />
                            </div>
                            <div>
                              <label class="field-label" [for]="'edit-prof-' + skill.id">Proficiency (0–100)</label>
                              <input [id]="'edit-prof-' + skill.id" type="number" min="0" max="100" class="input" [(ngModel)]="editDraft.proficiency" />
                            </div>
                            <div>
                              <label class="field-label" [for]="'edit-order-' + skill.id">Display order</label>
                              <input [id]="'edit-order-' + skill.id" type="number" min="0" class="input" [(ngModel)]="editDraft.order" />
                            </div>
                            <div class="sm:col-span-2">
                              <label class="field-label" [for]="'edit-icon-' + skill.id">Icon URL</label>
                              <input [id]="'edit-icon-' + skill.id" type="text" class="input" [(ngModel)]="editDraft.iconUrl" placeholder="Optional" />
                            </div>
                            <div class="sm:col-span-2">
                              <label class="field-label" [for]="'edit-desc-' + skill.id">Description</label>
                              <input [id]="'edit-desc-' + skill.id" type="text" class="input" [(ngModel)]="editDraft.description" placeholder="Optional — max 500 characters" />
                            </div>
                          </div>
                          @if (skillFormError) {
                            <p class="text-xs text-danger" role="alert">{{ skillFormError }}</p>
                          }
                          <div class="flex justify-end gap-2">
                            <button type="button" class="btn-ghost" (click)="cancelEditSkill()">Cancel</button>
                            <button
                              type="button"
                              class="btn-primary"
                              (click)="saveEditSkill()"
                              [disabled]="!editDraft.name.trim() || !editDraft.category.trim()"
                            >
                              Save changes
                            </button>
                          </div>
                        </div>
                      } @else {
                        <div class="min-w-0 flex-1">
                          <p class="text-sm font-semibold" [class]="skill.isActive ? 'text-ink' : 'text-muted line-through'">{{ skill.name }}</p>
                          <div class="mt-1.5 h-1.5 w-40 overflow-hidden rounded-full bg-line/10">
                            <div class="h-full rounded-full bg-accent" [style.width.%]="skill.proficiency"></div>
                          </div>
                        </div>
                        <span class="hidden text-xs text-muted sm:block">{{ skill.proficiency }}%</span>
                        <button type="button" class="btn-ghost" (click)="startEditSkill(skill)">Edit</button>
                        <button type="button" class="btn-ghost" (click)="toggleSkill(skill)">
                          {{ skill.isActive ? 'Hide' : 'Show' }}
                        </button>
                        <button type="button" class="icon-btn hover:border-danger/40 hover:text-danger" (click)="deleteSkill(skill)" [attr.aria-label]="'Delete ' + skill.name">
                          <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M19 7l-.867 12.142A2 2 0 0 1 16.138 21H7.862a2 2 0 0 1-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 0 0-1-1h-4a1 1 0 0 0-1 1v3M4 7h16"/></svg>
                        </button>
                      }
                    </li>
                  }
                </ul>
              </section>
            }

            <app-paginator
              [totalItems]="skillsByCategory().length"
              [pageSize]="skillGroupsPageSize"
              [page]="skillsPage()"
              (pageChange)="skillsPage.set($event)"
            />
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
        } @else {
          <!-- Toolbar -->
          <div class="flex flex-col justify-between gap-3 sm:flex-row sm:items-center">
            <p class="text-sm text-soft">{{ experienceService.experience().length }} entries — ordered by display order, then start date.</p>
            @if (!experienceFormVisible) {
              <button type="button" class="btn-primary shrink-0" (click)="startExperienceCreate()">
                <span class="text-lg leading-none">+</span> New experience
              </button>
            }
          </div>

          <!-- Create / edit form (shared) -->
          @if (experienceFormVisible && expDraft) {
            <form class="card space-y-5 p-5" (ngSubmit)="saveExperience()" novalidate>
              <div class="flex items-center justify-between">
                <h3 class="font-display text-lg font-semibold tracking-[-0.02em] text-ink">
                  {{ editingExperienceId ? 'Edit experience' : 'New experience' }}
                </h3>
                @if (editingExperienceId) { <span class="badge-accent">Editing</span> }
              </div>
              @if (experienceError) {
                <div role="alert" class="rounded-xl border border-danger/30 bg-danger/10 p-3 text-sm text-danger">{{ experienceError }}</div>
              }
              <div class="grid gap-4 sm:grid-cols-2">
                <div>
                  <label for="exp-position" class="field-label">Position <span class="text-danger">*</span></label>
                  <input id="exp-position" type="text" class="input" [(ngModel)]="expDraft.position" name="expPosition" placeholder="Senior Full Stack Engineer" />
                </div>
                <div>
                  <label for="exp-company" class="field-label">Company <span class="text-danger">*</span></label>
                  <input id="exp-company" type="text" class="input" [(ngModel)]="expDraft.company" name="expCompany" placeholder="Freelance / Company" />
                </div>
                <div>
                  <label for="exp-location" class="field-label">Location</label>
                  <input id="exp-location" type="text" class="input" [(ngModel)]="expDraft.location" name="expLocation" placeholder="Remote" />
                </div>
                <div class="grid grid-cols-2 gap-3">
                  <div>
                    <label for="exp-start" class="field-label">Start date <span class="text-danger">*</span></label>
                    <input id="exp-start" type="date" class="input" [(ngModel)]="expDraft.startDate" name="expStart" />
                  </div>
                  <div>
                    <label for="exp-end" class="field-label">End date</label>
                    <input id="exp-end" type="date" class="input" [(ngModel)]="expDraft.endDate" name="expEnd" [disabled]="expDraft.current" />
                  </div>
                </div>
                <div class="sm:col-span-2">
                  <label for="exp-desc" class="field-label">Description <span class="text-danger">*</span></label>
                  <textarea id="exp-desc" rows="3" class="input resize-y" [(ngModel)]="expDraft.description" name="expDescription" placeholder="What you built and owned in this role..."></textarea>
                </div>
                <div class="sm:col-span-2">
                  <label for="exp-tech" class="field-label">Technologies</label>
                  <input id="exp-tech" type="text" class="input" [(ngModel)]="expDraft.technologies" name="expTechnologies" placeholder=".NET, Angular, SQL Server" />
                  <p class="mt-2 text-xs text-muted">Separate with commas (max 20).</p>
                </div>
                <div>
                  <label for="exp-order" class="field-label">Display order</label>
                  <input id="exp-order" type="number" min="0" class="input" [(ngModel)]="expDraft.order" name="expOrder" />
                </div>
                <div class="flex items-end gap-5 pb-2">
                  <label class="flex cursor-pointer items-center gap-2 text-sm text-ink">
                    <input type="checkbox" [(ngModel)]="expDraft.current" name="expCurrent" class="h-4 w-4 accent-[rgb(var(--accent))]" />
                    Current role
                  </label>
                  <label class="flex cursor-pointer items-center gap-2 text-sm text-ink">
                    <input type="checkbox" [(ngModel)]="expDraft.isActive" name="expIsActive" class="h-4 w-4 accent-[rgb(var(--accent))]" />
                    Visible
                  </label>
                </div>
              </div>
              <div class="flex justify-end gap-3 border-t border-line/10 pt-4">
                <button type="button" class="btn-ghost" (click)="cancelExperienceForm()">Cancel</button>
                <button
                  type="submit"
                  class="btn-primary"
                  [disabled]="!expDraft.position.trim() || !expDraft.company.trim() || !expDraft.description.trim() || !expDraft.startDate"
                >
                  {{ editingExperienceId ? 'Save changes' : 'Create experience' }}
                </button>
              </div>
            </form>
          }

          @if (experienceService.experience().length === 0) {
            @if (!experienceFormVisible) {
              <div class="card empty-state">
                <p class="text-sm text-muted">No experience entries yet.</p>
                <button type="button" class="btn-primary mt-2" (click)="startExperienceCreate()">New experience</button>
              </div>
            }
          } @else {
            <div class="space-y-3">
              @for (item of pagedExperience(); track item.id) {
                <article class="card-raised flex flex-col gap-3 p-5 sm:flex-row sm:items-start">
                  <div class="min-w-0 flex-1">
                    <div class="flex flex-wrap items-center gap-2">
                      <h3 class="text-sm font-bold text-ink">{{ item.position }}</h3>
                      @if (item.current) { <span class="badge-success">Current</span> }
                      @if (!item.isActive) { <span class="badge-muted">Hidden</span> }
                    </div>
                    <p class="mt-0.5 text-sm text-accent-text">{{ item.company }}@if (item.location) { — {{ item.location }} }</p>
                    <p class="mt-1 text-xs text-muted">
                      {{ item.startDate | date: 'MMM y' }} — {{ item.current ? 'Present' : ((item.endDate | date: 'MMM y') || '—') }}
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
                  <div class="flex shrink-0 gap-2">
                    <button type="button" class="btn-ghost" (click)="startExperienceEdit(item)">Edit</button>
                    <button type="button" class="btn-danger" (click)="deleteExperience(item.id, item.position)">Delete</button>
                  </div>
                </article>
              }
            </div>

            <app-paginator
              [totalItems]="experienceService.experience().length"
              [pageSize]="experiencePageSize"
              [page]="experiencePage()"
              (pageChange)="experiencePage.set($event)"
            />
          }
        }
      }
    </div>
  `
})
export class ContentComponent implements OnInit {
  readonly skillService = inject(SkillService);
  readonly experienceService = inject(ExperienceService);
  private readonly dialog = inject(ConfirmDialogService);

  readonly tab = signal<ContentTab>('skills');

  readonly skillGroupsPageSize = SKILL_GROUPS_PER_PAGE;
  readonly experiencePageSize = EXPERIENCE_PER_PAGE;

  readonly skillsPage = signal(1);
  readonly experiencePage = signal(1);

  readonly tabs: { value: ContentTab; label: string }[] = [
    { value: 'skills', label: 'Skills' },
    { value: 'experience', label: 'Experience' }
  ];

  newSkillName = '';
  newSkillCategory = '';
  newSkillProficiency = 70;

  /** Inline skill editor state. */
  editingSkillId: number | null = null;
  editDraft: {
    name: string;
    category: string;
    proficiency: number;
    order: number;
    iconUrl: string;
    description: string;
  } | null = null;
  skillFormError: string | null = null;

  /** Shared create/edit experience form state. */
  experienceFormVisible = false;
  editingExperienceId: number | null = null;
  expDraft: {
    position: string;
    company: string;
    location: string;
    /** yyyy-MM-dd (input[type=date]) */
    startDate: string;
    /** yyyy-MM-dd, empty while current or unknown */
    endDate: string;
    description: string;
    technologies: string;
    order: number;
    current: boolean;
    isActive: boolean;
  } | null = null;
  experienceError: string | null = null;

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

  readonly pagedSkillGroups = computed(() => {
    const start = (this.skillsPage() - 1) * this.skillGroupsPageSize;
    return this.skillsByCategory().slice(start, start + this.skillGroupsPageSize);
  });

  readonly pagedExperience = computed(() => {
    const start = (this.experiencePage() - 1) * this.experiencePageSize;
    return this.experienceService.experience().slice(start, start + this.experiencePageSize);
  });

  ngOnInit(): void {
    this.loadAll();
  }

  selectTab(value: ContentTab): void {
    this.tab.set(value);
    this.skillsPage.set(1);
    this.experiencePage.set(1);
  }

  loadAll(): void {
    this.skillService.loadSkills().subscribe({ error: () => undefined });
    this.experienceService.loadExperience().subscribe({ error: () => undefined });
  }

  addSkill(): void {
    const name = this.newSkillName.trim();
    if (!name) return;

    this.skillService
      .createSkill({
        name,
        category: this.newSkillCategory.trim() || 'General',
        proficiency: Math.min(100, Math.max(0, Number(this.newSkillProficiency) || 0)),
        isActive: true
      })
      .subscribe({
        next: () => {
          this.newSkillName = '';
          this.newSkillCategory = '';
          this.newSkillProficiency = 70;
        },
        error: err => alert(err?.error?.message ?? 'Failed to add the skill.')
      });
  }

  // ---------------- Inline skill editor ----------------

  startEditSkill(skill: Skill): void {
    this.editingSkillId = skill.id;
    this.skillFormError = null;
    this.editDraft = {
      name: skill.name,
      category: skill.category,
      proficiency: skill.proficiency,
      order: skill.order,
      iconUrl: skill.iconUrl ?? '',
      description: skill.description ?? ''
    };
  }

  cancelEditSkill(): void {
    this.editingSkillId = null;
    this.editDraft = null;
    this.skillFormError = null;
  }

  saveEditSkill(): void {
    const id = this.editingSkillId;
    const draft = this.editDraft;
    if (id === null || !draft) return;

    this.skillFormError = null;
    // Preserve the current visibility flag — the editor doesn't expose it.
    const current = this.skillService.skills().find(skill => skill.id === id);

    this.skillService
      .updateSkill(id, {
        name: draft.name.trim(),
        category: draft.category.trim(),
        proficiency: Math.min(100, Math.max(0, Number(draft.proficiency) || 0)),
        iconUrl: draft.iconUrl.trim() || null,
        description: draft.description.trim() || null,
        order: Number(draft.order) || 0,
        isActive: current?.isActive ?? true
      })
      .subscribe({
        next: () => this.cancelEditSkill(),
        error: err => {
          this.skillFormError = err?.error?.message ?? 'Failed to save the skill.';
        }
      });
  }

  // ---------------- Experience create/edit ----------------

  startExperienceCreate(): void {
    this.experienceFormVisible = true;
    this.editingExperienceId = null;
    this.experienceError = null;
    this.expDraft = {
      position: '',
      company: '',
      location: '',
      startDate: '',
      endDate: '',
      description: '',
      technologies: '',
      order: this.experienceService.experience().length,
      current: false,
      isActive: true
    };
  }

  startExperienceEdit(item: Experience): void {
    this.experienceFormVisible = true;
    this.editingExperienceId = item.id;
    this.experienceError = null;
    this.expDraft = {
      position: item.position,
      company: item.company,
      location: item.location ?? '',
      startDate: item.startDate ? item.startDate.slice(0, 10) : '',
      endDate: item.endDate ? item.endDate.slice(0, 10) : '',
      description: item.description,
      technologies: item.technologies.join(', '),
      order: item.order,
      current: item.current,
      isActive: item.isActive
    };
  }

  cancelExperienceForm(): void {
    this.experienceFormVisible = false;
    this.editingExperienceId = null;
    this.expDraft = null;
    this.experienceError = null;
  }

  saveExperience(): void {
    const draft = this.expDraft;
    if (!draft || !draft.startDate) return;

    // Date-only values: keep them as local calendar dates (no toISOString() —
    // UTC conversion shifts the day in non-UTC timezones and drifts on re-edit).
    const payload: ExperiencePayload = {
      company: draft.company.trim(),
      position: draft.position.trim(),
      startDate: `${draft.startDate}T00:00:00`,
      endDate: draft.current || !draft.endDate ? null : `${draft.endDate}T00:00:00`,
      description: draft.description.trim(),
      technologies: draft.technologies
        .split(',')
        .map(item => item.trim())
        .filter(Boolean),
      location: draft.location.trim() || null,
      current: draft.current,
      order: Number(draft.order) || 0,
      isActive: draft.isActive
    };

    const request$: Observable<unknown> =
      this.editingExperienceId !== null
        ? this.experienceService.updateExperience(this.editingExperienceId, payload)
        : this.experienceService.createExperience(payload);

    request$.subscribe({
      next: () => this.cancelExperienceForm(),
      error: err => {
        this.experienceError = err?.error?.message ?? 'Failed to save the experience entry.';
      }
    });
  }

  toggleSkill(skill: Skill): void {
    this.skillService.toggleVisibility(skill).subscribe({
      error: err => alert(err?.error?.message ?? 'Failed to update the skill.')
    });
  }

  async deleteSkill(skill: Skill): Promise<void> {
    const confirmed = await this.dialog.confirm({
      title: 'Delete skill?',
      message: `“${skill.name}” will be removed from the skill stack.`,
      confirmLabel: 'Delete skill'
    });
    if (!confirmed) return;

    this.skillService.deleteSkill(skill.id).subscribe({
      error: err => alert(err?.error?.message ?? 'Failed to delete the skill.')
    });
  }

  async deleteExperience(id: number, title: string): Promise<void> {
    const confirmed = await this.dialog.confirm({
      title: 'Delete experience?',
      message: `The “${title}” entry will be removed from the Experience page.`,
      confirmLabel: 'Delete entry'
    });
    if (!confirmed) return;

    this.experienceService.deleteExperience(id).subscribe({
      error: err => alert(err?.error?.message ?? 'Failed to delete the experience.')
    });
  }
}
