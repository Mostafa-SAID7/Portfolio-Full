/** Mirrors PortfolioAPI.Core.DTOs.ExperienceDto (api/Experience, api/public/experience). */
export interface Experience {
  id: number;
  company: string;
  position: string;
  startDate: string;
  endDate: string | null;
  description: string;
  technologies: string[];
  location: string | null;
  current: boolean;
  order: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

/** Payload accepted by POST/PUT api/Experience (CreateExperienceDto/UpdateExperienceDto). */
export interface ExperiencePayload {
  company: string;
  position: string;
  /** ISO date/time string. */
  startDate: string;
  /** ISO date/time string, or null while still in the role. */
  endDate: string | null;
  description: string;
  technologies: string[];
  location?: string | null;
  current?: boolean;
  order?: number;
  isActive?: boolean;
}
