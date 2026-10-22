import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import {
  Outlet,
  createRootRouteWithContext,
  useRouter,
  HeadContent,
  Scripts,
} from "@tanstack/react-router";
import { Suspense, lazy, useEffect, useState, type ReactNode } from "react";

import appCss from "../styles.css?url";
import { createIsomorphicFn } from "@tanstack/react-start";
import { describeError } from "@/lib/error-capture";
import { LANG_COOKIE } from "@/lib/storage";
import { ThemeProvider, themeBootstrapScript } from "@/lib/theme";
import {
  ARABIC_FONTS_URL,
  asLang,
  I18nProvider,
  langBootstrapScript,
  useI18n,
  type Lang,
} from "@/lib/i18n";
import { MotionProvider } from "@/lib/motion-provider";
import { useSmoothScroll } from "@/lib/smooth-scroll";
import { PageSkeleton } from "@/components/ui/Skeletons";
import { AppPreloader, preloaderBootstrapScript } from "@/components/ui/AppPreloader";
import { Analytics } from "@/components/ui/Analytics";

// Client-only chrome and the rarely-hit 404 screen are pulled out of the
// initial bundle so the first route paints with the smallest possible JS.
const WelcomeModal = lazy(() =>
  import("@/components/ui/WelcomeModal").then((m) => ({ default: m.WelcomeModal })),
);
const Toaster = lazy(() => import("sonner").then((m) => ({ default: m.Toaster })));
const NotFoundScreen = lazy(() =>
  import("@/components/sections/NotFound").then((m) => ({ default: m.NotFound })),
);
// AppErrorBoundary imports 9 lucide icons — lazy-load so they are never
// evaluated unless an actual error occurs (never on the happy path).
const AppErrorBoundaryLazy = lazy(() =>
  import("@/components/ui/AppErrorBoundary").then((m) => ({ default: m.AppErrorBoundary }))
);

function NotFoundComponent() {
  return (
    <Suspense fallback={<PageSkeleton />}>
      <NotFoundScreen />
    </Suspense>
  );
}

/**
 * Runtime en → ar switch. First paint in Arabic is already covered by the
 * pre-paint bootstrap (see `langBootstrapScript`), whose link carries the
 * same `data-arabic-fonts` marker and is therefore left alone here.
 */
function ArabicFonts() {
  const { lang } = useI18n();

  useEffect(() => {
    if (lang !== "ar") return;

    // Guard against duplicate appends (e.g. strict-mode double effects).
    const existing = document.querySelector(`link[data-arabic-fonts]`);
    if (existing) {
      // Update href if it changed (e.g. after a deploy), then bail out.
      if ((existing as HTMLLinkElement).href !== ARABIC_FONTS_URL) {
        (existing as HTMLLinkElement).href = ARABIC_FONTS_URL;
      }
      return;
    }

    const link = document.createElement("link");
    link.rel = "stylesheet";
    link.href = ARABIC_FONTS_URL;
    link.setAttribute("data-arabic-fonts", "true");
    document.head.appendChild(link);

    // Cleanup: remove the link if this component ever unmounts (fixes M1).
    return () => {
      link.parentNode?.removeChild(link);
    };
  }, [lang]);

  return null;
}

function ErrorComponent({ error, reset }: { error: unknown; reset: () => void }) {
  return (
    <Suspense fallback={<PageSkeleton />}>
      <AppErrorBoundaryLazy error={error} reset={reset} />
    </Suspense>
  );
}

// The language cookie is only readable on the server, so the reader is an
// isomorphic function: erased from the client bundle (returns null there)
// while the server implementation dynamically imports TanStack's server
// module so it never enters the client graph (import-protection rules).
const readLangCookie = createIsomorphicFn()
  .server(async (): Promise<Lang | null> => {
    try {
      const { getCookie } = await import("@tanstack/react-start/server");
      return asLang(getCookie(LANG_COOKIE));
    } catch {
      return null;
    }
  })
  .client((): Lang | null => null);

export const Route = createRootRouteWithContext<{ queryClient: QueryClient }>()({
  // Search-param contract: `?lang=ar|en` is the shareable, device-portable
  // form of the language choice (validateSearch on the root keeps it alive
  // across every client navigation). The loader resolves the SSR language —
  // URL first (a shared link must render in its own language), then the
  // cookie (returning visitors) — and the serialized value is reused at
  // hydration, so server and client agree from the first byte.
  validateSearch: (search: Record<string, unknown>): { lang?: Lang } => {
    const lang = asLang(typeof search["lang"] === "string" ? (search["lang"] as string) : null);
    return lang ? { lang } : {};
  },
  loader: async (): Promise<{ lang: Lang | null }> => {
    if (typeof window !== "undefined") return { lang: null };
    return { lang: await readLangCookie() };
  },
  head: () => ({
    meta: [
      { charSet: "utf-8" },
      { name: "viewport", content: "width=device-width, initial-scale=1" },
      { title: "Mostafa Samir | Senior Full Stack Engineer" },
      {
        name: "description",
        content:
          "Portfolio of Mostafa Samir — .NET 8 microservices, React and Angular platforms, multi-vendor marketplaces and high-scale commerce systems.",
      },
      { name: "author", content: "Mostafa Samir" },
      { property: "og:type", content: "website" },
      { property: "og:site_name", content: "Mostafa Samir" },
      { name: "twitter:card", content: "summary_large_image" },
      { name: "theme-color", content: "#14100d" },
    ],
    links: [
      { rel: "icon", href: "/favicon.svg", type: "image/svg+xml" },
      { rel: "icon", href: "/favicon.png", type: "image/png", sizes: "64x64" },
      { rel: "apple-touch-icon", href: "/apple-touch-icon.png", sizes: "180x180" },
    ],
    scripts: [
      {
        type: "application/ld+json",
        children: JSON.stringify({
          "@context": "https://schema.org",
          "@type": "WebSite",
          name: "Mostafa Samir — Senior Full Stack Engineer",
          url: "/",
          author: { "@type": "Person", name: "Mostafa Samir" },
        }),
      },
    ],
  }),

  shellComponent: RootShell,
  component: RootComponent,
  notFoundComponent: NotFoundComponent,
  errorComponent: ErrorComponent,
});

const GOOGLE_FONTS_URL =
  "https://fonts.googleapis.com/css2?family=Archivo:wght@900&family=Caveat:wght@700&family=DM+Sans:wght@400;500;700&family=Oswald:wght@400;600;700&display=swap";
const DM_SANS_WOFF2_URL =
  "https://fonts.gstatic.com/s/dmsans/v17/rP2Yp2ywxg089UriI5-g4vlH9VoD8Cmcqbu0-K4.woff2";

const CRITICAL_CSS = `
:root{--font-sans:"DM Sans",system-ui,-apple-system,sans-serif;--font-display:"Oswald","Archivo",system-ui,-apple-system,sans-serif;--background:#14100d;--foreground:#f4ece4;--primary:#f0834e;--accent:#f0834e;--border:rgba(245,237,230,0.1);--spacing:0.25rem;--leading-relaxed:1.625;--nav-h:4.5rem}
html.light{--background:#faf6f1;--foreground:#1c1511;--primary:#f0834e;--accent:#f0834e;--border:rgba(28,21,17,0.1)}
*,::before,::after{box-sizing:border-box}
body{margin:0;background-color:var(--background);color:var(--foreground);font-family:var(--font-sans);-webkit-font-smoothing:antialiased}
.nav-offset{padding-top:clamp(5.5rem,8vw,7rem)}
.section-y{padding-block:clamp(3rem,6vw,5.5rem)}
.section-shell{position:relative}
.container-page{width:100%;max-width:72rem;margin-inline:auto;padding-inline:1.5rem}
.container-prose{width:100%;max-inline-size:56rem;margin-inline:auto}
header.max-w-2xl{max-width:42rem;margin-bottom:2rem;display:block}
.text-center{text-align:center}
@media(min-width:768px){.md\\:text-start{text-align:start}}
.mb-2{margin-bottom:0.5rem}
.mt-2{margin-top:calc(var(--spacing)*2)}
.eyebrow{display:inline-block;font-size:0.6875rem;font-weight:700;letter-spacing:0.25em;text-transform:uppercase;border-radius:0.75rem;background-color:rgba(245,237,230,0.1);color:var(--primary);border:1px solid var(--border);padding:0.25rem 0.75rem}
.type-h1{font-family:var(--font-display);font-size:clamp(2.25rem,6vw,4.25rem);font-weight:700;line-height:1.02;letter-spacing:-0.015em;text-transform:uppercase;margin:0;color:var(--foreground)}
/* Critical path for the LCP text
   NOTE: no color here on purpose. This rule is unlayered, so a colour in it
   would beat every Tailwind colour utility AND stop text from inheriting the
   colour of its surface — which made the dark "featured" cards render dark
   text on a dark background (invisible body copy in the light theme). The
   default copy colour lives in styles.css (base layer) where utilities can
   still override it. */
p,p.type-body,.type-body{margin-top:calc(var(--spacing)*2);font-size:0.9375rem;line-height:var(--leading-relaxed);text-wrap:pretty}
.text-foreground\\/90{color:color-mix(in oklab,var(--foreground) 90%,transparent)}
.text-foreground{color:var(--foreground)}
.text-primary{color:var(--primary)}
/* Pre-paint scroll lock: the splash covers the page while it settles, so the
   scrollbar must not appear (and the layout must not shift) behind it. Kept
   here as well as in styles.css so it applies on the very first paint, even
   before the external stylesheet has arrived. */
html[data-preloading],html[data-preloading] body{overflow:hidden}
`;

function RootShell({ children }: { children: ReactNode }) {
  return (
    <html lang="en" dir="ltr" suppressHydrationWarning>
      <head>
        {/* Bootstrap scripts come FIRST: an inline script that sits after a
            blocking stylesheet waits for that CSS to download and parse
            before it runs. Measured on the production build, that delay put
            the Arabic webfont request at 477ms instead of ~55ms. */}
        <script dangerouslySetInnerHTML={{ __html: themeBootstrapScript }} />
        <script dangerouslySetInnerHTML={{ __html: langBootstrapScript }} />
        <script dangerouslySetInnerHTML={{ __html: preloaderBootstrapScript }} />
        {/* Without JS nothing ever hydrates to dismiss the SSR-rendered splash,
            so it would cover the page permanently. */}
        <noscript>
          <style>{".app-preloader{display:none!important}"}</style>
        </noscript>

        {/* Critical inline CSS: handles typography, layout & variables for initial paint */}
        <style id="critical-css" dangerouslySetInnerHTML={{ __html: CRITICAL_CSS }} />

        {/* Early preconnects: start TLS handshakes in first few bytes */}
        <link rel="preconnect" href="https://fonts.googleapis.com" />
        <link rel="preconnect" href="https://fonts.gstatic.com" crossOrigin="anonymous" />

        {/* Font Preloading: fetch DM Sans woff2 immediately alongside HTML */}
        <link
          rel="preload"
          href={DM_SANS_WOFF2_URL}
          as="font"
          type="font/woff2"
          crossOrigin="anonymous"
        />

        {/* Application CSS: preloaded for high-priority fetch, then applied normally.
            The media=print async trick was causing FOUC — all Tailwind utilities
            (flex, hidden, nav-label, btn-accent, etc.) were missing until it loaded. */}
        <link rel="preload" as="style" href={appCss} />
        <link rel="stylesheet" href={appCss} />

        {/* Non-blocking Google Fonts: preload + async stylesheet + noscript fallback */}
        <link rel="preload" as="style" href={GOOGLE_FONTS_URL} />
        <link
          rel="stylesheet"
          href={GOOGLE_FONTS_URL}
          media="print"
          onLoad={(e) => {
            e.currentTarget.media = "all";
          }}
        />
        <noscript>
          <link rel="stylesheet" href={GOOGLE_FONTS_URL} />
        </noscript>

        <HeadContent />
      </head>
      <body suppressHydrationWarning>
        {children}
        <Scripts />
      </body>
    </html>
  );
}

function RootComponent() {
  const { queryClient } = Route.useRouteContext();
  // The SSR language contract, resolved in three layers (highest first):
  // 1. `?lang=` in the validated URL search — a shared link's own language,
  // 2. the language cookie read server-side in the root loader — a returning
  //    visitor's stored choice, so the server and client hydrate identically,
  // 3. null → the provider falls back to "en" and applies detected prefs
  //    after hydration.
  const search = Route.useSearch() as { lang?: Lang };
  const loaderData = Route.useLoaderData() as { lang?: Lang | null } | undefined;
  const ssrLang = search?.lang ?? (loaderData ? ((loaderData.lang ?? null) as Lang | null) : null);
  useSmoothScroll();
  // Overlay chrome is mounted after hydration so it never blocks first paint.
  const [hydrated, setHydrated] = useState(false);
  useEffect(() => setHydrated(true), []);

  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider>
        <I18nProvider initialLang={ssrLang}>
          <MotionProvider>
            <Analytics />
            <ArabicFonts />
            <AppPreloader />
            {hydrated && (
              <Suspense fallback={null}>
                <Toaster position="bottom-right" richColors />
                <WelcomeModal />
              </Suspense>
            )}
            {/* Required: nested routes render here. */}
            <Outlet />
          </MotionProvider>
        </I18nProvider>
      </ThemeProvider>
    </QueryClientProvider>
  );
}
