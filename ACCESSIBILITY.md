# Accessibility Statement & Guidelines

We are dedicated to ensuring digital accessibility for all users, including people with visual, auditory, motor, or cognitive disabilities.

---

## 1. Conformance Standards

This project aims to conform to the **Web Content Accessibility Guidelines (WCAG) 2.1 Level AA** standards across both user-facing applications:
- The **Public Portfolio Site** (`portfolio/` — React 19 / TanStack Start).
- The **Admin Dashboard** (`dashboard/` — Angular 20 Standalone Components).

---

## 2. Core Accessibility Implementations

### Semantic HTML & ARIA Landmarks
- Valid landmark roles (`<header>`, `<main>`, `<nav>`, `<aside>`, `<footer>`) ensure clear screen-reader navigation.
- Buttons, inputs, modals, and tab lists include appropriate `aria-label`, `aria-expanded`, `aria-controls`, and `aria-selected` attributes.
- Interactive custom elements use appropriate `role` attributes and ARIA states.

### Keyboard Navigation & Focus Management
- All interactive elements (links, buttons, form controls, dropdown menus, pagination buttons) are fully accessible via keyboard (`Tab`, `Shift+Tab`, `Enter`, `Space`, `Escape`, arrow keys).
- Visible focus rings (`focus-visible:ring-2`, `outline-offset-2`) are maintained across all light and dark themes.
- Modals, confirm dialogs, and slide-out mobile drawers trap focus while open and return focus to the triggering element upon close (`Escape` key supported).

### Color Contrast & Themes
- Dark and light theme semantic color tokens adhere to a minimum contrast ratio of **4.5:1** for regular body text and **3:1** for large text and UI components.
- State indicators (e.g. read/unread message badges, error states, form validations) do not rely on color alone; icons and descriptive text accompany every state change.

### Reduced Motion & Responsive Layouts
- CSS and animation libraries support the `prefers-reduced-motion: reduce` media query to disable non-essential animations.
- Clean responsive layout scaling from mobile viewports (320px) up to 4K displays without content clipping or horizontal overflow.

---

## 3. Contributor Accessibility Checklist

When contributing UI features or components to `portfolio/` or `dashboard/`:

1. [ ] Ensure all form fields have associated `<label>` elements or accessible `aria-label` tags.
2. [ ] Test keyboard navigation through the entire user flow without using a mouse.
3. [ ] Verify contrast ratios using automated audits (e.g. Chrome DevTools / Lighthouse / Axe).
4. [ ] Provide descriptive `alt` attributes for meaningful imagery; use `alt=""` for purely decorative illustrations.
5. [ ] Avoid autofocusing elements unexpectedly when users navigate between routes.

---

## 4. Reporting Accessibility Barriers

If you experience any accessibility barriers while using the Portfolio or Dashboard:

- Open an issue on GitHub using our [Issue Template](.github/ISSUE_TEMPLATE/bug_report.md) with the label `accessibility`.
- Specify the affected URL or component, the assistive technology used (e.g. NVDA, VoiceOver, JAWS), and the observed barrier.
- We prioritize accessibility defects and aim to resolve them promptly.
