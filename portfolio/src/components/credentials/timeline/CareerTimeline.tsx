import { useMemo, useState } from "react";
import { Link } from "@tanstack/react-router";
import { AnimatePresence, m as motion, useReducedMotion } from "framer-motion";
import { ArrowRight } from "lucide-react";
import { type CredentialTab } from "@/data";
import { usePortfolioCredentials } from "@/lib/content";
import { useI18n } from "@/lib/i18n";
import { SectionHeading } from "@/components/ui/SectionHeading";
import { CareerTabs } from "./CareerTabs";
import { CareerRow } from "./CareerRow";
import { tabIcons } from "./tabIcons";
import { timelineListVariants } from "./timelineVariants";

export interface CareerTimelineProps {
  showCta?: boolean;
}

/** Tabbed career + credentials timeline with expandable entries. */
export function CareerTimeline({ showCta = true }: CareerTimelineProps = {}) {
  const [activeTab, setActiveTab] = useState<CredentialTab>("work");
  const [openId, setOpenId] = useState<string | null>(null);
  const { tr } = useI18n();
  const reduce = !!useReducedMotion();
  const credentials = usePortfolioCredentials();

  const items = useMemo(
    () => credentials.filter((c) => c.tab === activeTab),
    [credentials, activeTab],
  );
  const ActiveIcon = tabIcons[activeTab];

  return (
    <section id="events" className="section-shell section-y">
      <div className="container-narrow">
        <SectionHeading title={tr("events.title")} description={tr("events.desc")} />


        <CareerTabs
          activeTab={activeTab}
          reduce={reduce}
          onSelect={(tab) => {
            setActiveTab(tab);
            setOpenId(null);
          }}
        />

        <AnimatePresence mode="wait" initial={false}>
          <motion.div
            key={activeTab}
            variants={timelineListVariants(reduce)}
            initial="hidden"
            animate="visible"
            exit="exit"
            className="flex flex-col gap-4"
          >
            {items.length === 0 && (
              <p className="type-body text-center text-muted-foreground">{tr("events.empty")}</p>
            )}

            {items.map((item) => (
              <CareerRow
                key={item.id}
                item={item}
                icon={ActiveIcon}
                reduce={reduce}
                open={openId === item.id}
                onToggle={() => setOpenId((current) => (current === item.id ? null : item.id))}
              />
            ))}
          </motion.div>
        </AnimatePresence>

        {showCta && (
          <div className="mt-10 flex justify-center">
            <Link
              to="/experience"
              className="type-label inline-flex items-center gap-2 rounded-xl border border-border bg-foreground/5 px-6 py-3 transition-colors hover:bg-foreground/15"
            >
              {tr("events.cta")}
              <ArrowRight className="size-3.5 rtl:rotate-180" />
            </Link>
          </div>
        )}
      </div>
    </section>
  );
}
