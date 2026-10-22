/**
 * The single owner of "freeze the page behind an overlay".
 *
 * App code used to capture `body.style.overflow`, write `hidden`, and hand the
 * captured value back on close. With more than one overlay that strands the
 * page: the second holder captures the first one's `hidden` and restores it
 * after everything is closed, so the scrollbar never comes back. A reference
 * count cannot drift — the class leaves the body only when the last holder
 * releases. See styles.css (`body.scroll-locked`).
 */
const LOCK_CLASS = "scroll-locked";

let holders = 0;

/** Lock the page and return the matching release (idempotent). */
export function holdBodyScroll(): () => void {
  if (typeof document === "undefined") return () => {};

  holders += 1;
  document.body.classList.add(LOCK_CLASS);

  let released = false;
  return () => {
    if (released) return;
    released = true;
    holders = Math.max(0, holders - 1);
    if (holders === 0) document.body.classList.remove(LOCK_CLASS);
  };
}
