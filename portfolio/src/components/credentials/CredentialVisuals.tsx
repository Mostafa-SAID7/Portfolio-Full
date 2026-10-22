import { m as motion } from "framer-motion";
import type { CredentialItem } from "@/data";
import { EASE } from "@/lib/motion";
import { iconForCredential } from "./credentialIcon";

interface CredentialVisualsProps {
  /** The credentials shown in the active slide — their icons ARE the artwork. */
  slide: CredentialItem[];
  dir: number;
  reduce: boolean;
}

/**
 * The artwork of the credentials carousel.
 *
 * It used to be three decorative Lottie files (5–7 KB each) that had to pull
 * the 1.2 MB dotLottie WebAssembly runtime into a page that is already above
 * the fold — and the frame stayed visibly EMPTY until (or unless) that
 * runtime arrived. The icons of the credentials themselves are the better
 * artwork: they are instant, they are the RIGHT symbol for the credential
 * type, they follow the light/dark theme, and they cost nothing.
 *
 * The slides cross-fade exactly like the artwork did, so the rhythm of the
 * carousel is unchanged.
 */
export function CredentialVisuals({ slide, dir, reduce }: CredentialVisualsProps) {
  const offset = reduce ? 0 : dir * 56;

  return (
    <div className="relative mx-auto w-full max-w-md overflow-hidden rounded-2xl border border-border bg-foreground/5 [contain:paint] md:max-w-lg lg:col-span-4 lg:col-start-1 lg:row-start-2 lg:max-w-none lg:self-start">
      <div className="relative aspect-[4/3] w-full sm:aspect-[16/10] lg:aspect-[5/4]">
        {/* Ambient glow so the frame reads as artwork, not as an empty box. */}
        <div
          aria-hidden="true"
          className="absolute inset-0 bg-[radial-gradient(circle_at_50%_45%,color-mix(in_oklab,var(--primary)_18%,transparent),transparent_62%)]"
        />

        <motion.div
          key={slide.map((item) => item.id).join("|")}
          initial={reduce ? { opacity: 0 } : { opacity: 0, x: offset }}
          animate={{ opacity: 1, x: 0 }}
          transition={
            reduce
              ? { duration: 0.2 }
              : { x: { type: "spring", stiffness: 480, damping: 44, mass: 0.55 }, opacity: { duration: 0.28, ease: EASE } }
          }
          className="absolute inset-0 grid grid-cols-3 items-center justify-items-center gap-3 p-6 sm:gap-5 sm:p-8"
        >
          {slide.map((item, index) => {
            const Icon = iconForCredential(item.tab);
            return (
              <span
                key={item.id}
                className="float-soft grid aspect-square w-full max-w-24 place-items-center rounded-2xl border border-border/60 bg-card/70 text-primary shadow-glow backdrop-blur-sm sm:max-w-28"
                style={reduce ? undefined : { animationDelay: `${index * 0.55}s` }}
                title={item.title.en || item.title.ar}
              >
                <Icon className="size-7 shrink-0 sm:size-9" strokeWidth={1.6} />
              </span>
            );
          })}
        </motion.div>
      </div>
    </div>
  );
}
