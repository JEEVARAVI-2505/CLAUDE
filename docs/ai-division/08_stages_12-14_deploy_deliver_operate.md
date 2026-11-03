# STAGES 12–14 — Deployment, Customer Delivery, Operations

---
# STAGE 12 — Production Deployment

**Owner:** DevOps. **Precondition:** Stage 10 report = GO.

## 12.1 Production checklist (each line has an owner and evidence link)

| Area | Requirement |
|---|---|
| Hosting | Managed container platform or Kubernetes in one region near customers; infrastructure as code in `infra/` (no click-ops) |
| Database | Managed PostgreSQL (+pgvector), encryption at rest, private network, automated backups + point-in-time recovery, restore tested |
| Vector storage | pgvector in same DB until measured need; backups included |
| Auth | OIDC provider (hosted IdP or open-source), MFA for staff/admin, SSO for Enterprise |
| Secrets | Cloud secret manager; rotation schedule; no secrets in env files on servers or images |
| Model providers | Production keys separate from dev; spend caps and alert emails; fallback provider configured |
| Usage & rate limits | Per-user, per-tenant, per-IP; token budgets; max file size; queue limits |
| Monitoring | Metrics (latency, errors, queue depth, token/cost), uptime probe, dashboards |
| Logging | Structured JSON, correlation IDs, **PII redaction**, retention policy; no prompts/documents in debug logs |
| Error tracking | e.g. Sentry-type tool; scrub personal data |
| Backups | Daily DB, object-storage versioning, config in Git; monthly **restore drill** |
| CI/CD | Build → test → image scan → deploy staging → smoke + eval → manual approval → prod (blue/green or canary) |
| Domain / DNS / TLS | Registrar with MFA; DNS records in IaC; TLS 1.2+ auto-renew; HSTS; SPF/DKIM/DMARC for email |
| Environments | dev / staging / prod separated by account, keys and data |
| Spend alerts | Cloud billing alerts + provider caps at 50/80/100% of monthly budget |

## 12.2 Release procedure

1. Tag `vX.Y.Z`; release notes list code, prompt, model, index and schema versions.
2. Deploy to staging; run smoke tests + eval subset + security scan.
3. Release manager approves (second person).
4. Canary 5% traffic for 30–60 min; watch error rate, latency, cost/answer, thumbs-down rate.
5. Promote to 100% or **roll back** (previous image + previous prompt pointer + previous model pins).
6. Post-release check at +1 h and +24 h.

## 12.3 Rollback & disaster recovery

* **Rollback** is one command/button and is rehearsed monthly. Database migrations are backward-compatible (expand → migrate → contract) so rollback never needs data loss.
* **DR objectives (set per plan):** RPO (max data loss) e.g. 15 min; RTO (max downtime) e.g. 4 h Pro / 1 h Enterprise.
* **DR steps:** declare incident → incident commander → restore DB to point-in-time in clean environment → redeploy from IaC → re-point DNS → verify with smoke tests → customer comms → post-mortem within 5 days (blameless).
* Provider outage: gateway switches to fallback model/provider; if both down, show "AI temporarily unavailable" with human contact.

## 12.4 Stage 12 gate

- [ ] Checklist in 12.1 100% evidenced
- [ ] Restore drill and rollback drill done in staging
- [ ] Alerts tested by triggering them
- [ ] Pen-test criticals closed; privacy notice live

---
# STAGE 13 — Customer Delivery & Commercialisation

**Owner:** PM + Customer Success. **Output:** the deliverables in §13.1.

## 13.1 Deliverables

| Item | Contents |
|---|---|
| Product demo | 10-min scripted demo on **synthetic** data: ask, cite, "I don't know", hand-off, admin view |
| Use-case demonstration | Customer's own sample docs (after NDA) showing before/after time saved |
| Customer onboarding plan | 3-week plan (§13.2) |
| Knowledge-base ingestion | Checklist: owners, document inventory, permission mapping, clean-up, ingest, verify |
| User manual | Task-based: ask questions, read citations, give feedback, escalate |
| Admin manual | Users/roles, upload/replace/delete docs, usage dashboard, budgets, audit logs |
| Subscription proposal | Plan, price, limits, onboarding, assumptions, success metrics |
| Commercial agreement | MSA + order form + usage limits + SLA (**lawyer reviews templates**) |
| Privacy documentation | Privacy notice, DPA, sub-processor list (model providers, cloud), data-flow, retention & deletion, security summary |
| Support agreement | Hours, channels, severity levels, response/resolve targets, escalation path |
| Customer training | 60-min live + recorded video + one-page cheat sheet |

## 13.2 Customer onboarding timeline

| Week | Activities | Output |
|---|---|---|
| 0 (pre-sale) | NDA, discovery call, success metrics, data inventory, security questionnaire | Signed order form |
| 1 | Create tenant, SSO/users, collect documents, define access groups, ingest | Knowledge base v1 |
| 2 | Pilot with 5–10 users; customer-specific test questions (≥ 50) scored | Pilot report; tune retrieval/prompts via change process |
| 3 | Training, go-live, hyper-care daily check-ins | Go-live sign-off |
| 4–8 | Weekly usage review, then monthly business review | Success metrics report |

## 13.3 Honest disclosure (must appear in proposal, manual, and in-product)

1. AI can make mistakes; answers show sources; users must verify important decisions.
2. Quality depends on the customer's documents (clean, current, permissioned).
3. Human review/hand-off exists; list which actions need approval.
4. Usage limits, fair-use policy and overage prices.
5. Data handling: what is stored, where, who can see it, retention, deletion on request, model-provider processing and whether provider retains/trains (per verified terms).
6. Measured accuracy figures from our evaluation with date and method — never "100% accurate" or "fully autonomous".
7. Not a substitute for legal/medical/financial/engineering professional judgement.

## 13.4 Stage 13 gate

- [ ] Demo rehearsed; every claim backed by an evaluation number
- [ ] Contracts/privacy docs reviewed by counsel
- [ ] Pilot success criteria agreed in writing
- [ ] Support rota and escalation contacts live

---
# STAGE 14 — Maintenance & AI Operations (continuous)

## 14.1 What we monitor (dashboard + alert)

| Signal | Source | Alert if |
|---|---|---|
| Availability, error rate | Uptime + app logs | Error rate > 2% for 5 min |
| Latency p95 | Traces | > SLO for 10 min |
| Token consumption / cost per task | Cost meter | > 120% of budget run-rate |
| Model availability / provider errors | Gateway | Fallback rate > 5% |
| Hallucination / groundedness (sampled) | Nightly eval on production samples + human review | Below release threshold |
| Retrieval quality | Nightly recall on labelled set; "no result" rate | Drop > 3 pts |
| Tool errors / failed runs | Orchestrator traces | > 3% |
| User feedback | Thumbs, tickets | Thumbs-down > 15% weekly |
| Security | Auth failures, injection detections, rate-limit hits | Spike vs baseline |
| Backups | Job status | Any failure |

## 14.2 Calendar

| Cadence | Activity | Owner |
|---|---|---|
| Daily | Dashboard check, error queue, spend vs cap | AI Ops on duty |
| Weekly | Eval review, top failing questions → backlog, prompt/retrieval fixes through change process | AI Eng + QA |
| Monthly | Cost review (actual vs model), backup restore drill, dependency updates, access review, customer business reviews | PM, DevOps, Security |
| Quarterly | Model re-evaluation vs newer/cheaper models, red-team, DR exercise, pricing review, privacy review | Architect, Security, PM |
| Annually | Pen-test, policy refresh, vendor/licence review, SOC 2 / ISO 27001 readiness review (if pursuing) | Head of AI Eng |

## 14.3 Incident handling

Severity: **S1** outage/data exposure (15-min response), **S2** major degradation, **S3** minor, **S4** request. Roles: incident commander, communications, fixer, scribe. Data exposure → follow breach-notification duties under applicable law (check with counsel; some regimes set notification windows as short as 72 hours).

## 14.4 Model replacement rule

New model/provider or version bump → run golden + adversarial + cost benchmark vs current → canary in staging → release only if all thresholds hold and cost per successful task is equal or better. Update `model_registry.yaml`, ADR, price and licence verification date, customer sub-processor list if provider changed (notify customers per contract).

## 14.5 Change management

Normal change (PR + CI + review), standard change (pre-approved runbook, e.g. rotate key), emergency change (hotfix with retrospective review within 24 h). Every production change is recorded in `09-Operations/change-log`.

## 14.6 Stage 14 gate (recurring)

- [ ] Monthly review minutes filed
- [ ] No overdue critical dependency updates
- [ ] Latest restore drill < 30 days old
- [ ] Latest model benchmark < 90 days old
