/**
 * Centralized content layer: the portfolio reads projects, skills and career
 * credentials from the PortfolioAPI (`/api/public/*`) — the single source of
 * truth shared with the admin dashboard — and falls back to the bundled
 * `src/data` content whenever the API is unreachable, so the site always
 * renders.
 *
 * The API owns all editable content, including the editorial project fields
 * (category, client, type, database, status, badges, metrics, gradient) and
 * the bilingual credentials behind the Experience page. The local dataset is
 * only a resilience fallback (and the source of the deep-link project ids and
 * case-study narratives, which stay stable regardless of the API).
 *
 * Dev: fetches `http://localhost:5039/api/public/*` directly (the API allows
 * any origin via CORS, so no dev proxy is involved). Prod: same-origin
 * `/api/public/*`, or set `VITE_API_BASE_URL` to an absolute API origin.
 */
import { useQuery } from "@tanstack/react-query";
import {
  credentials as localCredentials,
  projects as localProjects,
  skillGroups as localSkillGroups,
  type CredentialItem,
  type CredentialTab,
} from "@/data";
import type { Project } from "@/data";
import type { SkillIcon } from "@/data/skills";

const API_BASE = (import.meta.env["VITE_API_BASE_URL"] as string | undefined)?.replace(/\/+$/, "")
  // Dev: talk straight to the local PortfolioAPI on :5039 — it serves
  // `Access-Control-Allow-Origin: *`, so no dev proxy is needed.
  // Prod: same-origin `/api/public/*`, or an absolute origin via the env var.
  ?? (import.meta.env.DEV ? "http://localhost:5039" : "");
const FETCH_TIMEOUT_MS = 4_000;
const STALE_TIME_MS = 5 * 60_000;

/** Shape of PortfolioAPI.Core.DTOs.ProjectDto (camelCase JSON). */
interface ApiProject {
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
  /** Editorial fields owned by the backend/dashboard. */
  category: string | null;
  client: string | null;
  type: string | null;
  database: string | null;
  status: string | null;
  badges: string[] | null;
  metrics: { label: string; value: string }[] | null;
  gradient: string | null;
}

/** Shape of PortfolioAPI.Core.DTOs.SkillDto (camelCase JSON). */
interface ApiSkill {
  id: number;
  name: string;
  category: string;
  proficiency: number;
  order: number;
  isActive: boolean;
}

/** Shape of PortfolioAPI.Core.DTOs.CredentialDto (camelCase JSON). */
interface ApiCredential {
  id: number;
  tab: string;
  year: string;
  periodEn: string;
  periodAr: string;
  titleEn: string;
  titleAr: string;
  orgEn: string;
  orgAr: string;
  locationEn: string;
  locationAr: string;
  summaryEn: string;
  summaryAr: string;
  highlightsEn: string[];
  highlightsAr: string[];
  stack: string[];
  credentialId: string | null;
  statusEn: string | null;
  statusAr: string | null;
  featured: boolean;
  order: number;
  isActive: boolean;
}

/** GET a public endpoint; `null` on any failure (API down, timeout, non-2xx). */
async function getJson<T>(path: string): Promise<T | null> {
  try {
    const response = await fetch(`${API_BASE}/api/public/${path}`, {
      headers: { accept: "application/json" },
      signal: AbortSignal.timeout(FETCH_TIMEOUT_MS),
    });
    if (!response.ok) return null;
    return (await response.json()) as T;
  } catch {
    return null;
  }
}

const normalize = (value: string) =>
  value.toLowerCase().replace(/[^a-z0-9]+/g, " ").trim();

/** API project → portfolio Project (API values first, local dataset as fallback). */
function toPortfolioProject(dto: ApiProject): Project {
  const meta = localProjects.find((p) => normalize(p.title) === normalize(dto.title));

  const draft = {
    id: meta?.id ?? `api-${dto.id}`,
    title: dto.title,
    description: dto.description,
    category: dto.category ?? meta?.category ?? "Other",
    type: dto.type ?? meta?.type ?? "Self",
    tech: dto.technologies ?? [],
    live: dto.liveUrl || "#",
    client: dto.client ?? meta?.client,
    database: dto.database ?? meta?.database,
    status: dto.status ?? meta?.status,
    badges: dto.badges?.length ? dto.badges : (meta?.badges ?? (dto.featured ? ["Featured"] : undefined)),
    github: dto.githubUrl ?? undefined,
    image: dto.imageUrl ?? undefined,
    metrics: dto.metrics?.length ? dto.metrics : meta?.metrics,
    gradient: dto.gradient ?? meta?.gradient,
  };

  // The cast reconciles optional fields with `exactOptionalPropertyTypes`
  // (draft fields are explicitly `undefined` when metadata is missing).
  return draft as Project;
}

export async function fetchPortfolioProjects(): Promise<Project[]> {
  const data = await getJson<ApiProject[]>("projects");
  if (!data || data.length === 0) return localProjects;
  return data.map(toPortfolioProject);
}

/** Category title → icon key (the UI owns the actual icon components). */
const ICON_BY_KEYWORD: ReadonlyArray<readonly [keyword: string, icon: SkillIcon]> = [
  ["front", "frontend"],
  ["back", "backend"],
  ["language", "backend"],
  ["framework", "backend"],
  ["database", "database"],
  ["architect", "architecture"],
  ["real-time", "realtime"],
  ["realtime", "realtime"],
  ["devops", "devops"],
  ["cloud", "devops"],
  ["security", "security"],
  ["testing", "security"],
];

function iconForCategory(category: string): SkillIcon {
  const lower = category.toLowerCase();
  return ICON_BY_KEYWORD.find(([keyword]) => lower.includes(keyword))?.[1] ?? "architecture";
}

/** API skills → the portfolio's grouped skill-chip layout. */
export async function fetchPortfolioSkillGroups(): Promise<typeof localSkillGroups> {
  const data = await getJson<ApiSkill[]>("skills");
  if (!data || data.length === 0) return localSkillGroups;

  const groups = new Map<string, { icon: SkillIcon; skills: string[] }>();
  for (const skill of [...data].sort((a, b) => a.order - b.order)) {
    if (!skill.isActive) continue;
    let group = groups.get(skill.category);
    if (!group) {
      group = { icon: iconForCategory(skill.category), skills: [] };
      groups.set(skill.category, group);
    }
    group.skills.push(skill.name);
  }

  return [...groups.entries()].map(([title, group]) => ({ title, ...group }));
}

/** API credential → the portfolio's bilingual CredentialItem. */
function toCredentialItem(c: ApiCredential): CredentialItem {
  const draft = {
    id: `api-${c.id}`,
    tab: c.tab as CredentialTab,
    year: c.year,
    period: { en: c.periodEn, ar: c.periodAr },
    title: { en: c.titleEn, ar: c.titleAr },
    org: { en: c.orgEn, ar: c.orgAr },
    location: { en: c.locationEn ?? "", ar: c.locationAr ?? "" },
    summary: { en: c.summaryEn ?? "", ar: c.summaryAr ?? "" },
    highlights: { en: c.highlightsEn ?? [], ar: c.highlightsAr ?? [] },
    stack: c.stack ?? [],
    credentialId: c.credentialId ?? undefined,
    status: c.statusEn ? { en: c.statusEn, ar: c.statusAr ?? c.statusEn } : undefined,
    featured: c.featured,
  };
  return draft as CredentialItem;
}

export async function fetchPortfolioCredentials(): Promise<CredentialItem[]> {
  const data = await getJson<ApiCredential[]>("credentials");
  if (!data || data.length === 0) return localCredentials;
  return data.map(toCredentialItem);
}

/**
 * Projects for rendering: local content paints instantly (SSR + first paint),
 * then the API copy refines it. `queryFn` never rejects, so `data` is always
 * either the API list or the local fallback.
 */
export function usePortfolioProjects(): Project[] {
  const { data } = useQuery({
    queryKey: ["content", "projects"],
    queryFn: fetchPortfolioProjects,
    placeholderData: localProjects,
    staleTime: STALE_TIME_MS,
  });
  return data ?? localProjects;
}

/** Skill groups for rendering; see {@link usePortfolioProjects}. */
export function usePortfolioSkillGroups(): typeof localSkillGroups {
  const { data } = useQuery({
    queryKey: ["content", "skill-groups"],
    queryFn: fetchPortfolioSkillGroups,
    placeholderData: localSkillGroups,
    staleTime: STALE_TIME_MS,
  });
  return data ?? localSkillGroups;
}

/** Career credentials (work / projects / education / certifications); see {@link usePortfolioProjects}. */
export function usePortfolioCredentials(): CredentialItem[] {
  const { data } = useQuery({
    queryKey: ["content", "credentials"],
    queryFn: fetchPortfolioCredentials,
    placeholderData: localCredentials,
    staleTime: STALE_TIME_MS,
  });
  return data ?? localCredentials;
}

/** Query options for route loaders that need the same cached project list. */
export const portfolioProjectsQuery = {
  queryKey: ["content", "projects"] as const,
  queryFn: fetchPortfolioProjects,
  staleTime: STALE_TIME_MS,
};
