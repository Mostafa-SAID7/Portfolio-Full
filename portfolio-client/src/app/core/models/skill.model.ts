export interface Skill {
  id: number;
  name: string;
  category: string;
  proficiency: number; // 1-100
  iconUrl?: string;
  description?: string;
}

export interface SkillCategory {
  id: string;
  name: string;
  skills: Skill[];
}