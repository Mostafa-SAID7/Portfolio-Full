"use client";

import { useEffect, useState } from "react";
import { useRouterState } from "@tanstack/react-router";
import {
  ANALYTICS_EVENTS,
  analyticsConfigured,
  analyticsConsent,
  gtmConfigured,
  initAnalytics,
  initGtm,
  restoreAnalyticsConsent,
  setAnalyticsConsent,
  trackEvent,
  trackPageView,
  type AnalyticsConsent
} from "@/lib/analytics";

/**
 * Analytics bridge: loads GA4 after consent and reports one page view per
 * route change.
 *
 * The router hook is the single source of truth for "the visitor is looking at
 * X now" – the automatic Enhanced‑Measurement page view is switched off in
 * `initAnalytics`, so a client‑side navigation produces exactly one event.
 */
export function Analytics() {
  const [consent, setConsent] = useState<AnalyticsConsent>("denied");
  const location = useRouterState({ select: (state) => state.location });

  // Restore the stored choice and (re)start analytics when it was granted.
  useEffect(() => {
    setConsent(restoreAnalyticsConsent());
    // Restore the GTM container too if consent was granted previously.
    if (gtmConfigured()) initGtm();
  }, []);

  // One page view per route change – path only, never the query string.
  useEffect(() => {
    if (analyticsConsent() !== "granted") return;
    const path = location.pathname;
    const title = typeof document !== "undefined" ? document.title : undefined;
    trackPageView(path, title);
  }, [location.pathname]);

  if (!analyticsConfigured() && !gtmConfigured()) return null;

  return (
    <ConsentPrompt
      consent={consent}
      onGrant={() => {
        setAnalyticsConsent("granted");
        initAnalytics();
        if (gtmConfigured()) initGtm();
        setConsent("granted");
        // The page-view effect is keyed on the pathname, so it does not fire
        // on a plain consent change — send the first view explicitly.
        trackPageView(location.pathname, document.title);
        trackEvent(ANALYTICS_EVENTS.ctaClick, { cta: "analytics_consent_granted" });
      }}
      onDeny={() => {
        setAnalyticsConsent("denied");
        setConsent("denied");
      }}
    />
  );
}

/**
 * A single, quiet consent pill. It is the only place where analytics can be
 * turned on, so the visitor always has a visible control — and GA4 is never
 * loaded before this answer.
 */
function ConsentPrompt({
  consent,
  onGrant,
  onDeny
}: {
  consent: AnalyticsConsent;
  onGrant: () => void;
  onDeny: () => void;
}) {
  const [dismissed, setDismissed] = useState(false);
  if (consent !== "denied" || dismissed) return null;

  return (
    <div
      role="dialog"
      aria-label="Analytics"
      className="fixed bottom-4 start-4 z-[150] flex max-w-[22rem] flex-col gap-3 rounded-2xl border border-border bg-card/95 p-4 text-start shadow-glow backdrop-blur-md"
    >
      <p className="type-body text-card-foreground/85">
        May I collect anonymous usage statistics (no personal data) to see which parts of this
        portfolio are useful?
      </p>
      <div className="flex flex-wrap gap-2">
        <button type="button" className="btn-accent type-label" onClick={onGrant}>
          Allow
        </button>
        <button
          type="button"
          className="btn-surface type-label"
          onClick={() => {
            setDismissed(true);
            onDeny();
          }}
        >
          No thanks
        </button>
      </div>
    </div>
  );
}
