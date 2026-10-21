/** Mirrors PortfolioAPI.Core.DTOs.SkillDto (api/Skills, api/public/skills) — camelCase JSON. */
export interface Skill {
  id: number;
  nameEn: string;
  nameAr: string;
  categoryEn: string;
  categoryAr: string;
  /** 0-100. */
  proficiency: number;
  iconUrl: string | null;
  descriptionEn: string | null;
  descriptionAr: string | null;
  order: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

/** Payload accepted by POST/PUT api/Skills (CreateSkillDto/UpdateSkillDto on the API). */
export interface SkillPayload {
  nameEn: string;
  nameAr: string;
  categoryEn: string;
  categoryAr: string;
  proficiency: number;
  iconUrl?: string | null;
  descriptionEn?: string | null;
  descriptionAr?: string | null;
  order?: number;
  isActive?: boolean;
}
