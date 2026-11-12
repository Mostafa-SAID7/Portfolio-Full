# API Surface Specification

The Portfolio API exposes a versioned RESTful surface rooted at `/api/v1/`.

All requests and responses use `application/json` unless handling multipart uploads (`multipart/form-data`) or log streaming (`text/plain`).

---

## 1. Authentication & Security

- **Public Endpoints**: Accessible anonymously. Protected by sliding-window rate limiters.
- **Administrative Endpoints**: Require a valid signed JSON Web Token (JWT) passed in the `Authorization` HTTP header:
  ```http
  Authorization: Bearer <your-jwt-token>
  ```
- **Login Route**: `POST /api/v1/auth/login` exchanges administrator credentials for a signed JWT token valid for 7 days.

---

## 2. Complete Endpoint Inventory

### Public API Surface (Anonymous Access)

| Endpoint | Method | Rate Limit | Description |
| :--- | :--- | :--- | :--- |
| `/api/v1/public/projects` | `GET` | 60 req/min | Fetches active published projects. Supports filtering by `?website={slug}` or `?category={id}`. |
| `/api/v1/public/projects/{id}` | `GET` | 60 req/min | Retrieves detailed single project view by ID or slug. |
| `/api/v1/public/skills` | `GET` | 60 req/min | Returns categorized technical skills with proficiency levels. |
| `/api/v1/public/credentials` | `GET` | 60 req/min | Returns chronological career timeline, degrees, and certifications. |
| `/api/v1/public/categories` | `GET` | 60 req/min | Taxonomy categories used for filtering projects and skills. |
| `/api/v1/public/websites` | `GET` | 60 req/min | Registered portfolio website entities and metadata. |
| `/api/v1/public/contact` | `POST` | 5 req/min | Submits an inquiry message. Triggers background SMTP email notification. |

---

### Authentication (`/api/v1/auth`)

| Endpoint | Method | Access | Description |
| :--- | :--- | :--- | :--- |
| `/api/v1/auth/login` | `POST` | Anonymous (Rate-limited: 5 attempts/15m) | Authenticates credentials and issues signed JWT bearer token. |
| `/api/v1/auth/refresh` | `POST` | Bearer JWT | Refreshes active token prior to expiration. |

---

### Admin Operations (Bearer JWT Required)

| Resource | Method | Endpoint | Description |
| :--- | :--- | :--- | :--- |
| **Projects** | `GET` | `/api/v1/projects` | Full list including drafts, inactive projects, and metadata. |
| | `GET` | `/api/v1/projects/{id}` | Single project detail for editing. |
| | `POST` | `/api/v1/projects` | Create a new project record. |
| | `PUT` | `/api/v1/projects/{id}` | Update existing project record. |
| | `DELETE` | `/api/v1/projects/{id}` | Soft-delete or archive project. |
| **Skills** | `GET` | `/api/v1/skills` | List all skills across all categories. |
| | `POST` | `/api/v1/skills` | Create a new skill entity. |
| | `PUT` | `/api/v1/skills/{id}` | Update skill details, order, or proficiency. |
| | `DELETE` | `/api/v1/skills/{id}` | Delete skill entity. |
| **Experience** | `GET` | `/api/v1/experience` | List all career and credential entries. |
| | `POST` | `/api/v1/experience` | Create career or education milestone. |
| | `PUT` | `/api/v1/experience/{id}` | Update milestone entry. |
| | `DELETE` | `/api/v1/experience/{id}` | Delete milestone entry. |
| **Contact Messages**| `GET` | `/api/v1/contact` | Retrieve inbound contact inbox messages. |
| | `PUT` | `/api/v1/contact/{id}/read`| Mark message thread as read/unread. |
| | `DELETE` | `/api/v1/contact/{id}` | Permanently delete contact message. |
| **Media Upload** | `POST` | `/api/v1/upload` | Upload JPEG, PNG, or WebP image (max 10 MiB). |
| **Analytics** | `GET` | `/api/v1/analytics/overview` | Real-time and historical Google Analytics 4 summary. |

---

### System, Diagnostic & Observability Endpoints

| Endpoint | Method | Access | Description |
| :--- | :--- | :--- | :--- |
| `/api/v1/health` | `GET` | Anonymous | Readiness check verifying host runtime and SQL Server connectivity. |
| `/swagger` | `GET` | Anonymous | OpenAPI 3.0 specification & interactive Swagger test console. |
| `/logs` | `GET` | Anonymous / Admin | Interactive browser UI for live production Serilog files. |
| `/api/v1/logs/info` | `GET` | Bearer JWT | Active log file metrics, sizes, and line counts. |
| `/api/v1/logs/entries` | `GET` | Bearer JWT | Filtered log query search by log level and text query. |
| `/api/v1/logs/download` | `GET` | Bearer JWT | Raw rolling log file streaming download. |

---

## 3. Standard Error Format (RFC 7807)

When a request fails, the API responds with a standard RFC 7807 Problem Details payload:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "detail": "Invalid input provided.",
  "errors": {
    "Email": ["The Email field is not a valid e-mail address."]
  },
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

---

## 4. Further Reading
- [docs/API_VERSIONING.md](API_VERSIONING.md) — Detailed route versioning conventions and deprecation policies.
- [docs/ARCHITECTURE.md](ARCHITECTURE.md) — Architectural overview and edge proxy data flow.

