/**
 * Data module index — types and empty placeholders only.
 * Runtime portfolio content = PortfolioAPI (`@/lib/content`). Do not reintroduce static catalogs (#39).
 */

export { projects, projectFilters, type Project, type ProjectCategory } from "./projects";
export { skillGroups, type SkillIcon, type SkillGroup } from "./skills";
export {
  credentials,
  credentialTabs,
  type CredentialItem,
  type CredentialTab,
  type Bilingual,
  type BilingualList,
} from "./credentials";
