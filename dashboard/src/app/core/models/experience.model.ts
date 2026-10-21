/** Mirrors PortfolioAPI.Core.DTOs.ExperienceDto (api/Experience, api/public/experience). */
export interface Experience {
  id: number;
  companyEn: string;
  companyAr: string;
  positionEn: string;
  positionAr: string;
  startDate: string;
  endDate: string | null;
  descriptionEn: string;
  descriptionAr: string;
  technologies: string[];
  locationEn: string | null;
  locationAr: string | null;
  current: boolean;
  order: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

/** Payload accepted by POST/PUT api/Experience (CreateExperienceDto/UpdateExperienceDto). */
export interface ExperiencePayload {
  companyEn: string;
  companyAr: string;
  positionEn: string;
  positionAr: string;
  /** ISO date/time string. */
  startDate: string;
  /** ISO date/time string, or null while still in the role. */
  endDate: string | null;
  descriptionEn: string;
  descriptionAr: string;
  technologies: string[];
  locationEn?: string | null;
  locationAr?: string | null;
  current?: boolean;
  order?: number;
  isActive?: boolean;
}
