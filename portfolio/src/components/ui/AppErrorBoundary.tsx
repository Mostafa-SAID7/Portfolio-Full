import { useState } from "react";
import { Link, useRouter } from "@tanstack/react-router";
import {
  AlertTriangle,
  RotateCcw,
  Home,
  Briefcase,
  ChevronDown,
  ChevronUp,
  Copy,
  Check,
  Terminal,
} from "lucide-react";
import { describeError } from "@/lib/error-capture";
import { reportAppError } from "@/lib/error-reporting";

export interface AppErrorBoundaryProps {
  error: unknown;
  reset?: () => void;
  title?: string;
  subtitle?: string;
  fullPage?: boolean;
}

export function AppErrorBoundary({
  error,
  reset,
  title,
  subtitle,
  fullPage = true,
}: AppErrorBoundaryProps) {
  const router = useRouter();
  const [showDetails, setShowDetails] = useState(false);
  const [copied, setCopied] = useState(false);

  // Format detailed error string safely
  const formattedError = describeError(error);

  // Report the error to telemetry
  reportAppError(error, { source: "AppErrorBoundary" });

  const handleRetry = () => {
    try {
      router.invalidate();
    } catch {
      // router might not be ready
    }
    if (reset) {
      reset();
    } else if (typeof window !== "undefined") {
      window.location.reload();
    }
  };

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(formattedError);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch {
      // clipboard access denied
    }
  };

  const content = (
    <div className="relative mx-auto w-full max-w-2xl overflow-hidden rounded-3xl border border-border/50 bg-card/80 p-8 shadow-2xl backdrop-blur-xl sm:p-12">
      {/* Ambient background glows */}
      <div
        className="pointer-events-none absolute -top-24 -left-24 size-64 rounded-full bg-primary/10 blur-3xl"
        aria-hidden="true"
      />
      <div
        className="pointer-events-none absolute -bottom-24 -right-24 size-64 rounded-full bg-amber-500/10 blur-3xl"
        aria-hidden="true"
      />

      <div className="relative z-10 flex flex-col items-center text-center">
        {/* Status icon badge */}
        <div className="mb-6 flex size-16 items-center justify-center rounded-2xl border border-amber-500/30 bg-amber-500/10 text-amber-500 shadow-inner">
          <AlertTriangle className="size-8 animate-pulse text-amber-500" />
        </div>

        {/* Eyebrow */}
        <span className="eyebrow mb-2 inline-block rounded-xl border border-border/60 bg-foreground/5 px-3 py-1 text-xs text-primary">
          Application Exception
        </span>

        {/* Heading */}
        <h1 className="type-h2 text-foreground">
          {title ?? "Something went wrong"}
        </h1>

        {/* Description */}
        <p className="type-body mt-3 max-w-lg text-muted-foreground">
          {subtitle ??
            "An unexpected error occurred while rendering this view. You can reload the section or navigate back to safety."}
        </p>

        {/* Interactive Action Buttons */}
        <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
          <button
            onClick={handleRetry}
            className="btn-accent inline-flex items-center gap-2 px-5 py-2.5 text-sm font-medium shadow-lg transition-transform active:scale-95"
          >
            <RotateCcw className="size-4" />
            <span>Try again</span>
          </button>

          <Link
            to="/"
            className="inline-flex items-center gap-2 rounded-xl border border-border bg-foreground/5 px-4 py-2.5 text-sm font-medium text-foreground transition-colors hover:bg-foreground/10"
          >
            <Home className="size-4 text-muted-foreground" />
            <span>Return Home</span>
          </Link>

          <Link
            to="/projects"
            className="inline-flex items-center gap-2 rounded-xl border border-border bg-foreground/5 px-4 py-2.5 text-sm font-medium text-foreground transition-colors hover:bg-foreground/10"
          >
            <Briefcase className="size-4 text-muted-foreground" />
            <span>Browse Projects</span>
          </Link>
        </div>

        {/* Collapsible diagnostic details */}
        <div className="mt-8 w-full border-t border-border/40 pt-6">
          <button
            type="button"
            onClick={() => setShowDetails((prev) => !prev)}
            className="inline-flex items-center gap-2 text-xs font-medium text-muted-foreground transition-colors hover:text-foreground"
          >
            <Terminal className="size-3.5" />
            <span>{showDetails ? "Hide technical diagnostic" : "Show technical diagnostic"}</span>
            {showDetails ? <ChevronUp className="size-3.5" /> : <ChevronDown className="size-3.5" />}
          </button>

          {showDetails && (
            <div className="mt-4 text-left">
              <div className="flex items-center justify-between rounded-t-xl border border-border/40 bg-muted/60 px-4 py-2 text-xs text-muted-foreground">
                <span className="font-mono">error_stack_trace.log</span>
                <button
                  type="button"
                  onClick={handleCopy}
                  className="inline-flex items-center gap-1.5 rounded px-2 py-1 text-xs hover:bg-foreground/10 transition-colors"
                >
                  {copied ? (
                    <>
                      <Check className="size-3 text-emerald-400" />
                      <span className="text-emerald-400">Copied!</span>
                    </>
                  ) : (
                    <>
                      <Copy className="size-3" />
                      <span>Copy error</span>
                    </>
                  )}
                </button>
              </div>
              <pre className="max-h-60 overflow-x-auto rounded-b-xl border border-t-0 border-border/40 bg-black/60 p-4 font-mono text-[11px] leading-relaxed text-zinc-300">
                {formattedError || "Unknown error occurred"}
              </pre>
            </div>
          )}
        </div>
      </div>
    </div>
  );

  if (fullPage) {
    return (
      <main className="flex min-h-screen items-center justify-center bg-background px-4 py-12">
        {content}
      </main>
    );
  }

  return <div className="p-4">{content}</div>;
}
