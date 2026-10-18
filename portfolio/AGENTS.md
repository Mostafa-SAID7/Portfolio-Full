# Base44 Dev Environment

## Stack
TanStack Start (React 19 + TanStack Router) portfolio site built with Vite 8 and
Bun. SSR is handled by TanStack Start's nitro/vercel preset.

## Running
```
docker compose -f docker-compose.base44.yml up -d
```
- Single service `web` on the `oven/bun:1.2` image, source bind-mounted at `/app`.
- `bun install --frozen-lockfile` then `bun run dev` (Vite dev server, live reload).
- Vite listens on `0.0.0.0:5173`, mapped to host port **3000**.
- `node_modules` lives in a named volume so installs don't pollute the host.

## Notes / quirks
- `vite.config.ts` sets `server.allowedHosts: true` so the preview's external
  hostname is accepted (Vite 8 blocks unknown hosts by default).
- No external secrets required. `VITE_SITE_URL` is optional and defaults to a
  lovable.app URL; compose sets it to the preview origin for SEO/sitemap.
- The nitro vercel preset logs a harmless `VERCEL_OIDC_TOKEN is not set` warning
  in dev — ignore it.
- TanStack Start's client entry is a virtual module
  (`/@id/virtual:tanstack-start-dev-client-entry`), not `/src/main.tsx`.

## Verify
- `curl -sf http://localhost:3000/` returns the SSR HTML document.
- Dev assets (`/src/styles.css`, the virtual client entry) return 200 with the
  external preview Host header.
