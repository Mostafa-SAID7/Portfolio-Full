import type { SkillIcon } from "@/data";
import { useLocalizedContent } from "@/lib/localize";
import { getSkillIcon } from "./skillIcons";

export interface SkillCardProps {
  title: string;
  icon?: SkillIcon | string;
  skills: string[];
  maxSkills?: number;
  compact?: boolean;
}

/**
 * Reusable skill category card.
 * Used across the full skills catalog and the homepage skills preview.
 */
export function SkillCard({
  title,
  icon,
  skills,
  maxSkills,
  compact = false,
}: SkillCardProps) {
  const { skillGroupTitle } = useLocalizedContent();
  const Icon = icon ? getSkillIcon(icon) : null;

  const displaySkills = maxSkills ? skills.slice(0, maxSkills) : skills;
  const remainingCount = maxSkills && skills.length > maxSkills ? skills.length - maxSkills : 0;

  if (compact) {
    return (
      <div className="bg-card border border-border h-full rounded-2xl p-6 shadow-glow transition-transform hover:-translate-y-1">
        <h3 className="mb-4 flex items-center gap-3 type-h4">
          <span className="size-2 rounded-full bg-accent" />
          {skillGroupTitle(title)}
        </h3>
        <div className="flex flex-wrap gap-2">
          {displaySkills.map((skill) => (
            <span
              key={skill}
              dir="ltr"
              className="rounded-xl border border-border bg-foreground/10 px-3 py-1.5 type-body text-foreground/80"
            >
              {skill}
            </span>
          ))}
          {remainingCount > 0 && (
            <span className="rounded-xl px-3 py-1.5 type-body-strong text-accent">
              +{remainingCount}
            </span>
          )}
        </div>
      </div>
    );
  }

  return (
    <div className="surface-card p-8">
      <div className="mb-6 flex items-center gap-3">
        {Icon && (
          <div className="grid size-10 place-items-center rounded-xl border border-border bg-foreground/10 text-primary">
            <Icon className="size-5" />
          </div>
        )}
        <h2 className="type-h3 text-card-foreground">{skillGroupTitle(title)}</h2>
      </div>

      <div className="flex flex-wrap gap-2.5">
        {displaySkills.map((skill) => (
          <span
            key={skill}
            dir="ltr"
            className="rounded-xl border border-border bg-foreground/10 px-4 py-1.5 type-body-strong text-card-foreground shadow-sm"
          >
            {skill}
          </span>
        ))}
      </div>
    </div>
  );
}
