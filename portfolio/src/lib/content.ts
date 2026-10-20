/**
 * Centralized content layer: the portfolio renders projects, skills and career
 * credentials exclusively from the PortfolioAPI (`/api/v1/public/*`).
 * Presentation fields come from the API when present; otherwise generic defaults.
 * NO lookup into old static `src/data` catalogs (#39).
 */
import { useQuery } from "@tanstack/react-query";
import { apiOrigin } from "@/lib/api-origin";
import { PLACEHOLDER_COVER } from "@/lib/image";
import type { CredentialItem, CredentialTab, Project } from "@/data";
import type { SkillIcon } from "@/data/skills";

const FETCH_TIMEOUT_MS = 4_000;
const STALE_TIME_MS = 60_000;

interface ApiProject {
  id: number;
  slug: string | null;
  title: string;
  description: string;
  imageUrl: string | null;
  technologies: string[];
  githubUrl: string | null;
  liveUrl: string | null;
  featured: boolean;
  order: number;
  isActive: boolean;
  category: string | null;
  client: string | null;
  type: string | null;
  database: string | null;
  status: string | null;
  badges: string[] | null;
  metrics: { label: string; value: string }[] | null;
  gradient: string | null;
}

interface ApiCategory {
  id: number;
  name: string;
  order: number;
  isActive: boolean;
}

interface ApiSkill {
  id: number;
  name: string;
  category: string;
  proficiency: number;
  order: number;
  isActive: boolean;
}

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

async function getJson<T>(path: string): Promise<T | null> {
  try {
    const response = await fetch(`${apiOrigin()}/api/v1/public/${path}`, {
      headers: { accept: "application/json" },
      signal: AbortSignal.timeout(FETCH_TIMEOUT_MS),
    });
    if (!response.ok) return null;
    return (await response.json()) as T;
  } catch {
    return null;
  }
}

/** API project → portfolio Project. API fields only (#39). */
function toPortfolioProject(dto: ApiProject): Project {
  const draft = {
    id: dto.slug || `api-${dto.id}`,
    apiId: dto.id,
    title: dto.title,
    description: dto.description,
    category: (dto.category as Project["category"]) || "Other",
    type: (dto.type as Project["type"]) || "Self",
    tech: dto.technologies ?? [],
    live: dto.liveUrl || "#",
    client: dto.client ?? undefined,
    database: dto.database ?? undefined,
    status: (dto.status as Project["status"]) ?? undefined,
    badges: dto.badges?.length ? dto.badges : dto.featured ? ["Featured"] : undefined,
    github: dto.githubUrl ?? undefined,
    image: dto.imageUrl ?? PLACEHOLDER_COVER,
    metrics: dto.metrics?.length ? dto.metrics : undefined,
    gradient: dto.gradient ?? undefined,
  };
  return draft as Project;
}

export async function fetchPortfolioProjects(): Promise<Project[]> {
  const data = await getJson<ApiProject[]>("projects");
  if (!data) return [];
  return data.map(toPortfolioProject);
}

/** Resolve public link from API list only (slug or legacy api-<id>). */
export function resolvePortfolioProject(
  list: Project[],
  rawId: string,
): Project | undefined {
  const id = decodeURIComponent(rawId);
  const direct = list.find((p) => p.id === id);
  if (direct) return direct;
  const legacyApiId = /^api-(\d+)$/.exec(id);
  if (legacyApiId) {
    const numericId = Number(legacyApiId[1]);
    const byApiId = list.find((p) => p.apiId === numericId);
    if (byApiId) return byApiId;
  }
  return undefined;
}

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

export async function fetchPortfolioSkillGroups(): Promise<
  { title: string; icon: SkillIcon; skills: string[] }[]
> {
  const data = await getJson<ApiSkill[]>("skills");
  if (!data) return [];
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
  if (!data) return [];
  return data.map(toCredentialItem);
}

export function usePortfolioProjects(): Project[] {
  const { data } = useQuery({
    queryKey: ["content", "projects"],
    queryFn: fetchPortfolioProjects,
    staleTime: STALE_TIME_MS,
  });
  return data ?? [];
}

export function usePortfolioSkillGroups(): {
  title: string;
  icon: SkillIcon;
  skills: string[];
}[] {
  const { data } = useQuery({
    queryKey: ["content", "skill-groups"],
    queryFn: fetchPortfolioSkillGroups,
    staleTime: STALE_TIME_MS,
  });
  return data ?? [];
}

export async function fetchPortfolioCategories(): Promise<string[]> {
  const data = await getJson<ApiCategory[]>("categories");
  if (!data) return [];
  return data.filter((c) => c.isActive).map((c) => c.name).filter((name) => name.length > 0);
}

export function usePortfolioCredentials(): CredentialItem[] {
  const { data } = useQuery({
    queryKey: ["content", "credentials"],
    queryFn: fetchPortfolioCredentials,
    staleTime: STALE_TIME_MS,
  });
  return data ?? [];
}

export function usePortfolioCategories(): string[] {
  const { data } = useQuery({
    queryKey: ["content", "categories"],
    queryFn: fetchPortfolioCategories,
    staleTime: STALE_TIME_MS,
  });
  return data ?? [];
}

export const portfolioProjectsQuery = {
  queryKey: ["content", "projects"] as const,
  queryFn: fetchPortfolioProjects,
  staleTime: STALE_TIME_MS,
};
