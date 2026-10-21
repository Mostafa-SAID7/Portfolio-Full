import { cn } from "@/lib/utils";

export interface BrandLogoProps {
  /** Size variant: sm (navbar), md (preview), lg (footer) */
  size?: "sm" | "md" | "lg";
  /** Color theme variant: brand (accent red) or foreground (adaptive black/white) */
  variant?: "brand" | "foreground";
  className?: string;
  showText?: boolean;
}

const sizeConfig = {
  sm: {
    dot: "size-1",
    gap: "gap-[2px]",
    badge: "rounded-[4px] px-1.5 py-0.5",
    wordmark: "[--wordmark-size:0.75rem]",
    text: "type-micro",
  },
  md: {
    dot: "size-1",
    gap: "gap-[2px]",
    badge: "rounded-[6px] px-2.5 py-0.5",
    wordmark: "[--wordmark-size:0.875rem]",
    text: "type-micro",
  },
  lg: {
    dot: "size-1.5",
    gap: "gap-[3px]",
    badge: "rounded-[8px] px-2.5 py-1",
    wordmark: "[--wordmark-size:1.25rem] sm:[--wordmark-size:1.5rem]",
    text: "type-micro",
  },
};

/**
 * Reusable brand identity mark:
 * - 3 dot geometric prefix
 * - "MS" wordmark square badge
 * - Stacked "MOSTAFA SAMIR" latin typography
 */
export function BrandLogo({
  size = "md",
  variant = "brand",
  className,
  showText = true,
}: BrandLogoProps) {
  const cfg = sizeConfig[size];
  const isBrand = variant === "brand";

  const dotBg = isBrand ? "bg-brand" : "bg-foreground";
  const badgeBg = isBrand ? "bg-brand text-white" : "bg-foreground text-background";

  return (
    <div className={cn("inline-flex items-center gap-2 select-none", className)}>
      {/* Geometric prefix dots */}
      <div className={cn("flex items-end opacity-90", cfg.gap)} aria-hidden="true">
        <span className={cn("rounded-full mb-[1px]", cfg.dot, dotBg)} />
        <div className={cn("flex flex-col", cfg.gap)}>
          <span className={cn("rounded-full", cfg.dot, dotBg)} />
          <span className={cn("rounded-full", cfg.dot, dotBg)} />
        </div>
      </div>

      {/* MS Square Badge */}
      <div className={cn("grid place-items-center shadow-sm transition-transform duration-300", cfg.badge, badgeBg)}>
        <span className={cn("keep-latin type-wordmark", cfg.wordmark)}>MS</span>
      </div>

      {/* Stacked Wordmark */}
      {showText && (
        <div className={cn("keep-latin flex flex-col text-start text-foreground", cfg.text)}>
          <span>MOSTAFA</span>
          <span>SAMIR</span>
        </div>
      )}
    </div>
  );
}
