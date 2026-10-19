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
  - `/api/v1/health` → `200 {"status":"ok","database":"ok",...}` (real DB check) ✔
  - `/swagger/index.html` + `/swagger/v1/swagger.json` → `200` (Swagger ON in production by default) ✔
  - `/api/v1/public/projects`, `/api/v1/public/credentials` → `200` ✔
- **HTTPS on the API is NOT working yet** (TLS handshake fails) — manual panel step, see §6.
- Frontends: **production builds verified locally**, not yet deployed:
  - portfolio: `npm run build` → `.vercel/output/` (Vercel Build Output API v3: static + `__server.func` SSR) ✔
  - dashboard: `npm run build` → `dist/portfolio-dashboard/browser/index.html` ✔
- Tests: `PortfolioAPI` builds **0 errors**; `PortfolioAPI.IntegrationTests` compiles **0 errors**
  (running it needs Docker to pull `mcr.microsoft.com/mssql/server:2022-latest`, ~1.5 GB — not run);
  `PortfolioAPI.UnitTests` — **135/135 passing** against the current service/DTO API (rewritten
  during the 2026-10-19 review pass: `DtoMappingsTests` replaces the old AutoMapper tests,
  `UnitOfWorkTests`/`ContactServiceTests` updated for the trimmed `IUnitOfWork` and the
  `ContactService` email dependency).

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
- Schema: EF Core **migrations** are the source of truth (`EnsureCreated` is gone). At startup the
  API baselines legacy (pre-migration) databases by recording the existing migrations as applied —
  **no DDL runs against existing tables, no data is touched** — then `Migrate()` applies only
  migrations added after the baseline. Never destructive.

### Environment variables (optional — the file-based config already works)
Set these in the hosting panel only if you prefer env config (they override the JSON files):

| Variable | Value |
| --- | --- |
| `ConnectionStrings__DefaultConnection` | full SQL Server connection string |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `Cors__AllowedOrigins` | comma-separated allowed browser origins (only needed for *direct* browser calls, §4) |
| `Swagger__Enabled` | `true` (default) / `false` to hide `/swagger` |

### Contact-form email (optional — admin notification)
`POST /api/v1/public/contact` always succeeds. On success the API *attempts* to email the
submission to the admin via SMTP and logs a warning if it fails — the submission is never
rolled back because of an email problem. To enable it, set these in
`appsettings.Production.json` (or as env vars):

| Key | Env variable | Notes |
| --- | --- | --- |
| `EmailSettings:SmtpServer` | `EmailSettings__SmtpServer` | SMTP host (e.g. `smtp.gmail.com`) |
| `EmailSettings:SmtpPort` | `EmailSettings__SmtpPort` | default `587` (STARTTLS, `EnableSsl` is always on) |
| `EmailSettings:Username` / `EmailSettings:Password` | `EmailSettings__Username` / `EmailSettings__Password` | for Gmail use an **app password** |
| `EmailSettings:FromEmail` | `EmailSettings__FromEmail` | sender address; must match the authenticated account |
| `EmailSettings:AdminEmail` | `EmailSettings__AdminEmail` | recipient; falls back to `Auth:Email` |

JWT lifetime: `Auth:TokenHours` (default **2** hours — the dashboard clears the session and
prompts a re-login when the token expires).

### Updating the API later
Re-run the publish command, upload `publish/` again, done. Pending migrations apply automatically
at startup. **Schema changes** require a new migration — never edit an applied migration and never
use `EnsureCreated`:
`dotnet ef migrations add <Name> --project PortfolioAPI/src/PortfolioAPI.Infrastructure --startup-project PortfolioAPI/src/PortfolioAPI`

---

## 2. Vercel — main portfolio (`portfolio/`)

1. Push the repository to GitHub (see §6 for the manual API upload step).
2. Vercel → **Add New → Project** → import the repo → set **Root Directory = `portfolio`**.
3. Framework is auto-detected (Vite). Build command/output come from `portfolio/vercel.json`:
   - install: `npm ci` (pinned so the stray `bun.lock` never hijacks the build)
   - **Lock-file hygiene:** `npm ci` is strict — whenever `portfolio/package.json`
     changes, refresh `portfolio/package-lock.json` with
     `npm install --package-lock-only` (the bun-based dev flow only updates
     `bun.lock` and will not do this for you). Both CI and Vercel fail fast on a
     stale npm lock. TanStack Start requires **Node ≥ 22.12** (CI pins Node 22).
   - build: `npm run build` → nitro emits `.vercel/output/` → Vercel auto-switches to
     **Build Output API** mode. **Do not set an Output Directory in project settings** (it conflicts).
4. **Environment Variables** (Project → Settings → Environment Variables; scopes: Production + Preview,
   leave “Build” enabled):
   | Name | Value | Required? |
   | --- | --- | --- |
   | `VITE_SITE_URL` | `https://<your-portfolio-domain>` (no trailing slash) | Optional — the code defaults to `https://portfolio-market.vercel.app` (`portfolio/src/lib/site.ts`); **set it only if the site moves to a custom domain**. Build-time: changing it requires a redeploy |
   | `VITE_API_BASE_URL` | `https://portfolio-market.runasp.net` | Optional — only after TLS exists (§4). Default: same-origin `/api` proxy rewrite, works immediately |
5. Deploy. First deploy → you get `https://<project>.vercel.app`.
   `VITE_*` variables are **build-time** — changing them requires a redeploy.

The `vercel.json` rewrite `"source": "/api/:path*"` proxies to the API server-side, so
SSR and the browser both work with zero configuration. The pattern covers the
versioned namespace too (`/api/v1/...` → same proxy; see `docs/API_VERSIONING.md`).
If the API ever moves, edit that one URL.

## 3. Vercel — dashboard (`dashboard/`)

1. Vercel → **Add New → Project** → same repo → **Root Directory = `dashboard`**.
2. Everything else comes from `dashboard/vercel.json`:
   - build: `npm run build` (`ng build --configuration production`)
   - output: `dist/portfolio-dashboard/browser` (verified path)
   - rewrites: `/api/*` → proxied to the Portfolio API, everything else → `/index.html` (SPA routing)
3. **No environment variables needed.**
4. Deploy. The dashboard signs in through the API's `POST /api/v1/auth/login` (JWT). Set
   `Auth:Username` / `Auth:Password` on the API (§5) — while they are empty, login fails closed
   with `503`.

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
| MonsterASP (optional) | `Auth__Username` / `Auth__Password` | admin login credentials (required for dashboard sign-in — unset/empty = `POST /api/v1/auth/login` returns 503) |
| MonsterASP (optional) | `Auth__SigningKey` | JWT signing key (optional — an ephemeral per-process key is generated when unset, so tokens don't survive restarts) |
| MonsterASP (optional) | `Cors__AllowedOrigins` | browser origins for direct API calls (comma-separated, `*` wildcard allowed like `https://*.vercel.app`) |
| MonsterASP (optional) | `Swagger__Enabled` | Swagger UI toggle (default on) |

No secrets exist in either frontend. `VITE_*` values are public by design.

### SSR API origin resolution (portfolio) — required env vars

`portfolio/src/lib/api-origin.ts` resolves the absolute origin that SSR needs
for its API fetches, in this order:

| Variable | Scope | What it does |
| --- | --- | --- |
| `VITE_API_BASE_URL` | Build-time (`import.meta.env`) **and** runtime on the server (`process.env`) | Optional override of the API origin, read consistently on **both** the server and client paths with identical normalization (trailing slash stripped, `https://` prefixed when the scheme is missing; empty string = "not configured"). Leave unset to keep the default same-origin `/api` rewrite. |
| `VERCEL_PROJECT_PRODUCTION_URL` | Vercel-provided (build + runtime) | The deployment's own production hostname; SSR calls back as `https://<hostname>/api/...` through the existing `/api` rewrite. |
| `VERCEL_URL` | Vercel-provided (build + runtime) | Per-deployment hostname, used when the production URL is absent — so **preview deployments resolve to the same production API by default**. |

- When none of these resolve server-side, the resolver logs **one**
  `[api-origin] no VERCEL_*/VITE_API_BASE_URL configured …` warning and
  returns `""` (same-origin) — in that case the **`/api` rewrite in
  `portfolio/vercel.json` is the fallback** for both SSR and browser calls.
- The current production setup (Vercel same-origin rewrite, no
  `VITE_API_BASE_URL` set) behaves exactly as before; no API host is
  hardcoded in application code.

## 6. Manual checklist (things only you can do)

- [ ] **Commit & push** all work to GitHub `main` (everything below depends on it; awaiting your go-ahead).
- [ ] **Vercel project 1** — root directory `portfolio`, set `VITE_SITE_URL`, deploy.
- [ ] **Vercel project 2** — root directory `dashboard`, deploy.
- [ ] **TLS certificate** for `portfolio-market.runasp.net` in the MonsterASP panel (Let's Encrypt), then §4.
- [ ] **Rotate the SQL password** — the connection string was committed in earlier history
      (`appsettings.Development.json` up to commit `1c79423`; the repo is private, but rotation closes it).
      The current tracked `appsettings.json` has an **empty** connection string; the secret now lives only
      in git-ignored `appsettings.Development.json` / `appsettings.Production.json`.
- [ ] **Rotate the WebDeploy password** — `Properties/PublishProfiles/site99981-WebDeploy.pubxml`
      embeds deploy credentials and was committed in `02a5436`; it is untracked going forward
      (see `.gitignore`) but the password should be regenerated in the MonsterASP panel.
- [ ] Optional: run integration tests once — `dotnet test PortfolioAPI\tests\PortfolioAPI.IntegrationTests`
      (Docker pulls `mssql/server:2022-latest`, ~1.5 GB).
- [ ] Optional/follow-up: rewrite `PortfolioAPI.UnitTests` to the current `Result<T>` service API
      (pre-existing broken since HEAD — ~150 compile errors across `ControllerTests`, `ProjectServiceTests`,
      `ValidatorsTests`, `AutoMapperTests`, `RepositoryTests`…).
- [x] **JWT admin auth** — implemented: `POST /api/v1/auth/login` + `[Authorize]` on all admin
      mutations (rate-limited login).
- [ ] **Set `Auth:Username` / `Auth:Password`** in `appsettings.Production.json` (or the matching
      env vars) — while unset, login fails closed with `503` and no admin mutation is possible.
- [ ] **Keep `App_Data/uploads` when redeploying the API.** Uploaded project covers are written as
      **files** to `App_Data/uploads` (never into the database — a 1 MB cover must not become a 1 MB
      row) and are served back at `/api/files/*` by the static-file middleware. The database only
      stores the relative path. If a redeploy replaces the whole app folder, the files disappear
      while the database still references them → the cover 404s and the portfolio falls back to the
      gradient card. Either upload `publish/` **without deleting `App_Data`**, or point
      `FileStorage:Path` at an absolute path outside the app folder (e.g. `D:\sites\<app>\uploads`).
- [ ] **Upload project covers from the production dashboard** (`dashboard-portfolio-ms.vercel.app`),
      not from a local dev dashboard: the bytes are written to the disk of whichever API handled the
      request, while the database is shared. A cover uploaded through the local dev API exists only
      on that machine → production 404s it. (Alternative: copy the PNG into the production host's
      `App_Data/uploads` folder and point the project's image URL at `/api/files/<name>.png`.)

## 7. Troubleshooting

| Symptom | Cause / fix |
| --- | --- |
| Vercel portfolio build fails at install | `bun.lock` vs `package-lock.json` conflict — `installCommand: npm ci` in `vercel.json` prevents this (Vercel only reads `package-lock.json`; `bun.lock` may lag behind `package.json` and is ignored) |
| Vercel `403 Blocked by Vercel: @tanstack/... XSS (CVE-2026-102989)` | TanStack Start below the patched release — `package.json` now pins the matched set (`react-start 1.169.3`, `react-router 1.170.45`, `router-plugin 1.168.47`). Always move **all** `@tanstack/*` packages together: a single-package bump causes `MISSING_EXPORT` bundle errors from mismatched `router-core` |
| Portfolio shows empty content instead of API data | `/api` rewrite missing or API down — `curl https://<domain>/api/v1/health` (the portfolio never falls back to bundled data by design) |
| Dashboard deep link 404s on Vercel | SPA rewrite missing — must come *after* the `/api` rewrite in `dashboard/vercel.json` |
| API 502/500 after upload | `appsettings.Production.json` not uploaded next to `web.config`, or env var missing — check logs for the fail-fast connection-string message |
| SEO points to lovable.app | `VITE_SITE_URL` not set (build-time) — set it and **redeploy** |
| Direct API calls blocked in browser | Mixed content (API has no TLS) or origin not in `Cors__AllowedOrigins` — use the proxy rewrite (default) or complete §4 |
| `VERCEL_OIDC_TOKEN is not set` during build | Harmless nitro dev warning (per `portfolio/AGENTS.md`) |
| `msdeploy` 401 `ERROR_USER_UNAUTHORIZED` | WebDeploy password in the `.pubxml` is stale — reset it in the MonsterASP panel / VS publish settings, or just upload `publish/` manually (File Manager/FTP), which is the documented flow in §1 |
| Project cover shows the gradient card instead of the image | The cover file is missing on the server: `GET /api/files/<guid>.png` → 404. Either it was uploaded through a different deployment (dev vs production — the database is shared, the disk is not) or `App_Data/uploads` was wiped by a redeploy. Re-upload the cover from the production dashboard and keep `App_Data` when redeploying (§6). The dashboard now shows a "Cover not found — re-upload" badge instead of failing silently. |
