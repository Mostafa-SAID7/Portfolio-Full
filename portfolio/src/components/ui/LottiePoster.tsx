/**
 * Zero-byte stand-in shown where a decorative Lottie will eventually render.
 *
 * Pure CSS (design tokens only), so first visit costs nothing: the ~1.2 MB
 * dotLottie runtime is only fetched once the user hovers, taps or focuses the
 * artwork. Marked decorative — screen readers ignore it.
 */
export function LottiePoster({ className = "" }: { className?: string }) {
  return (
    <div
      aria-hidden="true"
      className={`relative size-full overflow-hidden rounded-[2rem] border border-border/50 bg-card/40 ${className}`}
    >
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_30%_25%,color-mix(in_oklab,var(--primary)_28%,transparent),transparent_60%)]" />
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_72%_78%,color-mix(in_oklab,var(--accent)_24%,transparent),transparent_62%)]" />
      <div className="absolute inset-6 rounded-[1.5rem] border border-border/40" />
      <div className="absolute inset-0 grid place-items-center">
        <span className="size-16 rounded-full bg-primary/15 blur-xl" />
      </div>
    </div>
  );
}
