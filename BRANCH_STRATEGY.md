# Branch Strategy & Workflow Guide

## Branch Structure

### Permanent Branches
| Branch | Purpose | Protected |
|--------|---------|-----------|
| `main` | Production-ready code, deployed to MonsterASP/Vercel | ✅ Yes |
| `develop` | Integration branch for features, staging environment | ✅ Yes |

### Temporary Branches (short-lived)
| Prefix | Purpose | Example | Base Branch |
|--------|---------|---------|-------------|
| `feat/` | New features | `feat/bilingual-skills` | `develop` |
| `fix/` | Bug fixes | `fix/500-startup-crash` | `develop` |
| `bug/` | Critical bug fixes | `bug/arabic-encoding` | `develop` |
| `release/` | Release preparation | `release/v1.2.0` | `develop` |
| `hotfix/` | Production hotfixes | `hotfix/startup-timeout` | `main` |
| `chore/` | Maintenance tasks | `chore/update-dependencies` | `develop` |
| `docs/` | Documentation updates | `docs/api-endpoints` | `develop` |

## Workflow

### 1. Feature Development
```bash
git checkout develop
git pull origin develop
git checkout -b feat/your-feature-name
# ... make changes ...
git commit -m "feat(scope): description"
git push -u origin feat/your-feature-name
# Create PR to develop
```

### 2. Bug Fixes
```bash
git checkout develop
git pull origin develop
git checkout -b fix/bug-description
# ... make changes ...
git commit -m "fix(scope): description"
git push -u origin fix/bug-description
# Create PR to develop
```

### 3. Release Preparation
```bash
git checkout develop
git pull origin develop
git checkout -b release/v1.x.x
# ... version bump, changelog, final testing ...
git commit -m "chore(release): prepare v1.x.x"
git push -u origin release/v1.x.x
# Create PR to main
# After merge, tag the release
git tag -a v1.x.x -m "Release v1.x.x"
git push origin v1.x.x
```

### 4. Hotfixes (Production)
```bash
git checkout main
git pull origin main
git checkout -b hotfix/critical-issue
# ... make fix ...
git commit -m "fix(scope): critical fix description"
git push -u origin hotfix/critical-issue
# Create PR to main AND develop
# After merge, tag the release
```

## Commit Message Convention

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

### Types
- `feat` — New feature
- `fix` — Bug fix
- `docs` — Documentation
- `style` — Formatting (no code change)
- `refactor` — Code refactoring
- `test` — Adding/updating tests
- `chore` — Maintenance tasks
- `perf` — Performance improvements
- `ci` — CI/CD changes

### Scopes
- `api` — Backend API
- `dashboard` — Angular dashboard
- `portfolio` — React portfolio
- `db` — Database/migrations
- `auth` — Authentication
- `ui` — UI components

### Examples
```
feat(api): add bilingual skill fields
fix(db): resolve EF Core version mismatch
chore(release): prepare v1.2.0
docs(api): update endpoint documentation
test(skills): add unit tests for skill service
```

## Branch Protection Rules

### main
- Require PR reviews (1+)
- Require status checks to pass
- No direct pushes
- Require up-to-date branches

### develop
- Require PR reviews (1+)
- Require status checks to pass
- No direct pushes

## Release Process

1. Create `release/vX.Y.Z` from `develop`
2. Update version numbers in project files
3. Update CHANGELOG.md
4. Run full test suite
5. Create PR to `main`
6. After merge, tag release: `git tag -a vX.Y.Z -m "Release vX.Y.Z"`
7. Deploy to MonsterASP/Vercel
8. Merge `release/vX.Y.Z` back to `develop`

## Hotfix Process

1. Create `hotfix/issue-description` from `main`
2. Fix the issue
3. Create PR to `main` (fast-track review)
4. After merge, tag: `git tag -a vX.Y.Z -m "Hotfix vX.Y.Z"`
5. Deploy immediately
6. Merge `hotfix/` branch to `develop`

## Current Branch Status

| Branch | Status | Last Commit |
|--------|--------|-------------|
| `main` | Production | 07450cf (all fixes) |
| `develop` | Integration | 07450cf (all fixes) |
| `origin/cleanup/api-only-static-data` | Stale | — |
| `origin/v0/fix-tanstack-security-update` | Stale | — |

## Cleanup Recommendations

The following remote branches are stale and can be deleted:
- `origin/cleanup/api-only-static-data`
- `origin/v0/fix-tanstack-security-update`
EOF