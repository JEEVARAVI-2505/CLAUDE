# Part 4: API Governance and Company Standards

These standards apply to every API VEL builds. A deviation needs a written exception approved by the API Architect.

## 4.1 Naming

| Item | Rule | Good | Bad |
|---|---|---|---|
| Resource paths | Plural nouns, lowercase, hyphens | `/v1/purchase-orders` | `/getPurchaseOrder`, `/v1/PurchaseOrder` |
| Actions | HTTP method, not verb in path | `POST /v1/orders` | `/v1/createOrder` |
| Sub-resources | Nest one level | `/v1/clients/12/contacts` | `/v1/clients/12/contacts/5/notes/9/files` |
| JSON fields | `snake_case`, consistent across the API | `created_at` | `CreatedAt`, `createdAt` mixed |
| Dates | ISO 8601 in UTC | `2026-11-04T09:30:00Z` | `04/11/26` |
| Money | Integer minor units + currency code | `{"amount":125000,"currency":"INR"}` | `1250.00` as float |
| Identifiers | Opaque, stable | `"id": "cus_8f3a..."` | Sequential ids that leak volume (for public APIs) |

## 4.2 Versioning

- Version in the path: `/v1/...`. A new major version only for **breaking** changes.
- **Non-breaking** (adding optional fields, new endpoints) stays in the same version.
- **Breaking:** removing/renaming fields, changing types or meaning, tightening validation, changing auth.
- Run at most **two** major versions in production. Announce the old one's end date at least the notice period promised in the client's agreement.

## 4.3 Authentication

| Method | Use when | Do not use when |
|---|---|---|
| **API key** (header `X-API-Key`) | Server-to-server, one known partner, low sensitivity | End-user identity is needed; the key could end up in a browser or mobile app |
| **OAuth 2.0** (client credentials) | Machine-to-machine with scopes and rotation | Simple internal tool where a key is enough |
| **OAuth 2.0 + OpenID Connect** (authorisation code with PKCE) | Acting on behalf of a user; single sign-on | Pure back-office batch jobs |
| **JWT** access tokens | Stateless validation across services; short life (minutes) | You need instant revocation and cannot afford a deny-list or short expiry |
| **Session cookie** | Browser applications of our own | Third-party API consumers |

Rules: keys sent in **headers only**, never in URLs · tokens expire · refresh tokens stored securely and rotated · every key/token has an owner and a rotation date in the access register · compare secrets with constant-time functions.

## 4.4 Error response standard

Every error uses this shape (Assignment 01 implements it):

```json
{
  "error": {
    "code": "validation_error",
    "message": "Request validation failed",
    "details": [{"field": "email", "problem": "String should match pattern"}],
    "request_id": "7c1e1f1e-3a3c-4c4a-9a4e-0c7f4d6b2a11"
  }
}
```

| Status | Meaning | Use for |
|---|---|---|
| 200 | OK | Successful read / update |
| 201 | Created (+ `Location` header) | Successful create |
| 202 | Accepted | Work queued, not finished (webhooks, async jobs) |
| 204 | No content | Successful delete |
| 400 | Bad request | Malformed syntax |
| 401 | Unauthorised | Missing/invalid credentials |
| 403 | Forbidden | Valid credentials, not allowed |
| 404 | Not found | Unknown resource (also used to hide existence from unauthorised callers) |
| 409 | Conflict | Duplicate, version conflict |
| 422 | Unprocessable | Validation failed |
| 429 | Too many requests (+ `Retry-After`) | Rate limit |
| 500 / 502 / 503 / 504 | Server / upstream error | Our fault or upstream unavailable |

Never return stack traces, SQL, internal paths or secrets in an error body.

## 4.5 Pagination, filtering, sorting

- Every list endpoint is paginated. Default `page_size` 20, maximum 100.
- Small, stable data: `page` + `page_size` and a `total`. Large or fast-changing data: **cursor-based** (`next_cursor`).
- Filters are query parameters named after the field: `?status=active&industry=retail`.
- Sorting: `?sort=name` ascending, `?sort=-created_at` descending; allow-list the sortable fields.

## 4.6 Documentation

- Every API has an **OpenAPI 3** description kept in `api-docs/openapi.yaml` (generated or hand-written, but always in the repository).
- Every endpoint has a summary, request/response examples and the error cases.
- Documentation is part of "done". A pull request that changes behaviour without changing docs is rejected.

## 4.7 Logging

- **Structured** (JSON in staging/production), one line per request: time, level, request ID, method, path, status, duration, caller id.
- Propagate the `X-Request-ID` header to every downstream call so one failure can be traced end-to-end.
- **Never log:** passwords, tokens, API keys, full card numbers, national IDs, health data, full request bodies of sensitive endpoints.

## 4.8 Security (summary)

HTTPS only · deny by default · validate every input · filter every output · least privilege · rate limit · strict CORS allow-list · secrets from environment/secrets manager · audit log for create/update/delete and authentication events · dependency and secret scanning in CI.

## 4.9 Testing

Every endpoint: success, validation failure, unauthenticated, unauthorised, not-found. Every connector: success, retry, duplicate, rate-limit, vendor-down. Minimum automated coverage and the contract test baseline are set per project in the test plan.

## 4.10 Release management

Semantic versioning for the service (`MAJOR.MINOR.PATCH`) · changelog entry per release · staging soak before production · production release by approval only · rollback tested at least once per project.

## 4.11 Deprecation

1. Announce in writing with a date. 2. Add `Deprecation` and `Sunset` response headers on the old version. 3. Provide a migration guide. 4. Monitor remaining callers. 5. Remove only after the notice period and with zero (or accepted) traffic.

## 4.12 Client access

Each client consumer gets its own credentials, scopes and rate limit. Access is requested by ticket, approved by the PM, recorded in the access register, reviewed every quarter and revoked on contract end or staff change.

## 4.13 API inventory

The Architect maintains one inventory of every API and integration. Fields:

| Field | Example |
|---|---|
| API / integration name | Client Registry API |
| Owner (person + team) | Senior Integration Engineer, API team |
| Version | v1.0.0 |
| Environment(s) | dev, staging, prod |
| Consumers | CRM sync worker, client portal |
| Authentication | API key (rotation due 2027-01-15) |
| Data classification | Internal / Confidential / Personal |
| Lifecycle status | Planned · Active · Deprecated · Retired |
| Documentation link | Repository `api-docs/openapi.yaml` |
| Last security review | Date |
