# STAGE 15 — Internal AI Engineering Management Portal

**Goal:** one internal web app where staff manage research, models, prompts, agents, evaluation, cost and customers.
**Build order:** do NOT build everything at once. Phase the portal; start with spreadsheets/Notion + the cost script, build the portal when manual work hurts.

## 15.1 Modules (all 20) and their data

| # | Module | Core data / screens | Phase |
|---|---|---|---|
| 1 | AI Product Research | Competitors, sources, scoring matrix, TAM/SAM/SOM | 3 |
| 2 | Customer Requirements | PRDs, requirement IDs, change requests | 2 |
| 3 | AI Model Registry | Provider, version, price, limits, licence, data terms, verified date | 1 |
| 4 | Model Benchmarking | Runs, per-model metrics, cost per successful task | 2 |
| 5 | Prompt Library | Prompt versions, approval status, eval links, release pointer | 1 |
| 6 | Agent Registry | Agent specs, versions, tools, budgets | 1 |
| 7 | Agent Workflow Designer | Visual graph editor for workflows (read-only viewer first) | 3 |
| 8 | Tool Registry | Tool schemas, risk class, roles, rate limits | 1 |
| 9 | RAG Document Management | Documents, versions, ingestion status, deletion requests | 2 |
| 10 | Knowledge Base Management | KB per tenant, access groups, freshness | 2 |
| 11 | AI Evaluation | Datasets, runs, trends, release gate status | 1 |
| 12 | Security Testing | Test suites, findings, retest status | 2 |
| 13 | Token Usage Tracking | Per tenant/model/agent/day | 1 |
| 14 | AI Cost Calculator | Web version of `cost_model.py` with editable assumptions | 2 |
| 15 | Infrastructure Cost Calculator | Tiers, scaling triggers | 3 |
| 16 | Subscription Pricing | Plans, limits, overages | 2 |
| 17 | Customer Management | Tenants, contracts, onboarding stage, health | 2 |
| 18 | Deployment Management | Versions per env, release/rollback log | 2 |
| 19 | Maintenance | Reviews calendar, incidents, tasks | 3 |
| 20 | Financial Analytics | Revenue, COGS, margin, CAC/LTV, break-even | 3 |

Phase 1 (weeks 1–6): modules 3, 5, 6, 8, 11, 13. Phase 2: 2, 4, 9, 10, 12, 14, 16, 17, 18. Phase 3: rest.

## 15.2 Dashboard must show

Actual token usage; estimated vs actual cost; cost per task; model performance (quality, latency, error); revenue per customer; gross margin per tenant; alerts (budget, quality drop, expiring price verification).

## 15.3 Access control and tenant separation

| Role | Can do |
|---|---|
| Super admin (Head of AI Eng) | Everything incl. role changes; MFA mandatory |
| Product | Research, requirements, pricing (read costs) |
| Engineer | Registries, prompts (draft), agents, evaluation; no customer data |
| QA | Evaluation datasets & results; approve/reject releases |
| Security | Security tests, audit logs, access reviews |
| Finance | Cost, pricing, analytics; no prompt/doc content |
| Support | Customer view, tickets, masked conversations only with customer-consented "support access" |
| Read-only | Dashboards |

* `tenant_id` on every customer-related row; PostgreSQL Row-Level Security; staff access to a tenant's data requires a logged, time-limited "break-glass" grant.
* All actions audit-logged (who, what, when, before/after). Logs are append-only.

## 15.4 Tech suggestion

FastAPI + PostgreSQL + a React admin UI; reuse the same `src/` packages (registries are DB tables with YAML export to Git). Auth via company IdP with MFA. Same CI/CD as the product.

## 15.5 Gate

- [ ] Phase 1 modules live and used weekly
- [ ] RBAC and tenant isolation tests pass
- [ ] Dashboard figures reconcile to provider invoices within 5%
