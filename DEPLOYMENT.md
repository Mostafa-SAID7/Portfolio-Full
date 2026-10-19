# Production Deployment Guide — Portfolio-Full

Three deployables, one backend:

| App | Stack | Hosting | Deployment config in repo |
| --- | --- | --- | --- |
| `PortfolioAPI` | ASP.NET Core / .NET 9 + EF Core (SQL Server) | **MonsterASP.NET** (IIS) | `PortfolioAPI/src/PortfolioAPI/appsettings.Production.json` (local only, git-ignored) |
| `portfolio/` | TanStack Start (React 19, Vite 8, nitro SSR → Vercel Build Output API) | **Vercel** | `portfolio/vercel.json` |
| `dashboard/` | Angular 20 SPA | **Vercel** | `dashboard/vercel.json` |

How the frontends reach the API: both are served over **HTTPS by Vercel** and call
the API through a **same-origin `/api` server-side rewrite** (in each `vercel.json`)
that proxies to the MonsterASP API. This means **no CORS and no mixed-content
problems even while the API still has no TLS certificate.**

---

## 0. Verified status (2026-10-19)

Verified by direct HTTP probes — not claims:

- Backend live: `http://portfolio-market.runasp.net`
  - `/` → `302` to `/swagger` (root 404 fixed) ✔
  - `/api/health` → `200 {"status":"ok","database":"ok",...}` (real DB check) ✔
  - `/swagger/index.html` + `/swagger/v1/swagger.json` → `200` (Swagger ON in production by default) ✔
  - `/api/public/projects`, `/api/public/credentials` → `200` ✔
- **HTTPS on the API is NOT working yet** (TLS handshake fails) — manual panel step, see §6.
- Frontends: **production builds verified locally**, not yet deployed:
  - portfolio: `npm run build` → `.vercel/output/` (Vercel Build Output API v3: static + `__server.func` SSR) ✔
  - dashboard: `npm run build` → `dist/portfolio-dashboard/browser/index.html` ✔
- Tests: `PortfolioAPI` builds **0 errors**; `PortfolioAPI.IntegrationTests` compiles **0 errors**
  (running it needs Docker to pull `mcr.microsoft.com/mssql/server:2022-latest`, ~1.5 GB — not run);
  `PortfolioAPI.UnitTests` was **already broken at commit `1c79423`** (tests call old service
  names like `GetAllProjectsAsync` that don't exist in `IProjectService` even in HEAD — pre-existing,
  needs a rewrite against the current `Result<T>` API; listed in §6).

---

## 1. Backend — MonsterASP.NET (already deployed)

### Already done / how it works
- Publish: `dotnet publish PortfolioAPI\src\PortfolioAPI\PortfolioAPI.csproj -c Release -o publish`
- The publish folder contains `web.config` (IIS/ANCM), `PortfolioAPI.dll`, and **`appsettings.Production.json`
  with the real connection string** — that file is **local-only/git-ignored**, so secrets never
  enter GitHub. Upload the *contents* of `publish/` over the existing app folder (File Manager/FTP/Git),
  keeping `web.config` at the app root. The already-configured ASP.NET Core site picks it up.
- Startup is fail-fast: if `ConnectionStrings:DefaultConnection` is missing the app logs
  `Connection string 'DefaultConnection' is not configured…` and refuses to start (check §6 if the site 500s).
- Content root: IIS runs the app from the folder containing `web.config`, which is where
  `appsettings.Production.json` lives. (If you ever run `dotnet PortfolioAPI.dll` by hand,
  do it *from the app folder* — settings are resolved relative to the working directory.)
- Schema: at startup the API runs `EnsureCreated` + idempotent `ALTER TABLE` patches against the
  remote DB (no migrations; never destructive).

### Environment variables (optional — the file-based config already works)
Set these in the hosting panel only if you prefer env config (they override the JSON files):

| Variable | Value |
| --- | --- |
| `ConnectionStrings__DefaultConnection` | full SQL Server connection string |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `Cors__AllowedOrigins` | comma-separated allowed browser origins (only needed for *direct* browser calls, §4) |
| `Swagger__Enabled` | `true` (default) / `false` to hide `/swagger` |

### Updating the API later
Re-run the publish command, upload `publish/` again, done. No migrations to run.

---

## 2. Vercel — main portfolio (`portfolio/`)

1. Push the repository to GitHub (see §6 — commit not yet made).
2. Vercel → **Add New → Project** → import the repo → set **Root Directory = `portfolio`**.
3. Framework is auto-detected (Vite). Build command/output come from `portfolio/vercel.json`:
   - install: `npm ci` (pinned so the stray `bun.lock` never hijacks the build)
   - build: `npm run build` → nitro emits `.vercel/output/` → Vercel auto-switches to
     **Build Output API** mode. **Do not set an Output Directory in project settings** (it conflicts).
4. **Environment Variables** (Project → Settings → Environment Variables; scopes: Production + Preview,
   leave “Build” enabled):
   | Name | Value | Required? |
   | --- | --- | --- |
   | `VITE_SITE_URL` | `https://<your-portfolio-domain>` (no trailing slash) | **YES** — otherwise canonical URLs, sitemap and structured data still emit the stale `https://cozy-darkness.lovable.app` default (`portfolio/src/lib/site.ts`) |
   | `VITE_API_BASE_URL` | `https://portfolio-market.runasp.net` | Optional — only after TLS exists (§4). Default: same-origin `/api` proxy rewrite, works immediately |
5. Deploy. First deploy → you get `https://<project>.vercel.app`.
   `VITE_*` variables are **build-time** — changing them requires a redeploy.

The `vercel.json` rewrite `"source": "/api/:path*"` proxies to the API server-side, so
SSR and the browser both work with zero configuration. If the API ever moves, edit that one URL.

## 3. Vercel — dashboard (`dashboard/`)

1. Vercel → **Add New → Project** → same repo → **Root Directory = `dashboard`**.
2. Everything else comes from `dashboard/vercel.json`:
   - build: `npm run build` (`ng build --configuration production`)
   - output: `dist/portfolio-dashboard/browser` (verified path)
   - rewrites: `/api/*` → proxied to the Portfolio API, everything else → `/index.html` (SPA routing)
3. **No environment variables needed.**
4. Deploy. Dashboard auth remains the local unlock session (backend JWT is a later, isolated change
   in `AuthService`).

Both apps must sit at their **domain root** (base href `/`). If you ever mount one under a path,
adjust `--base-href`/`baseHref` accordingly.

---

## 4. When you enable TLS on MonsterASP (recommended, §6)

1. Issue the free Let's Encrypt certificate in the hosting panel for `portfolio-market.runasp.net`.
2. In **both** `vercel.json` files change the rewrite destination
   `http://portfolio-market.runasp.net/…` → `https://portfolio-market.runasp.net/…` (one line each).
   (The current `http://` works today because the proxy hop is server-side, but API traffic should be encrypted.)
3. Optionally set `VITE_API_BASE_URL=https://portfolio-market.runasp.net` on the portfolio for direct
   calls (better SSR data) — this path *is* browser → API, so it needs TLS **and** the origin added to
   `Cors:AllowedOrigins` on the API (`https://<your-portfolio-domain>` and, for branch previews,
   `https://*.vercel.app`). The dashboard needs none of this (always proxied).

## 5. Environment variable reference

| Where | Name | Purpose |
| --- | --- | --- |
| Vercel (portfolio) | `VITE_SITE_URL` | canonical site origin for SEO/sitemap (**required**) |
| Vercel (portfolio) | `VITE_API_BASE_URL` | optional direct API origin (post-TLS only) |
| MonsterASP (optional) | `ConnectionStrings__DefaultConnection` | SQL Server connection string |
| MonsterASP (optional) | `ASPNETCORE_ENVIRONMENT` | `Production` |
| MonsterASP (optional) | `Cors__AllowedOrigins` | browser origins for direct API calls (comma-separated, `*` wildcard allowed like `https://*.vercel.app`) |
| MonsterASP (optional) | `Swagger__Enabled` | Swagger UI toggle (default on) |

No secrets exist in either frontend. `VITE_*` values are public by design.

## 6. Manual checklist (things only you can do)

- [ ] **Commit & push** all work to GitHub `main` (everything below depends on it; awaiting your go-ahead).
- [ ] **Vercel project 1** — root directory `portfolio`, set `VITE_SITE_URL`, deploy.
- [ ] **Vercel project 2** — root directory `dashboard`, deploy.
- [ ] **TLS certificate** for `portfolio-market.runasp.net` in the MonsterASP panel (Let's Encrypt), then §4.
- [ ] **Rotate the SQL password** — the connection string was committed in earlier history
      (`appsettings.Development.json` up to commit `1c79423`; the repo is private, but rotation closes it).
      The current tracked `appsettings.json` has an **empty** connection string; the secret now lives only
      in git-ignored `appsettings.Development.json` / `appsettings.Production.json`.
- [ ] Optional: run integration tests once — `dotnet test PortfolioAPI\tests\PortfolioAPI.IntegrationTests`
      (Docker pulls `mssql/server:2022-latest`, ~1.5 GB).
- [ ] Optional/follow-up: rewrite `PortfolioAPI.UnitTests` to the current `Result<T>` service API
      (pre-existing broken since HEAD — ~150 compile errors across `ControllerTests`, `ProjectServiceTests`,
      `ValidatorsTests`, `AutoMapperTests`, `RepositoryTests`…).
- [ ] Before opening the admin API to the world: add JWT in `AuthService` — admin `/api/*` write endpoints
      are currently unauthenticated (Swagger documents them; the dashboard uses a local unlock).

## 7. Troubleshooting

| Symptom | Cause / fix |
| --- | --- |
| Vercel portfolio build fails at install | `bun.lock` vs `package-lock.json` conflict — `installCommand: npm ci` in `vercel.json` prevents this; if overridden, remove `bun.lock` |
| Portfolio shows fallback (local) content instead of API data | `/api` rewrite missing or API down — `curl https://<domain>/api/health` |
| Dashboard deep link 404s on Vercel | SPA rewrite missing — must come *after* the `/api` rewrite in `dashboard/vercel.json` |
| API 502/500 after upload | `appsettings.Production.json` not uploaded next to `web.config`, or env var missing — check logs for the fail-fast connection-string message |
| SEO points to lovable.app | `VITE_SITE_URL` not set (build-time) — set it and **redeploy** |
| Direct API calls blocked in browser | Mixed content (API has no TLS) or origin not in `Cors__AllowedOrigins` — use the proxy rewrite (default) or complete §4 |
| `VERCEL_OIDC_TOKEN is not set` during build | Harmless nitro dev warning (per `portfolio/AGENTS.md`) |
