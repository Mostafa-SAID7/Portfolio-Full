import {
  Award,
  BadgeCheck,
  Briefcase,
  Compass,
  GraduationCap,
  Rocket,
  type LucideIcon,
} from "lucide-react";
import type { CredentialTab } from "@/data";

/**
 * The icon that tells the visitor what a credential IS.
 *
 * The previous mapping hashed the id and picked one of seven glyphs, so an
 * education credential could show a database icon and a certification could
 * show a rocket. The tab is the real signal, so the icon follows it.
 */
const TAB_ICONS: Record<CredentialTab, LucideIcon> = {
  work: Briefcase,
  projects: Rocket,
  education: GraduationCap,
  certifications: BadgeCheck,
};

/** Fallback for a tab we do not know yet. */
const FALLBACK_ICON: LucideIcon = Award;

/** The right icon for a credential type. */
export function iconForCredential(tab: CredentialTab | string): LucideIcon {
  return TAB_ICONS[tab as CredentialTab] ?? FALLBACK_ICON;
}

/** Icons used by the awards/credentials artwork — one per credential type. */
export const CREDENTIAL_TYPE_ICONS = {
  education: GraduationCap,
  certifications: BadgeCheck,
  award: Award,
  direction: Compass,
} as const;
