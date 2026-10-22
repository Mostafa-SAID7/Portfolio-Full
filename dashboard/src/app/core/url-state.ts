import { ActivatedRoute, Router } from '@angular/router';

/**
 * Shared persistence layer for pages whose position (search / page / filter)
 * lives in the URL: `/projects?q=&page=`, `/messages?filter=&page=`.
 *
 * Reload, bookmarks, shared links and the Back button all restore the exact
 * view — this replaces scattered per-page implementations. `null` values drop
 * their param, and every write is a replace-navigation so the history is not
 * flooded.
 */
export function replaceUrlParams(
  route: ActivatedRoute,
  router: Router,
  params: Record<string, string | number | null>,
): void {
  router.navigate([], {
    relativeTo: route,
    queryParams: params,
    queryParamsHandling: 'merge',
    replaceUrl: true,
  });
}

/** Reads an integer param, defaulting to null when missing/invalid or below `min`. */
export function readUrlInt(
  params: { get(key: string): string | null },
  key: string,
  min = 1,
): number | null {
  const raw = Number(params.get(key));
  return Number.isFinite(raw) && raw >= min ? Math.floor(raw) : null;
}
