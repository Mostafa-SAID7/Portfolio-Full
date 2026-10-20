/** Mirrors PortfolioAPI.Core.DTOs.SkillDto (api/Skills, api/public/skills). */
export interface Skill {
  id: number;
  NameEn: string;
  NameAr: string;
  CategoryEn: string;
  CategoryAr: string;
  /** 0-100. */
  Proficiency: number;
  IconUrl: string | null;
  DescriptionEn: string | null;
  DescriptionAr: string | null;
  order: number;
  IsActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

/** Payload accepted by POST/PUT api/Skills (CreateSkillDto/UpdateSkillDto on the API). */
export interface SkillPayload {
  NameEn: string;
  NameAr: string;
  CategoryEn: string;
  CategoryAr: string;
  Proficiency: number;
  IconUrl?: string | null;
  DescriptionEn?: string | null;
  DescriptionAr?: string | null;
  Order?: number;
  IsActive?: boolean;
}
