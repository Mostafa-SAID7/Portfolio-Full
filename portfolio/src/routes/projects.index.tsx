import { createFileRoute } from "@tanstack/react-router";
import { PageShell } from "@/components/layout/PageShell";
import { ProjectsExplorer } from "@/components/projects/explorer/ProjectsExplorer";
import { PageSkeleton } from "@/components/ui/Skeletons";
import { portfolioProjectsQuery } from "@/lib/content";
import { pageSeo, pageTitle } from "@/lib/seo";
import { absoluteUrl } from "@/lib/site";

const PROJECTS_DESCRIPTION =
  "Browse every project: multi-vendor marketplaces, e-commerce platforms and high-scale systems, with search, filters and sorting.";

export const Route = createFileRoute("/projects/")({
  // Prime the live API list server-side: SSR emits real project cards in the
  // HTML, and the client hydrates the same cache instead of starting empty.
  // API down → the query resolves to [] → the explorer's existing EmptyState
  // renders (never a bundled catalog fallback).
  loader: ({ context }) => context.queryClient.ensureQueryData(portfolioProjectsQuery),
  head: () =>
    pageSeo({
      title: pageTitle("All Projects"),
      description: PROJECTS_DESCRIPTION,
      path: "/projects",
      jsonLd: [
        {
          "@context": "https://schema.org",
          "@type": "CollectionPage",
          name: "All Projects",
          description: PROJECTS_DESCRIPTION,
          url: absoluteUrl("/projects"),
        },
      ],
    }),
  component: ProjectsPage,
  pendingComponent: PageSkeleton,
});

function ProjectsPage() {
  return (
    <PageShell contained={false} padded={false}>
      <ProjectsExplorer />
    </PageShell>
  );
}
