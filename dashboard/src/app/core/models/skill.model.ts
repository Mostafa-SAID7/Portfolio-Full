/** Mirrors PortfolioAPI.Core.DTOs.SkillDto (api/Skills, api/public/skills). */
export interface Skill {
  id: number;
  name: string;
  category: string;
  /** 0-100. */
  proficiency: number;
  iconUrl: string | null;
  description: string | null;
  order: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

/** Payload accepted by POST/PUT api/Skills (entity binding on the API). */
export interface SkillPayload {
  id?: number;
  name: string;
  category: string;
  proficiency?: number;
  iconUrl?: string | null;
  description?: string | null;
  order?: number;
  isActive?: boolean;
}
