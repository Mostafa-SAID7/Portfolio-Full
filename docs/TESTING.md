# Testing & CI Verification Guide

This guide describes our automated testing strategy, local test commands, and CI validation pipelines across the monorepo.

---

## 1. Testing Strategy

The repository employs a multi-tiered verification approach:

```text
+-------------------------------------------------------+
|                 End-to-End & SSR Checks               |
|         React 19 TanStack Start Production Build      |
+-------------------------------------------------------+
                           |
+-------------------------------------------------------+
|                 Frontend Static Checks                |
|           Angular 20 Production Build & Typing        |
+-------------------------------------------------------+
                           |
+-------------------------------------------------------+
|                 Integration Testing                   |
|        Testcontainers MsSql Real Database Suite       |
+-------------------------------------------------------+
                           |
+-------------------------------------------------------+
|                   Unit Testing                        |
|        246 xUnit Tests (Controllers, Services, SRP)   |
+-------------------------------------------------------+
```

---

## 2. Running Backend Tests Locally

### Unit Tests (`PortfolioAPI.UnitTests`)
Our unit test suite contains **246 tests** covering controllers, domain logic, FluentValidation rules, and service mappings without needing a database.

```bash
# Run all unit tests
dotnet test PortfolioAPI/tests/PortfolioAPI.UnitTests/PortfolioAPI.UnitTests.csproj

# Run tests with detailed console logger
dotnet test PortfolioAPI/tests/PortfolioAPI.UnitTests/PortfolioAPI.UnitTests.csproj --logger "console;verbosity=normal"

# Run tests matching a specific filter
dotnet test PortfolioAPI/tests/PortfolioAPI.UnitTests/PortfolioAPI.UnitTests.csproj --filter "FullyQualifiedName~Project"
```

### Integration Tests (`PortfolioAPI.IntegrationTests`)
The integration test suite uses [Testcontainers for .NET](https://dotnet.testcontainers.org/) to spin up a real Microsoft SQL Server container in Docker:

```bash
# Ensure Docker Desktop / daemon is running, then:
dotnet test PortfolioAPI/tests/PortfolioAPI.IntegrationTests/PortfolioAPI.IntegrationTests.csproj
```

---

## 3. Running Frontend Builds & Verifications

### Admin Dashboard (Angular 20)
Verifies TypeScript compilation, template type-checking, and AOT production bundling:

```bash
cd dashboard
npm ci
cmd /c "npm run build"
```

### Public Portfolio (React 19 / TanStack Start)
Verifies React 19 SSR build, Nitro bundling, and client hydration manifests:

```bash
cd portfolio
npm ci
npm run build
```

---

## 4. GitHub Actions CI Pipeline

On every `push` and `pull_request` targeting `main` and `develop`, the workflow [.github/workflows/ci.yml](../.github/workflows/ci.yml) triggers three parallel jobs:

| Job | Environment | Target | Steps |
| :--- | :--- | :--- | :--- |
| **Backend CI** | `ubuntu-latest` | `Portfolio.sln` | 1. Setup .NET 9 SDK<br>2. Restore dependencies<br>3. `dotnet build --no-restore`<br>4. `dotnet test PortfolioAPI.UnitTests` |
| **Dashboard CI** | `ubuntu-latest` | `dashboard/` | 1. Setup Node.js 22<br>2. `npm ci`<br>3. `npm run build --configuration=production` |
| **Portfolio CI** | `ubuntu-latest` | `portfolio/` | 1. Setup Node.js 22<br>2. `npm ci`<br>3. `npm run build` |

All three jobs must pass green before a Pull Request is eligible for merging into `main` or `develop`.

---

## 5. Writing New Tests

When adding new features or fixing bugs:
1. **Unit Tests**: Place in `PortfolioAPI/tests/PortfolioAPI.UnitTests/`. Follow the naming convention `[Feature]Tests.cs` and use the Arrange-Act-Assert (AAA) pattern.
2. **Mocking**: Use `Moq` for abstract service and repository interfaces (`Mock<IRepository<T>>`).
3. **Assertions**: Prefer fluent assertions (`result.Should().NotBeNull()`).
4. **Validation Tests**: Ensure every new FluentValidation rule has corresponding positive and negative test cases.

