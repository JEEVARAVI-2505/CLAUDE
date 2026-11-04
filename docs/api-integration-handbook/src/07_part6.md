# Part 6: The 90-Day Learning Roadmap

The roadmap has **three phases**, three **gates**, and **ten practical projects**. A gate must be passed before the next phase starts. If you do not pass, you repeat the weakest module with your mentor (one repeat is normal).

## 6.1 Roadmap at a glance

| Phase | Days | Weeks | Levels | Projects | Gate |
|---|---|---|---|---|---|
| **1 Foundations** | 1 to 30 | 1 to 4 | 1 · 2 · 3 · start of 4 | P1 Basic REST API | **Gate 1** |
| **2 Build and secure** | 31 to 60 | 5 to 9 | 4 · 5 · 6 · 7 | P2 API + PostgreSQL · P3 Authentication · P4 Integrate two sample apps | **Gate 2** |
| **3 Integrate and operate** | 61 to 90 | 10 to 13 | 8 · 9 | P5 Webhook receiver · P6 Scheduled sync · P7 Retry + idempotency · P8 Monitoring dashboard · P9 Containerise · P10 Capstone | **Gate 3** |

## 6.2 Week-by-week plan

| Week | Focus | You will | Evidence |
|---|---|---|---|
| **1** | Onboarding, Levels 1 and 2 | Install tools; learn client-server, HTTP, DNS, ports, TLS, headers, cookies, CORS | Week-1 criteria (5.6) |
| **2** | Level 3 Python I | Variables, functions, data structures, errors, JSON, files | 20 exercises passed |
| **3** | Level 3 Python II + Level 4 start | Environment variables, logging, async basics; REST concepts, methods, status codes | Postman collection of a public API |
| **4** | Level 4 + **Project 1** | Build, test and document the Client Registry API (Assignment 01) | **Gate 1** review |
| **5** | Level 7 (SQL) + Level 6 (FastAPI deep dive) | PostgreSQL, tables, relationships, indexes; dependency injection, routers | SQL exercise set |
| **6** | **Project 2** | Replace the in-memory store with PostgreSQL, migrations (Alembic), connection pooling | Migration history in Git |
| **7** | Level 5 + **Project 3** | API keys, OAuth 2.0, JWT, RBAC, scopes, secrets; implement token auth | Security tests green |
| **8** | **Project 4 (part 1)** | Two sample apps (a "shop" and an "accounting" app); read vendor-style docs; design the mapping | Data-mapping document |
| **9** | **Project 4 (part 2)** | Build the integration; introduce NestJS / Express to compare | **Gate 2** review |
| **10** | Level 8 + **Project 5** | Webhooks: receive, verify signature, acknowledge fast, process async | Replay-attack test |
| **11** | **Projects 6 and 7** | Scheduled sync with cursors; retries with back-off, idempotency keys, dead-letter queue (RabbitMQ) | Failure-scenario test report |
| **12** | Level 9 + **Projects 8 and 9** | OpenAPI, contract tests, load test; Prometheus/Grafana dashboard; Docker image and compose | Dashboard + image |
| **13** | **Project 10: Capstone** | Full simulated enterprise integration, Stages 03 to 15 in miniature, plus a demo | **Gate 3** board review |

<div class="note" markdown="1">
**Daily rhythm (about 7 hours):** 1 h theory with the official source · 3 h hands-on build · 1 h testing and documentation · 1 h review or pairing · 1 h reading / reflection. Fridays: review, report, assessment.
</div>

## 6.3 The three gates

### Gate 1 (end of week 4): "Can build a correct basic API"

| Criterion | Pass mark |
|---|---|
| Quiz on Levels 1 to 4 | ≥ 80% |
| Assignment 01 delivered per the checklist in Part 8 | All "must" items |
| Code review feedback addressed | Yes |
| Can explain a request/response cycle at a whiteboard | Yes |

### Gate 2 (end of week 9): "Can build a secure API and connect two systems"

| Criterion | Pass mark |
|---|---|
| Quiz on Levels 5 to 7 | ≥ 80% |
| Projects 2, 3, 4 | Reviewed and merged |
| OWASP API Top 10 oral check | Explain each risk and our control |
| No secret found in any repository | Zero |

### Gate 3 (end of week 13): "Ready for supervised commercial projects"

| Criterion | Pass mark |
|---|---|
| Quiz on Levels 8 to 9 | ≥ 80% |
| Projects 5 to 9 | Reviewed and merged |
| Capstone (Project 10) | Demonstrated live; failure scenarios shown |
| Panel (Architect + Sr. Engineer + QA) | Recommends "supervised commercial project" |
| Communication check | Writes a client-ready technical summary |

**Outcome of Gate 3:** *Junior Integration Engineer, supervised.* Works on a live project with a named senior reviewer, no production access, and weekly review.

## 6.4 Assessment rubric (every project)

| Dimension | Weight | What "excellent" looks like |
|---|---|---|
| Correctness | 25% | All requirements work; edge cases handled |
| Failure handling | 20% | Retries, duplicates, timeouts, invalid data tested |
| Security | 20% | No secrets in Git; auth enforced; input validated |
| Tests | 15% | Automated, meaningful, run in CI |
| Documentation | 10% | A stranger can run it from the README |
| Code quality | 10% | Layered, readable, small functions, typed |

Score each 0 to 4. **Pass** needs ≥ 70% overall **and** no zero in Security or Failure handling.

## 6.5 What "supervised commercial project" means

You may: implement tickets, write tests and docs, join client calls as an observer, work in staging with sandbox credentials.
You may not: deploy to production, hold production credentials, commit to client dates, or change scope.
Every pull request is reviewed by your named senior. After two clean projects you are considered for promotion to Integration Engineer (section 2.3).
