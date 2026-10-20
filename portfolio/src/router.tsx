import { QueryClient } from "@tanstack/react-query";
import { createRouter } from "@tanstack/react-router";
import { setupRouterSsrQueryIntegration } from "@tanstack/react-router-ssr-query";
import { routeTree } from "./routeTree.gen";

/**
 * Creates a new QueryClient with the default configuration.
 * A fresh instance is created per request on the server to prevent
 * cache leakage between concurrent users.
 */
function createQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        // Portfolio data is static-ish: avoid refetch storms on focus/mount.
        staleTime: 5 * 60 * 1000,
        gcTime: 30 * 60 * 1000,
        refetchOnWindowFocus: false,
        retry: 1,
      },
    },
  });
}

/**
 * The single router factory used by both SSR (server entry) and the browser
 * client. TanStack Start calls `getRouter()` once per request on the server
 * and once on the client — creating a fresh QueryClient each time prevents
 * cross-request state leakage.
 *
 * `setupRouterSsrQueryIntegration` automatically:
 *   - Dehydrates the QueryClient into the HTML stream after SSR rendering
 *   - Rehydrates it in the browser before the first client render
 * This eliminates the hydration mismatch caused by the client starting with
 * an empty QueryClient while the server had 14 projects in its cache.
 */
export function getRouter() {
  const queryClient = createQueryClient();

  const router = createRouter({
    routeTree,
    context: { queryClient },
    scrollRestoration: true,
    // Warm the next route (code + data) as soon as a link is hovered/focused.
    defaultPreload: "intent",
    defaultPreloadDelay: 40,
    defaultPreloadStaleTime: 30_000,
    // Avoid skeleton flashes on instant transitions.
    defaultPendingMs: 180,
    defaultPendingMinMs: 320,
  });

  // Automatically wire up SSR dehydration -> HTML stream -> client rehydration.
  setupRouterSsrQueryIntegration({ router, queryClient });

  return router;
}
