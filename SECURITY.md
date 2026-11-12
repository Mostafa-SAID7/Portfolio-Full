# Security Policy

We take the security of Portfolio Full-Stack seriously. This policy outlines how security vulnerabilities should be responsibly disclosed and how updates are handled.

---

## 1. Supported Versions

Security updates and critical patches are actively provided for the following software baselines:

| Component | Active Version | Security Support Status |
| :--- | :--- | :--- |
| **PortfolioAPI (Backend)** | `.NET 9.0.x` | Supported (:white_check_mark:) |
| **Admin Dashboard** | `Angular 20.x` | Supported (:white_check_mark:) |
| **Public Portfolio** | `React 19.x / TanStack Start` | Supported (:white_check_mark:) |
| **Older versions** | `< 9.0 (Backend)` / `< 19.0 (Frontend)` | End of Life (:x:) |

---

## 2. Security Design Principles

The application implements defense-in-depth security across all architectural layers:

1. **Authentication & Authorization**:
   - Cryptographically signed JWT tokens with strict audience, issuer, signing key, and expiration validations.
   - Admin mutating endpoints (`POST`, `PUT`, `DELETE`) are strictly guarded with `[Authorize]`.
   - Ephemeral or vault-stored secret keys; no credentials checked into version control.
2. **Rate Limiting & Abuse Prevention**:
   - Public contact form submission (`POST /api/v1/public/contact`) and authentication (`POST /api/v1/auth/login`) are protected with strict IP rate-limiting policies.
3. **Input Validation & Sanitization**:
   - Strongly-typed DTOs validated via FluentValidation before domain entity mutation.
   - String trimming, length clamps, and null-safety guards.
4. **Media & File Upload Safeguards**:
   - Extension allowlists (`.jpg`, `.jpeg`, `.png`, `.gif`, `.webp`). SVGs are rejected to prevent stored XSS.
   - Content-type verification and magic-byte signature validation.
   - ImageSharp decompression-bomb protection (capped pixel count and dimensions).
5. **Transport & HTTP Security Headers**:
   - Baseline HTTP security headers (`X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`).
   - HSTS enabled in production environments.

---

## 3. Reporting a Vulnerability

If you discover a security vulnerability or potential exploit, please do **NOT** open a public issue or discuss it publicly.

Instead, report it privately through one of the following channels:

- **GitHub Security Advisory**: Open a private advisory under the [Security tab](https://github.com/Mostafa-SAID7/Portfolio-Full/security/advisories/new).
- **Security Contact**: Email `m.ssaid356@gmail.com` with the subject tag `[SECURITY VULNERABILITY] Portfolio-Full`.

### What to Include in Your Report

To help us triage and resolve the issue quickly, please provide:
1. Type of issue (e.g. Broken Access Control, SQL Injection, SSRF, Information Disclosure, XSS).
2. Affected component, endpoint, or file.
3. Step-by-step instructions or proof-of-concept (PoC) script/request demonstrating the behavior.
4. Assessment of potential impact and suggested remediation (if known).

### Response SLA

- **Initial Acknowledgment**: Within 48 hours of report receipt.
- **Triage & Assessment**: Within 5 business days.
- **Remediation & Patch**: Addressed on an expedited priority hotfix branch (`hotfix/`), validated, and deployed to production.
