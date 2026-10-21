# Storage & Media — Architecture and Operations

How the Portfolio API stores, validates, delivers and deletes project cover
images — across its two providers: **local disk** and **Cloudinary**.

---

## 1. Overview

| | Local | Cloudinary |
|---|---|---|
| Identity | file path under `App_Data/uploads` | public id under `portfolio/projects/…` |
| Delivery | API serves `/api/files/*` | Cloudinary CDN (HTTPS, `f_auto`/`q_auto`) |
| Variants | pre-generated `-640` / `-1280` WebP siblings | on-the-fly width transforms in the URL |
| Selection | default in Development | default in Production |

Both providers implement `IFileStorageService` (`PortfolioAPI.Core.Interfaces`).
Controllers and services never know which one is active — they speak in
`MediaReference` / `StoredImage` / `ImageUploadRequest` terms only:

- **`MediaReference(url, publicId, provider)`** — a stored asset's handle.
  `provider` decides who owns the cleanup; `publicId` is the provider-native
  identity. **The public id is the identity; the URL is not** (URLs can be
  re-derived / transformed; ids cannot).
- **`StoredImage`** — what an upload returns: both delivery URLs (`Url`,
  `Url640`, `Url1280`) and the storage metadata (`Provider`, `PublicId`,
  width/height/bytes) that must be **persisted with the entity**.
- **`ImageUploadRequest`** — content + file name + the `Slug` that scopes the
  stored id, e.g. `portfolio/projects/{slug}/cover/{name}`.

The DB carries two columns per project (`StorageProvider`, `StoragePublicId`,
migration `AddProjectStorageMetadata`) so a later replace/delete names the
exact asset instead of parsing URLs.

---

## 2. Configuration

```jsonc
// appsettings.json (checked in — never contains secrets)
"FileStorage": {
  "Path": "App_Data/uploads",       // local provider root
  "BaseUrl": "/api/files",          // delivery prefix for local files
  "MaxFileSize": 10485760,          // 10 MiB
  "MaxImageWidth": 8000,            // per-dimension caps
  "MaxImageHeight": 8000,
  "MaxImagePixels": 40000000        // decompression-bomb guard (40 MP)
},
"Cloudinary": {
  "CloudName": "",                  // set in production or env vars
  "ApiKey": "",
  "ApiSecret": "",                  // secret — never in git, never logged
  "Folder": "portfolio"
}
```

Provider selection (`Storage:Provider`) — first non-empty wins:

1. `Storage__Provider` env var (MonsterASP panel: `Storage__Provider=cloudinary`)
2. `Storage:Provider` config value (e.g. in git-ignored `appsettings.Production.json`)
3. Implicit default: **`cloudinary` in Production, `local` otherwise**

Cloudinary settings can come from the `Cloudinary:*` config keys or, as env
vars: the individual `CLOUDINARY_CLOUD_NAME`, `CLOUDINARY_API_KEY`,
`CLOUDINARY_API_SECRET`, `CLOUDINARY_FOLDER`, or the single well-known
`CLOUDINARY_URL=cloudinary://<apiKey>:<apiSecret>@<cloudName>` that
Cloudinary's console hands out (an explicitly configured value always wins;
`CLOUDINARY_URL` carries no folder, so `Folder` keeps its default). Individual
Env paths are checked before `CLOUDINARY_URL`. If the selected provider is
Cloudinary but none of the sources yields a complete configuration, **startup
fails immediately** with `Cloudinary configuration is incomplete.` — the
message never prints values.

Secret handling: `Cloudinary:ApiSecret` lives only in env vars or the
git-ignored `appsettings.Production.json`. Upload/delete errors are reported to
users generically; real provider details stay in server logs.

---

## 3. Upload pipeline (`ImageValidator`)

Every upload — either provider — passes one shared validator:

1. **Extension allowlist** — `.jpg .jpeg .png .gif .webp` only (SVG is refused:
   it can carry scripts).
2. **Content-type ↔ extension** check (skipped when no file name was sent).
3. **Size cap** (`MaxFileSize`) with a capped read of the stream.
4. **Magic bytes** — the signature must match the extension, so a JPEG renamed
   `.png` never gets through.
5. **Header-only identify** (`Image.Identify`) — rejects a decompression bomb
   (width/height/pixels caps) before any decode.
6. **Full decode** (`Image.Load`) — catches truncated/corrupt files.

Rejection messages are short and technical-detail-free; limits are configurable
via `FileStorage:*`.

### Public ids (`CloudinaryStorageService.BuildPublicId`)

```
{folder}/projects/{sanitized-slug}/{assetType}/{sanitized-name}
```

Every segment is reduced to `a-z0-9-`, so a file name or title can never inject
`/`, `..` or a foreign location. The id carries **no extension** — format is a
delivery decision. If the base id is already taken (schema verified with one
`GetAsync` lookup), a `-2`, `-3…` suffix is reserved; uploads never overwrite
an existing asset.

### Delivery URLs

The upload result's `Url` is Cloudinary's raw `secure_url`. The 640/1280
variants are **not pre-generated files** — they are the same asset addressed
with a delivery transform inserted right after `/image/upload/` (extension
stripped so `f_auto` can negotiate AVIF/WebP per browser):

```
https://res.cloudinary.com/<cloud>/image/upload/f_auto,q_auto,c_limit,w_640/v123/portfolio/projects/<slug>/cover/<name>
```

`c_limit` never upscales a small upload. The base URL always stays the identity
of truth; transformed URLs are conveniences derived from it.

---

## 4. Replace & delete ordering

**Replace** (`ReplaceImageAsync`): upload the new asset → verify it → update
the DB → delete the old one. If the upload fails, the old cover is untouched
and the site never shows a broken image. If the new upload lands under the
**same public id** (an intentional overwrite case), no delete is issued.

**Delete** (`DeleteImageAsync(MediaReference)`): dispatches on the reference's
provider, which may differ from the active one (rows written before Cloudinary
was enabled still point at local files):

- Cloudinary → delete by public id; fall back to recovering the id from a URL
  this API produced. Unrecognised URLs are never guessed into delete requests.
- Local → deletes only the exact managed file (plus its same-counter sibling,
  e.g. `{slug}-640[-n].webp` ↔ `{slug}-1280[-n].webp`).
- External URLs (GitHub avatars, bundled `/images/...`) are never touched.

Controllers treat cleanup as best-effort: a failed delete is logged and warned
about, but never fails the CRUD operation. Legacy `DeleteFileAsync(string url)`
remains for backward compatibility with old callers.

**Legacy URLs keep working.** `/api/files/*` is still served even when
Cloudinary is active, so pre-migration rows render until they are migrated.

---

## 5. Media migration

```
dotnet run --project PortfolioAPI/src/PortfolioAPI -- migrate-media [--dry-run]
```

On a hosting panel **without shell access** (runasp.net), the same service is
available over an authenticated request instead of the CLI:

```
POST /api/v1/admin/media/migrate?dryRun=true   (admin JWT — dashboard token)
```

`dryRun=true` classifies every row and changes nothing; run it first, then
repeat with `dryRun=false`. The response contains the same counters and
failure list the CLI prints, so the run is observable either way.

Moves every project cover that still lives in `App_Data/uploads` to the active
provider (requires `Storage:Provider=cloudinary`, otherwise the run aborts with
a hint — migrating while "local" is selected would look like a success without
doing anything).

Rules:

- **Idempotent** — rows already marked `cloudinary` are skipped; the command
  can simply be re-run if it was interrupted. A Cloudinary URL without stored
  metadata (pre-public-id rows) is recognised and back-filled with the
  recovered public id, without re-uploading anything. Recognition is scoped to
  the configured cloud (`res.cloudinary.com/{cloud}/image/upload/…`); a URL
  from any other account is treated as external media the API does not own —
  it can never acquire a public id that a later delete would aim at.
- **`--dry-run`** — classifies every project and reports what *would* happen;
  nothing is uploaded or written.
- **Rollback source kept** — the local files are never deleted; until the CDN
  copy is verified they are the escape hatch.
- **Row-by-row commit** — each project is saved independently; one bad file
  cannot discard the others' work. Failures are listed in the report with a
  clear reason (missing local file, invalid legacy image, provider rejection).
- Exit code is `1` when any row failed, so CI/scripts can react.

The same validator gates migration uploads, so a corrupt legacy file is
reported — never stored on the CDN.

---

## 6. Frontend consumption

The API is the source of truth; no frontend assumes a storage provider.

**Portfolio** (`portfolio/src/lib/image.ts` → `resolveImage`):
- Cloudinary URL → responsive widths built by inserting `f_auto,q_auto,c_limit,w_N`
  transforms into the delivery URL (`isCloudinaryUrl` recognises `/image/upload/`);
  no `<picture>` AVIF duplicate since `f_auto` already negotiates format.
- `/api/files/...` → resolved against the API origin (same-origin `/api` proxy
  in production) — legacy local covers keep rendering.
- Bundled logical paths (`/projects/...`) keep their pre-generated
  `-640/-1280` WebP/AVIF variants with no API involvement.

**Dashboard** (`dashboard/src/app/core/portfolio-origin.ts`):
- `resolvePortfolioImage` renders a Cloudinary URL **as-is** (the CDN owns
  resizing) and keeps legacy paths backward-compatible.
- The upload response now also carries `provider` + `publicId`; the project
  form stores them with the row (`storageProvider` / `storagePublicId`) and
  only attaches metadata that actually belongs to the saved URL, so a
  hand-typed external URL never inherits a stale Cloudinary public id.
- **Secret rule**: the dashboard never sees, requests or derives Cloudinary
  credentials — it only receives the final delivery URLs from the API.

---

## 7. Testing

- `ImageValidatorTests` — allowlist, magic-byte/extension mismatch,
  content-type mismatch, corruption, dimension/pixel caps, config-driven limits.
- `CloudinaryStorageServiceTests` — the provider behind a fake
  `ICloudinaryGateway`: id reservation/collision, sanitisation (traversal
  attempts fail closed), delivery-URL construction, replace ordering (old
  asset deleted only after a good upload; same-id overwrite deletes nothing),
  delete dispatch by public id / legacy URL recovery / foreign-URL refusal,
  and the secret-free fail-fast constructor check.
- `MediaMigrationServiceTests` — idempotency, dry-run purity, row update with
  local files kept, legacy-Cloudinary back-fill, external-URL skip, missing
  file reporting, cancellation.
- `portfolio-origin.spec.ts` — Cloudinary passthrough, legacy `/api/files`
  compat, placeholder fallback, host-detection helpers.
