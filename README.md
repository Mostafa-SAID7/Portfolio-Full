# Portfolio — Full-Stack Ecosystem

<p align="center">
  <img src="https://img.shields.io/badge/.NET%209.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 9" />
  <img src="https://img.shields.io/badge/Angular%2020-DD0031?style=for-the-badge&logo=angular&logoColor=white" alt="Angular 20" />
  <img src="https://img.shields.io/badge/React%2019-61DAFB?style=for-the-badge&logo=react&logoColor=black" alt="React 19" />
  <img src="https://img.shields.io/badge/TypeScript%205-3178C6?style=for-the-badge&logo=typescript&logoColor=white" alt="TypeScript" />
  <img src="https://img.shields.io/badge/SQL%20Server-CC292B?style=for-the-badge&logo=microsoftsqlserver&logoColor=white" alt="SQL Server" />
  <img src="https://img.shields.io/badge/Tailwind%20CSS-06B6D4?style=for-the-badge&logo=tailwindcss&logoColor=white" alt="Tailwind CSS" />
  <img src="https://img.shields.io/badge/License-MIT-green?style=for-the-badge" alt="MIT License" />
</p>

An enterprise-grade personal portfolio platform engineered around a centralized, Clean Architecture REST API, an administrative control center, and a public-facing SSR web application.

---

## 🌐 Live Deployments

| Surface | URL | Environment / Host | Status |
| :--- | :--- | :--- | :--- |
| **Public Portfolio** | [portfolio-market.vercel.app](https://portfolio-market.vercel.app) | Vercel (Edge / SSR) | Active |
| **Admin Dashboard** | [dashboard-portfolio-ms.vercel.app](https://dashboard-portfolio-ms.vercel.app) | Vercel (SPA) | Active |
| **Portfolio API** | [portfolio-market.runasp.net](http://portfolio-market.runasp.net) | MonsterASP.NET (IIS) | Active |
| **Swagger UI** | [portfolio-market.runasp.net/swagger](http://portfolio-market.runasp.net/swagger) | ASP.NET Core Swagger | Active |
| **Production Logs UI** | [portfolio-market.runasp.net/logs](http://portfolio-market.runasp.net/logs) | Portfolio Diagnostics | Active |

> [!NOTE]
> Frontends communicate with the API via same-origin `/api` server-side proxy rewrites in their respective `vercel.json` configurations. This eliminates CORS overhead and prevents mixed-content restrictions. Neither frontend ever falls back to stale bundled fixtures if the API is offline — lists render intentional empty/error states and detail routes return 404.

---

## 📦 Ecosystem Applications

```text
.
├── portfolio/          # Public Portfolio Site (React 19 / TanStack Start SSR)
├── dashboard/          # Operations Admin Panel (Angular 20 Standalone / Signals SPA)
└── PortfolioAPI/       # Unified REST Backend (ASP.NET Core 9 / Clean Architecture)
```

1. **[Public Portfolio Site](portfolio/)** — High-performance, server-rendered platform built with React 19, TanStack Start & Nitro.
2. **[Admin Dashboard](dashboard/)** — Operations control center built with Angular 20 Standalone Components & Signals.
3. **[Portfolio API](PortfolioAPI/)** — Centralized REST service built on ASP.NET Core 9, Entity Framework Core 9, and SQL Server.

---

## 📚 Documentation Hub

To maintain clarity and prevent duplication, detailed specifications are modularized under [`docs/`](docs/):

| Guide | Description |
| :--- | :--- |
| ⚡ **[Quick Start Guide](docs/QUICK_START.md)** | Step-by-step local setup, database provisioning, and run commands. |
| 🏗️ **[System Architecture](docs/ARCHITECTURE.md)** | High-level topology diagram, edge proxy data flows, and layer responsibilities. |
| 📡 **[API Surface](docs/API_SURFACE.md)** | Complete `/api/v1/*` endpoint inventory, authentication, and error standards. |
| 🧪 **[Testing & CI Verification](docs/TESTING.md)** | Local test execution (246 xUnit tests, Testcontainers) and GitHub Actions pipeline. |
| 🏛️ **[Backend Clean Architecture](docs/BACK_STRUCTURE.md)** | Authoritative source file blueprint and DDD folder layout. |
| 🌿 **[Branching & Release Strategy](docs/BRANCH_STRATEGY.md)** | GitFlow branch taxonomy, release tags, and Conventional Commits. |
| 🚀 **[Production Deployment](docs/DEPLOYMENT.md)** | Hosting on MonsterASP.NET (IIS) and Vercel edge infrastructure. |
| 📦 **[Storage Pipeline](docs/STORAGE.md)** | Dual-provider image handling (Local disk dev + Cloudinary production). |
| 🏷️ **[API Versioning](docs/API_VERSIONING.md)** | Route versioning rules (`/api/v1/`), deprecation policies, and headers. |
| 🗂️ **[Full Structure Map](docs/FULL_STRUCTURE.md)** | Monorepo directory tree and file location index. |
| 📊 **[Dashboard Audit](docs/DASHBOARD_AUDIT.md)** | Frontend technical audit history and architectural decisions. |

---

## ✨ Key Architectural Highlights

- **Clean Architecture & SRP**: Strict inward dependency flow. DTOs are isolated per feature (1 class per file); mappings are compiled partial extensions without reflection overhead.
- **Interactive Production Logs Browser**: Dedicated HTML viewer at `/logs` and API at `/api/v1/logs/*` for real-time telemetry, log level filtering, and streaming downloads.
- **Google Analytics 4 Integration**: Outbound GA4 Data API client delivering real-time user metrics, page views, and geographic breakdowns to the admin panel.
- **Multi-Website Registry**: Ability to tag projects and content to distinct frontend website targets across the ecosystem.
- **Dual-Provider Media Pipeline**: Transparent image upload validation, decompression-bomb protection, and delivery supporting local filesystem in dev and Cloudinary CDN in production.
- **Resilient Startup Fallbacks**: Graceful handling of transient configuration and logging issues to guarantee maximum uptime on IIS hosting environments.

---

## ⚡ Quick Start Summary

To spin up the ecosystem locally:

```bash
# 1. Start Backend API (:5039)
dotnet run --project PortfolioAPI/src/PortfolioAPI/PortfolioAPI.csproj

# 2. Start Admin Dashboard (:4200)
cd dashboard && npm ci && npm run dev

# 3. Start Public Portfolio (:5173)
cd portfolio && npm ci && npm run dev
```

For full environment variable configuration, local SQL Server setup, and user secrets, see the **[Quick Start Guide](docs/QUICK_START.md)**.

---

## 🤝 Contributing & Policies

- **[CONTRIBUTING.md](CONTRIBUTING.md)** — Development standards, PR guidelines, and code review criteria.
- **[SECURITY.md](SECURITY.md)** — Vulnerability reporting policy and security controls.
- **[ACCESSIBILITY.md](ACCESSIBILITY.md)** — WCAG 2.1 AA accessibility guidelines.

---

## 📄 License

This project is licensed under the [MIT License](LICENSE.txt).
