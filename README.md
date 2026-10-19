# Portfolio — Full Stack

A personal portfolio website backed by a centralized REST API, with a separate
admin dashboard for managing content. Three independently deployable apps share
one backend: the **portfolio site** (public, SSR), the **dashboard** (Angular
admin), and the **PortfolioAPI** (ASP.NET Core 9 + EF Core / SQL Server) which is
the single source of truth for every piece of content.

## Live

| | URL |
| --- | --- |
| Portfolio site | https://portfolio-market.vercel.app |
| Admin dashboard | https://dashboard-portfolio-ms.vercel.app |
| API | http://portfolio-market.runasp.net |
| Swagger UI | http://portfolio-market.runasp.net/swagger |

> Swagger is enabled in production via explicit `Swagger:Enabled` config (off by
> default outside Development). The API currently serves HTTP only — TLS is a
> manual hosting-panel step (see [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md) §4/§6).

## Architecture

| App | Stack | Dev port | Purpose | Hosting |
| --- | --- | --- | --- | --- |
| `PortfolioAPI/` | ASP.NET Core 9, EF Core (SQL Server), JWT auth | `:5039` | REST API — all content + admin mutations | MonsterASP.NET (IIS) |
| `portfolio/` | TanStack Start (React 19, TanStack Router, Vite 8, nitro SSR) | `:5173` | Public SSR portfolio site | Vercel |
| `dashboard/` | Angular 20 SPA (Tailwind, signals) | `:4200` | Admin panel (CRUD, messages, auth) | Vercel |

**Data flow:** the API is the single source of truth. Both frontends reach it
through a same-origin `/api` server-side rewrite (their `vercel.json` proxies to
the MonsterASP API — no CORS, no mixed content), and **neither frontend ever
falls back to bundled data** — if the API is down, lists render empty/error UI
and detail pages 404, so deleted content can never resurface. Public reads
(`/api/v1/public/*`) are anonymous; admin **mutating** endpoints require a JWT from
`POST /api/v1/auth/login`.

## Repository layout

```
.
├── README.md               # this file
├── LICENSE.txt
├── Portfolio.sln           # solution for all backend projects + tests
├── docs/                   # reference documentation
│   ├── DEPLOYMENT.md       # production deployment guide (MonsterASP + Vercel)
│   ├── FULL_STRUCTURE.md   # historical full-repo structure snapshot
│   └── BACK_STRUCTURE.md   # historical backend structure snapshot
├── .github/                # CI workflow + issue/PR templates
├── PortfolioAPI/
│   ├── src/
│   │   ├── PortfolioAPI/             # API host: controllers, auth, Program.cs
│   │   ├── PortfolioAPI.Core/        # entities, DTOs, interfaces
│   │   ├── PortfolioAPI.Application/ # services, validators, mappings
│   │   └── PortfolioAPI.Infrastructure/ # DbContext, repositories, migrations
│   └── tests/
│       ├── PortfolioAPI.UnitTests/       # runs in CI
│       └── PortfolioAPI.IntegrationTests/ # needs Docker; local/manual only
├── dashboard/              # Angular 20 admin SPA (Vercel)
└── portfolio/              # TanStack Start public site (Vercel)
```

## Quick start

Prerequisites: .NET SDK 9, Node.js 20+.

### 1. API

```bash
dotnet run --project PortfolioAPI/src/PortfolioAPI/PortfolioAPI.csproj
# → http://localhost:5039 (Swagger at /swagger in Development)
```

Configuration lives in `PortfolioAPI/src/PortfolioAPI/appsettings.Development.json`
(connection string, SMTP, `Auth:Username`/`Auth:Password`) — this file is
**git-ignored**; create it from `appsettings.json` and fill in your local values.
The database schema is managed by **EF Core migrations** and applied
automatically at startup (legacy databases are baselined, never rebuilt).

### 2. Dashboard (`:4200`)

```bash
cd dashboard
npm ci
npm run dev
# dev server proxies /api → :5039 (proxy.conf.json), so no CORS setup needed
```

### 3. Portfolio site (`:5173`)

```bash
cd portfolio
npm ci
npm run dev
```

## Features

- **Content API** — CRUD for projects, skills, experience, testimonials, and
  career credentials, plus a public contact form; all admin mutations guarded by
  `[Authorize]`.
- **Public read API** — `/api/v1/public/*` serves only ACTIVE/published rows to the
  portfolio site (anonymous).
- **JWT admin auth** — `POST /api/v1/auth/login` validates `Auth:Username` /
  `Auth:Password` (unconfigured server fails closed with `503`) and issues the
  JWT the dashboard sends on every admin request.
- **Rate limiting** — public contact submission and admin login are throttled
  per IP.
- **Versioned routes** — all JSON endpoints live under `/api/v1/...` with
  deprecated unversioned aliases during the transition (see
  [docs/API_VERSIONING.md](docs/API_VERSIONING.md)).
- **Image uploads** — backend-owned cover uploads served at `/api/files/…`
  (stored under `App_Data/`, git-ignored).
- **EF Core migrations** — schema is migration-driven (`dotnet ef migrations
  add <Name>`), applied at startup; `EnsureCreated` is gone.
- **Health endpoint** — `/api/v1/health` verifies process + database connectivity.
- **Admin dashboard** — projects CRUD with search/pagination, messages inbox,
  skills/experience management, JWT login.
- **SSR portfolio** — server-rendered pages, SEO/sitemap driven by live API data.

## API surface (selected)

| Endpoint | Auth | Notes |
| --- | --- | --- |
| `POST /api/v1/auth/login` | anonymous, rate-limited | returns JWT for admin requests; `503` when credentials unconfigured |
| `GET /api/v1/public/projects` (also `skills`, `experience`, `testimonials`, `credentials`) | anonymous | only ACTIVE/published rows |
| `POST`/`PUT`/`DELETE` on `/api/v1/Projects`, `/api/v1/Skills`, `/api/v1/Experience`, `/api/v1/Testimonials`, `/api/v1/Credentials` | JWT (admin) | admin mutations |
| `GET` on the same admin controllers | anonymous | list reads stay public |
| `POST /api/v1/public/contact` | anonymous, rate-limited | public contact form |
| `POST /api/v1/Upload` | JWT (admin) | cover-image upload, stored under `App_Data/uploads` |
| `GET /api/v1/health` | anonymous | process + database connectivity |

Legacy unversioned paths (`/api/Projects`, …) still answer as **deprecated
aliases** during the transition — see
[docs/API_VERSIONING.md](docs/API_VERSIONING.md) for the full policy.

Interactive documentation: `/swagger` (Swagger documents the Bearer scheme —
paste a token from `POST /api/v1/auth/login` to try admin endpoints).

## Contributing

- Bug reports and feature requests: use the
  [issue templates](.github/ISSUE_TEMPLATE/bug_report.md).
- Pull requests: fill in the [PR template](.github/PULL_REQUEST_TEMPLATE.md);
  CI must be green (backend build + unit tests, both frontend builds).
- Keep the root tidy: reference docs belong in `docs/`, not the repository root.

## Verification & testing

```bash
# Backend unit tests (CI runs these)
dotnet test PortfolioAPI/tests/PortfolioAPI.UnitTests/PortfolioAPI.UnitTests.csproj

# Integration tests — need Docker (SQL Server container); local opt-in only
dotnet test PortfolioAPI/tests/PortfolioAPI.IntegrationTests/PortfolioAPI.IntegrationTests.csproj
```

CI ([.github/workflows/ci.yml](.github/workflows/ci.yml)) runs three parallel
jobs on every push/PR to `main`: **backend** (build `Portfolio.sln` + unit
tests), **dashboard** and **portfolio** (`npm ci` + production build). Deployments
are not part of CI — see [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md).

## Key docs

- [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md) — production deployment & operations
  (MonsterASP.NET + Vercel, env vars, troubleshooting)
- [docs/API_VERSIONING.md](docs/API_VERSIONING.md) — versioned routes (`/api/v1`)
  and the deprecation/breaking-change policy
- [docs/FULL_STRUCTURE.md](docs/FULL_STRUCTURE.md) — historical repo structure
  snapshot (pre-implementation planning doc)
- [docs/BACK_STRUCTURE.md](docs/BACK_STRUCTURE.md) — historical backend structure
  snapshot
- [dashboard/README.md](dashboard/README.md) — dashboard screens & dev notes
- [portfolio/README.md](portfolio/README.md) — portfolio site dev notes &
  API-down behavior policy
- [portfolio/AGENTS.md](portfolio/AGENTS.md) — portfolio dev environment quirks

## License

MIT — see [LICENSE.txt](LICENSE.txt).
