Copy to `docs/requirements.md` in the project repository. Every requirement has a unique id, a priority and **testable** acceptance criteria. Version this document in Git; changes after the feature freeze follow change control.

## 1. Document control

| Project | | Client | |
|---|---|---|---|
| Version | | Date | |
| Author | | Approved by (client) | |

## 2. Business context

- **Problem statement** (confirmed at Gate 02):
- **Goals and success measures** (numbers, dates):
- **Stakeholders** (name, role, decision rights):
- **Target users / personas:**
- **Assumptions and constraints:**
- **Out of scope (explicit):**

## 3. Functional requirements

| ID | Priority (M/S/C/W) | Requirement (user story) | Acceptance criteria (testable) | Status |
|---|---|---|---|---|
| REQ-001 | M | As a *visitor* I want to *request a call-back* so that *the clinic contacts me* | Form validates name, phone, e-mail; success message shown; staff e-mail received within 1 minute; data stored; spam protected | Agreed |
| REQ-002 | M | | | |
| REQ-003 | S | | | |

M = must, S = should, C = could, W = will not this time (MoSCoW).

## 4. Non-functional requirements

| ID | Category | Requirement | Measure / verification |
|---|---|---|---|
| NFR-001 | Performance | Mobile LCP at 2.5 s or less on the key pages | Lighthouse CI / field data |
| NFR-002 | Accessibility | WCAG 2.2 AA | axe + manual keyboard test |
| NFR-003 | Security | OWASP ASVS level agreed in the SOW; no critical/high open findings at release | Security report |
| NFR-004 | Availability | Target uptime agreed in SLA (no guarantee outside the SLA) | Monitoring report |
| NFR-005 | Compatibility | Latest 2 versions of Chrome, Edge, Firefox, Safari | Cross-browser test |
| NFR-006 | Backup | RPO / RTO as agreed; restore tested | Restore-test log |
| NFR-007 | SEO | Titles, meta, sitemap, redirects | SEO checklist |
| NFR-008 | Privacy | Personal data minimised; retention defined; consent where needed | Data inventory |

## 5. Integrations

| System | Purpose | Direction | Auth | Owner of account | Sandbox available | Notes |
|---|---|---|---|---|---|---|
| | | | | | | |

## 6. Data and content

| Item | Source | Owner | Due | Status |
|---|---|---|---|---|
| Logo / brand files | Client | | | |
| Page text | Client | | | |
| Product / service data | Client | | | |
| Photos | Client / stock | | | |

## 7. Roles and permissions (access matrix draft)

| Action | Visitor | Customer | Staff | Admin |
|---|---|---|---|---|
| View public pages | yes | yes | yes | yes |
| Manage own profile | no | yes | yes | yes |
| Manage all bookings | no | no | yes | yes |
| Manage users and settings | no | no | no | yes |

## 8. Sitemap and user flows
(Insert diagrams or links.)

## 9. Risks and dependencies

| ID | Risk / dependency | Probability | Impact | Mitigation | Owner |
|---|---|---|---|---|---|
| R-01 | Client content delivered late | M | H | Dates in SOW; placeholder content approved | PM |

## 10. Approval

| Role | Name | Signature | Date |
|---|---|---|---|
| Client sponsor | | | |
| Project Manager | | | |
| Technical Lead | | | |
