import { Component, DestroyRef, OnInit, computed, inject, linkedSignal, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule, NgForm } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EMPTY, Observable, catchError, concatMap, finalize } from 'rxjs';
import { SkillService } from '../../core/services/skill.service';
import { ExperienceService } from '../../core/services/experience.service';
import { CategoryService } from '../../core/services/category.service';
import { Skill, SkillPayload } from '../../core/models/skill.model';
import { Experience, ExperiencePayload } from '../../core/models/experience.model';
import { Category } from '../../core/models/category.model';
import { ConfirmDialogOptions, ConfirmDialogService } from '../../shared/confirm-dialog.component';
import { ToastService } from '../../shared/toast.component';
import { PaginatorComponent } from '../../shared/paginator.component';
import { EmptyStateComponent } from '../../shared/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state.component';
import { ExperienceDraft, ExperienceFormComponent } from './experience-form.component';

type ContentTab = 'skills' | 'experience' | 'categories';
interface SkillDraft {
  nameEn: string;
  nameAr: string;
  categoryEn: string;
  categoryAr: string;
  proficiency: number;
  order: number;
  iconUrl: string;
  descriptionEn: string;
  descriptionAr: string;
}

@Component({
  selector: 'app-content',
  standalone: true,
  imports: [CommonModule, FormsModule, PaginatorComponent, EmptyStateComponent, ErrorStateComponent, ExperienceFormComponent],
  templateUrl: './content.component.html'
})
export class ContentComponent implements OnInit {
  readonly skillService = inject(SkillService);
  readonly experienceService = inject(ExperienceService);
  readonly categoryService = inject(CategoryService);
  private readonly dialog = inject(ConfirmDialogService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private confirmingDelete = false;

  readonly tab = signal<ContentTab>('skills');
  readonly tabs: { value: ContentTab; label: string }[] = [
    { value: 'skills', label: 'Skills' },
    { value: 'experience', label: 'Experience' },
    { value: 'categories', label: 'Categories' }
  ];
  readonly skillsBusy = signal(false);
  readonly categoriesBusy = signal(false);
  readonly experienceBusy = signal(false);
  readonly skillGroupsPageSize = 4;
  readonly experiencePageSize = 6;

  readonly skillsByCategory = computed(() => {
    const groups = new Map<string, Skill[]>();
    for (const skill of this.skillService.skills()) {
      const category = skill.categoryEn?.trim() || 'Uncategorized';
      if (!groups.has(category)) groups.set(category, []);
      groups.get(category)!.push(skill);
    }
    return [...groups.entries()]
      .map(([category, skills]) => ({ category, skills }))
      .sort((a, b) => a.category.localeCompare(b.category));
  });
  // Writable page state automatically clamps when a load/delete shrinks the total.
  readonly skillsPage = linkedSignal<number, number>({
    source: () => this.skillsByCategory().length,
    computation: (total, previous) => this.clampPage(previous?.value ?? 1, total, this.skillGroupsPageSize)
  });
  readonly experiencePage = linkedSignal<number, number>({
    source: () => this.experienceService.experience().length,
    computation: (total, previous) => this.clampPage(previous?.value ?? 1, total, this.experiencePageSize)
  });
  readonly pagedSkillGroups = computed(() => {
    const start = (this.skillsPage() - 1) * this.skillGroupsPageSize;
    return this.skillsByCategory().slice(start, start + this.skillGroupsPageSize);
  });
  readonly pagedExperience = computed(() => {
    const start = (this.experiencePage() - 1) * this.experiencePageSize;
    return this.experienceService.experience().slice(start, start + this.experiencePageSize);
  });

  newSkillName = '';
  newSkillNameAr = '';
  newSkillCategory = '';
  newSkillCategoryAr = '';
  newSkillProficiency = 70;
  editingSkillId: number | null = null;
  editDraft: SkillDraft | null = null;
  skillFormError: string | null = null;

  newCategoryName = '';
  newCategoryNameAr = '';
  editingCategoryId: number | null = null;
  categoryDraftName = '';
  categoryDraftNameAr = '';
  categoryFormError: string | null = null;

  experienceFormVisible = false;
  editingExperienceId: number | null = null;
  expDraft: ExperienceDraft | null = null;
  experienceError: string | null = null;

  ngOnInit(): void {
    this.loadAll();
  }

  selectTab(value: ContentTab): void {
    this.tab.set(value);
    this.skillsPage.set(1);
    this.experiencePage.set(1);
  }

  loadAll(): void {
    this.reload('skills');
    this.reload('experience');
    this.reload('categories');
  }

  reload(source: ContentTab): void {
    if (this.loading(source) || this.busy(source)()) return;
    const request: Observable<unknown> = source === 'skills' ? this.skillService.loadSkills()
      : source === 'experience' ? this.experienceService.loadExperience() : this.categoryService.loadCategories();
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ error: () => undefined });
  }

  unavailable(source: ContentTab): boolean {
    const error = source === 'skills' ? this.skillService.error()
      : source === 'experience' ? this.experienceService.error() : this.categoryService.error();
    return this.busy(source)() || this.loading(source) || !!error;
  }

  addCategory(form: NgForm): void {
    if (this.unavailable('categories')) return;
    this.categoryFormError = this.categoryNamesError(this.newCategoryName, this.newCategoryNameAr);
    if (form.invalid || this.categoryFormError) { form.control.markAllAsTouched(); return; }
    this.mutate('categories', this.categoryService.createCategory({
      nameEn: this.newCategoryName.trim(), nameAr: this.newCategoryNameAr.trim(),
      order: Math.max(0, ...this.categoryService.categories().map(category => category.order)) + 1, isActive: true
    }), () => {
      this.newCategoryName = '';
      this.newCategoryNameAr = '';
      form.resetForm({ categoryNameEn: '', categoryNameAr: '' });
    }, 'Could not add the category.');
  }

  startEditingCategory(category: Category): void {
    if (this.unavailable('categories')) return;
    this.editingCategoryId = category.id;
    this.categoryDraftName = category.nameEn;
    this.categoryDraftNameAr = category.nameAr;
    this.categoryFormError = null;
  }

  cancelEditCategory(): void {
    if (this.categoriesBusy()) return;
    this.editingCategoryId = null;
    this.categoryFormError = null;
  }

  saveCategory(category: Category, form: NgForm): void {
    if (this.unavailable('categories')) return;
    this.categoryFormError = this.categoryNamesError(this.categoryDraftName, this.categoryDraftNameAr);
    if (form.invalid || this.categoryFormError) { form.control.markAllAsTouched(); return; }
    this.mutate('categories', this.categoryService.updateCategory(category.id, {
      nameEn: this.categoryDraftName.trim(), nameAr: this.categoryDraftNameAr.trim(),
      order: category.order, isActive: category.isActive
    }), () => { this.editingCategoryId = null; }, 'Could not rename the category.');
  }

  toggleCategory(category: Category): void {
    if (this.unavailable('categories')) return;
    this.mutate('categories', this.categoryService.updateCategory(category.id, {
      nameEn: category.nameEn, nameAr: category.nameAr, order: category.order, isActive: !category.isActive
    }), () => undefined, 'Could not update the category.');
  }

  moveCategory(category: Category, direction: -1 | 1): void {
    if (this.unavailable('categories')) return;
    const list = this.categoryService.categories();
    const index = list.findIndex(item => item.id === category.id);
    const target = index + direction;
    if (index < 0 || target < 0 || target >= list.length) return;
    // Snapshot both entries before the service updates/sorts its local list.
    const original = { ...list[index] };
    const neighbour = { ...list[target] };
    const request = this.categoryService.updateCategory(original.id, {
      nameEn: original.nameEn, nameAr: original.nameAr, order: neighbour.order, isActive: original.isActive
    }).pipe(
      concatMap(() => this.categoryService.updateCategory(neighbour.id, {
        nameEn: neighbour.nameEn, nameAr: neighbour.nameAr, order: original.order, isActive: neighbour.isActive
      })),
      catchError((error: unknown) => {
        this.reportMutationError('categories', error, 'Could not reorder the categories.');
        // A two-request swap is not atomic. Re-fetch truth, never pretend rollback succeeded.
        return this.categoryService.loadCategories().pipe(catchError(() => EMPTY));
      })
    );
    this.mutate('categories', request, () => undefined, 'Could not reorder the categories.');
  }

  deleteCategory(category: Category): Promise<void> {
    return this.confirmRemoval('categories', {
      title: 'Delete category?', message: `“${category.nameEn}” will be removed. Categories in use cannot be deleted.`,
      confirmLabel: 'Delete category'
    }, () => this.categoryService.deleteCategory(category.id), 'Could not delete the category.');
  }

  addSkill(form: NgForm): void {
    if (this.unavailable('skills')) return;
    this.skillFormError = this.skillFieldsError({
      nameEn: this.newSkillName, nameAr: this.newSkillNameAr,
      categoryEn: this.newSkillCategory, categoryAr: this.newSkillCategoryAr,
      proficiency: this.newSkillProficiency
    });
    if (form.invalid || this.skillFormError) { form.control.markAllAsTouched(); return; }
    this.mutate('skills', this.skillService.createSkill({
      nameEn: this.newSkillName.trim(), nameAr: this.newSkillNameAr.trim(),
      categoryEn: this.newSkillCategory.trim(), categoryAr: this.newSkillCategoryAr.trim(),
      proficiency: this.newSkillProficiency, iconUrl: null, descriptionEn: null, descriptionAr: null,
      order: Math.max(0, ...this.skillService.skills().map(skill => skill.order)) + 1, isActive: true
    }), () => {
      this.newSkillName = this.newSkillNameAr = this.newSkillCategory = this.newSkillCategoryAr = '';
      this.newSkillProficiency = 70;
      form.resetForm({ skillNameEn: '', skillNameAr: '', skillCategoryEn: '', skillCategoryAr: '', skillProficiency: 70 });
    }, 'Failed to add the skill.');
  }

  startEditSkill(skill: Skill): void {
    if (this.unavailable('skills')) return;
    this.editingSkillId = skill.id;
    this.skillFormError = null;
    this.editDraft = {
      nameEn: skill.nameEn, nameAr: skill.nameAr, categoryEn: skill.categoryEn, categoryAr: skill.categoryAr,
      proficiency: skill.proficiency, order: skill.order, iconUrl: skill.iconUrl ?? '',
      descriptionEn: skill.descriptionEn ?? '', descriptionAr: skill.descriptionAr ?? ''
    };
  }

  cancelEditSkill(): void {
    if (this.skillsBusy()) return;
    this.clearSkillEditor();
  }

  saveEditSkill(form: NgForm): void {
    if (this.unavailable('skills')) return;
    const id = this.editingSkillId;
    const draft = this.editDraft;
    if (id === null || !draft) return;
    const current = this.skillService.skills().find(skill => skill.id === id);
    if (!current) return;
    this.skillFormError = this.skillFieldsError(draft);
    if (draft.descriptionEn.length > 500 || draft.descriptionAr.length > 500 || draft.iconUrl.length > 500) {
      this.skillFormError = 'Descriptions and icon URL must not exceed 500 characters.';
    }
    if (!Number.isInteger(draft.order) || draft.order < 0) this.skillFormError = 'Display order must be a non-negative integer.';
    if (form.invalid || this.skillFormError) { form.control.markAllAsTouched(); return; }
    this.mutate('skills', this.skillService.updateSkill(id, {
      nameEn: draft.nameEn.trim(), nameAr: draft.nameAr.trim(), categoryEn: draft.categoryEn.trim(), categoryAr: draft.categoryAr.trim(),
      proficiency: draft.proficiency, iconUrl: draft.iconUrl.trim() || null,
      descriptionEn: draft.descriptionEn.trim() || null, descriptionAr: draft.descriptionAr.trim() || null,
      order: draft.order, isActive: current.isActive
    }), () => this.clearSkillEditor(), 'Failed to save the skill.');
  }

  toggleSkill(skill: Skill): void {
    if (this.unavailable('skills')) return;
    // Full-replacement PUT: retain both descriptions even if the service toggle helper omits one.
    const payload: SkillPayload = {
      nameEn: skill.nameEn, nameAr: skill.nameAr, categoryEn: skill.categoryEn, categoryAr: skill.categoryAr,
      proficiency: skill.proficiency, iconUrl: skill.iconUrl,
      descriptionEn: skill.descriptionEn, descriptionAr: skill.descriptionAr, order: skill.order, isActive: !skill.isActive
    };
    this.mutate('skills', this.skillService.updateSkill(skill.id, payload), () => undefined, 'Failed to update the skill.');
  }

  deleteSkill(skill: Skill): Promise<void> {
    return this.confirmRemoval('skills', {
      title: 'Delete skill?', message: `“${skill.nameEn}” will be removed from the skill stack.`, confirmLabel: 'Delete skill'
    }, () => this.skillService.deleteSkill(skill.id), 'Failed to delete the skill.');
  }

  startExperienceCreate(): void {
    if (this.unavailable('experience')) return;
    this.experienceFormVisible = true;
    this.editingExperienceId = null;
    this.experienceError = null;
    this.expDraft = {
      companyEn: '', companyAr: '', positionEn: '', positionAr: '', descriptionEn: '', descriptionAr: '', locationEn: '', locationAr: '',
      startDate: '', endDate: '', technologies: '', current: false,
      order: Math.max(0, ...this.experienceService.experience().map(item => item.order)) + 1, isActive: true
    };
  }

  startExperienceEdit(item: Experience): void {
    if (this.unavailable('experience')) return;
    this.experienceFormVisible = true;
    this.editingExperienceId = item.id;
    this.experienceError = null;
    this.expDraft = {
      companyEn: item.companyEn, companyAr: item.companyAr, positionEn: item.positionEn, positionAr: item.positionAr,
      descriptionEn: item.descriptionEn, descriptionAr: item.descriptionAr, locationEn: item.locationEn ?? '', locationAr: item.locationAr ?? '',
      startDate: item.startDate.slice(0, 10), endDate: item.endDate?.slice(0, 10) ?? '',
      technologies: item.technologies.join(', '), current: item.current, order: item.order, isActive: item.isActive
    };
  }

  cancelExperienceForm(): void {
    if (this.experienceBusy()) return;
    this.clearExperienceEditor();
  }

  saveExperience(payload: ExperiencePayload): void {
    if (this.unavailable('experience') || !this.expDraft) return;
    const request: Observable<unknown> = this.editingExperienceId !== null
      ? this.experienceService.updateExperience(this.editingExperienceId, payload)
      : this.experienceService.createExperience(payload);
    this.mutate('experience', request, () => this.clearExperienceEditor(), 'Failed to save the experience entry.');
  }

  deleteExperience(id: number, title: string): Promise<void> {
    return this.confirmRemoval('experience', {
      title: 'Delete experience?', message: `The “${title}” entry will be removed from the Experience page.`, confirmLabel: 'Delete entry'
    }, () => this.experienceService.deleteExperience(id), 'Failed to delete the experience.');
  }

  private mutate(source: ContentTab, request: Observable<unknown>, success: () => void, fallback: string): void {
    this.busy(source).set(true);
    this.setFormError(source, null);
    request.pipe(finalize(() => this.busy(source).set(false)), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: success,
      error: (error: unknown) => this.reportMutationError(source, error, fallback)
    });
  }

  private async confirmRemoval(source: ContentTab, options: ConfirmDialogOptions, request: () => Observable<unknown>, fallback: string): Promise<void> {
    if (this.unavailable(source) || this.confirmingDelete) return;
    this.confirmingDelete = true;
    this.busy(source).set(true);
    let confirmed = false;
    try {
      confirmed = await this.dialog.confirm(options);
    } catch (error: unknown) {
      if (!this.destroyRef.destroyed) this.reportMutationError(source, error, fallback);
    } finally {
      this.confirmingDelete = false;
      this.busy(source).set(false);
    }
    if (!confirmed || this.destroyRef.destroyed) return;
    this.mutate(source, request(), () => undefined, fallback);
  }

  private reportMutationError(source: ContentTab, error: unknown, fallback: string): void {
    const body: unknown = error instanceof HttpErrorResponse ? error.error : error;
    const message = this.domainMessage(body) || (error instanceof Error ? error.message : '') || fallback;
    this.setFormError(source, message);
    this.toast.show(message, 'error');
  }

  private domainMessage(value: unknown): string {
    if (typeof value === 'string') return value.trim();
    if (Array.isArray(value)) return value.map((entry: unknown) => this.domainMessage(entry)).filter(Boolean).join(' ');
    if (!value || typeof value !== 'object') return '';
    const record = value as Record<string, unknown>;
    if (record['errors'] && typeof record['errors'] === 'object') {
      const messages = Object.values(record['errors']).map((entry: unknown) => this.domainMessage(entry)).filter(Boolean).join(' ');
      if (messages) return messages;
    }
    for (const key of ['errorMessage', 'message', 'detail', 'title']) {
      if (typeof record[key] === 'string' && record[key].trim()) return record[key].trim();
    }
    return '';
  }

  private setFormError(source: ContentTab, message: string | null): void {
    if (source === 'skills') this.skillFormError = message;
    else if (source === 'categories') this.categoryFormError = message;
    else this.experienceError = message;
  }

  private busy(source: ContentTab) {
    return source === 'skills' ? this.skillsBusy : source === 'categories' ? this.categoriesBusy : this.experienceBusy;
  }

  private loading(source: ContentTab): boolean {
    return source === 'skills' ? this.skillService.loading() : source === 'categories' ? this.categoryService.loading() : this.experienceService.loading();
  }

  private categoryNamesError(nameEn: string, nameAr: string): string | null {
    if (!nameEn.trim() || !nameAr.trim()) return 'Both English and Arabic category names are required.';
    return nameEn.length > 100 || nameAr.length > 100 ? 'Category names must not exceed 100 characters.' : null;
  }

  private skillFieldsError(draft: Pick<SkillDraft, 'nameEn' | 'nameAr' | 'categoryEn' | 'categoryAr' | 'proficiency'>): string | null {
    if (!draft.nameEn.trim() || !draft.nameAr.trim() || !draft.categoryEn.trim() || !draft.categoryAr.trim()) {
      return 'Both English and Arabic skill names and categories are required.';
    }
    if (draft.nameEn.length > 50 || draft.nameAr.length > 50) return 'Skill names must not exceed 50 characters.';
    if (draft.categoryEn.length > 100 || draft.categoryAr.length > 100) return 'Skill categories must not exceed 100 characters.';
    if (!Number.isInteger(draft.proficiency) || draft.proficiency < 0 || draft.proficiency > 100) return 'Proficiency must be an integer from 0 to 100.';
    return null;
  }

  private clampPage(page: number, total: number, size: number): number {
    return Math.max(1, Math.min(page, Math.ceil(total / size) || 1));
  }

  private clearSkillEditor(): void {
    this.editingSkillId = null;
    this.editDraft = null;
    this.skillFormError = null;
  }

  private clearExperienceEditor(): void {
    this.experienceFormVisible = false;
    this.editingExperienceId = null;
    this.expDraft = null;
    this.experienceError = null;
  }
}
