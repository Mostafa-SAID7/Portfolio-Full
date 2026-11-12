# Portfolio API — Backend Service

A high-performance, modular RESTful API service engineered with **ASP.NET Core 9** and **Entity Framework Core**. It serves as the single source of truth for all content consumed by the public portfolio platform and managed through the administrative dashboard.

---

## 1. Clean Architecture Overview

The backend strictly adheres to Clean Architecture and Single Responsibility principles, maintaining loose coupling across four distinct layers:

```
PortfolioAPI/
├── src/
│   ├── PortfolioAPI/               # Web API Host & Composition Root
│   │   ├── Controllers/            # API Controllers (Admin, Public, Auth, Logs, Upload)
│   │   ├── Configuration/          # IServiceCollection extension modules
│   │   ├── Pipelines/              # Middleware pipeline definitions (Error, Logging, Security)
│   │   ├── Endpoints/              # Minimal API endpoints (Health, Info, Root redirect)
│   │   ├── Auth/                   # JWT Token generation & claim services
│   │   └── Program.cs              # Assembly & pipeline startup contract
│   │
│   ├── PortfolioAPI.Core/          # Domain Layer (Pure Domain — 0 External Dependencies)
│   │   ├── Entities/               # Domain models (Project, Skill, Experience, Contact, etc.)
│   │   ├── Interfaces/             # Domain contracts (IRepository<T>, IUnitOfWork, IEmailService)
│   │   ├── Common/                 # Result<T> pattern, Error primitives
│   │   └── Exceptions/             # Domain exceptions
│   │
│   ├── PortfolioAPI.Application/   # Use Cases & Application Business Logic
│   │   ├── DTOs/                   # Feature-specific DTOs (1 class per file, SRP)
│   │   │   ├── Projects/           # ProjectDto, CreateProjectDto, UpdateProjectDto, etc.
│   │   │   ├── Skills/             # SkillDto, CreateSkillDto, UpdateSkillDto
│   │   │   ├── Contacts/           # ContactDto, CreateContactDto, ReplyToContactDto
│   │   │   ├── Analytics/          # AnalyticsOverviewDto, etc.
│   │   │   └── ...                 # Experiences, Credentials, Categories, Websites, Testimonials
│   │   ├── Mappings/               # Pure manual mapping extensions (EntityMappings.*.cs)
│   │   ├── Interfaces/             # Application service contracts (IProjectService, etc.)
│   │   ├── Services/               # Domain service implementations (CrudService<T>, etc.)
│   │   ├── Validators/             # FluentValidation rule definitions per payload
│   │   └── Exceptions/             # Application-level validation & not-found exceptions
│   │
│   └── PortfolioAPI.Infrastructure/# Infrastructure Adapters & Persistence
│       ├── Data/                   # PortfolioDbContext, Model configurations, DbInitializer
│       ├── Repositories/           # Generic Repository<T> & UnitOfWork implementations
│       ├── Emailing/               # SMTP client, embedded HTML templates, delivery handling
│       ├── Storage/                # Local disk & Cloudinary image storage providers
│       ├── Services/               # Google Analytics 4 Data API client & reporting service
│       └── Migrations/             # EF Core incremental database schema migrations
│
└── tests/
    ├── PortfolioAPI.UnitTests/     # 246+ Unit tests (xUnit, Moq, FluentAssertions)
    └── PortfolioAPI.IntegrationTests/ # Integration tests with Testcontainers (Docker SQL Server)
```

---

## 2. API Surface & Endpoints

All JSON endpoints are versioned under the `/api/v1/` prefix with case-insensitive routing.

### Public Read API (Anonymous, Cached)
- `GET /api/v1/public/projects` — Active, published projects with website and category filtering.
- `GET /api/v1/public/skills` — Active skills categorized by group.
- `GET /api/v1/public/credentials` — Education, certifications, and work credentials.
- `GET /api/v1/public/categories` — Active category list for client-side filter chips.
- `GET /api/v1/public/websites` — Registered portfolio websites in the multi-site ecosystem.
- `POST /api/v1/public/contact` — Public contact inquiry submission (rate-limited, best-effort email notification).

### Admin API (JWT-Guarded Mutations)
- `POST /api/v1/auth/login` — Sign in and issue signed Bearer JWT token (rate-limited).
- `GET`/`POST`/`PUT`/`DELETE` on `/api/v1/Projects` — Project portfolio management.
- `GET`/`POST`/`PUT`/`DELETE` on `/api/v1/Skills` — Skills management.
- `GET`/`POST`/`PUT`/`DELETE` on `/api/v1/Experience` — Career history management.
- `GET`/`POST`/`PUT`/`DELETE` on `/api/v1/Credentials` — Career credentials management.
- `GET`/`POST`/`PUT`/`DELETE` on `/api/v1/Categories` — Project category taxonomies.
- `GET`/`POST`/`PUT`/`DELETE` on `/api/v1/Websites` — Frontend website registry.
- `GET`/`PUT`/`DELETE` on `/api/v1/Contact` — Contact message inbox, unread status, thread replies.
- `POST /api/v1/Upload` — Multipart image uploads (JPEG, PNG, WebP, GIF, AVIF up to 10 MiB).
- `GET /api/v1/analytics/overview` — GA4 real-time and historical telemetry metrics.

### System Diagnostics & Operational Telemetry
- `GET /api/v1/health` — Readiness & liveness probe (checks runtime and SQL Server connectivity).
- `GET /logs` — Production-grade interactive HTML UI for real-time log browsing, filtering, and inspection.
- `GET /api/v1/logs/info` — Log file telemetry (current file size, line counts, timestamps).
- `GET /api/v1/logs/entries` — Filtered log search by level (VERBOSE, DEBUG, INFO, WARN, ERROR, FATAL) and query.
- `GET /api/v1/logs/download` — Authenticated streaming download of current log files.

---

## 3. Local Development

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server (LocalDB, SQL Express, or Docker container)

### 1. Configuration Setup
Create `src/PortfolioAPI/appsettings.Development.json` (git-ignored) with your local database connection:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=PortfolioDb_Dev;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "Auth": {
    "Username": "admin",
    "Password": "YourDevPassword123!"
  }
}
```

### 2. Apply Migrations & Run
```bash
# Update database schema to latest migration
dotnet ef database update --project src/PortfolioAPI.Infrastructure --startup-project src/PortfolioAPI

# Start the Web API
dotnet run --project src/PortfolioAPI/PortfolioAPI.csproj
```

The server listens on:
- **API**: `http://localhost:5039`
- **Swagger Documentation**: `http://localhost:5039/swagger`
- **Production Logs UI**: `http://localhost:5039/logs`

---

## 4. Test Suites

```bash
# Run unit tests (246 tests across Application, Core, Infrastructure, and Controllers)
dotnet test tests/PortfolioAPI.UnitTests

# Run integration tests (Requires Docker daemon running for Testcontainers MsSql)
dotnet test tests/PortfolioAPI.IntegrationTests
```
