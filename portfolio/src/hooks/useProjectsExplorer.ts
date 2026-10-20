import { useDeferredValue, useEffect, useMemo, useState } from "react";
import { useNavigate, useSearch } from "@tanstack/react-router";
import { type Project } from "@/data";
import { usePortfolioProjects } from "@/lib/content";
import type { ProjectView } from "@/components/projects/types";

export const PROJECT_SORTS = [
  { value: "default", key: "projects.sort.default" },
  { value: "az", key: "projects.sort.az" },
  { value: "za", key: "projects.sort.za" },
  { value: "category", key: "projects.sort.category" },
  { value: "tech", key: "projects.sort.tech" },
] as const;

export type SortValue = (typeof PROJECT_SORTS)[number]["value"];
/**
 * The active category filter. The list itself is served by the API (managed
 * in the dashboard), so it is a plain string rather than a fixed union.
 */
export type ProjectFilter = string;

/**
 * Search params of the projects explorer - the explorer state lives in the
 * URL, so a link can be shared and the Back button restores the position.
 */
export interface ProjectsSearch {
  filter?: string | undefined;
  q?: string | undefined;
  sort?: SortValue | undefined;
  view?: ProjectView | undefined;
  page?: number | undefined;
}

/** Normalises (and validates) the search params of the /projects route. */
export function validateProjectsSearch(
  search: Record<string, unknown>,
): ProjectsSearch {
  const rawSort = typeof search["sort"] === "string" ? search["sort"] : undefined;
  const sort = PROJECT_SORTS.some((option) => option.value === rawSort)
    ? (rawSort as SortValue)
    : undefined;
  const rawPage =
    typeof search["page"] === "number"
      ? search["page"]
      : Number(search["page"]);

  return {
    filter:
      typeof search["filter"] === "string" && search["filter"].length > 0
        ? search["filter"]
        : undefined,
    q:
      typeof search["q"] === "string" && search["q"].length > 0
        ? search["q"]
        : undefined,
    sort,
    view: search["view"] === "list" ? "list" : undefined,
    page:
      Number.isFinite(rawPage) && rawPage > 0 ? Math.floor(rawPage) : undefined,
  };
}

/** True when both search objects carry the same values. */
function sameSearch(a: ProjectsSearch, b: ProjectsSearch): boolean {
  return (
    a.filter === b.filter &&
    a.q === b.q &&
    a.sort === b.sort &&
    a.view === b.view &&
    a.page === b.page
  );
}

const PER_PAGE: Record<ProjectView, number> = { grid: 9, list: 6 };

function matchesQuery(project: Project, query: string) {
  return (
    project.title.toLowerCase().includes(query) ||
    project.description.toLowerCase().includes(query) ||
    project.category.toLowerCase().includes(query) ||
    (project.client?.toLowerCase().includes(query) ?? false) ||
    project.tech.some((tech) => tech.toLowerCase().includes(query))
  );
}

function sortProjects(list: Project[], sort: SortValue) {
  const sorted = [...list];
  switch (sort) {
    case "az":
      return sorted.sort((a, b) => a.title.localeCompare(b.title));
    case "za":
      return sorted.sort((a, b) => b.title.localeCompare(a.title));
    case "category":
      return sorted.sort(
        (a, b) => a.category.localeCompare(b.category) || a.title.localeCompare(b.title),
      );
    case "tech":
      return sorted.sort((a, b) => b.tech.length - a.tech.length);
    default:
      return sorted;
  }
}

/**
 * Search / filter / sort / paginate state for the projects explorer.
 *
 * The state lives in the URL (search params) so the browser's Back button
 * returns to the exact position the visitor left - before, every Back from a
 * project detail landed on page 1 of the list. As a bonus the URL is now
 * shareable: "?filter=Clothing&page=2" reopens the same view.
 */
export function useProjectsExplorer() {
  const search = useSearch({ from: "/projects/" });
  const navigate = useNavigate({ from: "/projects/" });

  const [filter, setFilter] = useState<ProjectFilter>(search.filter ?? "All");
  const [searchQuery, setSearchQuery] = useState(search.q ?? "");
  const [sort, setSort] = useState<SortValue>(search.sort ?? "default");
  const [view, setView] = useState<ProjectView>(search.view ?? "grid");
  const [currentPage, setCurrentPage] = useState(search.page ?? 1);

  // Live content from the PortfolioAPI (empty while loading/offline — the
  // bundled src/data catalog is never used as a runtime fallback).
  const projects = usePortfolioProjects();

  const deferredQuery = useDeferredValue(searchQuery);
  const isFiltering = deferredQuery !== searchQuery;
  const perPage = PER_PAGE[view];

  const counts = useMemo(() => {
    const map: Record<string, number> = { All: projects.length };
    for (const project of projects) {
      map[project.category] = (map[project.category] ?? 0) + 1;
    }
    return map;
  }, [projects]);

  const filtered = useMemo(() => {
    const byCategory = filter === "All" ? projects : projects.filter((p) => p.category === filter);
    const query = deferredQuery.trim().toLowerCase();
    const searched = query ? byCategory.filter((p) => matchesQuery(p, query)) : byCategory;
    return sortProjects(searched, sort);
  }, [projects, filter, deferredQuery, sort]);

  const totalPages = Math.max(1, Math.ceil(filtered.length / perPage));

  useEffect(() => {
    setCurrentPage(1);
  }, [filter, searchQuery, sort, view]);

  // Mirror the state into the URL (replace, so the history is not flooded
  // with every keystroke) - that is what makes Back restore the position.
  useEffect(() => {
    const query = searchQuery.trim();
    const next: ProjectsSearch = {};
    if (filter !== "All") next.filter = filter;
    if (query) next.q = query;
    if (sort !== "default") next.sort = sort;
    if (view !== "grid") next.view = view;
    if (currentPage > 1) next.page = currentPage;

    if (!sameSearch(next, search)) {
      navigate({ search: next, replace: true });
    }
    // `search` is intentionally not a dependency: it is the value we compare
    // against, and including it would re-run this effect on every navigation.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filter, searchQuery, sort, view, currentPage, navigate]);

  const page = Math.min(currentPage, totalPages);

  const reset = () => {
    setFilter("All");
    setSearchQuery("");
    setSort("default");
  };

  return {
    filter,
    setFilter,
    searchQuery,
    setSearchQuery,
    sort,
    setSort,
    view,
    setView,
    page,
    setCurrentPage,
    totalPages,
    perPage,
    counts,
    filtered,
    paginated: filtered.slice((page - 1) * perPage, page * perPage),
    isFiltering,
    hasActiveFilters: filter !== "All" || searchQuery.trim().length > 0 || sort !== "default",
    reset,
  };
}
