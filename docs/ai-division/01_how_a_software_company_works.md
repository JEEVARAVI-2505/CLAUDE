# STAGE 0 — How a Software Company Works (and how we set ours up)

**Goal:** teach a beginner the corporate environment: roles, workflow, environments, folders, meetings, quality rules.
**Gate to Stage 1:** every box in §10 is ticked.

---

## 1. Roles and who owns what

| Role | Owns | Beginner-level meaning |
|---|---|---|
| Head of AI Engineering | Delivery, hiring, standards | Final technical + schedule accountability |
| Product Manager (PM) | *What* and *why*: PRD, priorities, pricing | Talks to customers, writes requirements |
| AI Architect | *How*: architecture, model choice, agent design | Draws the system, writes ADRs |
| AI / LLM Engineer | Agents, prompts, RAG, evaluation code | Builds the AI parts |
| Backend Engineer | API, database, auth, billing | Builds FastAPI services |
| Frontend Engineer | Web UI | Builds React/Next.js screens |
| QA / Evaluation Engineer | Test plans, golden sets, release gate | Tries to break things, measures quality |
| Security Engineer (part-time) | Threat model, pen-tests, secrets | Approves security gate |
| DevOps / SRE (part-time) | CI/CD, cloud, monitoring, backups | Keeps it running |
| Support / Customer Success | Onboarding, tickets, training | Customer's human contact |
| Finance (part-time) | Costing, invoicing, tax | Verifies unit economics |

**RACI rule:** every deliverable has exactly one **A**ccountable person, plus **R**esponsible, **C**onsulted, **I**nformed.
Example: PRD → A: PM, R: PM, C: Architect+QA, I: Everyone.

## 2. The delivery lifecycle (what "stage by stage" means)

```
Idea → Research → Requirements → Design → Build → Test → Release → Operate → Improve
         ↑___________________ feedback from customers & metrics ____________________|
```

Each arrow is a **gate**: a meeting + checklist where a named approver signs "go / no-go".

## 3. Daily and weekly rhythm

| Ritual | When | Length | Purpose |
|---|---|---|---|
| Stand-up | Daily | 15 min | Yesterday / today / blocked (no problem-solving) |
| Sprint planning | Every 2 weeks, Monday | 1.5 h | Pick tickets that fit capacity |
| Backlog grooming | Weekly | 1 h | Make tickets clear: *acceptance criteria* |
| Demo | Sprint end, Friday | 45 min | Show working software to PM + stakeholders |
| Retrospective | After demo | 45 min | One thing to start, stop, continue |
| Eval review | Weekly | 30 min | Look at quality, cost, failures dashboards |
| Cost review | Monthly | 30 min | Actual vs budgeted spend |

## 4. Tickets — how work is tracked

Use GitHub Issues + Projects (free). Every ticket must have:

1. **Title** (verb first: "Add citation check to answer validator").
2. **Why** (link to PRD requirement ID, e.g. `FR-12`).
3. **Acceptance criteria** (testable bullets).
4. **Estimate** (S ≤ ½ day, M ≤ 2 days, L ≤ 5 days; bigger → split).
5. **Owner**, **sprint**, **labels** (`agent`, `rag`, `security`, `bug`, `docs`).

**Definition of Ready:** points 1–4 filled. **Definition of Done:** see §8.

## 5. Git and GitHub workflow (step by step)

```
main ← protected (nobody pushes directly)
 └─ feature/FR-12-citation-check   ← you work here
```

1. `git switch main && git pull`
2. `git switch -c feature/FR-12-citation-check`
3. Write a **failing test** first, then code until it passes.
4. `git add -p` (review your own changes) → `git commit -m "rag: validate citations against retrieved chunks (FR-12)"`
5. `git push -u origin feature/FR-12-citation-check`
6. Open a **Pull Request (PR)**: fill the template (what, why, how tested, screenshots, risk).
7. CI runs automatically: lint, type-check, unit tests, secret scan, dependency scan, eval smoke-test.
8. A teammate reviews. Fix comments. **Never** merge your own PR.
9. Squash-merge. Branch auto-deleted.

**Branch protection settings on `main`:** require PR, ≥1 approval, CI green, up-to-date branch, no force-push, signed commits recommended, CODEOWNERS for `security/`, `infra/`, `prompts/`.

**Commit message format:** `<area>: <what> (<ticket>)` e.g. `agents: cap retries at 2 (FR-31)`.

## 6. Environments

| Env | Purpose | Data | Who deploys | Models/limits |
|---|---|---|---|---|
| **local** | Your laptop | Fake/sample only | You | Cheapest model, low budget |
| **dev** | Shared integration | Synthetic | CI on merge to `main` | Cheap model |
| **staging** | Exact copy of prod config | Anonymised test data | CI after dev passes | Same as prod, tiny budget |
| **prod** | Customers | Real | Release manager, manual approval | Real limits, alerts on |

Rules: separate cloud accounts/projects and separate API keys per environment; **real customer data never enters local/dev/staging**; prod logs mask personal data.

## 7. Company folder structure (documents, not code)

```
Company-Drive/
├─ 00-Governance/            policies, org chart, RACI, approvals, ADR index
├─ 01-Market-Research/       one folder per product; sources, TAM/SAM/SOM sheets
├─ 02-Product/               PRDs, user journeys, scope freezes, roadmaps
├─ 03-Architecture/          diagrams, ADRs, threat models, data-flow maps
├─ 04-Models-and-Prompts/    benchmark reports, prompt release notes
├─ 05-Quality/               golden datasets (access-controlled), eval + security reports
├─ 06-Finance/               cost models, pricing, invoices, budgets, spend alerts
├─ 07-Legal/                 templates: MSA, DPA, privacy notice, support SLA, NDAs
├─ 08-Customers/<tenant>/    proposals, contracts, onboarding logs, training material
├─ 09-Operations/            runbooks, incident reports, DR tests, change log
└─ 10-HR-Training/           onboarding checklists, learning paths, SOP sign-offs
```

Naming: `YYYY-MM-DD_topic_vN_owner.ext` e.g. `2026-11-03_prd_knowledgedesk_v1_pm.md`. Only the latest *approved* version is marked `APPROVED`.

## 8. Definition of Done (applies to every ticket)

- [ ] Acceptance criteria met and demonstrated
- [ ] Unit tests written first, passing; coverage not reduced
- [ ] If AI behaviour changed: eval run attached to PR, no regression beyond threshold
- [ ] Inputs/outputs validated with schemas
- [ ] No secrets, no customer data, no debug prints of prompts with personal data
- [ ] Logging/tracing added; errors have clear messages
- [ ] Docs updated (README / docs/ / prompt changelog)
- [ ] Peer review approved; CI green
- [ ] Security checklist ticked if touching auth, tools, files, or prompts

## 9. Coding standards (Python)

* Python ≥ 3.11 (verify the current supported version at python.org), type hints everywhere, `ruff` (lint+format), `mypy --strict` on `src/`.
* Pydantic v2 models for every schema (inputs, outputs, tool args, config).
* Functions ≤ 40 lines, files ≤ 400 lines, one responsibility each; public functions have docstrings.
* No global state; pass dependencies in (makes tests easy).
* Randomness takes a `seed`. Time comes from an injectable clock.
* Config from environment variables via `pydantic-settings`; never hard-code keys.
* Tests: `pytest`; folders `tests/unit`, `integration`, `agent`, `rag`, `security`.

## 10. Stage 0 gate checklist

- [ ] GitHub organisation created (private repos by default, 2FA enforced for everyone)
- [ ] Password manager + secret manager chosen; no keys in chat or email
- [ ] Company Drive structure created (§7) with access groups
- [ ] Roles assigned with RACI for the first product
- [ ] Laptop standard installed (see Stage 7) and each person has done "hello world" PR end-to-end
- [ ] Company intake table in `00_START_HERE.md` §5 filled with real values
- [ ] Budget owner and monthly spend-alert contacts named
- [ ] Employee has read the 10 golden rules and signed the acceptable-use + data-handling policy

## Common mistakes

* Starting to code before the PRD is frozen → endless rework.
* "It works on my machine" → always run through CI.
* Sharing an API key in WhatsApp → rotate immediately, tell Security.
* Testing with real customer files on a laptop → incident; use synthetic data.
