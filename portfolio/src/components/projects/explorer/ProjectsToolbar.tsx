import { useEffect, useState } from "react";
import { ArrowUpDown, LayoutGrid, Rows3, Search, X } from "lucide-react";
import { SelectField } from "@/components/ui/select-field";
import { useI18n } from "@/lib/i18n";
import { PROJECT_SORTS, type SortValue } from "@/hooks/useProjectsExplorer";
import type { ProjectView } from "../types";

interface ProjectsToolbarProps {
  searchQuery: string;
  onSearchChange: (value: string) => void;
  sort: SortValue;
  onSortChange: (value: SortValue) => void;
  view: ProjectView;
  onViewChange: (value: ProjectView) => void;
}

/** Sticky search + sort + view-mode bar above the project results. */
export function ProjectsToolbar({
  searchQuery,
  onSearchChange,
  sort,
  onSortChange,
  view,
  onViewChange,
}: ProjectsToolbarProps) {
  const { tr } = useI18n();
  // @radix-ui/react-select's SelectProvider calls useMemo internally which
  // crashes during SSR when React's dispatcher is null. The shared
  // SelectField handles that (it renders an inert placeholder until
  // hydration), so the toolbar can stay simple.

  return (
    <div
      className="sticky z-20 bleed-gutter mb-5 py-1.5"
      style={{ top: "calc(var(--nav-h, 4.5rem) + 0.25rem)" }}
    >
      <div className="rounded-2xl bg-card p-2.5 border border-border shadow-glow grid grid-cols-1 gap-2 md:grid-cols-[minmax(0,1fr)_auto_auto] md:items-center">
        <div className="relative min-w-0">
          <label htmlFor="project-search" className="sr-only">
            {tr("projects.index.search")}
          </label>
          <Search className="pointer-events-none absolute start-3.5 top-1/2 size-4 -translate-y-1/2 text-card-foreground/70" />
          <input
            id="project-search"
            type="search"
            placeholder={tr("projects.index.searchPlaceholder")}
            value={searchQuery}
            onChange={(e) => onSearchChange(e.target.value)}
            className="h-9 w-full rounded-xl border border-border bg-foreground/10 ps-10 pe-9 type-control text-card-foreground placeholder:text-card-foreground/60 transition-all focus:border-foreground/40 focus:bg-foreground/15 focus:outline-none"
          />
          {searchQuery && (
            <button
              type="button"
              onClick={() => onSearchChange("")}
              aria-label={tr("projects.index.clear")}
              className="absolute end-2.5 top-1/2 -translate-y-1/2 rounded-xl p-1 text-card-foreground/70 hover:text-card-foreground transition-colors"
            >
              <X className="size-4" />
            </button>
          )}
        </div>

        <div className="relative">
          <SelectField
            id="project-sort"
            label={tr("projects.index.sort")}
            labelClassName="sr-only"
            value={sort}
            onValueChange={(value) => onSortChange(value as SortValue)}
            options={PROJECT_SORTS.map((option) => ({
              value: option.value,
              label: tr(option.key),
            }))}
            icon={<ArrowUpDown className="size-4 shrink-0 text-primary" />}
            triggerClassName="h-9 gap-2 px-3 py-0 shadow-none transition-all hover:bg-foreground/15 focus:ring-0 md:w-44"
          />
        </div>

        <div
          role="group"
          aria-label={tr("projects.index.viewMode")}
          className="flex h-9 shrink-0 items-center gap-1 rounded-xl border border-border bg-foreground/10 p-1"
        >
          <ViewButton
            active={view === "grid"}
            onClick={() => onViewChange("grid")}
            label={tr("projects.index.grid")}
          >
            <LayoutGrid className="size-4" />
          </ViewButton>
          <ViewButton
            active={view === "list"}
            onClick={() => onViewChange("list")}
            label={tr("projects.index.list")}
          >
            <Rows3 className="size-4" />
          </ViewButton>
        </div>
      </div>
    </div>
  );
}

function ViewButton({
  active,
  onClick,
  label,
  children,
}: {
  active: boolean;
  onClick: () => void;
  label: string;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-pressed={active}
      aria-label={label}
      title={label}
      className={`inline-flex size-9 items-center justify-center rounded-xl transition-all ${
        active
          ? "bg-foreground text-background shadow-sm"
          : "text-foreground/70 hover:text-foreground"
      }`}
    >
      {children}
    </button>
  );
}
