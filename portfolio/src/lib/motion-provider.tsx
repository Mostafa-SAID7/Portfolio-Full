import { LazyMotion } from "framer-motion";
import type { ReactNode } from "react";

/**
 * Framer Motion's full feature set (~100 KB) is pulled out of the entry chunk
 * and fetched asynchronously after hydration. Components use `m.*` (via the
 * `m as motion` alias), which is a few hundred bytes each.
 */
const loadFeatures = () => import("framer-motion").then((mod) => mod.domMax);

export function MotionProvider({ children }: { children: ReactNode }) {
  return <LazyMotion features={loadFeatures}>{children}</LazyMotion>;
}
