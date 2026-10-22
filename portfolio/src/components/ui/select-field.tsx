"use client";

import { useEffect, useId, useState, type ReactNode } from "react";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { cn } from "@/lib/utils";

export interface SelectFieldOption {
  value: string;
  label: string;
}

export interface SelectFieldProps {
  /** Visible label. Rendered above the control and referenced by it. */
  label: string;
  /** Controlled value. An empty string shows the placeholder. */
  value: string;
  onValueChange: (value: string) => void;
  options: readonly SelectFieldOption[];
  placeholder?: string;
  disabled?: boolean;
  /** id of the element that describes the control (usually the error text). */
  describedBy?: string | undefined;
  invalid?: boolean | undefined;
  required?: boolean | undefined;
  /** Explicit control id; a stable one is generated otherwise. */
  id?: string;
  /** Extra classes for the wrapper. */
  className?: string;
  /** Extra classes for the label (use `sr-only` for a visually hidden one). */
  labelClassName?: string;
  /** Extra classes for the trigger (merged over the shared defaults). */
  triggerClassName?: string;
  /** Optional leading icon inside the trigger. */
  icon?: ReactNode;
}

/**
 * The one select in the app.
 *
 * A native `<select>` renders its popup with the operating system: on Windows
 * it is a light panel that ignores the dark theme, so the option text almost
 * disappears against it. This component therefore draws the control with the
 * same Radix primitives the projects explorer already uses, wrapped in the
 * site's own field styling — so every dropdown in the portfolio looks and
 * behaves identically.
 *
 * It also owns the two behaviours that are easy to forget per call site:
 *  - SSR safety (Radix's SelectProvider cannot render on the server), and
 *  - the scroll-lock flag that keeps the page scrollbar visible while a
 *    small picker is open (see `.select-scroll-keep` in styles.css).
 */
export function SelectField({
  label,
  value,
  onValueChange,
  options,
  placeholder,
  disabled,
  describedBy,
  invalid,
  required,
  id,
  className,
  labelClassName,
  triggerClassName,
  icon,
}: SelectFieldProps) {
  const generatedId = useId();
  const controlId = id ?? `select-field-${generatedId}`;
  const labelId = `${controlId}-label`;

  // Radix's SelectProvider calls useMemo internally, which crashes during SSR
  // when React's dispatcher is null. Until hydration the control is rendered
  // as an inert button with the same look, so there is no layout shift.
  const [isClient, setIsClient] = useState(false);
  useEffect(() => setIsClient(true), []);

  const [open, setOpen] = useState(false);
  /*
   * Radix hides the page scrollbar while a picker is open. Flag the body for
   * exactly that window so styles.css can neutralize it (.select-scroll-keep).
   * The flag is derived from committed state and only ever removes its own
   * class: react-remove-scroll owns `data-scroll-locked` and the inline
   * overflow/margin styles, so clearing them from here would leave its lock
   * counter pointing at a page that can never scroll again.
   *
   * The flag is NOT dropped the moment the menu closes: Radix keeps the
   * scroll lock for the whole exit animation, so removing it early would make
   * the scrollbar blink out while the menu is still fading. It is dropped as
   * soon as the lock itself is gone.
   */
  useEffect(() => {
    const body = document.body;
    if (open) {
      body.classList.add("select-scroll-keep");
      return;
    }
    if (!body.hasAttribute("data-scroll-locked")) {
      body.classList.remove("select-scroll-keep");
      return;
    }
    const observer = new MutationObserver(() => {
      if (!body.hasAttribute("data-scroll-locked")) {
        body.classList.remove("select-scroll-keep");
        observer.disconnect();
      }
    });
    observer.observe(body, { attributes: true, attributeFilter: ["data-scroll-locked"] });
    return () => observer.disconnect();
  }, [open]);

  // Unmount safety net: the flag must never outlive the component.
  useEffect(
    () => () => {
      document.body.classList.remove("select-scroll-keep");
    },
    [],
  );

  const selectedLabel = options.find((option) => option.value === value)?.label;

  return (
    <div className={cn("relative", className)}>
      <label
        id={labelId}
        htmlFor={controlId}
        className={cn(LABEL_CLASS, labelClassName)}
      >
        {label}
      </label>

      {isClient ? (
        <Select
          value={value}
          onValueChange={onValueChange}
          open={open}
          onOpenChange={setOpen}
          disabled={disabled ?? false}
        >
          <SelectTrigger
            id={controlId}
            aria-labelledby={labelId}
            aria-describedby={describedBy}
            aria-invalid={invalid || undefined}
            aria-required={required || undefined}
            className={cn(TRIGGER_CLASS, triggerClassName)}
          >
            {icon}
            <SelectValue placeholder={placeholder} />
          </SelectTrigger>
          <SelectContent className={CONTENT_CLASS}>
            {options.map((option) => (
              <SelectItem key={option.value} value={option.value} className={ITEM_CLASS}>
                {option.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      ) : (
        // SSR placeholder: same box, same text, inert until hydration.
        <button
          id={controlId}
          type="button"
          disabled
          aria-labelledby={labelId}
          className={cn(TRIGGER_CLASS, "opacity-80", triggerClassName)}
        >
          <span className="truncate">{value && selectedLabel ? selectedLabel : placeholder}</span>
        </button>
      )}
    </div>
  );
}

/** Shared label look. */
const LABEL_CLASS = "mb-2 block type-body-strong text-card-foreground";

/**
 * Shared trigger look — the same box the text inputs use.
 *
 * `h-auto` is load-bearing: Radix's trigger ships with `h-9`, and tailwind-merge
 * can only drop it if this class names a height. Left at its default the field
 * was 36 px tall next to 50 px inputs; a caller that genuinely wants the compact
 * bar (the projects toolbar) still overrides this with its own `h-9`.
 */
const TRIGGER_CLASS =
  "h-auto w-full rounded-xl border border-border bg-foreground/10 px-4 py-3 text-start type-control text-foreground outline-none transition-colors placeholder:text-foreground/50 focus:border-primary/60 focus:bg-foreground/15 disabled:cursor-not-allowed disabled:opacity-60 data-[placeholder]:text-foreground/60";

/** Shared popup look — matches the projects explorer's filter dropdown. */
const CONTENT_CLASS = "rounded-xl border-border bg-card text-card-foreground";

/** Shared option look. */
const ITEM_CLASS =
  "cursor-pointer rounded-xl type-control text-card-foreground hover:bg-foreground/10 focus:bg-foreground/10";
