# Portfolio Admin — Dashboard

Angular admin dashboard for the **Portfolio API** (`PortfolioAPI/`). It replaces the old
TaskFlow demo: every screen now reads and writes real backend data — no mock tasks, no fake
activity feeds.

## Screens

| Route        | What it does                                                                  |
| ------------ | ----------------------------------------------------------------------------- |
| `/dashboard` | Overview: live stats (projects, skills, posts, unread messages), portfolio-health checklist, latest messages & projects |
| `/projects`  | Full CRUD for `api/Projects` (list, search, create, edit, delete, featured flag) |
| `/messages`  | Inbox for `api/Contact`: read/unread filter, mark-as-read, delete              |
| `/content`   | `api/Skills` (add, show/hide, delete), `api/Experience` (list, delete), `api/Blog` (publish/unpublish, delete) |
| `/auth/login`| Local admin unlock (see below)                                                |

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
  `ExperienceService`, `BlogService`, `ContactService`) with loading/error signals and
  optimistic local updates after mutations. Updates use **PUT** with the full entity,
  matching the backend controllers.
- **Design system** — semantic tokens in `tailwind.config.js` backed by CSS variables in
  `src/styles.css` (dark theme by default, working light theme via `html.light`). All text
  pairings are WCAG-AA checked; small body text is at least 4.5:1.
- **Layout** — `AppComponent` renders a fixed, full-height sidebar (independent scroll,
  pinned footer) on desktop and an off-canvas drawer with scrim on mobile (hamburger,
  Escape and route changes close it). The header is sticky.
- **Auth** — PortfolioAPI has no auth endpoints yet, so signing in is a **local unlock**
  (name + email stored in `localStorage`). When JWT auth is added to the API, only
  `core/services/auth.service.ts` needs to change; the guard and screens stay the same.

## Tests

```bash
npm test
```
