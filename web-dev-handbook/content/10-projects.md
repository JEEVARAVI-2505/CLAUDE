Seven practical projects take the trainee from a single page to a complete simulated client engagement. Each project has a **brief, acceptance criteria, skills practised, deliverables and a reviewer**. Projects are built in the training organisation on GitHub, follow the repository standards (Section 6) and are accepted only after code review and demonstration.

| # | Project | Level | Duration | Reviewer |
|---|---|---|---|---|
| 1 | Responsive business website | 2 | 1 week | Mentor |
| 2 | Multi-page corporate website | 2 | 2 weeks | Mentor + UX |
| 3 | Authentication-enabled web application | 3 | 2 weeks | Technical Lead |
| 4 | CRUD application with database | 3-4 | 2 weeks | Technical Lead |
| 5 | E-commerce prototype | 5 | 2 weeks | Technical Lead |
| 6 | SaaS admin dashboard | 5 | 2 weeks | Technical Lead + QA |
| 7 | Complete simulated client project | 1-5 | 3 weeks | PM + Technical Lead + QA |

## Project 1 - Responsive business website

| Item | Detail |
|---|---|
| Brief | A 3-5 section one-page site for a fictional local business (clinic, bakery, gym) with hero, services, about, contact form and footer |
| Stack | HTML5, CSS3 (Flexbox/Grid), a little JavaScript |
| Must include | Semantic structure; mobile-first responsive layout; accessible navigation and form; optimised images; SEO metadata; Lighthouse >= 90 (Performance, Accessibility, Best Practices, SEO) |
| Deliverables | GitHub repo with README, deployed page on free static hosting, Lighthouse report |
| Acceptance | Passes W3C validator; works at 360/768/1024/1440 px; keyboard-only usable; no console errors |

## Project 2 - Multi-page corporate website

| Item | Detail |
|---|---|
| Brief | A 6-8 page corporate site (Home, About, Services, Service detail, Blog/News, Contact) with a design from the UX Designer |
| Stack | Next.js + TypeScript; content in Markdown/MDX or a headless CMS |
| Must include | Shared layout and component library; SEO (metadata, sitemap, `robots.txt`, structured data); image optimisation; a working contact form with server-side validation and e-mail; 404 page |
| Deliverables | Repo with docs, Storybook or component catalogue, Playwright smoke test, deployed preview, performance report |
| Acceptance | Matches the design within tolerance; LCP <= 2.5 s on mobile profile; accessibility checklist passed |

## Project 3 - Authentication-enabled web application

| Item | Detail |
|---|---|
| Brief | A member area - register, verify e-mail, log in, log out, reset password, profile, roles (member/admin) |
| Stack | Next.js + FastAPI (or Node) + PostgreSQL |
| Must include | Argon2id hashing; httpOnly cookies or short-lived JWT + refresh; rate limiting; RBAC with ownership checks; validation; error format; request ids; tests for the access matrix |
| Deliverables | Repo, API specification, security notes (`docs/security.md`), test report |
| Acceptance | Technical Lead's five attack tests fail (credential stuffing, token tampering, IDOR, CSRF, brute force); no secrets in Git history |

## Project 4 - CRUD application with a database

| Item | Detail |
|---|---|
| Brief | A bookings or inventory app with at least six related tables - list, search, filter, paginate, create, edit, delete, export CSV, upload a document |
| Stack | Next.js + API + PostgreSQL + migrations |
| Must include | ER diagram and data dictionary; migrations and seeds; indexes backed by `EXPLAIN`; transactions for multi-step operations; file upload hardening; Docker Compose; CI pipeline; backup and restore drill |
| Deliverables | Repo, `docs/database-design.md`, restore-test log, CI run |
| Acceptance | Fresh clone runs with one command; migrations build an identical schema; concurrency test passes; restore completes within the stated RTO |

## Project 5 - E-commerce prototype

| Item | Detail |
|---|---|
| Brief | Catalogue, product page, cart, checkout, order history, admin product management; payment in **sandbox/test mode only** |
| Stack | Next.js + API + PostgreSQL + payment provider sandbox |
| Must include | Server-side price and total calculation; tax and discount rules with unit tests; order state machine; webhook verification and idempotency; order e-mails; inventory handling with transactions |
| Deliverables | Repo, API spec, payment flow diagram, test matrix (success, decline, duplicate webhook, refund) |
| Acceptance | No price or total can be changed from the browser; duplicate webhooks do not double-process; Playwright test completes a purchase |

{{diagram: payment_flow | Figure 10.1 - Payment flow used in Project 5 (server-side order, signed webhook)}}

## Project 6 - SaaS admin dashboard

| Item | Detail |
|---|---|
| Brief | A multi-tenant dashboard - organisations, users and roles, invitations, subscription plan display, usage charts, audit log |
| Stack | Next.js + API + PostgreSQL; a charts library |
| Must include | Tenant isolation on every query; role-based UI and API; audit trail; responsive data tables with server pagination; accessibility for tables and charts; monitoring and logging; CI/CD to staging |
| Deliverables | Repo, architecture doc with tenant model, security tests for cross-tenant access, dashboards for logs/uptime |
| Acceptance | A user from tenant A can never read tenant B data (tests prove it); p95 API latency within target under a load test |

## Project 7 - Complete simulated client project

The capstone runs the **full 27-stage lifecycle** with a trainer acting as the client.

| Item | Detail |
|---|---|
| Brief | The trainer supplies a one-paragraph enquiry for a realistic client (for example a dental clinic wanting a website with online booking and payment) |
| Team | 3-4 trainees in roles (PM, UX, full-stack, QA/DevOps); roles rotate within the capstone |
| Required artefacts | Qualified-lead sheet, questionnaire answers, requirements, competitor matrix, costed SOW, feature-freeze register, sitemap, Figma design, architecture + ADRs, DB design, repository, working application, test report, security report, staging deployment, UAT sign-off, production-like deployment with domain and HTTPS, handover pack, monitoring and a first monthly report |
| Assessment | Trainer-client acceptance; Technical Lead code and security review; PM review of documents and costing; 30-minute oral defence by each trainee |
| Pass mark | Every gate evidenced; no critical security finding; costing reconciles; the client can operate the system from the handover pack |

!!! gate "CERTIFICATION"
    On passing Project 7 and all level gates, the Technical Lead and Director issue the **Delivery Readiness Certificate**. The employee may then join supervised client projects. Certification is reviewed annually.

## Project rubric (used by reviewers)

| Dimension | Weight | Excellent means |
|---|---|---|
| Functionality | 20% | All acceptance criteria met; edge cases handled |
| Code quality | 20% | Clean, typed, tested, small PRs, consistent standards |
| Security | 20% | Controls implemented and verified; no secrets; attack tests pass |
| UX and accessibility | 15% | Responsive, accessible, polished states |
| Engineering practice | 15% | Git history, CI, review, Docker, docs |
| Documentation and communication | 10% | Clear README, ADRs, honest status, good demo |

A project is accepted at 70% overall with no dimension below 50% and **no failure in Security**.
