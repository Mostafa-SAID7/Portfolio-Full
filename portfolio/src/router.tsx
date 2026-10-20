import { QueryClient, dehydrate, hydrate } from "@tanstack/react-query";
import { createRouter } from "@tanstack/react-router";
import { routeTree } from "./routeTree.gen";

interface RouterContext {
  queryClient: QueryClient;
}

/**
 * Creates a new QueryClient with the default configuration.
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
 * Creates a router instance with a QueryClient.
 * On the server, this creates a fresh QueryClient for prefetching.
 * On the client, this creates a fresh QueryClient that will be hydrated.
 */
function createRouterInstance(queryClient: QueryClient) {
  return createRouter({
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
}

/**
 * Creates a router instance for the server with a fresh QueryClient.
 * Used during SSR for prefetching and dehydration.
 */
export function createServerRouter(): {
  router: ReturnType<typeof createRouterInstance>;
  queryClient: QueryClient;
} {
  const queryClient = createQueryClient();
  const router = createRouterInstance(queryClient);
  return { router, queryClient };
}

/**
 * Creates a router instance for the client with a hydrated QueryClient.
 * Used during client-side hydration.
 */
export function createClientRouter(dehydratedState?: unknown): {
  router: ReturnType<typeof createRouterInstance>;
  queryClient: QueryClient;
} {
  const queryClient = createQueryClient();

  // Hydrate the QueryClient with the dehydrated state from the server
  if (dehydratedState) {
    hydrate(queryClient, dehydratedState as ReturnType<typeof dehydrate>);
  }

  const router = createRouterInstance(queryClient);
  return { router, queryClient };
}

/**
 * Legacy export for backward compatibility.
 * Creates a router with a fresh QueryClient (used in tests or non-SSR contexts).
 */
export const getRouter = () => {
  const queryClient = createQueryClient();
  const router = createRouterInstance(queryClient);
  return router;
};
