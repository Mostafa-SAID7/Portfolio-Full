# Branch Strategy & Workflow Guide

An engineering guide to Git workflows, branch lifecycle, conventions, and release management for the Portfolio Full-Stack ecosystem.

---

## 1. Branch Taxonomy

### Permanent Branches

| Branch | Environment | Purpose | Protection |
| :--- | :--- | :--- | :--- |
| `main` | Production (`runasp.net` & Vercel) | Production-ready, validated code. Every merge triggers CI and production deployment readiness. | Protected (PR review, CI checks pass, no direct pushes) |
| `develop` | Staging / Pre-release | Active integration branch for completed feature branches. | Protected (PR review, CI checks pass) |

### Ephemeral Branches (Short-Lived)

Ephemeral branches should branch off `develop` (or `main` for hotfixes), stay small, and merge back promptly via Pull Requests.

| Prefix | Base Branch | Target Branch | Purpose | Example |
| :--- | :--- | :--- | :--- | :--- |
| `feat/` | `develop` | `develop` | New features & enhancements | `feat/logs-viewer-ui`, `feat/hangfire-jobs` |
| `fix/` | `develop` | `develop` | Bug fixes & stability improvements | `fix/db-connection-resilience` |
| `refactor/` | `develop` | `develop` | Code restructuring without behavior changes | `refactor/split-dtos-and-mappings` |
| `perf/` | `develop` | `develop` | Performance optimizations | `perf/output-cache-tuning` |
| `docs/` | `develop` | `develop` | Documentation additions or updates | `docs/modernize-architecture-guides` |
| `release/` | `develop` | `main` & `develop` | Version bump, release changelog, final staging tests | `release/v2.1.0` |
| `hotfix/` | `main` | `main` & `develop` | Critical production issues requiring immediate patch | `hotfix/smtp-auth-timeout` |

---

## 2. Core Workflows

### Feature & Refactor Workflow

```bash
# 1. Start from up-to-date develop
git checkout develop
git pull origin develop
git checkout -b feat/your-feature-name

# 2. Develop & test locally
dotnet test PortfolioAPI/tests/PortfolioAPI.UnitTests
cmd /c "npm run build" # In dashboard/

# 3. Commit using Conventional Commits
git commit -m "feat(api): implement background jobs with Hangfire"

# 4. Push and open PR
git push -u origin feat/your-feature-name
# Open Pull Request targeting `develop`
```

### Hotfix Workflow (Emergency Production Patch)

```bash
# 1. Branch from current production release
git checkout main
git pull origin main
git checkout -b hotfix/critical-fix-name

# 2. Implement fix, write regression test, verify
dotnet test PortfolioAPI/tests/PortfolioAPI.UnitTests

# 3. Commit and push
git commit -m "fix(auth): fix token validation header extraction"
git push -u origin hotfix/critical-fix-name

# 4. PR into main (fast-track)
# 5. After merge to main, back-merge or cherry-pick into develop:
git checkout develop
git pull origin develop
git merge main
git push origin develop
```

### Release Workflow

```bash
# 1. Cut release branch from develop
git checkout develop
git pull origin develop
git checkout -b release/v2.1.0

# 2. Bump versions and finalize release notes
git commit -m "chore(release): prepare v2.1.0"
git push -u origin release/v2.1.0

# 3. PR into main -> Merge when CI passes
# 4. Tag the commit on main
git checkout main
git pull origin main
git tag -a v2.1.0 -m "Release v2.1.0"
git push origin v2.1.0

# 5. Sync back to develop
git checkout develop
git pull origin develop
git merge main
git push origin develop
```

---

## 3. Commit Message Standards

This repository follows [Conventional Commits v1.0.0](https://www.conventionalcommits.org/).

### Format

```text
<type>(<scope>): <summary>

[optional detailed body explaining *why*, not just *what*]

[optional footer(s): Resolves #123, Breaking Changes]
```

### Standard Types

- `feat`: A new user-facing feature or API capability.
- `fix`: A bug fix or patch.
- `refactor`: Structural code reorganization without functional changes.
- `perf`: Performance optimization (memory, database, queries, bundles).
- `test`: Adding or correcting tests.
- `docs`: Documentation changes only.
- `chore`: Build tooling, dependencies, repo hygiene.
- `ci`: GitHub Actions workflow configuration.

### Common Scopes

`api`, `application`, `core`, `infrastructure`, `dashboard`, `portfolio`, `db`, `auth`, `storage`, `email`, `analytics`, `logs`

### Concrete Examples

```text
feat(logs): add interactive production log browser and download endpoint
refactor(application): split monolithic entity mappings into partial classes
fix(email): handle STARTTLS timeout gracefully with circuit breaker
docs(readme): modernize architecture diagram and quickstart guide
test(contacts): add test coverage for reply delivery and background jobs
```

---

## 4. Quality Gates & PR Verification

Every pull request must pass the automated CI pipeline before merge approval:

1. **Backend CI**:
   - `dotnet build Portfolio.sln -c Release` (0 errors)
   - `dotnet test PortfolioAPI/tests/PortfolioAPI.UnitTests/` (all unit tests green)
2. **Dashboard CI**:
   - `npm ci`
   - `npm run build` (production Angular compilation without bundle budget breach)
3. **Portfolio CI**:
   - `npm ci`
   - `npm run build` (Vercel nitro / TanStack Start SSR output generation)
