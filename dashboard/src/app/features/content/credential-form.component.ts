import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import {
  CREDENTIAL_TAB_LABELS,
  CREDENTIAL_TABS,
  type CredentialPayload,
  type CredentialTabKey
} from '../../core/models/credential.model';

/** The editable shape of one credential row. */
export interface CredentialDraft {
  tab: CredentialTabKey;
  year: string;
  periodEn: string;
  periodAr: string;
  titleEn: string;
  titleAr: string;
  orgEn: string;
  orgAr: string;
  locationEn: string;
  locationAr: string;
  summaryEn: string;
  summaryAr: string;
  highlightsEn: string;
  highlightsAr: string;
  stack: string;
  credentialId: string;
  statusEn: string;
  statusAr: string;
  featured: boolean;
  order: number;
  isActive: boolean;
}

/** A blank draft for a new row. */
export function emptyCredentialDraft(order: number): CredentialDraft {
  return {
    tab: 'work',
    year: '',
    periodEn: '',
    periodAr: '',
    titleEn: '',
    titleAr: '',
    orgEn: '',
    orgAr: '',
    locationEn: '',
    locationAr: '',
    summaryEn: '',
    summaryAr: '',
    highlightsEn: '',
    highlightsAr: '',
    stack: '',
    credentialId: '',
    statusEn: '',
    statusAr: '',
    featured: false,
    order,
    isActive: true
  };
}

/** Split a comma/newline separated list into trimmed, non-empty entries. */
export function toList(value: string): string[] {
  return value
    .split(/[\n,]/)
    .map(entry => entry.trim())
    .filter(Boolean);
}

@Component({
  selector: 'app-credential-form',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './credential-form.component.html'
})
export class CredentialFormComponent {
  @Input({ required: true }) draft!: CredentialDraft;
  @Input() editing = false;
  @Input() busy = false;
  @Input() error: string | null = null;
  @Output() save = new EventEmitter<CredentialPayload>();
  @Output() cancel = new EventEmitter<void>();

  submitted = false;

  readonly tabs = CREDENTIAL_TABS;
  readonly tabLabels = CREDENTIAL_TAB_LABELS;

  validationError(): string | null {
    const d = this.draft;
    const required: [string, string, number][] = [
      ['Year', d.year, 10],
      ['Title (English)', d.titleEn, 200],
      ['Title (Arabic)', d.titleAr, 200],
      ['Period (English)', d.periodEn, 100],
      ['Period (Arabic)', d.periodAr, 100],
      ['Organisation (English)', d.orgEn, 200],
      ['Organisation (Arabic)', d.orgAr, 200],
      ['Summary (English)', d.summaryEn, 1000],
      ['Summary (Arabic)', d.summaryAr, 1000]
    ];
    for (const [label, value, max] of required) {
      if (!value.trim()) return `${label} is required.`;
      if (value.length > max) return `${label} must not exceed ${max} characters.`;
    }
    if (!Number.isInteger(d.order) || d.order < 0) return 'Display order must be a non-negative integer.';
    if (d.credentialId.length > 100) return 'Credential ID must not exceed 100 characters.';
    if (d.locationEn.length > 200 || d.locationAr.length > 200) return 'Location must not exceed 200 characters.';
    if (d.statusEn.length > 100 || d.statusAr.length > 100) return 'Status must not exceed 100 characters.';
    if (toList(d.highlightsEn).length > 12 || toList(d.highlightsAr).length > 12) {
      return 'A credential can carry at most 12 highlight points.';
    }
    if (toList(d.stack).length > 20) return 'A credential can carry at most 20 stack entries.';
    return null;
  }

  onSubmit(form: NgForm): void {
    this.submitted = true;
    const error = this.validationError();
    if (error || form.invalid) {
      form.control.markAllAsTouched();
      return;
    }
    const d = this.draft;
    this.save.emit({
      tab: d.tab,
      year: d.year.trim(),
      periodEn: d.periodEn.trim(),
      periodAr: d.periodAr.trim(),
      titleEn: d.titleEn.trim(),
      titleAr: d.titleAr.trim(),
      orgEn: d.orgEn.trim(),
      orgAr: d.orgAr.trim(),
      locationEn: d.locationEn.trim(),
      locationAr: d.locationAr.trim(),
      summaryEn: d.summaryEn.trim(),
      summaryAr: d.summaryAr.trim(),
      highlightsEn: toList(d.highlightsEn),
      highlightsAr: toList(d.highlightsAr),
      stack: toList(d.stack),
      credentialId: d.credentialId.trim() || null,
      statusEn: d.statusEn.trim() || null,
      statusAr: d.statusAr.trim() || null,
      featured: d.featured,
      order: d.order,
      isActive: d.isActive
    });
  }
}
