/** Mirrors PortfolioAPI.Core.DTOs.ProjectDto (api/Projects, api/public/projects). */
export interface ProjectMetric {
  label: string;
  value: string;
}

/** Mirrors PortfolioAPI.Core.DTOs.ProjectDto (api/Projects, api/public/projects). */
export interface Project {
  id: number;
  title: string;
  description: string;
  imageUrl: string | null;
  technologies: string[];
  githubUrl: string | null;
  liveUrl: string | null;
  featured: boolean;
  order: number;
  isActive: boolean;
  /** Editorial fields consumed by the public portfolio (filters, chips, badges). */
  category: string | null;
  client: string | null;
  type: string | null;
  database: string | null;
  status: string | null;
  badges: string[];
  metrics: ProjectMetric[];
  gradient: string | null;
  createdAt: string;
  updatedAt: string | null;
}

/** Payload accepted by POST/PUT api/Projects. */
export interface ProjectPayload {
  title: string;
  description: string;
  imageUrl?: string | null;
  technologies?: string[];
  githubUrl?: string | null;
  liveUrl?: string | null;
  featured?: boolean;
  order?: number;
  isActive?: boolean;
  category?: string | null;
  client?: string | null;
  type?: string | null;
  database?: string | null;
  status?: string | null;
  badges?: string[];
  metrics?: ProjectMetric[];
  gradient?: string | null;
}
