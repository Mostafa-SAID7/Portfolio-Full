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
