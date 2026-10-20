import { createFileRoute, Link, notFound, redirect } from "@tanstack/react-router";
import { m as motion } from "framer-motion";
import { ArrowLeft } from "lucide-react";
import {
  portfolioProjectsQuery,
  resolvePortfolioProject,
  usePortfolioProjects,
} from "@/lib/content";
import { getProjectDetail, getRelatedProjects } from "@/data/project-details";
import { PageShell } from "@/components/layout/PageShell";
import { CtaLink } from "@/components/ui/CtaLink";
import { ProjectDetailSkeleton } from "@/components/ui/Skeletons";
import { ProjectDetailHeader } from "@/components/projects/detail/ProjectDetailHeader";
import { ProjectHighlights } from "@/components/projects/detail/ProjectHighlights";
import { ProjectShowcase } from "@/components/projects/detail/ProjectShowcase";
import {
  ProjectChallengeSolution,
  ProjectFeatures,
  ProjectOverview,
} from "@/components/projects/detail/ProjectNarrative";
import {
  ProjectArchitecture,
  ProjectOutcomes,
  ProjectProcess,
  ProjectResponsibilities,
} from "@/components/projects/detail/ProjectDelivery";
import { ProjectBrief } from "@/components/projects/detail/ProjectBrief";
import { RelatedProjects } from "@/components/projects/detail/RelatedProjects";
import { ProjectPager } from "@/components/projects/detail/ProjectPager";
import { useI18n } from "@/lib/i18n";
import { pageSeo } from "@/lib/seo";
import { absoluteUrl } from "@/lib/site";

export const Route = createFileRoute("/projects/$id")({
  loader: async ({ context, params }) => {
    const list = await context.queryClient.ensureQueryData(portfolioProjectsQuery);
    const project = resolvePortfolioProject(list, params.id);
    if (!project) throw notFound();
    if (project.id !== params.id) {
      throw redirect({ to: "/projects/$id", params: { id: project.id } });
    }
    return { project };
  },
  head: ({ params, loaderData }) => {
    const project = loaderData?.project;
    if (!project) {
      return {
        meta: [{ title: "Project not found" }, { name: "robots", content: "noindex" }],
      };
    }
    const title = `${project.title} | Marketplace Systems Architect`;
    const description = project.description;
    const path = `/projects/${params.id}`;

    return pageSeo({
      title,
      description,
      path,
      type: "article",
      jsonLd: [
        {
          "@context": "https://schema.org",
          "@type": "CreativeWork",
          name: project.title,
          description,
          url: absoluteUrl(path),
          creator: { "@type": "Person", name: "Mostafa Samir" },
        },
        {
          "@context": "https://schema.org",
          "@type": "BreadcrumbList",
          itemListElement: [
            { "@type": "ListItem", position: 1, name: "Home", item: absoluteUrl("/") },
            { "@type": "ListItem", position: 2, name: "Projects", item: absoluteUrl("/projects") },
            { "@type": "ListItem", position: 3, name: project.title, item: absoluteUrl(path) },
          ],
        },
      ],
    });
  },
  component: ProjectDetail,
  notFoundComponent: ProjectNotFound,
  pendingComponent: ProjectDetailSkeleton,
});

function ProjectNotFound() {
  const { tr } = useI18n();
  const projects = usePortfolioProjects();

  return (
    <PageShell contained={false}>
      <div className="flex flex-1 flex-col items-center justify-center py-32 text-center">
        <h1 className="mb-4 type-h1 text-foreground">{tr("project.notFound.title")}</h1>
        <CtaLink to="/projects" variant="secondary" withArrow={false}>
          <ArrowLeft className="size-4 rtl:rotate-180" />
          {tr("project.notFound.back")}
        </CtaLink>

        {projects.length > 0 && (
          <div className="mt-10 w-full max-w-2xl text-left">
            <p className="mb-4 text-center text-sm text-soft">
              These projects are live right now:
            </p>
            <ul className="grid gap-2 sm:grid-cols-2">
              {projects.slice(0, 8).map((item) => (
                <li key={item.id}>
                  <Link
                    to="/projects/$id"
                    params={{ id: item.id }}
                    className="block rounded-xl border border-line/10 px-4 py-3 text-sm text-ink transition hover:border-accent/40 hover:text-accent"
                  >
                    {item.title}
                  </Link>
                </li>
              ))}
            </ul>
          </div>
        )}
      </div>
    </PageShell>
  );
}

function ProjectDetail() {
  const { project } = Route.useLoaderData();
  const projects = usePortfolioProjects();
  const detail = getProjectDetail(project);
  const related = getRelatedProjects(project, projects);

  const hasLiveSite = project.live !== "#";
  const index = projects.findIndex((item) => item.id === project.id);
  const nextProject = projects[index + 1] ?? null;
  const prevProject = index > 0 ? (projects[index - 1] ?? null) : null;

  const hasChallengeSolution = Boolean(detail.challenge?.trim() || detail.solution?.trim());
  const hasFeatures = detail.features.length > 0;
  const hasArchitecture = detail.architecture.length > 0;
  const hasResponsibilities = detail.responsibilities.length > 0;
  const hasProcess = detail.process.length > 0;
  const hasOutcomes = detail.outcomes.length > 0;
  const hasHighlights = detail.highlights.length > 0;

  return (
    <PageShell contained={false} padded={false} disableOverflowX className="nav-offset pb-24">
      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.6 }}
        className="container-page page-gutter"
      >
        <ProjectDetailHeader project={project} />
        {hasHighlights && <ProjectHighlights highlights={detail.highlights} />}

        <div className="grid grid-cols-1 items-start gap-12 lg:grid-cols-12 lg:gap-16">
          <div className="space-y-20 lg:col-span-8">
            <ProjectShowcase project={project} />
            <ProjectOverview detail={detail} />
            {hasChallengeSolution && <ProjectChallengeSolution detail={detail} />}
            {hasFeatures && <ProjectFeatures detail={detail} />}
            {hasArchitecture && <ProjectArchitecture techs={project.tech} detail={detail} />}
            {hasResponsibilities && <ProjectResponsibilities detail={detail} />}
            {hasProcess && <ProjectProcess detail={detail} />}
            {hasOutcomes && <ProjectOutcomes detail={detail} />}
          </div>

          <ProjectBrief project={project} detail={detail} hasLiveSite={hasLiveSite} />
        </div>

        <RelatedProjects projects={related} />
        <ProjectPager prev={prevProject} next={nextProject} />
      </motion.div>
    </PageShell>
  );
}
