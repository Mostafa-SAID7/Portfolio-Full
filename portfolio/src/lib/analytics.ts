/**
 * Google Analytics 4 collection for the public portfolio.
 *
 * Design rules (privacy first):
 *  - Nothing is loaded and no event is sent before the visitor consents.
 *  - No personally identifiable information ever reaches GA4: no names, no
 *    e‑mails, no message content, no query strings (the search term of the
 *    project explorer can contain personal data, so only the path is sent).
 *  - The Measurement ID is public by design (it is not a secret) and comes
 *    from the build environment.
 *  - Everything is a no-op when GA4 is not configured, so a build without a
 *    Measurement ID behaves exactly like today.
 */

/** The Measurement ID, e.g. "G-XXXXXXXXXX". Empty = analytics disabled. */
export const GA_MEASUREMENT_ID = (
  import.meta.env["VITE_GA_MEASUREMENT_ID"] as string | undefined
)?.trim() ?? "";

/** Every event the site reports. One place, so the names never drift. */
export const ANALYTICS_EVENTS = {
  /** Automatic – sent on every route change. */
  pageView: "page_view",
  /** A contact message was accepted by the API (never on a mere click). */
  contactSubmit: "contact_form_submission",
  /** The CV / resume file was downloaded. */
  cvDownload: "cv_download",
  /** Outbound profile links. */
  githubClick: "github_profile_click",
  linkedinClick: "linkedin_profile_click",
  /** A project card was actually seen, and its live demo was opened. */
  projectView: "project_view",
  projectDemo: "project_demo_click",
  /** A call‑to‑action was pressed. */
  ctaClick: "cta_click",
} as const;

export type AnalyticsEvent = (typeof ANALYTICS_EVENTS)[keyof typeof ANALYTICS_EVENTS];

/**
 * Event parameters. Kept deliberately small and free of personal data:
 * only short identifiers, never free text from the visitor.
 */
export type AnalyticsParams = Record<string, string | number | boolean | undefined>;

/** Where the visitor's choice is remembered. */
const CONSENT_KEY = "portfolio_analytics_consent";
export type AnalyticsConsent = "granted" | "denied";

type GtagWindow = Window & {
  gtag?: (...args: unknown[]) => void;
  dataLayer?: unknown[];
};

let initialized = false;
let consent: AnalyticsConsent = "denied";

function readConsent(): AnalyticsConsent {
  try {
    const stored = localStorage.getItem(CONSENT_KEY);
    return stored === "granted" ? "granted" : "denied";
  } catch {
    return "denied";
  }
}

/** Has the visitor allowed analytics? */
export function analyticsConsent(): AnalyticsConsent {
  return consent;
}

/**
 * Record the visitor's choice. Only "granted" starts GA4; "denied" stops it
 * and removes the class that keeps the script out of the page.
 */
export function setAnalyticsConsent(next: AnalyticsConsent): void {
  consent = next;
  try {
    localStorage.setItem(CONSENT_KEY, next);
  } catch {
    /* storage blocked – the choice lasts for this page view only */
  }
  if (next === "granted") {
    initAnalytics();
  } else {
    // Withdraw consent: forget the visitor for the rest of the session.
    consent = "denied";
  }
}

/** Is GA4 configured at all? */
export function analyticsConfigured(): boolean {
  return /^G-[A-Z0-9]+$/.test(GA_MEASUREMENT_ID);
}

/**
 * Loads gtag.js once and configures the property.
 *
 * `send_page_view: false` – the page views are sent by the router hook, so a
 * client‑side navigation produces exactly one event per route (no duplicates
 * from the automatic Enhanced‑Measurement page view).
 */
export function initAnalytics(): void {
  if (initialized || !analyticsConfigured()) return;
  if (typeof window === "undefined") return;

  const w = window as GtagWindow;
  w.dataLayer = w.dataLayer || [];
  if (typeof w.gtag !== "function") {
    w.gtag = function gtagShim(...args: unknown[]) {
      (w.dataLayer as unknown[]).push(args);
    };
  }

  const script = document.createElement("script");
  script.async = true;
  script.src = `https://www.googletagmanager.com/gtag/js?id=${GA_MEASUREMENT_ID}`;
  script.dataset["analyticsLoader"] = "true";
  document.head.appendChild(script);

  w.gtag("js", new Date());
  w.gtag("config", GA_MEASUREMENT_ID, {
    anonymize_ip: true,
    send_page_view: false,
    // The site is bilingual: the page title carries the active language, the
    // path carries the route. Nothing else is transmitted.
    allow_google_signals: false,
  });

  initialized = true;
}

/**
 * Reports an event. A no-op without configuration, without consent, or when
 * GA4 is blocked by an ad blocker – analytics must never break the site.
 */
export function trackEvent(event: AnalyticsEvent, params: AnalyticsParams = {}): void {
  if (!analyticsConfigured() || consent !== "granted") return;
  if (typeof window === "undefined") return;
  const w = window as GtagWindow;
  if (typeof w.gtag !== "function") return;
  const payload: Record<string, unknown> = {};
  for (const [key, value] of Object.entries(params)) {
    if (value === undefined) continue;
    payload[key] = value;
  }
  try {
    w.gtag("event", event, payload);
  } catch {
    /* never let analytics break the UI */
  }
}

/**
 * Reports a page view. Only the path is sent – query strings can carry
 * personal data (the project search), so they never leave the browser.
 */
export function trackPageView(path: string, title?: string): void {
  if (!analyticsConfigured() || consent !== "granted") return;
  if (typeof window === "undefined") return;
  const w = window as GtagWindow;
  if (typeof w.gtag !== "function") return;
  try {
    w.gtag("event", ANALYTICS_EVENTS.pageView, {
      page_path: path,
      page_location: `${window.location.origin}${path}`,
      ...(title ? { page_title: title } : {})
    });
  } catch {
    /* ignore */
  }
}

/** Restores the stored choice and starts GA4 when it was granted. */
export function restoreAnalyticsConsent(): AnalyticsConsent {
  consent = readConsent();
  if (consent === "granted") initAnalytics();
  return consent;
}
