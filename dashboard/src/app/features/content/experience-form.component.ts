import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { ExperiencePayload } from '../../core/models/experience.model';

/** Date inputs deliberately hold calendar dates, not JavaScript Date objects. */
export interface ExperienceDraft {
  companyEn: string;
  companyAr: string;
  positionEn: string;
  positionAr: string;
  descriptionEn: string;
  descriptionAr: string;
  locationEn: string;
  locationAr: string;
  startDate: string;
  endDate: string;
  technologies: string;
  current: boolean;
  order: number;
  isActive: boolean;
}

function validCalendarDate(value: string): boolean {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) return false;
  const [year, month, day] = value.split('-').map(Number);
  if (year < 1 || month < 1 || month > 12 || day < 1) return false;
  const leap = year % 4 === 0 && (year % 100 !== 0 || year % 400 === 0);
  const days = [31, leap ? 29 : 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];
  return day <= days[month - 1];
}

@Component({
  selector: 'app-experience-form',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './experience-form.component.html'
})
export class ExperienceFormComponent {
  @Input({ required: true }) draft!: ExperienceDraft;
  @Input() editing = false;
  @Input() busy = false;
  @Input() error: string | null = null;
  @Output() save = new EventEmitter<ExperiencePayload>();
  @Output() cancel = new EventEmitter<void>();

  submitted = false;

  validationError(): string | null {
    const draft = this.draft;
    for (const [label, value, max] of [
      ['Company (English)', draft.companyEn, 100],
      ['Company (Arabic)', draft.companyAr, 100],
      ['Position (English)', draft.positionEn, 100],
      ['Position (Arabic)', draft.positionAr, 100],
      ['Description (English)', draft.descriptionEn, 1000],
      ['Description (Arabic)', draft.descriptionAr, 1000]
    ] as const) {
      if (!value.trim()) return `${label} is required.`;
      if (value.length > max) return `${label} must not exceed ${max} characters.`;
    }
    if (draft.locationEn.length > 100 || draft.locationAr.length > 100) {
      return 'Locations must not exceed 100 characters.';
    }
    if (!validCalendarDate(draft.startDate)) return 'A valid start date is required.';
    if (!draft.current) {
      if (!validCalendarDate(draft.endDate)) return 'A past role needs a valid end date.';
      if (draft.endDate < draft.startDate) return 'End date must be on or after the start date.';
    }
    if (this.technologies().length > 20) return 'An experience can list at most 20 technologies.';
    if (!Number.isInteger(draft.order) || draft.order < 0) return 'Display order must be a non-negative integer.';
    return null;
  }

  submit(form: NgForm): void {
    if (this.busy) return;
    this.submitted = true;
    if (form.invalid || this.validationError()) {
      form.control.markAllAsTouched();
      return;
    }
    const draft = this.draft;
    this.save.emit({
      companyEn: draft.companyEn.trim(),
      companyAr: draft.companyAr.trim(),
      positionEn: draft.positionEn.trim(),
      positionAr: draft.positionAr.trim(),
      descriptionEn: draft.descriptionEn.trim(),
      descriptionAr: draft.descriptionAr.trim(),
      locationEn: draft.locationEn.trim() || null,
      locationAr: draft.locationAr.trim() || null,
      // Never use toISOString(): converting to UTC can change the selected day.
      startDate: `${draft.startDate}T00:00:00`,
      endDate: draft.current ? null : `${draft.endDate}T00:00:00`,
      technologies: this.technologies(),
      current: draft.current,
      order: draft.order,
      isActive: draft.isActive
    });
  }

  private technologies(): string[] {
    return this.draft.technologies.split(',').map(value => value.trim()).filter(Boolean);
  }
}
