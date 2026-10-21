import { createFileRoute } from "@tanstack/react-router";
import { PageSkeleton } from "@/components/ui/Skeletons";
import { PageShell } from "@/components/layout/PageShell";
import { PageIntro } from "@/components/ui/PageIntro";
import { CtaLink } from "@/components/ui/CtaLink";
import { CtaBand } from "@/components/ui/CtaBand";
import { usePortfolioSkillGroups, portfolioSkillGroupsQuery } from "@/lib/content";
import { pageSeo, pageTitle } from "@/lib/seo";
import { useI18n } from "@/lib/i18n";
import { SkillCard } from "@/components/skills/SkillCard";

export const Route = createFileRoute("/skills")({
  loader: ({ context: { queryClient } }) =>
    queryClient.ensureQueryData(portfolioSkillGroupsQuery),
  head: () =>
    pageSeo({
      title: pageTitle("Skills & Stack"),
      description:
        "Full technical skills & stack: .NET 8 Microservices, Clean Architecture, Angular, Next.js, SQL Server, Redis, and Azure DevOps.",
      path: "/skills",
    }),
  pendingComponent: PageSkeleton,
  component: SkillsPage,
});

function SkillsPage() {
  const { tr } = useI18n();
  // Live content from the PortfolioAPI (empty while loading/offline — the
  // bundled src/data catalog is never used as a runtime fallback).
  const skillGroups = usePortfolioSkillGroups();

  return (
    <PageShell>
      <PageIntro
        eyebrow={tr("skills.page.eyebrow")}
        title={tr("skills.title")}
        description={tr("skills.page.desc")}
      />

      <section className="section-y-sm">
        <div className="grid gap-6 sm:grid-cols-2">
          {skillGroups.map(({ title, icon, skills }) => (
            <SkillCard key={title} title={title} icon={icon} skills={skills} />
          ))}
        </div>


        <CtaBand
          lottie="/lottie/skills-side.lottie"
          eyebrow={tr("cta.band.eyebrow")}
          title={tr("skills.cta.title")}
          description={tr("skills.cta.desc")}
        >
          <CtaLink to="/projects" variant="secondary">
            {tr("skills.page.projectsCta")}
          </CtaLink>
          <CtaLink to="/contact">{tr("skills.page.contactCta")}</CtaLink>
        </CtaBand>
      </section>
    </PageShell>
  );
}
