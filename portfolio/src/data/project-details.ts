import type { Project } from "./projects";

/**
 * Case-study presentation derived only from the API-backed Project row.
 * No static catalog, category essay templates, or detailOverrides (#41).
 * Long-form CMS fields can return later via the backend.
 */

export interface ProjectHighlight {
  label: string;
  value: string;
  hint?: string;
}

export interface ProjectFeature {
  title: string;
  description: string;
}

export interface ProcessStep {
  phase: string;
  title: string;
  description: string;
}

export interface ProjectDetailContent {
  tagline: string;
  overview: string[];
  challenge: string;
  solution: string;
  features: ProjectFeature[];
  highlights: ProjectHighlight[];
  responsibilities: string[];
  architecture: string[];
  process: ProcessStep[];
  outcomes: string[];
  role: string;
  timeline: string;
  teamSize: string;
  platforms: string[];
}

const typeRole: Record<Project["type"], string> = {
  Team: "Full Stack Engineer (team delivery)",
  Freelance: "Lead Developer (end-to-end)",
  Self: "Solo Engineer & Designer",
};

const typeTeam: Record<Project["type"], string> = {
  Team: "Cross-functional team",
  Freelance: "Solo, direct with client",
  Self: "Solo project",
};

/** Build detail content exclusively from the API project — no dummy narratives. */
export function getProjectDetail(project: Project): ProjectDetailContent {
  const highlights: ProjectHighlight[] = [];
  if (project.metrics?.length) {
    for (const m of project.metrics) {
      highlights.push({ label: m.label, value: m.value });
    }
  } else {
    if (project.category) highlights.push({ label: "Category", value: project.category });
    if (project.type) highlights.push({ label: "Engagement", value: project.type });
    if (project.status) highlights.push({ label: "Status", value: project.status });
  }

  const overview = project.description
    ? [project.description]
    : ["No additional case-study narrative is stored for this project yet."];

  const architecture = project.tech.length ? project.tech.map((t) => t) : [];

  return {
    tagline: project.title,
    overview,
    challenge: "",
    solution: "",
    features: [],
    highlights,
    responsibilities: [],
    architecture,
    process: [],
    outcomes: project.badges?.length ? project.badges.map((b) => b) : [],
    role: typeRole[project.type] ?? "Engineer",
    timeline: project.status === "In Progress" ? "Ongoing" : (project.status ?? "—"),
    teamSize: typeTeam[project.type] ?? "—",
    platforms: ["Web"],
  };
}

/** Related projects from the same API list (category, then shared tech). */
export function getRelatedProjects(project: Project, all: Project[], limit = 3): Project[] {
  const sameCategory = all.filter((p) => p.id !== project.id && p.category === project.category);
  if (sameCategory.length >= limit) return sameCategory.slice(0, limit);

  const sharesTech = all.filter(
    (p) =>
      p.id !== project.id &&
      p.category !== project.category &&
      p.tech.some((t) => project.tech.includes(t)),
  );
  return [...sameCategory, ...sharesTech].slice(0, limit);
}
