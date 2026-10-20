/**
 * Credential types for the portfolio timeline.
 * Static credential rows removed (#39) — content comes from PortfolioAPI only.
 */

export type CredentialTab = "work" | "projects" | "education" | "certifications";

export interface Bilingual {
  en: string;
  ar: string;
}

export interface BilingualList {
  en: string[];
  ar: string[];
}

export interface CredentialItem {
  id: string;
  tab: CredentialTab;
  year: string;
  period: Bilingual;
  title: Bilingual;
  org: Bilingual;
  location: Bilingual;
  summary: Bilingual;
  highlights: BilingualList;
  stack: string[];
  credentialId?: string;
  status?: Bilingual;
  featured?: boolean;
  url?: string;
}

/** Empty: do not import for content. Use usePortfolioCredentials(). */
export const credentials: CredentialItem[] = [];

/** Stable tab IDs; visible labels are translated by the i18n layer. */
export const credentialTabs: CredentialTab[] = ["work", "projects", "education", "certifications"];
