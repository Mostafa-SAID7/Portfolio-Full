# API versioning & route conventions

Status: implemented (GitHub issue #24).

## Strategy

**URL-segment versioning**: every JSON endpoint is served under a version
prefix whose current (and only) version is **`v1`**:

```
/api/v1/<resource>          admin + shared endpoints
/api/v1/public/<resource>   public read endpoints for the portfolio site
/api/v1/auth/login          admin sign-in
/api/v1/health              process + database health
```

Canonical examples: `GET /api/v1/public/projects`, `POST /api/v1/Skills`,
`POST /api/v1/auth/login`.

Conventions:

- Route templates follow `api/v1/[controller]` (ASP.NET token) — no
  hand-written per-controller paths.
- Routing is **case-insensitive**; documentation and clients always use
  lowercase segments (`/api/v1/projects`), while templates keep the ASP.NET
  `[controller]` casing. Both forms resolve to the same endpoint.
- Query parameters, request bodies and response shapes are **not** versioned;
  only breaking contract changes trigger a version bump (see policy below).

## What is versioned — and what deliberately is not

| Surface | Versioned? | Notes |
| --- | --- | --- |
| All JSON controllers (admin + public + auth + health) | ✅ `/api/v1/...` | legacy unversioned aliases kept as deprecated |
| `POST /api/public/contact` (public contact form) | ✅ `/api/v1/public/contact` | same rate-limit policy on both paths |
| `GET /api/health` | ✅ `/api/v1/health` | legacy alias kept (ops tooling); deprecation header applies |
| `GET /api/files/{name}` (uploaded images) | ❌ intentionally | immutable asset URLs stored in content data; versioning file names has no compatibility value |

## Backward compatibility (transition period)

Every JSON endpoint responds on **both** paths:

- `/api/v1/...` — canonical, no deprecation headers.
- `/api/...` (unversioned) — **deprecated alias**, flagged with
  `Deprecation: true` on the response and hidden from Swagger.

Both frontends already call only `/api/v1/...` (dashboard: `API_URL` in
`dashboard/src/app/core/api.config.ts`; portfolio: the fetch in
`portfolio/src/lib/content.ts`). The Vercel rewrites (`"/api/:path*"` in both
`vercel.json` files) forward the versioned paths unchanged, so no
infrastructure edit is required.

Swagger UI (`/swagger`) documents **v1 routes only**.

## Deprecation policy (for future changes)

1. **Additive changes never bump the version**: new endpoints, new optional
   request fields and new response fields are always safe for existing
   clients.
2. **Breaking changes** (removing/renaming fields, changing types, tightening
   validation, changing status codes) require a new segment — `/api/v2/...`.
3. **v1 lifetime**: once v2 ships, v1 keeps working for **at least 6 months**
   with `Deprecation: true` on v1 responses. Removal is announced at least
   30 days ahead in a GitHub issue and tracked to closure.
4. **Unversioned aliases** (the transition mechanism above) follow the same
   rule: they are deprecated now and will be removed no sooner than the
   announced window; clients must migrate to `/api/v1/...`.
5. Version segments appear only in URLs — never in headers, payloads or
   media types (keeps proxies, caches and logs simple).

## Client migration checklist

| Client | Switch point | Status |
| --- | --- | --- |
| Dashboard | `API_URL = '/api/v1'` (`core/api.config.ts`) | ✅ done |
| Portfolio | `` fetch(`${apiOrigin()}/api/v1/public/...`) `` (`lib/content.ts`) | ✅ done |
| Portfolio cache rules | `isApiRoute()` folds `/api/v1` (`lib/cache-headers.ts`) | ✅ done |
| Health probes / curl | use `/api/v1/health` going forward | legacy still answers |
