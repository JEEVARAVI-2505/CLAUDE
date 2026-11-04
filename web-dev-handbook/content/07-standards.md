These standards define what "professional" means in our deliverables. Each row has a **measurable target** and a **way to verify it**. A release is not accepted until the QA checklist (Appendix A6) shows every applicable standard as met or as a documented, client-approved exception.

## 7.1 Coding conventions

| Standard | Requirement | Verified by |
|---|---|---|
| Language | TypeScript `strict` for frontend/Node; Python 3.11+ with type hints for FastAPI | `tsc --noEmit`, `mypy`/`pyright` in CI |
| Formatting | Prettier (JS/TS/CSS/MD), Black + Ruff (Python); format on save | CI format check |
| Linting | ESLint with accessibility plugin; Ruff; zero warnings in `main` | CI |
| Naming | Per Section 6 table; names describe intent (`calculateInvoiceTotal`, not `calc2`) | Review |
| Function size | One responsibility; aim below about 40 lines; avoid deep nesting (more than 3 levels) | Review |
| Comments | Explain **why**, not what; public functions have docstrings/JSDoc | Review |
| Dead code | No commented-out code or unused exports in `main` | Linter |
| Dependencies | Justify each; pinned via lock file; licence recorded; no abandoned packages | Dependabot, review |
| Secrets | Read from environment variables only; the app fails fast at startup if one is missing | Config tests, secret scan |
| Error handling | Never swallow exceptions; catch where you can act | Review |

## 7.2 Reusable components

| Standard | Requirement |
|---|---|
| Design system | Colours, type scale, spacing and radii live as design tokens (CSS variables), mirrored from Figma |
| Component API | Typed props with sensible defaults; no hidden global state; composable (children/slots) |
| States | Every interactive component has default, hover, focus-visible, active, disabled, loading and error states |
| Accessibility | Built on semantic HTML; keyboard operable; labelled; documented in the catalogue |
| Documentation | Name, purpose, props table and usage example (Storybook for larger projects) |
| Testing | Each shared component has at least a render test and an interaction test |
| Reuse rule | The second use of a pattern -> extract a component; the third -> it must already be one |

## 7.3 Responsive behaviour

| Standard | Target |
|---|---|
| Approach | Mobile-first CSS; `<meta name="viewport">` on every page |
| Breakpoints | 360 (base), 768, 1024, 1440 px - test at each; also 320 and 1920 |
| Layout | Flexbox/Grid, relative units; no horizontal scrolling at any width |
| Touch | Tap targets at least 44x44 px; spacing between targets; no hover-only actions |
| Typography | Body text at least 16 px; line length about 45-75 characters; fluid headings (`clamp()`) |
| Images | `srcset`/`sizes`, modern formats (WebP/AVIF), explicit width/height to prevent layout shift |
| Verification | DevTools device mode + at least one real phone and one tablet |

## 7.4 Accessibility (WCAG 2.2 AA)

| Standard | Requirement |
|---|---|
| Structure | Landmarks (`header`, `nav`, `main`, `footer`), one `h1`, logical heading order, `lang` attribute |
| Keyboard | Everything reachable and operable by keyboard; visible focus; no keyboard traps; skip-to-content link |
| Contrast | Text 4.5:1 (large text 3:1); UI components and focus indicators 3:1 |
| Alternatives | Meaningful `alt` text; captions/transcripts for media; no information by colour alone |
| Forms | Visible labels, error messages linked with `aria-describedby`, clear instructions, correct `autocomplete` |
| Motion | Respect `prefers-reduced-motion`; no auto-playing audio; no flashing content |
| Verification | axe/Lighthouse >= 90 with zero critical issues + manual keyboard test + one screen-reader pass |

## 7.5 Performance

| Metric (mobile, field/lab) | Target ("good" thresholds per web.dev) |
|---|---|
| Largest Contentful Paint (LCP) | 2.5 s or less |
| Interaction to Next Paint (INP) | 200 ms or less |
| Cumulative Layout Shift (CLS) | 0.1 or less |
| Lighthouse Performance (mobile) | 90 or more for content sites; 80 or more for complex apps |
| API latency | p95 under 500 ms for standard endpoints (agreed per project) |
| Page weight | Initial JavaScript under about 200 KB gzipped for content pages; images optimised |
| Practices | Compress and lazy-load images, subset fonts, cache static assets with long TTL + content hashing, use a CDN, avoid blocking scripts, paginate lists, add DB indexes for measured queries |
| Verification | Lighthouse CI, WebPageTest, k6/Locust load test, DB `EXPLAIN` evidence |

## 7.6 SEO (where relevant)

| Standard | Requirement |
|---|---|
| Rendering | Content visible in HTML (SSR/SSG), not only after client JavaScript |
| Metadata | Unique `<title>` and meta description per page; canonical URL; Open Graph tags |
| Structure | Clean URLs, XML sitemap, `robots.txt`, correct redirects (301) for changed URLs |
| Content | One primary topic per page; descriptive headings and link text; alt text |
| Structured data | JSON-LD (Organization, LocalBusiness, Product, Article) validated in the Rich Results test |
| Technical | HTTPS, mobile-friendly, fast, no broken links; submit sitemap in Search Console |
| Honesty | No ranking guarantees - SEO results depend on competition and time |

## 7.7 Secure authentication

| Standard | Requirement |
|---|---|
| Password storage | Argon2id (or bcrypt) with per-user salt; never reversible encryption |
| Password policy | Minimum 10 characters, allow long passphrases, check against breached-password lists, no forced periodic rotation without cause |
| Sessions/tokens | httpOnly, Secure, SameSite cookies; access tokens 15 minutes or less; refresh rotation; logout invalidates |
| Brute force | Rate limit and backoff per account and per IP; generic error messages |
| MFA | Required for admin/staff accounts; offered to customers |
| Recovery | Single-use, expiring, random reset tokens; notify the user of changes |
| Authorisation | Server-side RBAC + ownership checks on every request; deny by default |
| Verification | Attack tests (credential stuffing, token tampering, IDOR, CSRF, replay) pass |

## 7.8 Database security

| Standard | Requirement |
|---|---|
| Network | Private network only; no public database port |
| Access | Separate least-privilege roles for application, migrations, read-only reporting |
| Queries | Parameterised queries / ORM only - never string-built SQL |
| Encryption | TLS in transit; encryption at rest; field-level encryption for highly sensitive data |
| Data minimisation | Store only what is needed; define retention; pseudonymise where possible |
| Auditing | Log privileged access and changes to sensitive tables |
| Backups | Encrypted, off-site, restore-tested (Section 7.12) |
| Test data | Synthetic or anonymised data only outside production |

## 7.9 API validation

| Standard | Requirement |
|---|---|
| Schemas | Every request validated by a typed schema (Pydantic/Zod); unknown fields rejected where relevant |
| Limits | Maximum body size, string lengths, page sizes, upload sizes |
| Output | Response models whitelist fields; never return password hashes or internal ids you do not need |
| Idempotency | `Idempotency-Key` for payment-like POSTs; webhooks verified by signature and de-duplicated |
| Rate limiting | Per user/IP; stricter on login, reset and expensive endpoints |
| CORS | Allow-list of origins - never `*` with credentials |
| Contract | OpenAPI is the contract; breaking changes require a new version |

## 7.10 Error handling

| Standard | Requirement |
|---|---|
| Format | One JSON error shape: `{ "error": { "code", "message", "request_id" } }` |
| Status codes | 400/422 validation, 401 unauthenticated, 403 forbidden, 404 missing, 409 conflict, 429 rate limited, 5xx server |
| User messages | Plain language, actionable, no technical details |
| Frontend | Loading, empty and error state for every data view; retry where safe; global error boundary |
| Resilience | Timeouts on all outbound calls; retries only for idempotent operations; graceful degradation |

## 7.11 Logging and monitoring

| Standard | Requirement |
|---|---|
| Format | Structured JSON with timestamp, level, message, request id, user id |
| Content | No passwords, tokens, full card or national-ID numbers; mask personal data |
| Levels | ERROR for action needed, WARNING for unexpected but handled, INFO for business events, DEBUG off in production |
| Retention | Defined per project and privacy law (for example 30-90 days operational; longer for audit as agreed) |
| Monitoring | Uptime check, error tracking, latency/error-rate dashboard, certificate expiry, disk and database alerts, backup-failure alert |

## 7.12 Backup and recovery

| Standard | Requirement |
|---|---|
| Objectives | RPO and RTO agreed in the SOW |
| Method | Automated daily backups plus point-in-time recovery where the database supports it; uploaded files and configuration included |
| Location | At least one copy outside the primary account/region; encrypted |
| Testing | Restore test before go-live and at least quarterly, with timing recorded |
| Retention | Documented (for example 7 daily, 4 weekly, 12 monthly) |

## 7.13 Browser and device compatibility

| Standard | Requirement |
|---|---|
| Supported | Latest two versions of Chrome, Edge, Firefox and Safari (desktop and mobile); anything different is agreed in the SOW |
| Method | Feature support checked on MDN/Can I use; progressive enhancement; no browser sniffing |
| Verification | Playwright projects for Chromium, Firefox and WebKit + real-device spot checks |

## 7.14 Documentation

| Standard | Requirement |
|---|---|
| Required set | `README`, requirements, architecture, database design, API specification, deployment runbook, security notes, user guide, CHANGELOG |
| Quality | Accurate, dated, versioned in Git, written for the next person; diagrams as source (draw.io/Mermaid) |
| Update rule | A change is not "done" until its documentation is updated |
| Handover | The complete pack is delivered and walked through with the client (Stage 26) |
