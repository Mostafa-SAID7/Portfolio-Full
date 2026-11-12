# Full Repository Architecture & Structure Reference

An authoritative snapshot of the entire **Portfolio Full-Stack** monorepo ecosystem.

---

## 1. High-Level Monorepo Architecture

```
Portfolio-Full/
├── PortfolioAPI/       # ASP.NET Core 9 Clean Architecture REST API
├── dashboard/          # Angular 20 Standalone Admin Dashboard
├── portfolio/          # React 19 / TanStack Start Public SSR Website
├── docs/               # Engineering documentation & architecture specifications
└── .github/            # GitHub Actions CI workflows & issue/PR templates
```

---

## 2. Directory Structure Breakdown

### 2.1 Backend (`PortfolioAPI/`)
*For in-depth backend design, see [docs/BACK_STRUCTURE.md](BACK_STRUCTURE.md).*

```
PortfolioAPI/
├── src/
│   ├── PortfolioAPI/               # Web API Host (Controllers, Middlewares, Configurations, Program.cs)
│   ├── PortfolioAPI.Core/          # Pure Domain Entities, Contracts & Value Objects
│   ├── PortfolioAPI.Application/   # Feature DTOs, Mappings, Services & FluentValidators
│   └── PortfolioAPI.Infrastructure/# EF Core DbContext, Repositories, SMTP, Cloudinary, GA4 Client
└── tests/
    ├── PortfolioAPI.UnitTests/     # 246+ Unit tests (xUnit, Moq, FluentAssertions)
    └── PortfolioAPI.IntegrationTests/ # Integration tests (MsSql Testcontainers)
```

### 2.2 Admin Dashboard (`dashboard/`)
*Angular 20 administrative panel communicating with `PortfolioAPI` via JWT bearer tokens.*

```
dashboard/
├── src/
│   ├── app/
│   │   ├── core/                          # Singleton services, models, interceptors & guards
│   │   │   ├── api.config.ts              # API URL resolution & configuration
│   │   │   ├── guards/                    # AuthGuard & login redirection
│   │   │   ├── interceptors/              # Auth & ApiError interceptors
│   │   │   ├── models/                    # TypeScript interfaces for API models
│   │   │   └── services/                  # Signal-based domain services (Projects, Contacts, Logs, etc.)
│   │   ├── features/                      # Standalone feature modules (lazy-loaded routes)
│   │   │   ├── analytics/                 # GA4 metrics, charts & device breakdowns
│   │   │   ├── auth/                      # JWT login screen
│   │   │   ├── categories/                # Category management
│   │   │   ├── contacts/                  # Inquiries inbox & reply threads
│   │   │   ├── credentials/               # Certifications & career credentials
│   │   │   ├── dashboard/                 # System health overview & summary metrics
│   │   │   ├── experiences/               # Career timeline management
│   │   │   ├── logs/                      # Production log browser, search & download
│   │   │   ├── projects/                  # Project CRUD & image uploads
│   │   │   ├── skills/                    # Technical skills catalog
│   │   │   ├── testimonials/              # Client recommendations
│   │   │   └── websites/                  # Multi-site frontend registry
│   │   ├── shared/                        # Shared UI components (Paginator, Dialogs, Inputs)
│   │   ├── app.component.ts               # Layout shell: sidebar, navigation, drawer
│   │   └── app.routes.ts                  # Declarative route configuration
│   ├── styles.css                         # Tailwind CSS semantic custom properties & theme tokens
│   └── index.html
├── proxy.conf.json                        # Local dev proxy forwarding /api -> :5039
├── tailwind.config.js
└── package.json
```

### 2.3 Public Portfolio Site (`portfolio/`)
*Public-facing platform engineered with React 19, TanStack Start, TanStack Router, and Nitro SSR.*

```
portfolio/
├── src/
│   ├── routes/                            # File-based TanStack Router routes
│   │   ├── index.tsx                      # Landing page (Hero, Featured Projects, Skills)
│   │   ├── about.tsx                      # Professional bio & experience summary
│   │   ├── projects/                      # Project catalog & detail case studies
│   │   ├── skills.tsx                     # Skill matrices & tech stacks
│   │   ├── experience.tsx                 # Career history timeline
│   │   └── contact.tsx                    # Interactive inquiry submission form
│   ├── components/                        # Radix UI & Tailwind presentation components
│   ├── lib/                               # API client, origin resolution & SSR cache helpers
│   └── data/                              # Types, Arabic translation maps & presentation metadata
├── vercel.json                            # Server-side /api rewrite rules to backend
└── package.json
```

### 2.4 Documentation (`docs/`)
```
docs/
├── API_VERSIONING.md                      # URL segment versioning (/api/v1/) and deprecation rules
├── BACK_STRUCTURE.md                      # Authoritative Clean Architecture backend guide
├── BRANCH_STRATEGY.md                     # Git branching model, conventional commits & release process
├── DASHBOARD_AUDIT.md                     # Frontend audit notes & refactoring decisions
├── DEPLOYMENT.md                          # MonsterASP.NET & Vercel deployment guide
├── FULL_STRUCTURE.md                      # Full monorepo structure reference (this file)
└── STORAGE.md                             # Media storage architecture (Local disk & Cloudinary)
```
