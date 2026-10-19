# Clean Code Canvas

A modern portfolio platform for showcasing multi-vendor marketplaces, real-time bidding engines, and high-scale commerce systems.

## Quick Start

### Prerequisites
- Node.js (v18+)
- npm or yarn

### Installation

```bash
git clone <repository-url>
cd clean-code-canvas
npm install
npm run dev
```

The app will start at `http://localhost:5173`

## Development

### Available Scripts

- `npm run dev` — Start development server
- `npm run build` — Build for production
- `npm run preview` — Preview production build locally
- `npm run lint` — Run ESLint
- `npm run format` — Format code with Prettier

## Content source & API-down behavior (offline fallback policy)

**The PortfolioAPI (`http://localhost:5039` in dev, the same-origin `/api`
rewrite in production) is the single source of truth** for projects, skills and
career credentials. Its public endpoints return only ACTIVE/published rows, so
content deleted or unpublished in the admin dashboard disappears from the site
on the next fetch.

When the API is **down, slow or erroring**:

- **Entity lists render empty/error UI — never the bundled catalog.** List
  loaders resolve to `[]`, so the existing empty state (e.g. the projects
  explorer's `EmptyState`) or error UI renders. No stale cards can appear.
- **Project detail pages 404.** The detail loader looks the id up in the API
  list only; an unknown id or an unreachable API throws `notFound()` and the
  route's not-found screen renders. There is no bundled lookup fallback.
- **The sitemap drops project URLs.** `/sitemap.xml` then contains only the
  static routes (`/`, `/about`, `/skills`, `/experience`, `/projects`,
  `/contact`) — no `/projects/<id>` detail URLs, so deleted content never
  re-enters the crawl graph.

`src/data/` is **legacy/sample data only**: it holds TypeScript types
(`Project`, `CredentialItem`, `SkillIcon`…), static configuration
(`projectFilters`, `credentialTabs`, icon keys), Arabic translation maps, and
presentation metadata (case-study copy, gradients) that is matched *by title
against rows the API already returned*. It is never a runtime fallback for
entity membership, and it must not be re-introduced as one.

### Required environment variables (SSR API origin resolution)

SSR (`fetch()` in Node) needs an absolute API origin, resolved in
`portfolio/src/lib/api-origin.ts` in this order:

| Variable | Read from | Purpose |
| --- | --- | --- |
| `VITE_API_BASE_URL` | server: `process.env` first, then build-time `import.meta.env`; client: `import.meta.env` | Optional explicit API origin override on **both** paths, with identical normalization (trailing slash stripped, `https://` added when the scheme is missing; empty string = "not configured"). |
| `VERCEL_PROJECT_PRODUCTION_URL` | server (`process.env`) | Vercel's production hostname — SSR calls back through its own public `/api` rewrite. |
| `VERCEL_URL` | server (`process.env`) | Deployment hostname fallback (used when `VERCEL_PROJECT_PRODUCTION_URL` is absent). Preview deployments therefore resolve to the same production API by default. |

- When none of these resolve server-side, `apiOrigin()` logs **one**
  `console.warn` (`[api-origin] no VERCEL_*/VITE_API_BASE_URL configured …`)
  and returns `""` — same-origin — so the **`/api` rewrite in `vercel.json` is
  the fallback** when the origin is `""`. The browser always uses `""`
  (same-origin `/api/v1/...` through the `/api` rewrite) in production.
- No hardcoded API hosts exist in app code; dev mode talks to
  `http://localhost:5039`.

### Manual test checklist — API-down path

1. Stop the PortfolioAPI (kill the `:5039` process) while the frontend runs.
2. **Home** (`/`): hero/sections render, but the featured-projects and
   skills-preview areas show **no stale cards** (empty section), not bundled
   ones.
3. **Projects** (`/projects`): the explorer renders the existing empty state
   ("no projects") — zero bundled project cards; filters show `All 0`.
4. **Project detail** (`/projects/<any-id>`): the not-found screen renders
   (404), never a bundled case study.
5. **Skills / Experience** (`/skills`, `/experience`): skill groups and
   credentials render empty — no bundled entries.
6. **Sitemap** (`/sitemap.xml`): contains only the static routes — assert
   there are **no** `/projects/<id>` URLs and nothing matching bundled ids.
7. Restart the API → hard-refresh: content returns from the API (no reload of
   bundled data at any point).

## Tech Stack

- **Frontend**: React 19, TanStack Router, TanStack Query
- **Styling**: Tailwind CSS, Radix UI
- **Forms**: React Hook Form with Zod validation
- **Build Tool**: Vite
- **Animation**: Framer Motion

## Project Structure

```
src/
├── routes/          # TanStack Router pages
├── components/      # Reusable UI components
├── lib/            # Utilities and helpers
├── styles.css      # Global styles
└── main.tsx        # Entry point
```

## Features

- Responsive design with Tailwind CSS
- Type-safe routing with TanStack Router
- Form validation with Zod
- Dark/light theme support
- Smooth scroll animations
- i18n ready

## License

MIT

