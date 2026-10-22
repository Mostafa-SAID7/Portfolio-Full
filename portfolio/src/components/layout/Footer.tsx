import { useState } from "react";
import { Github, Linkedin, Mail, MessageSquare, Play } from "lucide-react";
import { Link } from "@tanstack/react-router";
import { useI18n } from "@/lib/i18n";
import { LottieIcon } from "@/components/ui/LottieIcon";
import { NAV_LINKS, SOCIAL_LINKS } from "@/lib/site";
import { BrandLogo } from "@/components/ui/BrandLogo";

const socials = [
  {
    Icon: Github,
    lottie: SOCIAL_LINKS.github.lottie,
    label: SOCIAL_LINKS.github.label,
    href: SOCIAL_LINKS.github.url,
    bgClass: SOCIAL_LINKS.github.bgClass,
    featured: SOCIAL_LINKS.github.featured,
  },
  {
    Icon: Linkedin,
    lottie: SOCIAL_LINKS.linkedin.lottie,
    label: SOCIAL_LINKS.linkedin.label,
    href: SOCIAL_LINKS.linkedin.url,
    bgClass: SOCIAL_LINKS.linkedin.bgClass,
    featured: SOCIAL_LINKS.linkedin.featured,
  },
  {
    Icon: Mail,
    lottie: SOCIAL_LINKS.email.lottie,
    label: SOCIAL_LINKS.email.label,
    href: SOCIAL_LINKS.email.url,
    bgClass: SOCIAL_LINKS.email.bgClass,
    featured: SOCIAL_LINKS.email.featured,
  },
  {
    Icon: MessageSquare,
    lottie: SOCIAL_LINKS.whatsapp.lottie,
    label: SOCIAL_LINKS.whatsapp.label,
    href: SOCIAL_LINKS.whatsapp.url,
    bgClass: SOCIAL_LINKS.whatsapp.bgClass,
    featured: SOCIAL_LINKS.whatsapp.featured,
  },
];

type Social = (typeof socials)[number];


/**
 * Footer social tile: static lucide glyph by default, swapped for a matching
 * Lottie that replays on every hover / keyboard focus.
 */
function SocialTile({
  Icon,
  lottie,
  label,
  href,
  bgClass,
  featured,
}: Social & { featured?: boolean }) {
  const [plays, setPlays] = useState(0);
  const [active, setActive] = useState(false);

  const start = () => {
    setActive(true);
    setPlays((n) => n + 1);
  };

  return (
    <a
      href={href}
      target="_blank"
      rel="noopener noreferrer"
      aria-label={label}
      onMouseEnter={start}
      onFocus={start}
      onMouseLeave={() => setActive(false)}
      onBlur={() => setActive(false)}
      className={`group relative grid place-items-center border-[3.5px] border-[var(--social-foreground)] shadow-md transition-all duration-300 hover:-translate-y-1.5 hover:shadow-xl ${bgClass} ${
        featured
          ? "size-16 sm:size-20 md:size-[5rem] rounded-xl md:rounded-xl scale-105"
          : "size-13 sm:size-15 md:size-[4.15rem] rounded-xl md:rounded-xl"
      }`}
    >
      <Icon
        className={`text-[var(--social-foreground)] transition-all duration-200 ${
          active ? "scale-90 opacity-0" : "opacity-100"
        } ${featured ? "size-8 sm:size-9" : "size-6 sm:size-7"}`}
        strokeWidth={2.2}
      />
      {plays > 0 && (
        <span
          aria-hidden="true"
          className={`pointer-events-none absolute inset-0 grid place-items-center transition-opacity duration-200 ${
            active ? "opacity-100" : "opacity-0"
          }`}
        >
          <LottieIcon
            key={plays}
            src={lottie}
            loop={false}
            playOnce
            speed={1.1}
            // The project badge is a small delight, not content: it must not
            // drag the 1.2 MB dotLottie runtime into a page just because the
            // footer scrolled into view. It loads on the first real intent.
            activateOn="interaction"
            className={featured ? "size-10 sm:size-11" : "size-8 sm:size-9"}
          />
        </span>
      )}
      {featured && (
        <span className="absolute -bottom-3 grid size-6 sm:size-7 place-items-center rounded-xl bg-card shadow-md border border-border">
          <Play className="size-3 sm:size-3.5 fill-primary text-primary ms-0.5" />
        </span>
      )}
    </a>
  );
}

export function Footer() {
  const { tr } = useI18n();

  return (
    <footer className="w-full text-foreground select-none overflow-hidden">
      {/* Main Footer Container */}
      <div className="section-shell pb-10">
        <div className="container-page">
          {/* Inner Card Panel */}
          <div className="relative rounded-2xl bg-card p-8 sm:p-10 md:p-14 border border-t-0 border-border shadow-glow mb-8 md:mb-12 overflow-hidden">
            {/* Center Divider */}
            <div className="hidden md:block absolute left-1/2 top-0 bottom-0 w-[1px] bg-border -translate-x-1/2 pointer-events-none" />

            <div className="flex flex-col items-center justify-between gap-8 md:flex-row md:gap-6 relative z-10">
              {/* Headline */}
              <h2 className="type-h2 text-card-foreground text-center md:text-start">
                {tr("footer.headline1")}
                <br />
                {tr("footer.headline2")}
              </h2>

              {/* Social Icons Row */}
              <div className="flex flex-wrap items-center justify-center gap-3 sm:gap-4 md:gap-5 py-1">
                {socials.map((social) => (
                  <SocialTile key={social.label} {...social} />
                ))}
              </div>
            </div>
          </div>

          {/* Bottom Bar */}
          <div className="flex flex-col items-center justify-between gap-6 px-2 md:flex-row md:px-4">
            {/* Logo & Brand Name */}
            <Link to="/" className="flex items-center gap-3.5 group" aria-label="Home">
              <BrandLogo
                size="lg"
                variant="foreground"
                className="transition-transform duration-300 group-hover:scale-105"
              />
            </Link>

            {/* Navigation Links */}
            <nav className="flex flex-wrap items-center justify-center gap-6 sm:gap-8 md:gap-10">
              {NAV_LINKS.map((item) => (
                <Link
                  key={item.key}
                  to={item.to}
                  className="nav-label text-foreground transition-opacity duration-200 hover:opacity-75"
                >
                  {tr(item.key)}
                </Link>
              ))}
            </nav>

            {/* Copyright */}
            <p className="text-center md:text-end type-micro text-foreground/80">
              {tr("footer.copyright").replace("{year}", String(new Date().getFullYear()))}
              <br />
              {tr("footer.rights")}
            </p>
          </div>
        </div>
      </div>
    </footer>
  );
}
