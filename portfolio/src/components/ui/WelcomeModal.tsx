import { Link } from "@tanstack/react-router";
import { ArrowRight, Sparkles, X } from "lucide-react";
import { m as motion, AnimatePresence, type Transition } from "framer-motion";
import { useI18n } from "@/lib/i18n";
import { LottieIcon } from "@/components/ui/LottieIcon";
import { useWelcomeModal, RING_RADIUS, CIRCUMFERENCE } from "@/hooks/useWelcomeModal";

const AUTO_CLOSE_SECONDS = 5;

/**
 * Welcome invitation modal that appears once per session.
 * Interaction & state management is separated into `useWelcomeModal` hook (SRP).
 */
export function WelcomeModal() {
  const { tr, dir } = useI18n();
  const {
    open,
    close,
    seconds,
    paused,
    progress,
    dashOffset,
    closeRef,
    dialogRef,
    hold,
    release,
    prefersReducedMotion,
  } = useWelcomeModal();

  if (!open) return null;

  const transition: Transition = prefersReducedMotion
    ? { duration: 0 }
    : { duration: 0.35, ease: [0.22, 1, 0.36, 1] };

  return (
    <AnimatePresence>
      <motion.div
        key="welcome-backdrop"
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        exit={{ opacity: 0 }}
        transition={transition}
        className="fixed inset-0 z-[100] flex items-center justify-center p-3 sm:p-6"
        role="presentation"
        onClick={close}
      >
        <div className="absolute inset-0 bg-foreground/60 backdrop-blur-sm" />

        <motion.div
          ref={dialogRef}
          role="dialog"
          aria-modal="true"
          aria-labelledby="welcome-modal-title"
          aria-describedby="welcome-modal-body welcome-modal-desc welcome-modal-countdown"
          tabIndex={-1}
          dir={dir}
          onClick={(event) => event.stopPropagation()}
          onMouseEnter={hold}
          onMouseLeave={release}
          onFocusCapture={(event) => {
            if ((event.target as Node) !== closeRef.current) hold();
          }}
          onBlurCapture={(event) => {
            const next = event.relatedTarget as Node | null;
            if (!next || !dialogRef.current?.contains(next)) release();
          }}
          onTouchStart={hold}
          onTouchEnd={release}
          initial={{ opacity: 0, scale: 0.92, y: 24 }}
          animate={{ opacity: 1, scale: 1, y: 0 }}
          exit={{ opacity: 0, scale: 0.96, y: 16 }}
          transition={transition}
          className="relative w-full max-w-[min(100%,34rem)] overflow-hidden rounded-2xl border border-border bg-card shadow-2xl md:max-w-3xl"
        >
          <div className="grid-bg pointer-events-none absolute inset-0 opacity-40" />

          <button
            ref={closeRef}
            type="button"
            onClick={close}
            aria-label={tr("welcome.close")}
            className="absolute end-3 top-3 z-20 rounded-xl bg-card/80 p-2 text-muted-foreground transition-colors hover:bg-muted hover:text-foreground sm:end-4 sm:top-4"
          >
            <X className="h-4 w-4" />
          </button>

          {/* Two-column layout: Visual rail + Message column */}
          <div className="relative grid gap-6 p-6 pb-10 text-center sm:p-8 sm:pb-12 md:grid-cols-[minmax(0,13rem)_minmax(0,1fr)] md:items-stretch md:gap-8 md:p-10 md:pb-14 md:text-start">
            {/* Visual rail */}
            <div className="grid min-h-[22rem] grid-rows-2 items-center justify-items-center md:min-h-full">
              {/* Top half — Lottie */}
              <div className="flex h-full w-full flex-col items-center justify-center gap-3">
                <LottieIcon
                  src="/lottie/welcome-hello.lottie"
                  className="h-full max-h-[8.5rem] w-auto min-w-32"
                  fallback={<Sparkles className="h-12 w-12 text-accent" />}
                />
                <span className="chip">
                  <Sparkles className="h-3.5 w-3.5 shrink-0" />
                  {tr("welcome.eyebrow")}
                </span>
              </div>

              {/* Bottom half — countdown ring */}
              <motion.div
                aria-hidden="true"
                className="relative flex h-full w-full flex-col items-center justify-center gap-2"
                initial={false}
                animate={paused && !prefersReducedMotion ? { scale: 1.02 } : { scale: 1 }}
                transition={{ duration: 0.25 }}
              >
                <div className="relative grid aspect-square h-full max-h-[8.5rem] place-items-center">
                  <svg
                    viewBox={`0 0 ${RING_RADIUS * 2 + 12} ${RING_RADIUS * 2 + 12}`}
                    className="absolute inset-0 size-full -rotate-90"
                  >
                    <circle
                      cx={RING_RADIUS + 6}
                      cy={RING_RADIUS + 6}
                      r={RING_RADIUS}
                      className="fill-none stroke-muted"
                      strokeWidth="6"
                    />
                    <motion.circle
                      cx={RING_RADIUS + 6}
                      cy={RING_RADIUS + 6}
                      r={RING_RADIUS}
                      className="fill-none stroke-accent"
                      strokeWidth="6"
                      strokeLinecap="round"
                      strokeDasharray={CIRCUMFERENCE}
                      style={{ strokeDashoffset: dashOffset }}
                    />
                  </svg>

                  <div className="relative flex items-center justify-center">
                    {prefersReducedMotion ? (
                      <span className="type-h1 absolute font-bold tabular-nums text-foreground">
                        {paused ? "II" : seconds}
                      </span>
                    ) : (
                      <AnimatePresence mode="popLayout" initial={false}>
                        <motion.span
                          key={paused ? "paused" : seconds}
                          initial={{ opacity: 0, y: 12, scale: 0.8 }}
                          animate={{ opacity: 1, y: 0, scale: 1 }}
                          exit={{ opacity: 0, y: -12, scale: 0.8 }}
                          transition={{ duration: 0.25, ease: [0.22, 1, 0.36, 1] }}
                          className="type-h1 absolute font-bold tabular-nums text-foreground"
                        >
                          {paused ? "II" : seconds}
                        </motion.span>
                      </AnimatePresence>
                    )}
                  </div>
                </div>

                <motion.span
                  key={paused ? "paused" : "counting"}
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  className="mt-2 text-[10px] font-medium uppercase tracking-widest text-muted-foreground"
                >
                  {paused ? tr("welcome.paused") : tr("welcome.autocloseLabel")}
                </motion.span>
              </motion.div>
            </div>

            {/* Message column */}
            <div className="min-w-0">
              <h2 id="welcome-modal-title" className="type-h2 text-balance text-foreground">
                {tr("welcome.title")}
              </h2>

              <p id="welcome-modal-body" className="mt-3 type-lead text-muted-foreground">
                {tr("welcome.body")}
              </p>

              <p className="mt-3 type-body-strong text-accent">{tr("welcome.highlight")}</p>

              <div className="mt-6 grid grid-cols-1 gap-3 sm:grid-cols-2">
                <Link
                  to="/contact"
                  onClick={close}
                  className="btn-accent w-full items-center justify-center gap-2 whitespace-nowrap py-3.5 type-body-sm"
                >
                  {tr("welcome.primary")}
                  <ArrowRight className="h-4 w-4 shrink-0 rtl:rotate-180" />
                </Link>
                <Link
                  to="/projects"
                  onClick={close}
                  className="btn-accent-outline w-full items-center justify-center gap-2 whitespace-nowrap py-3.5 type-body-sm"
                >
                  {tr("welcome.secondary")}
                </Link>
              </div>

              <div className="mt-5 flex flex-col items-center gap-2 sm:flex-row sm:items-center sm:justify-between sm:gap-4">
                <p className="eyebrow min-w-0 text-muted-foreground">{tr("welcome.footnote")}</p>
                <span
                  aria-live="polite"
                  className="eyebrow shrink-0 tabular-nums text-muted-foreground"
                >
                  {paused
                    ? tr("welcome.paused")
                    : tr("welcome.autoclose").replace("{s}", String(seconds))}
                </span>
              </div>
            </div>
          </div>

          {/* Auto-close progress bar */}
          <div
            role="progressbar"
            aria-label={tr("welcome.autoclose").replace("{s}", String(seconds))}
            aria-valuemin={0}
            aria-valuemax={100}
            aria-valuenow={Math.round((seconds / AUTO_CLOSE_SECONDS) * 100)}
            className="absolute inset-x-0 bottom-0 h-1.5 overflow-hidden bg-muted"
          >
            <motion.div
              className="h-full w-full origin-left bg-accent rtl:origin-right"
              style={{ scaleX: progress, willChange: "transform" }}
            />
          </div>
        </motion.div>
      </motion.div>
    </AnimatePresence>
  );
}
