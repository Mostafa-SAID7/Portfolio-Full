/**
 * A career credential managed from the dashboard (api/v1/Credentials).
 *
 * The public portfolio renders these rows in four tabs — Experience
 * (work), Platforms (projects), Education and Certifications — so every
 * field below is shown on the site; nothing here is dashboard-only.
 */
export interface Credential {
  id: number;
  /** work | projects | education | certifications — which portfolio tab it belongs to. */
  tab: string;
  /** Short year label shown on the card (e.g. "2025"). */
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
  /** Bullet points of the expanded card. */
  highlightsEn: string[];
  highlightsAr: string[];
  /** Tech chips of the expanded card. */
  stack: string[];
  /** Public certificate/credential reference (e.g. "MS-1234"). */
  credentialId: string | null;
  statusEn: string | null;
  statusAr: string | null;
  /** Featured rows are rendered as the inverted (dark) card. */
  featured: boolean;
  order: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

/** Payload for create/update (server-managed fields are never sent). */
export interface CredentialPayload {
  tab: string;
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
  highlightsEn: string[];
  highlightsAr: string[];
  stack: string[];
  credentialId: string | null;
  statusEn: string | null;
  statusAr: string | null;
  featured: boolean;
  order: number;
  isActive: boolean;
}

/** The four portfolio tabs a credential can live in. */
export const CREDENTIAL_TABS = ['work', 'projects', 'education', 'certifications'] as const;

export type CredentialTabKey = (typeof CREDENTIAL_TABS)[number];

/** Human labels for the tab keys (the dashboard is English-only). */
export const CREDENTIAL_TAB_LABELS: Record<CredentialTabKey, string> = {
  work: 'Experience',
  projects: 'Platforms',
  education: 'Education',
  certifications: 'Certifications'
};
