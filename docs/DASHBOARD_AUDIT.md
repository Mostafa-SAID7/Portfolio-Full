# Dashboard audit and controlled refactor

Audit baseline: 2026-10-21, current local dashboard, after the startup repairs in this session. Evidence below uses pre-refactor line numbers. The dashboard source, routes, infrastructure, shared UI and corresponding backend contracts were inspected before implementation.

## Baseline

- `npm --prefix dashboard run build`: passed, 376.82 kB initial bundle, eight redundant-nullability template warnings.
- `npm --prefix dashboard test`: failed before executing tests: no Angular test target. The existing shell spec has only two smoke tests.
- Backend unit tests: 152 passed. SQL Server integration tests require Docker and authenticated setup; not run.
- Browser: login renders; no authenticated dashboard CRUD verified because development credentials are unavailable. Authentication must not be bypassed.
- Both development processes and the dashboard-to-API health proxy were running. Real API response casing is camelCase.
- Local main contains five unpublished commits with unrelated portfolio/backend changes and tracked pnpm cache content. The PR must be based on remote main and restricted to the dashboard and this report, not publish that history or cache.

## Findings and corrections

| Finding | Pre-refactor evidence | Impact | Controlled correction |
| --- | --- | --- | --- |
| Substantial inline templates throughout the shell, six pages and seven shared components | `dashboard/src/app/app.component.ts:25`, `features/content/content.component.ts:24`, `features/projects/project-form/project-form.component.ts:20`, shared component metadata | Presentation dominates TypeScript; maintainability suffers | Adjacent HTML templates, retain standalone imports and lazy routes; no empty CSS/SCSS files |
| Centralized styles already exist, but consumers duplicate state styling | `dashboard/src/styles.css:117-247`, `shared/empty-state.component.ts:9`, `shared/error-state.component.ts:29` | Error retry uses undefined `btn-secondary`; repeated empty/error surfaces drift | Reuse shared empty/error controls and global Tailwind component classes |
| Experience uses obsolete unsuffixed fields | `core/models/experience.model.ts:4-10`, `features/content/content.component.ts:741-754`; backend `ExperiencePayloadDto.cs:9-22` | Blank display values and invalid create/update payloads | CamelCase bilingual model, draft, labels and complete payloads; enforce current/end-date invariant |
| Project form mirrors English into Arabic and metrics | `features/projects/project-form/project-form.component.ts:451-486,530-574`; backend `ProjectService.cs:237-279` | Existing translations and independent Arabic metrics overwritten on full-replacement PUT | Explicit bilingual controls; preserve both metric lists and gradient independently; use owned Project type |
| Category create/rename omit required Arabic name | `features/content/content.component.ts:524,550`; backend `CategoryValidator.cs:10-16` | HTTP 400 validation errors | Required Arabic inputs and round-trip both names |
| Skill UI lacks Arabic controls; edit/toggle omit Arabic description | `features/content/content.component.ts:167-180,670-681`, `core/services/skill.service.ts:59-69`; backend `SkillValidator.cs:10-24`, `Entities/Skill.cs:27-38` | Creates rejected; updates erase descriptions while local state hides loss | Complete bilingual forms and update payloads |
| Unread filter is not a signal | `features/messages/messages.component.ts:113,120-142` | Computed list does not invalidate when filter changes | Signal filter, reset and clamp pagination; existing shared paginator retained |
| Validation details and HTTP headers discarded | `core/interceptors/api-error.interceptor.ts:25-55`; backend `Program.cs:337-370` | Useful field failures lost, generic retry errors | Typed normalization preserving domain errors/ProblemDetails and headers |
| Health checks count hidden content; any partial failure hides all successful data | `features/dashboard/dashboard.component.ts:242-248,292-296` | Misleading publication score and unavailable healthy sections | Publication-aware counts, deliberate per-source unavailable states; do not turn failed sources into zeros |
| Shared form controls do not implement disabled state or complete keyboard selection | `shared/select-field.component.ts:157-364`, `shared/tags-input.component.ts:92-225` | Disabled reactive controls remain interactive; keyboard focus and selected suggestions are incomplete | ControlValueAccessor disabled handling, active-descendant wiring and keyboard regression tests |
| Modal/sidebar hide visually without containing keyboard focus | `shared/confirm-dialog.component.ts:50-80`, `app.component.ts:39-45` | Keyboard can enter hidden/off-modal UI | Native modal semantics or equivalent contained focus, restoration, and hidden drawer inertness |
| Installed animations patch differs from core | npm lock: animations 20.3.34, core 20.3.30 | Angular peer dependency mismatch remains after hybrid install repair | Align locked Angular framework versions; keep lockfiles consistent |

## API contract inventory

All JSON routes use `/api/v1`; legacy aliases remain supported. Writes send request DTOs, not entities. Timestamps, route identifiers and project slugs are server-owned. ASP.NET serializes camelCase. PUTs are full replacement, generally returning 204.

| Resource | Request/response relationship | Endpoints | Persistence/validation notes |
| --- | --- | --- | --- |
| Projects | Project entity → ProjectDto → frontend Project; Create/UpdateProjectDto → ProjectPayload; separate reactive form state | GET/POST `/Projects`, GET/PUT/DELETE `/Projects/{id}` | Stable slug, bilingual required title/description; optional editorial fields, two metric lists, arrays, nullable URLs; image removal may delete owned file |
| Categories | Category → CategoryDto → Category; Create/UpdateCategoryDto → CategoryPayload | GET/POST `/Categories`, PUT/DELETE `/Categories/{id}` | Both names required; order, active flag and server-derived projectCount; in-use deletion rejected; rename propagates English project category |
| Skills | Skill → SkillDto → Skill; Create/UpdateSkillDto → SkillPayload | GET/POST `/Skills`, PUT/DELETE `/Skills/{id}` | Required bilingual names/categories, proficiency 0–100, nullable bilingual descriptions/icon; update replaces all fields |
| Experience | Experience → ExperienceDto → Experience; Create/UpdateExperienceDto → ExperiencePayload; date-only UI draft differs | GET/POST `/Experience`, PUT/DELETE `/Experience/{id}` | Required bilingual company/position/description; nullable locations; current means null endDate, past means endDate >= startDate; <=20 technologies |
| Messages | Contact → ContactDto → ContactMessage | GET `/Contact`, PUT `/Contact/{id}/read`, DELETE `/Contact/{id}` | Authenticated inbox, persisted read flag/readAt, public submission elsewhere; unread derived from stored flags |
| Authentication | Login request → server credentials validation → token, expiry and numeric user ID → AdminSession | POST `/auth/login` | Bearer interceptor and expiry-aware guard; invalid login must not clear another session; no invented refresh flow |
| Upload | Multipart `file` → backend-owned file → UploadedImage `{url,fileName,size}` | POST `/upload/image`, static `/api/files/...` | Authorized; allowed JPEG/PNG/GIF/WebP/AVIF; 10 MiB; relative URLs through same-origin proxy |

## Architecture and scope

Keep the existing feature/core/shared boundaries, signal-based services, lazy routes, paginator, tags input, select field, toast and confirmation service. No state library, generic registries, barrel layer, API entity leakage or redesign is warranted. Separate content responsibilities only where complexity justifies it. Tailwind already scans both HTML and TS. Use `src/styles.css` for repeated style tokens/patterns and utilities in templates; no per-page CSS/SCSS is required.

## Backend and deployment risks not changed by this frontend PR

- Historical bilingual project migration renames legacy columns into mismatched fields and introduces empty required translations (`MakeProjectsBilingual.cs:20-133`). Repair requires deployed-data verification and a forward migration, not editing migration history.
- Project search SQL still references removed `Title`/`Description` columns (`ProjectRepository.cs:21-37`). Dashboard filtering is client-side; this unused API endpoint needs separate backend repair.
- Category/project rename synchronization is non-atomic and only propagates English (`CategoryService.cs:114-164`, `ProjectRepository.cs:77-84`).
- Initializer does not apply migrations and can enqueue duplicate categories (`DbInitializer.cs:17-55,132-184`). Deployment migration/backfill policy needs explicit review.
- Login/contact rate limit policies are shared buckets rather than partitioned IP buckets (`Program.cs:218-245`).
- The production Vercel proxy forwards to an HTTP upstream (`dashboard/vercel.json:5-9`). Do not guess or switch hosting URLs without verified TLS availability.
- Upload cancellation/replacement can leave files without a saved project; there is no supported frontend delete-upload endpoint. Do not invent one.
- Public API caching means saved content may not appear instantly. Avoid promising immediate public visibility.
- Existing integration harness starts its client before SQL Server and omits auth in mutation tests. Passing in-memory unit tests cannot validate SQL migrations/search or production deployment.

## Verification requirements

Production build and strict templates; runnable existing tests plus focused regression tests for bilingual payload preservation, dates, filtering, validation errors, shared control keyboard/disabled states and loading/empty/error UI. Browser-check login and accessible navigation without injected credentials; explicitly distinguish isolated test fixtures from authenticated live-backend flows. No test/compiler weakening or backend/security changes.

Final results and remaining limitations will be recorded after implementation.

## Implementation follow-up — 2026-10-21

### Dashboard overview continuation

- Extracted dashboard markup to `dashboard/src/app/features/dashboard/dashboard.component.html`; the component continues to use centralized Tailwind utilities and `dashboard/src/styles.css`. No component CSS/SCSS files were added.
- Kept the existing feature-oriented structure, standalone components, lazy routes, signals, and shared empty/error controls. The project, skill, experience, and contact services remain the API owners.
- Completed per-source retry callbacks and tied dashboard subscriptions to component destruction. Manual retry refreshes the inbox while preserving the shell's short-lived cache for ordinary navigation.
- Changed statistics and snapshot sections to distinguish loading and unavailable data from legitimate zero values. A partial API failure no longer makes the whole overview disappear or reports an empty state for a failed request. Portfolio-health checks are withheld while a required source is loading or unavailable.
- Fixed strict template/type errors in the shared select and tags controls and in project-form regression fixtures.
- Removed the committed build-output artifact from the dashboard folder. Kept package and lockfile changes aligned with the configured Angular test builder; no runtime dependency was added.

### Current validation

- `npx ng build --configuration development`: passed with strict Angular template compilation.
- `npm test -- --watch=false`: test bundle compiled. Karma could not launch ChromeHeadless in this environment, so the specs did not execute.
- `npm run build`: blocked while Angular attempted to inline Google Fonts from `fonts.googleapis.com`, which is unreachable in this network-restricted environment. A production build with optimization disabled compiled the app but correctly failed the configured bundle budget; this is not a substitute for the production build.
- No authenticated browser session or live backend mutation flow was available; these remain unverified.
- Existing backend unit test results recorded in the baseline section belong to the prior audit run and were not rerun in this continuation.

### Remaining risks

The backend issues in the earlier audit remain outside this frontend-only PR. Live CRUD, upload persistence, responsive browser behavior, and assistive-technology interaction need verification in an environment with the API, credentials, and Chrome available. No production API contract or authentication behavior was changed.
