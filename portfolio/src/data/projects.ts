export type ProjectCategory =
  | "Clothing"
  | "Beauty"
  | "Jewelry"
  | "Furniture"
  | "Dashboard"
  | "Web3"
  | "Sports"
  | "Tech Accessories"
  | "Backend"
  /** Projects from the API/dashboard without a narrower category. */
  | "Other";

export interface Project {
  id: string;
  /** @deprecated Removed with static catalog (#39). Kept optional for type compat. */
  metaId?: string;
  /** Numeric id of the API row (legacy "api-<id>" links resolve to it). */
  apiId?: number;
  title: string;
  description: string;
  category: ProjectCategory;
  client?: string;
  type: "Team" | "Freelance" | "Self";
  tech: string[];
  database?: string;
  status?: "In Progress" | "Completed" | "Deployed";
  badges?: string[];
  live: string;
  github?: string;
  image?: string;
  metrics?: { label: string; value: string }[];
  gradient?: string;
}

/**
 * Static catalog removed (#39). Content comes from PortfolioAPI only.
 * Empty array retained so any leftover import does not resurrect dummy rows.
 */
export const projects: Project[] = [];

/** Prefer API categories via usePortfolioCategories(); kept for type unions. */
export const projectFilters: (ProjectCategory | "All")[] = [
  "All",
  "Clothing",
  "Beauty",
  "Jewelry",
  "Furniture",
  "Dashboard",
  "Web3",
  "Sports",
  "Tech Accessories",
  "Backend",
  "Other",
];
