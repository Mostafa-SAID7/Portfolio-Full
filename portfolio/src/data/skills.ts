/** UI-agnostic icon key; the presentation layer maps it to an icon component. */
export type SkillIcon =
  "frontend" | "backend" | "database" | "architecture" | "realtime" | "devops" | "security";

export interface SkillGroup {
  title: string;
  /** Icon key resolved by the UI, keeping the data layer free of components. */
  icon: SkillIcon;
  skills: string[];
}

/**
 * Static skill groups removed (#39). Runtime data comes from PortfolioAPI
 * via fetchPortfolioSkillGroups / usePortfolioSkillGroups.
 */
export const skillGroups: SkillGroup[] = [];
