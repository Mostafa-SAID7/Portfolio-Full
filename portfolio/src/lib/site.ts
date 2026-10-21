export const NAV_LINKS = [
  { key: "nav.works" as const, to: "/projects" as const, label: "Projects" },
  { key: "nav.about" as const, to: "/about" as const, label: "About" },
  { key: "nav.experience" as const, to: "/experience" as const, label: "Experience" },
  { key: "nav.contact" as const, to: "/contact" as const, label: "Contact" },
] as const;

export type NavLink = (typeof NAV_LINKS)[number];

export const SOCIAL_LINKS = {
  github: {
    label: "GitHub",
    url: "https://github.com/Mostafa-SAID7",
    lottie: "/lottie/social/github.lottie",
    bgClass: "bg-[var(--social-github)]",
    featured: true,
  },
  linkedin: {
    label: "LinkedIn",
    url: "https://linkedin.com/in/mostafasamirsaid",
    lottie: "/lottie/social/linkedin.lottie",
    bgClass: "bg-[var(--social-linkedin)]",
    featured: true,
  },
  email: {
    label: "Email",
    url: "mailto:m.ssaid356@gmail.com",
    address: "m.ssaid356@gmail.com",
    lottie: "/lottie/social/mail.lottie",
    bgClass: "bg-[var(--social-instagram)]",
    featured: false,
  },
  whatsapp: {
    label: "WhatsApp",
    url: "https://wa.me/+201067358073",
    number: "+201067358073",
    lottie: "/lottie/social/whatsapp.lottie",
    bgClass: "bg-[#25D366]",
    featured: false,
  },
} as const;

/**
 * Single source of truth for site-wide identity used by SEO, sitemap and
 * structured data. Change it here only.
 */
export const SITE = {
  name: "Mostafa Samir",
  role: "Senior Full Stack Engineer",
  titleSuffix: "Mostafa Samir | Senior Full Stack Engineer",
  /** Absolute origin, no trailing slash. Override with VITE_SITE_URL if needed. */
  url:
    (import.meta.env["VITE_SITE_URL"] as string | undefined)?.replace(/\/$/, "") ??
    "https://portfolio-market.vercel.app",
  twitter: "@MostafaSaid",
  socials: SOCIAL_LINKS,
  navLinks: NAV_LINKS,
} as const;

/** Build an absolute URL when an origin is configured, else keep it relative. */
export function absoluteUrl(path: string): string {
  const normalized = path.startsWith("/") ? path : `/${path}`;
  return `${SITE.url}${normalized}`;
}
