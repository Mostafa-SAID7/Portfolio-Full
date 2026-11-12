# Local Development Quick Start Guide

This guide walks you through setting up and running all three projects in the Portfolio Full-Stack ecosystem locally.

---

## 1. Prerequisites

Ensure the following runtimes and tools are installed:

| Component | Required Version | Verification Command | Notes |
| :--- | :--- | :--- | :--- |
| **.NET SDK** | `9.0.x` | `dotnet --version` | Powers the `PortfolioAPI` backend |
| **Node.js** | `22.x` (LTS) or `>= 20.x` | `node -v` | Required for Dashboard and Portfolio |
| **npm / pnpm** | `npm 10+` | `npm -v` | Package manager |
| **SQL Server** | 2022 / LocalDB / Docker | `sqlcmd -?` | Relational database engine |

---

## 2. Backend Service (`PortfolioAPI`)

The backend REST API listens on `http://localhost:5039`.

### Step 1: Configure Local Settings
Create `src/PortfolioAPI/appsettings.Development.json` (git-ignored) or set user-secrets with your local SQL connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=PortfolioDb_Dev;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;"
  },
  "Auth": {
    "Username": "admin",
    "Password": "DevAdminPassword123!"
  }
}
```

### Step 2: Apply Migrations & Launch
```bash
# Apply EF Core migrations to local database
dotnet ef database update --project PortfolioAPI/src/PortfolioAPI.Infrastructure --startup-project PortfolioAPI/src/PortfolioAPI

# Launch Web API
dotnet run --project PortfolioAPI/src/PortfolioAPI/PortfolioAPI.csproj
```

- **Base URL**: `http://localhost:5039`
- **Swagger Documentation**: `http://localhost:5039/swagger`
- **Production Logs Viewer**: `http://localhost:5039/logs`
- **Health Endpoint**: `http://localhost:5039/api/v1/health`

---

## 3. Admin Dashboard (`dashboard/`)

The admin control center is an **Angular 20** SPA running on `http://localhost:4200`.

```bash
cd dashboard

# Install dependencies
npm ci

# Start development server
npm run dev # or npm start
```

> [!NOTE]
> The Angular dev server uses `proxy.conf.json` to proxy `/api` calls directly to `http://localhost:5039`, avoiding CORS complications during development.

---

## 4. Public Portfolio Site (`portfolio/`)

The public website is a **React 19 / TanStack Start** SSR platform running on `http://localhost:5173`.

```bash
cd portfolio

# Install dependencies
npm ci

# Start development server
npm run dev
```

The site will be accessible at `http://localhost:5173`. In development mode, API queries automatically resolve to `http://localhost:5039`.

---

## 5. Next Steps
- Review [docs/ARCHITECTURE.md](ARCHITECTURE.md) to understand data flows and layer responsibilities.
- Review [docs/API_SURFACE.md](API_SURFACE.md) for endpoint specifications.
- Review [docs/TESTING.md](TESTING.md) for running automated test suites.

