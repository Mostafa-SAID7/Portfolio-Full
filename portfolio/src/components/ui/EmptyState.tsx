import { ReactNode } from "react";
import { FolderSearch, RotateCcw } from "lucide-react";

export interface EmptyStateProps {
  title?: string;
  message?: string;
  icon?: ReactNode;
  actionLabel?: string;
  onAction?: () => void;
  secondaryActionLabel?: string;
  onSecondaryAction?: () => void;
  className?: string;
}

export function EmptyState({
  title,
  message,
  icon,
  actionLabel,
  onAction,
  secondaryActionLabel,
  onSecondaryAction,
  className = "",
}: EmptyStateProps) {
  return (
    <div
      className={`relative mx-auto my-12 flex max-w-xl flex-col items-center justify-center overflow-hidden rounded-3xl border border-dashed border-border/70 bg-card/40 px-6 py-16 text-center backdrop-blur-md transition-all sm:px-12 ${className}`}
    >
      {/* Ambient background accent */}
      <div
        className="pointer-events-none absolute inset-0 -z-10 bg-radial from-primary/5 via-transparent to-transparent"
        aria-hidden="true"
      />

      {/* Icon badge */}
      <div className="mb-5 flex size-16 items-center justify-center rounded-2xl border border-border/60 bg-foreground/5 text-primary shadow-sm">
        {icon ?? <FolderSearch className="size-8 text-primary/80" />}
      </div>

      {/* Title */}
      {title && (
        <h3 className="type-h3 text-foreground font-semibold tracking-tight">
          {title}
        </h3>
      )}

      {/* Message */}
      {message && (
        <p className={`type-body max-w-md text-muted-foreground ${title ? "mt-2" : ""}`}>
          {message}
        </p>
      )}

      {/* Actions */}
      {(actionLabel || secondaryActionLabel) && (
        <div className="mt-6 flex flex-wrap items-center justify-center gap-3">
          {actionLabel && onAction && (
            <button
              type="button"
              onClick={onAction}
              className="btn-accent inline-flex items-center gap-2 px-4 py-2 text-sm font-medium shadow-sm transition-transform active:scale-95"
            >
              <RotateCcw className="size-3.5" />
              <span>{actionLabel}</span>
            </button>
          )}

          {secondaryActionLabel && onSecondaryAction && (
            <button
              type="button"
              onClick={onSecondaryAction}
              className="inline-flex items-center gap-2 rounded-xl border border-border bg-foreground/5 px-4 py-2 text-sm font-medium text-foreground transition-colors hover:bg-foreground/10"
            >
              <span>{secondaryActionLabel}</span>
            </button>
          )}
        </div>
      )}
    </div>
  );
}
