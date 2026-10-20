# Portfolio Admin — Dashboard

Angular admin dashboard for the **Portfolio API** (`PortfolioAPI/`). Every screen
reads and writes real backend data — no mocks, no local fixtures — behind the
API's JWT-protected admin endpoints.

## Screens

| Route        | What it does                                                                  |
| ------------ | ----------------------------------------------------------------------------- |
| `/dashboard` | Overview: live stats (projects, skills, unread messages), portfolio-health checklist, latest messages & projects |
| `/projects`  | Full CRUD for `api/v1/Projects` (list, search, create, edit, delete, featured flag) |
| `/messages`  | Inbox for `api/v1/Contact`: read/unread filter, mark-as-read, delete              |
| `/content`   | `api/v1/Skills` (add, show/hide, delete), `api/v1/Experience` (list, delete) |
| `/auth/login`| JWT sign-in against the API (see below)                                     |

## Running it

```bash
# 1. Start the API (listens on http://localhost:5039)
cd ../PortfolioAPI/src/PortfolioAPI
dotnet run

# 2. Start the dashboard (proxy.conf.json forwards /api -> :5039)
cd ../../dashboard
npm install
npm start        # http://localhost:4200
```

The dev server proxies `/api` to the API (see `proxy.conf.json`), so no CORS setup is
needed. In production, serve the API behind the same domain (or set an absolute
`API_URL` in `src/app/core/api.config.ts`).

## Architecture notes

- **State** — one signal-based service per API resource (`ProjectService`, `SkillService`,
  `ExperienceService`, `ContactService`) with loading/error signals and
  optimistic local updates after mutations. Updates use **PUT** with the full entity,
  matching the backend controllers.
- **Design system** — semantic tokens in `tailwind.config.js` backed by CSS variables in
  `src/styles.css` (dark theme by default, working light theme via `html.light`). All text
  pairings are WCAG-AA checked; small body text is at least 4.5:1.
- **Layout** — `AppComponent` renders a fixed, full-height sidebar (independent scroll,
  pinned footer) on desktop and an off-canvas drawer with scrim on mobile (hamburger,
  Escape and route changes close it). The header is sticky.
- **Auth** — JWT sign-in against the API: `POST /api/v1/auth/login` with the
  `Auth:Username` / `Auth:Password` configured on the server (empty config = 503),
  then `core/services/auth.service.ts` stores the token and attaches it to every
  admin request; admin mutations on the API are `[Authorize]`.

## Tests

```bash
npm test
```
