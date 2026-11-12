# Portfolio Admin — Dashboard

A modern administrative control center built with **Angular 20**, **TypeScript**, and **Tailwind CSS**. Designed with Angular Signals and standalone components, it provides direct management across all Portfolio API resources with zero mock data.

---

## 1. Feature Screens & Routes

| Route | Feature | Description |
| :--- | :--- | :--- |
| `/dashboard` | **Overview** | System health checklist, quick counts (projects, skills, unread messages), and recent inquiry feeds. |
| `/projects` | **Projects** | Project CRUD: bilingual titles/descriptions, cover image upload, tags, category filters, and website assignments. |
| `/skills` | **Skills** | Technical skills management: proficiency sliders (0–100%), icon selection, category groupings, and bilingual descriptions. |
| `/experiences` | **Experiences** | Career history timeline: role, company, date range, current role flag, and technology tags. |
| `/credentials` | **Credentials** | Academic degrees, professional certifications, and awards ordered chronologically. |
| `/testimonials` | **Testimonials** | Client endorsements, author titles, avatars, and recommendation quotes. |
| `/categories` | **Categories** | Project category taxonomies used for public portfolio filter chips. |
| `/websites` | **Websites** | Multi-website registry enabling a single backend to drive multiple distinct client frontends. |
| `/contacts` | **Inbox** | Visitor inquiry management: unread filtering, thread detail, mark-as-read, and in-app visitor reply dispatch. |
| `/analytics` | **Analytics** | Google Analytics 4 dashboard: active users, session graphs, geographical traffic, and device breakdowns. |
| `/logs` | **Logs Viewer** | Production log telemetry browser: level filtering (VERBOSE to FATAL), live query search, file stats, and raw log download. |
| `/auth/login` | **Authentication** | Secure JWT sign-in interface communicating with `POST /api/v1/auth/login`. |

---

## 2. Local Development

### Prerequisites
- Node.js `20.x` or `22.x`
- Running backend API instance at `http://localhost:5039`

### Setup and Start
```bash
# 1. Install dependencies
npm ci

# 2. Launch development server
npm run dev # or npm start
```

The application will serve on `http://localhost:4200`.

> [!NOTE]
> `proxy.conf.json` forwards `/api` requests directly to `http://localhost:5039`, avoiding CORS complications during local development.

---

## 3. Frontend Architecture Highlights

- **Reactive State with Signals**: Each feature domain is governed by a dedicated signal-driven service (`ProjectsService`, `SkillsService`, `ContactsService`, `LogsService`, etc.) providing immutable signals for loading states, errors, and entity collections.
- **Standalone Components & Lazy Routing**: 100% standalone components configured with route-level lazy loading (`loadComponent`) in `src/app/app.routes.ts` for minimal initial bundle overhead.
- **OnPush Change Detection**: Ensures high UI performance by evaluating templates only when signals change or inputs receive new references.
- **Tailwind Design System**: Semantic color tokens configured in `tailwind.config.js` backed by CSS custom properties in `src/styles.css`, supporting both dark mode and light mode themes.
- **Authentication & Interceptors**:
  - `auth.interceptor.ts`: Attaches the JWT Bearer token to outgoing admin mutation requests.
  - `api-error.interceptor.ts`: Intercepts 401/403/500 responses and standardizes user feedback via toast notifications.

---

## 4. Verification & Testing

```bash
# Production bundle build
cmd /c "npm run build"

# Run Angular unit tests
npm test
```
