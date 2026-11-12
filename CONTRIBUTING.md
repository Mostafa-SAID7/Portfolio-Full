# Contributing to Portfolio Full-Stack

Thank you for your interest in contributing! This document details how to set up your local development environment, adhere to project conventions, run the test suites, and submit quality pull requests.

---

## 1. Code of Conduct

By participating in this project, you agree to abide by our [Code of Conduct](CODE_OF_CONDUCT.md). Please treat all community members with empathy and respect.

---

## 2. Getting Started

### Prerequisites

| Component | Minimum Version | Notes |
| :--- | :--- | :--- |
| **.NET SDK** | `9.0.x` | Required for `PortfolioAPI` backend |
| **Node.js** | `22.x` (or `>= 20.x` for dashboard) | Recommended: LTS Node 22 |
| **pnpm / npm** | `npm 10+` or `pnpm 9+` | Frontend package management |
| **SQL Server** | LocalDB / SQL Server 2022 / Docker | Persistent storage provider |

### Clone and Setup

```bash
git clone https://github.com/Mostafa-SAID7/Portfolio-Full.git
cd Portfolio-Full
```

### Running the Solutions

- **Backend API**:
  ```bash
  dotnet run --project PortfolioAPI/src/PortfolioAPI/PortfolioAPI.csproj
  # Endpoints: http://localhost:5039, Swagger UI: http://localhost:5039/swagger
  ```
- **Admin Dashboard**:
  ```bash
  cd dashboard
  npm install
  npm start
  # Serves at: http://localhost:4200 (proxies /api to :5039)
  ```
- **Public Portfolio (SSR)**:
  ```bash
  cd portfolio
  npm install
  npm run dev
  # Serves at: http://localhost:5173
  ```

---

## 3. Development Guidelines & Architecture

### Backend Architectural Boundaries (Clean Architecture)

- **`PortfolioAPI.Core`**:
  - Pure domain models, value objects, domain exceptions, repository interfaces (`IRepository<T>`, `IUnitOfWork`), and common results (`Result<T>`).
  - **Zero dependencies** on external frameworks, database ORMs, or UI concerns.
- **`PortfolioAPI.Application`**:
  - DTOs (organized by feature folder under `DTOs/{Feature}/`, 1 class per file per Single Responsibility Principle).
  - Explicit partial mapping classes (`Mappings/`).
  - Application service interfaces (`Interfaces/`) and implementations (`Services/`).
  - FluentValidation rule validators (`Validators/`).
- **`PortfolioAPI.Infrastructure`**:
  - EF Core `PortfolioDbContext`, EntityTypeConfigurations, Migrations, Repositories.
  - External adapters: SMTP Emailing, Local disk / Cloudinary storage, Google Analytics 4 API client.
- **`PortfolioAPI` (Web Host)**:
  - Controllers, REST endpoints, JSON serialization options.
  - Middleware pipelines (Error handling, Correlation ID, Serilog HTTP logging, Security headers).
  - Production log browser UI (`/logs`) and API (`/api/v1/logs/*`).

### Frontend Standards

- **Dashboard (Angular 20)**:
  - Standalone components, signals for reactive state, OnPush change detection.
  - Strict typing: use generated or strongly-typed models from `src/app/core/models/`.
  - Tailwind CSS with semantic dark/light color tokens.
- **Portfolio (React 19 / TanStack Start)**:
  - Server-Side Rendering (SSR) driven exclusively by live API data.
  - Never fall back to static bundled data for entity collections (API-down resilience).

---

## 4. Branching & Pull Request Process

Detailed branch management rules are documented in [docs/BRANCH_STRATEGY.md](docs/BRANCH_STRATEGY.md).

1. **Create a branch**:
   - `feat/feature-name` for new features (from `develop`).
   - `fix/bug-name` for bug fixes (from `develop`).
   - `hotfix/issue-name` for production emergency patches (from `main`).
2. **Verify locally before pushing**:
   ```bash
   # Backend: Build + Test
   dotnet build Portfolio.sln
   dotnet test PortfolioAPI/tests/PortfolioAPI.UnitTests/

   # Frontend: Build check
   cmd /c "npm run build" # In dashboard/
   ```
3. **Commit Messages**:
   - Follow [Conventional Commits](https://www.conventionalcommits.org/):
   - Example: `feat(logs): add level filtering to log entry controller`
4. **Submit PR**:
   - Fill in the [Pull Request Template](.github/PULL_REQUEST_TEMPLATE.md).
   - Ensure all automated CI checks pass.
   - Maintainers will review and merge when validated.

---

## 5. Security & Vulnerabilities

Please do not report security vulnerabilities via public GitHub issues. See [SECURITY.md](SECURITY.md) for our responsible disclosure policy.
