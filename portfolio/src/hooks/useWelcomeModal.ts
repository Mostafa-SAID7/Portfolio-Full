import { useCallback, useEffect, useRef, useState } from "react";
import {
  animate,
  useMotionValue,
  useReducedMotion,
  useTransform,
  type MotionValue,
} from "framer-motion";
import { sessionFlags } from "@/lib/storage";
import { holdBodyScroll } from "@/lib/scroll-lock";

const DELAY_MS = 10_000;
const AUTO_CLOSE_MS = 5_000;
export const RING_RADIUS = 54;
export const CIRCUMFERENCE = 2 * Math.PI * RING_RADIUS;

export interface UseWelcomeModalResult {
  open: boolean;
  close: () => void;
  seconds: number;
  paused: boolean;
  progress: MotionValue<number>;
  dashOffset: MotionValue<number>;
  closeRef: React.RefObject<HTMLButtonElement | null>;
  dialogRef: React.RefObject<HTMLDivElement | null>;
  hold: () => void;
  release: () => void;
  prefersReducedMotion: boolean | null;
}

/**
 * Handles the session persistence, entrance delay, auto-dismiss countdown,
 * pause-on-hover/touch interactions, and accessibility focus trap for WelcomeModal.
 */
export function useWelcomeModal(): UseWelcomeModalResult {
  const prefersReducedMotion = useReducedMotion();
  const [open, setOpen] = useState(false);
  const [seconds, setSeconds] = useState(AUTO_CLOSE_MS / 1000);
  const [paused, setPaused] = useState(false);
  const progress = useMotionValue(1);
  const dashOffset = useTransform(progress, (v: number) => CIRCUMFERENCE * (1 - v));

  const closeRef = useRef<HTMLButtonElement | null>(null);
  const dialogRef = useRef<HTMLDivElement | null>(null);
  const previouslyFocused = useRef<Element | null>(null);

  const close = useCallback(() => {
    setOpen(false);
    sessionFlags.markWelcomeSeen();
    if (previouslyFocused.current instanceof HTMLElement) {
      previouslyFocused.current.focus();
    }
  }, []);

  // Show once per session after initial entrance delay
  useEffect(() => {
    if (sessionFlags.welcomeSeen()) return;
    const timer = window.setTimeout(() => setOpen(true), DELAY_MS);
    return () => window.clearTimeout(timer);
  }, []);

  // Focus trapping and Escape key dismiss
  useEffect(() => {
    if (!open) return;
    previouslyFocused.current = document.activeElement;
    closeRef.current?.focus();

    const releaseScroll = holdBodyScroll();

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        event.stopPropagation();
        close();
        return;
      }
      if (event.key !== "Tab") return;

      const root = dialogRef.current;
      if (!root) return;
      const focusables = Array.from(
        root.querySelectorAll<HTMLElement>(
          'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])',
        ),
      ).filter((el) => el.offsetParent !== null || el === document.activeElement);
      if (focusables.length === 0) return;

      const first = focusables[0]!;
      const last = focusables[focusables.length - 1]!;
      const active = document.activeElement as HTMLElement | null;

      if (!active || !root.contains(active)) {
        event.preventDefault();
        first.focus();
      } else if (event.shiftKey && active === first) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && active === last) {
        event.preventDefault();
        first.focus();
      }
    };
    document.addEventListener("keydown", onKeyDown);

    return () => {
      document.removeEventListener("keydown", onKeyDown);
      releaseScroll();
    };
  }, [open, close]);

  // Reset countdown upon opening
  useEffect(() => {
    if (!open) return;
    progress.set(1);
    setSeconds(AUTO_CLOSE_MS / 1000);
  }, [open, progress]);

  // Sync visible seconds counter
  useEffect(() => {
    const unsubscribe = progress.on("change", (value) => {
      const next = Math.max(0, Math.ceil((value * AUTO_CLOSE_MS) / 1000));
      setSeconds((current) => (current === next ? current : next));
    });
    return unsubscribe;
  }, [progress]);

  // Run progress animation unless paused
  useEffect(() => {
    if (!open || paused) return;
    const left = progress.get() * AUTO_CLOSE_MS;
    if (left <= 0) {
      close();
      return;
    }

    if (prefersReducedMotion) {
      let remaining = Math.ceil(left / 1000) * 1000;
      const id = window.setInterval(() => {
        remaining -= 1000;
        progress.set(Math.max(0, remaining / AUTO_CLOSE_MS));
        if (remaining <= 0) {
          window.clearInterval(id);
          close();
        }
      }, 1000);
      return () => window.clearInterval(id);
    }

    const controls = animate(progress, 0, {
      duration: left / 1000,
      ease: "linear",
      onComplete: close,
    });
    return () => controls.stop();
  }, [open, paused, progress, close, prefersReducedMotion]);

  const hold = () => setPaused(true);
  const release = () => setPaused(false);

  return {
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
    prefersReducedMotion: prefersReducedMotion ?? false,
  };
}
