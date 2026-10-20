import { createFileRoute } from "@tanstack/react-router";
import { Suspense, lazy } from "react";
import { PageShell } from "@/components/layout/PageShell";
import { Hero } from "@/components/sections/Hero";
import { Projects } from "@/components/sections/Projects";
import { CredentialsCarouselLazy } from "@/components/credentials/CredentialsCarouselLazy";
import { LazyIsland } from "@/components/ui/LazyIsland";
import { pageSeo } from "@/lib/seo";
import { SITE, absoluteUrl } from "@/lib/site";
import {
  portfolioProjectsQuery,
  portfolioSkillGroupsQuery,
  portfolioCredentialsQuery,
} from "@/lib/content";

// Below-fold sections: lazy-loaded to remove their icons & framer-motion
// from the hydration critical path. Hero + Projects stay eager (above fold).
const AboutPreview = lazy(() =>
  import("@/components/sections/home/AboutPreview").then((m) => ({ default: m.AboutPreview }))
);
const SkillsPreview = lazy(() =>
  import("@/components/sections/home/SkillsPreview").then((m) => ({ default: m.SkillsPreview }))
);
const ExperiencePreview = lazy(() =>
  import("@/components/sections/home/ExperiencePreview").then((m) => ({ default: m.ExperiencePreview }))
);
const ContactCta = lazy(() =>
  import("@/components/sections/home/ContactCta").then((m) => ({ default: m.ContactCta }))
);
const heroIsoAvif = "/images/hero-iso.avif";

const DESCRIPTION =
  "Portfolio of Mostafa Samir, Senior Full Stack Engineer specializing in .NET 8 microservices, React.js and Angular platforms.";

export const Route = createFileRoute("/")({
  // Pre-warm all home page data in parallel during SSR so components
  // hydrate from cache instantly — no client-side waterfall fetches.
  loader: ({ context: { queryClient } }) =>
    Promise.all([
      queryClient.ensureQueryData(portfolioProjectsQuery),
      queryClient.ensureQueryData(portfolioSkillGroupsQuery),
      queryClient.ensureQueryData(portfolioCredentialsQuery),
    ]),
  head: () => {
    const seo = pageSeo({
      title: `${SITE.name} | ${SITE.role}`,
      description: DESCRIPTION,
      path: "/",
      jsonLd: [
        {
          "@context": "https://schema.org",
          "@type": "Person",
          name: SITE.name,
          jobTitle: SITE.role,
          url: absoluteUrl("/"),
          knowsAbout: [
            ".NET 8 microservices",
            "Next.js",
            "Multi-vendor marketplaces",
            "Real-time bidding systems",
          ],
        },
      ],
    });

    return {
      ...seo,
      links: [
        ...seo.links,
        {
          rel: "preload",
          as: "image",
          type: "image/avif",
          href: heroIsoAvif,
          fetchPriority: "high",
        },
      ],
    };
  },
  component: Index,
});

function Index() {
  return (
    <PageShell contained={false} padded={false}>
      <Hero />
      <Projects />
      <div className="defer-paint">
        <Suspense fallback={<div className="section-y min-h-[40vh]" aria-hidden />}>
          <SkillsPreview />
        </Suspense>
      </div>
      <div className="defer-paint">
        <Suspense fallback={<div className="section-y min-h-[40vh]" aria-hidden />}>
          <ExperiencePreview />
        </Suspense>
      </div>
      <div className="defer-paint">
        <Suspense fallback={<div className="section-y min-h-[30vh]" aria-hidden />}>
          <AboutPreview />
        </Suspense>
      </div>
      <LazyIsland className="defer-paint">
        <CredentialsCarouselLazy />
      </LazyIsland>
      <LazyIsland className="defer-paint">
        <Suspense fallback={null}>
          <ContactCta />
        </Suspense>
      </LazyIsland>
    </PageShell>
  );
}
