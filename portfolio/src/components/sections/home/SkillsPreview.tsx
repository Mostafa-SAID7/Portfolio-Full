import { Link } from "@tanstack/react-router";
import { ArrowRight } from "lucide-react";
import { Reveal } from "@/components/ui/Reveal";
import { SectionHeading } from "@/components/ui/SectionHeading";
import { useI18n } from "@/lib/i18n";
import { usePortfolioSkillGroups } from "@/lib/content";
import { SkillCard } from "@/components/skills/SkillCard";

export function SkillsPreview() {
  const { tr } = useI18n();
  // Live content from the PortfolioAPI (empty while loading/offline — the
  // bundled src/data catalog is never used as a runtime fallback).
  const skillGroups = usePortfolioSkillGroups();
  const featured = skillGroups.slice(0, 4);

  return (
    <section id="skills" className="section-shell section-y scroll-mt-24">
      <div className="container-narrow">
        <SectionHeading title={tr("skills.title")} />

        <div className="grid gap-4 sm:grid-cols-2">
          {featured.map((group, i) => (
            <Reveal key={group.title} delay={i * 0.08}>
              <SkillCard
                title={group.title}
                skills={group.skills}
                compact
                maxSkills={5}
              />
            </Reveal>
          ))}
        </div>


        <Reveal delay={0.3}>
          <div className="mt-10 flex justify-center">
            <Link to="/skills" className="group btn-accent-outline">
              Explore the full stack
              <ArrowRight className="size-4 transition-transform group-hover:translate-x-0.5 rtl:rotate-180" />
            </Link>
          </div>
        </Reveal>
      </div>
    </section>
  );
}
