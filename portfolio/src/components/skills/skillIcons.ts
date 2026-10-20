import { Code, Cpu, Database, GitBranch, Layers, Lock, Radio, type LucideIcon } from "lucide-react";
import type { SkillIcon } from "@/data";

/**
 * Domain mapping for skill group icons.
 * Keeps presentation-layer icon decisions centralized and reusable across routes and preview sections.
 */
export const SKILL_ICONS: Record<SkillIcon, LucideIcon> = {
  frontend: Cpu,
  backend: Code,
  database: Database,
  architecture: Layers,
  realtime: Radio,
  devops: GitBranch,
  security: Lock,
};

export function getSkillIcon(iconKey: SkillIcon | string): LucideIcon {
  return (SKILL_ICONS as Record<string, LucideIcon>)[iconKey] ?? Code;
}
