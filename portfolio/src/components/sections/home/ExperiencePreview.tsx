import { CareerTimeline, type CareerTimelineProps } from "@/components/credentials/timeline/CareerTimeline";

/**
 * Career & Credentials preview section.
 * Delegates to the modular CareerTimeline domain component, eliminating monolithic duplication.
 */
export function ExperiencePreview(props: CareerTimelineProps) {
  return <CareerTimeline {...props} />;
}
