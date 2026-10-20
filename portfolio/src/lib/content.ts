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

const FETCH_TIMEOUT_MS = 2_000; // 2 s max — don't block SSR streaming longer
const STALE_TIME_MS = 5 * 60 * 1_000; // 5 min — matches router.tsx defaultOptions

interface ApiProject {
  id: number;
  slug: string | null;
  // Bilingual title/description (API v2 schema)
  titleEn: string;
  titleAr: string;
  descriptionEn: string;
  descriptionAr: string;
  imageUrl: string | null;
  technologies: string[];
  githubUrl: string | null;
  liveUrl: string | null;
  featured: boolean;
  order: number;
  isActive: boolean;
  categoryEn: string | null;
  categoryAr: string | null;
  clientEn: string | null;
  clientAr: string | null;
  typeEn: string | null;
  typeAr: string | null;
  databaseEn: string | null;
  databaseAr: string | null;
  statusEn: string | null;
  statusAr: string | null;
  badgesEn: string[] | null;
  badgesAr: string[] | null;
  metricsEn: { labelEn: string; labelAr: string; value: string }[] | null;
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

interface CacheEntry<T> {
  data: T;
  timestamp: number;
}
const contentMemoryCache = new Map<string, CacheEntry<unknown>>();
const CACHE_TTL_MS = 60_000;

async function fetchFromApi<T>(url: string): Promise<T | null> {
  try {
    const response = await fetch(url, {
      headers: { accept: "application/json" },
      signal: AbortSignal.timeout(FETCH_TIMEOUT_MS),
    });
    if (!response.ok) {
      console.error(
        `[content] API returned ${response.status} for ${url}. ` +
          "Content will be empty until the API recovers.",
      );
      return null;
    }
    const data = (await response.json()) as T;
    contentMemoryCache.set(url, { data, timestamp: Date.now() });
    return data;
  } catch (error) {
    const isAbort =
      error instanceof DOMException &&
      (error.name === "AbortError" || error.name === "TimeoutError");

    if (!isAbort) {
      console.error(
        `[content] Failed to fetch ${url}:`,
        error,
        "Content will be empty. Check that the API is reachable.",
      );
    }
    return null;
  }
}

async function getJson<T>(path: string): Promise<T | null> {
  const url = `${apiOrigin()}/api/v1/public/${path}`;
  const cached = contentMemoryCache.get(url) as CacheEntry<T> | undefined;
  const now = Date.now();

  if (cached) {
    // If fresh, return immediately (0ms)
    if (now - cached.timestamp < CACHE_TTL_MS) {
      return cached.data;
    }
    // If stale, return stale data immediately and revalidate in background (SWR)
    void fetchFromApi<T>(url);
    return cached.data;
  }

  // Cold cache: fetch and await
  return fetchFromApi<T>(url);
}

/** API project → portfolio Project. Bilingual API fields (v2 schema). */
function toPortfolioProject(dto: ApiProject): Project {
  const draft = {
    id: dto.slug || `api-${dto.id}`,
    apiId: dto.id,
    // Use English title/description as canonical presentation fields
    title: dto.titleEn ?? dto.titleAr ?? "",
    description: dto.descriptionEn ?? dto.descriptionAr ?? "",
    category: ((dto.categoryEn ?? dto.categoryAr) as Project["category"]) || "Other",
    type: ((dto.typeEn ?? dto.typeAr) as Project["type"]) || "Self",
    tech: dto.technologies ?? [],
    live: dto.liveUrl || "#",
    client: dto.clientEn ?? dto.clientAr ?? undefined,
    database: dto.databaseEn ?? dto.databaseAr ?? undefined,
    status: ((dto.statusEn ?? dto.statusAr) as Project["status"]) ?? undefined,
    badges: dto.badgesEn?.length ? dto.badgesEn : dto.featured ? ["Featured"] : undefined,
    github: dto.githubUrl ?? undefined,
    image: dto.imageUrl ?? PLACEHOLDER_COVER,
    metrics: dto.metricsEn?.length
      ? dto.metricsEn.map((m) => ({ label: m.labelEn ?? m.labelAr ?? "", value: m.value }))
      : undefined,
    gradient: dto.gradient ?? undefined,
  };
  return draft as Project;
}

export async function fetchPortfolioProjects(): Promise<Project[]> {
  const data = await getJson<ApiProject[]>("projects");
  if (!data || !Array.isArray(data)) return [];
  return data.filter(Boolean).map(toPortfolioProject);
}

/**
 * Build a lookup map from a project list for O(1) resolution by either the
 * slug or the legacy `api-<id>` fallback identifier. Call this once per list
 * reference and cache the result rather than calling resolvePortfolioProject
 * repeatedly on the same list (fix for PORTFOLIO-M2).
 */
export function buildProjectMap(list: Project[]): Map<string, Project> {
  const map = new Map<string, Project>();
  for (const p of list) {
    if (p.id) map.set(p.id, p);
    if (p.apiId != null) map.set(`api-${p.apiId}`, p);
  }
  return map;
}

/** Resolve a public project link from an already-built map (O(1)). */
export function resolvePortfolioProject(list: Project[], rawId: string): Project | undefined {
  const id = decodeURIComponent(rawId);
  // Build on demand (callers that render a single project detail page call
  // this once; the map cost is negligible vs. SSR fetch latency).
  const map = buildProjectMap(list);
  return map.get(id);
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

function iconForCategory(category?: string | null): SkillIcon {
  if (!category || typeof category !== "string") return "architecture";
  const lower = category.toLowerCase();
  return ICON_BY_KEYWORD.find(([keyword]) => lower.includes(keyword))?.[1] ?? "architecture";
}

export async function fetchPortfolioSkillGroups(): Promise<
  { title: string; icon: SkillIcon; skills: string[] }[]
> {
  const data = await getJson<ApiSkill[]>("skills");
  if (!data || !Array.isArray(data)) return [];
  const groups = new Map<string, { icon: SkillIcon; skills: string[] }>();
  for (const skill of [...data].sort((a, b) => (a?.order ?? 0) - (b?.order ?? 0))) {
    if (!skill || !skill.isActive) continue;
    const cat = (typeof skill.category === "string" ? skill.category.trim() : "") || "General";
    let group = groups.get(cat);
    if (!group) {
      group = { icon: iconForCategory(cat), skills: [] };
      groups.set(cat, group);
    }
    if (skill.name && typeof skill.name === "string") {
      group.skills.push(skill.name);
    }
  }
  return [...groups.entries()].map(([title, group]) => ({ title, ...group }));
}

function toCredentialItem(c: ApiCredential): CredentialItem {
  const draft = {
    id: `api-${c.id}`,
    tab: (c.tab as CredentialTab) || "work",
    year: c.year ?? "",
    period: { en: c.periodEn ?? "", ar: c.periodAr ?? c.periodEn ?? "" },
    title: { en: c.titleEn ?? "", ar: c.titleAr ?? c.titleEn ?? "" },
    org: { en: c.orgEn ?? "", ar: c.orgAr ?? c.orgEn ?? "" },
    location: { en: c.locationEn ?? "", ar: c.locationAr ?? "" },
    summary: { en: c.summaryEn ?? "", ar: c.summaryAr ?? "" },
    highlights: { en: c.highlightsEn ?? [], ar: c.highlightsAr ?? [] },
    stack: c.stack ?? [],
    credentialId: c.credentialId ?? undefined,
    status: c.statusEn ? { en: c.statusEn, ar: c.statusAr ?? c.statusEn } : undefined,
    featured: Boolean(c.featured),
  };
  return draft as CredentialItem;
}

export async function fetchPortfolioCredentials(): Promise<CredentialItem[]> {
  const data = await getJson<ApiCredential[]>("credentials");
  if (!data || !Array.isArray(data)) return [];
  return data.filter(Boolean).map(toCredentialItem);
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
  return data
    .filter((c) => c.isActive)
    .map((c) => c.name)
    .filter((name) => name.length > 0);
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

export const portfolioSkillGroupsQuery = {
  queryKey: ["content", "skill-groups"] as const,
  queryFn: fetchPortfolioSkillGroups,
  staleTime: STALE_TIME_MS,
};

export const portfolioCredentialsQuery = {
  queryKey: ["content", "credentials"] as const,
  queryFn: fetchPortfolioCredentials,
  staleTime: STALE_TIME_MS,
};

export const portfolioCategoriesQuery = {
  queryKey: ["content", "categories"] as const,
  queryFn: fetchPortfolioCategories,
  staleTime: STALE_TIME_MS,
};
