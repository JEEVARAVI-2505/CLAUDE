Complete in staging before UAT (Stage 19-21) and re-run before production. Mark **Pass**, **Fail**, **N/A** (with reason). Attach evidence (screenshots, reports, logs) in `qa/`. Release only when there are **no open critical/high defects** and every N/A is justified.

| Project | | Build / tag | | Tester | | Date | |
|---|---|---|---|---|---|---|---|

## 1. Functional

| # | Check | Pass | Fail | N/A | Evidence |
|---|---|---|---|---|---|
| 1.1 | Every "must" requirement has a passing test (traceability matrix) | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |
| 1.2 | All forms validate on client and server; errors are clear | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |
| 1.3 | Happy paths and edge cases for each critical journey (E2E suite green) | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |
| 1.4 | E-mails sent with correct content, links and sender; unsubscribe where required | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |
| 1.5 | Payment sandbox: success, decline, timeout, duplicate webhook, refund | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |
| 1.6 | Uploads: size and type limits, malicious files rejected | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |
| 1.7 | Search, filters, pagination, sorting correct | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |
| 1.8 | 404, 403, 500 pages friendly; no stack traces | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |

## 2. Security

| # | Check | Pass | Fail | N/A | Evidence |
|---|---|---|---|---|---|
| 2.1 | Access matrix tests pass; no IDOR (other users' records unreachable) | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |
| 2.2 | Passwords hashed with Argon2id/bcrypt; rate limiting and lockout work | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |
| 2.3 | Cookies HttpOnly, Secure, SameSite; CSRF protection where cookies are used | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |
| 2.4 | Input validation and output encoding; no SQL injection / XSS in tests and ZAP baseline | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |
| 2.5 | Security headers present (HSTS, CSP, nosniff, frame protection, referrer policy) | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |
| 2.6 | Dependency, secret, container scans: no unresolved critical/high | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |
| 2.7 | No secrets in repository history, logs, client bundle | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |
| 2.8 | CORS allow-list correct; admin routes protected; debug off | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |

## 3. Accessibility (WCAG 2.2 AA)

| # | Check | Pass | Fail | N/A | Evidence |
|---|---|---|---|---|---|
| 3.1 | Keyboard-only operation of all journeys; visible focus; no traps | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |
| 3.2 | Headings and landmarks logical; `lang` set; skip link | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |
| 3.3 | Contrast 4.5:1 text, 3:1 UI; no colour-only meaning | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |
| 3.4 | Images have alt; forms have labels and linked errors | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |
| 3.5 | axe/Lighthouse: no critical issues; screen-reader pass on key flows | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |

## 4. Responsive and browser

| # | Check | Pass | Fail | N/A | Evidence |
|---|---|---|---|---|---|
| 4.1 | 360 / 768 / 1024 / 1440 px - no horizontal scroll, readable text, usable controls | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |
| 4.2 | Latest 2 versions: Chrome, Edge, Firefox, Safari (desktop + mobile) | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |
| 4.3 | At least one real iOS and one real Android device | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |

## 5. Performance and SEO

| # | Check | Pass | Fail | N/A | Evidence |
|---|---|---|---|---|---|
| 5.1 | Mobile LCP <= 2.5 s, INP <= 200 ms, CLS <= 0.1 on key pages | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |
| 5.2 | Lighthouse targets met (see Section 7.5) | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |
| 5.3 | Load test at agreed traffic: p95 and error rate within target | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |
| 5.4 | Titles, meta, canonical, sitemap, robots, redirects, structured data valid | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |
| 5.5 | Staging blocked from indexing; production allowed | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |

## 6. Data and reliability

| # | Check | Pass | Fail | N/A | Evidence |
|---|---|---|---|---|---|
| 6.1 | Schema built only from migrations; staging = production schema | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |
| 6.2 | Backup exists and **restore tested** within RTO | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |
| 6.3 | Logs structured, no sensitive data; request ids present | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |
| 6.4 | Health endpoint and uptime monitor; alert fire-drill passed | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |
| 6.5 | Rollback rehearsed | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |

## 7. Documentation

| # | Check | Pass | Fail | N/A | Evidence |
|---|---|---|---|---|---|
| 7.1 | README, architecture, database, API, deployment, security, user guide current | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |
| 7.2 | CHANGELOG and release notes updated | <span class="box"></span> | <span class="box"></span> | <span class="box"></span> | |

## Defect log

| ID | Severity (C/H/M/L) | Steps to reproduce | Expected / actual | Evidence | Owner | Status |
|---|---|---|---|---|---|---|
| QA-001 | | | | | | |

## Release recommendation

| Result | Name | Signature | Date |
|---|---|---|---|
| QA engineer: Ready / Not ready | | | |
| Technical Lead: Go / No-go | | | |
