# Portfolio API — Backend Service

A centralized RESTful API service built with **ASP.NET Core 9** and **Entity Framework Core**, providing the single source of truth for all content consumed by the public portfolio site and manipulated via the admin dashboard.

---

## Architecture Overview

The solution follows Clean Architecture principles divided into decoupled layers:

```
PortfolioAPI/
├── src/
│   ├── PortfolioAPI/               # API Host: Controllers, Middleware, Auth & Program.cs
│   ├── PortfolioAPI.Core/          # Domain layer: Entities, Specifications, Interfaces, DTOs
│   ├── PortfolioAPI.Application/   # Use cases: Services, Validators (FluentValidation), Mappings
│   └── PortfolioAPI.Infrastructure/# Persistence: DbContext, SQL Server Configurations, Repositories, Migrations
└── tests/
    ├── PortfolioAPI.UnitTests/     # Unit tests (xUnit, Moq, FluentAssertions)
    └── PortfolioAPI.IntegrationTests/ # Integration tests with Testcontainers (Docker)
```

---

## Endpoints & Versioning

The API uses **URL-segment versioning** under `/api/v1/`:

- **Public endpoints (anonymous reads)**:
  - `GET /api/v1/public/projects` — Active projects catalog
  - `GET /api/v1/public/skills` — Active skills categorized
  - `GET /api/v1/public/credentials` — Work experience, education & certifications
  - `GET /api/v1/public/categories` — Active category list
  - `POST /api/v1/public/contact` — Contact form submission (rate-limited)
- **Admin endpoints (JWT-authenticated mutations)**:
  - `POST /api/v1/auth/login` — Sign in and obtain bearer JWT token
  - `CRUD /api/v1/Projects` — Project management
  - `CRUD /api/v1/Skills` — Skills management
  - `CRUD /api/v1/Experience` — Career history management
  - `GET /api/v1/Contact` — Contact inbox management
- **Health check**:
  - `GET /api/v1/health` — Database and process readiness

---

## Running Locally

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server (LocalDB, SQL Express, or Docker container)

### 1. Database Setup & Migrations
Ensure your connection string in `src/PortfolioAPI/appsettings.Development.json` is configured, then apply EF Core migrations:

```bash
dotnet ef database update --project src/PortfolioAPI.Infrastructure --startup-project src/PortfolioAPI
```

### 2. Start the API
```bash
dotnet run --project src/PortfolioAPI/PortfolioAPI.csproj
```

The service will listen on:
- API: `http://localhost:5039`
- Swagger UI (Development mode): `http://localhost:5039/swagger`

---

## Running Tests

```bash
# Run unit tests
dotnet test tests/PortfolioAPI.UnitTests

# Run integration tests (Docker daemon must be running)
dotnet test tests/PortfolioAPI.IntegrationTests
```
