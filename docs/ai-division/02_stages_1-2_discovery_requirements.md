# STAGES 1–2 — Market Research, then Requirements

---
# STAGE 1 — Market Research & Product Discovery

**Goal:** decide *which* AI product to build, using evidence, not opinion.
**Output:** `AI Product Research and Opportunity Report` (template: `templates/research_report.md`).
**Owner:** PM. **Duration:** 2 weeks.

## 1.1 Where to look (all 16 opportunity areas)

Score each area on the matrix in §1.3. Starting hypotheses (verify, do not trust):

| Area | Typical buyer | Typical pain | Our difficulty |
|---|---|---|---|
| Customer-support AI | Support head | Repetitive tickets, slow replies | Medium |
| Enterprise knowledge / RAG | Ops / IT head | "Where is that document?" | Medium |
| Document intelligence | Finance, legal, admin | Manual data entry from PDFs/invoices | Medium-High (OCR) |
| Research assistants | Consultants, analysts | Hours of reading | Medium |
| Business analyst / data analysis (NL→SQL) | Managers | Waiting for reports | High (safety) |
| Financial reporting assistance | Finance team | Month-end narrative drafting | High (accuracy) |
| Sales assistants | Sales head | Lead research, follow-ups | Medium |
| Recruitment assistance | HR | CV screening volume | High (fairness/legal) |
| Voice agents | Call-centres, clinics | Missed calls, after-hours | High (latency, consent) |
| Coding assistants | Dev teams | Boilerplate, review | Very high (crowded) |
| Workflow agents | Operations | Multi-step admin tasks | High (permissions) |
| Recommendation engines | E-commerce | Conversion | Medium (needs data) |
| Industry-specific assistants | Vertical niches | Domain jargon | Medium (domain expert needed) |
| Private AI assistants | Regulated firms | Data cannot leave premises | High (infra) |

## 1.2 Research procedure (step by step)

1. **List 15–25 competitors** per shortlisted area (search engines, G2/Capterra-type review sites, Product Hunt, app marketplaces, GitHub for open-source).
2. **Fill the competitor sheet** per product: capabilities, pricing (plans + usage limits), model providers used, target customers, security claims (SOC 2? data residency?), deployment options (SaaS / private cloud / on-prem), integrations, customer reviews (recurring praise and complaints), limitations.
3. **Record the source URL and access date** in every cell. No source → mark "unverified".
4. **Interview 15+ target customers** (30 min each, script in `templates/interview_script.md`). Ask about the *last time* the problem happened, current workaround, what they pay (time + money), who signs the cheque. **Do not pitch.**
5. **Willingness to pay (WTP):** use Van Westendorp 4-question price survey or a "pre-order/pilot LOI" ask. A signed letter of intent beats 100 "sounds great" comments.
6. **Size the market (§1.4).**
7. **Choose** using the scoring matrix. Write a one-page recommendation + risks.

## 1.3 Scoring matrix (weights sum to 100)

| Criterion | Weight | 1 (bad) … 5 (great) |
|---|---|---|
| Pain severity (evidence from interviews) | 25 | |
| Willingness to pay (LOIs, price tests) | 20 | |
| Reachability of buyers (can we get meetings?) | 10 | |
| Competitive gap | 10 | |
| Technical feasibility with current models | 15 | |
| Risk (legal, safety, accuracy) — inverted | 10 | |
| Time-to-first-revenue | 10 | |

Score = Σ(weight × score)/5. Build the top-scoring product only if ≥ 70 **and** ≥ 3 interviewees would pilot.

## 1.4 TAM / SAM / SOM — transparent method

```
TAM = (# target companies worldwide in segment) × (annual price per company)
SAM = TAM × (% in countries/languages/industries we can serve with our channels)
SOM = SAM × (% we can realistically win in 3 years given sales capacity)
```

Reproducible rules: every input has a source and date; show low / base / high cases; **SOM must be bottom-up too**:
`SOM customers = (sales people × qualified demos/month × close rate × 36 months) − churn`.

Worked illustration (ASSUMPTIONS ONLY — replace with sourced numbers):

| Input | Low | Base | High |
|---|---|---|---|
| SMEs in target countries in segment | 200,000 | 400,000 | 600,000 |
| Share that would buy such a tool | 3% | 5% | 8% |
| Avg annual price (Professional plan = $399 × 12) | $4,788 | $4,788 | $4,788 |
| **SAM** | $28.7 M | $95.8 M | $229.8 M |
| Bottom-up customers in 3 yrs (2 reps × 12 demos × 20% × 36, less churn) | 120 | 170 | 250 |
| **SOM (ARR at year 3)** | $0.57 M | $0.81 M | $1.2 M |

Lesson: SOM from bottom-up (≈ $0.8 M) is the number you plan with; TAM is only for investors.

## 1.5 Stage 1 gate

- [ ] ≥ 15 competitors profiled with sources + dates
- [ ] ≥ 15 customer interviews logged
- [ ] ≥ 3 pilot commitments (LOI or paid pilot)
- [ ] TAM/SAM/SOM sheet with low/base/high and sources
- [ ] Recommendation signed by Head of AI Eng and Finance

**Common mistakes:** trusting vendor blogs for prices; counting "everyone with a website" as TAM; interviewing friends.

---
# STAGE 2 — Customer Requirements & Product Definition

**Goal:** a frozen, testable PRD. **Owner:** PM. **Duration:** 1–2 weeks.
**Output:** `AI Product Requirements Document` using `templates/prd.md`.

## 2.1 The 17 mandatory sections of the PRD

| # | Section | What to write |
|---|---|---|
| 1 | Target user | Persona: job, skill level, device, language |
| 2 | Business problem | One sentence + measured baseline (e.g. "40 tickets/day, 6 min each") |
| 3 | AI use case | Why AI, why not a rule or search box? |
| 4 | User journeys | Step-by-step, including failure and hand-off paths |
| 5 | System inputs | Types, size limits, languages, allowed file types |
| 6 | Expected outputs | Format, length, citation style, schema |
| 7 | Knowledge sources | Which documents/systems, owners, update frequency |
| 8 | AI capabilities | Q&A, summarise, extract, classify … only what is needed |
| 9 | Available tools | Whitelist with purpose, e.g. `search_kb`, `create_ticket_draft` |
| 10 | User permissions | Roles (admin/editor/viewer), document-level access |
| 11 | Human approval | Table in §2.2 |
| 12 | Data privacy | Personal data categories, residency, retention, deletion |
| 13 | Performance | p95 latency, availability target, concurrency |
| 14 | Accuracy | Numeric release thresholds (see Stage 10) |
| 15 | Cost limits | Max cost per conversation, per tenant per month |
| 16 | Usage limits | Requests/min, tokens/day, file size, storage |
| 17 | Integrations | Email, WhatsApp, CRM, SSO, webhooks |

## 2.2 Autonomy matrix (decide *per action*)

| Risk class | Examples | Rule |
|---|---|---|
| **A — Read-only, low impact** | Search KB, summarise a doc, answer FAQ with citation | Autonomous |
| **B — Draft, reversible** | Draft reply, prepare ticket, fill a form (unsent) | Autonomous *draft*; human sends |
| **C — Writes internal data** | Update CRM field, create calendar event | Human approval, or autonomous only with allow-list + undo + audit log |
| **D — External/irreversible/financial** | Send email to customer, payment, delete data, change permissions | **Always human approval**; never in v1 unless justified in an ADR |

Never give any agent: unrestricted shell, raw SQL write, payment execution, admin API, or the ability to modify its own permissions.

## 2.3 Requirement writing rules

* ID every requirement: `FR-001` functional, `NFR-001` non-functional, `SEC-001` security.
* Use **testable** language: "p95 first-token latency ≤ 3 s at 50 concurrent users" ✔ / "fast" ✘.
* Each requirement → at least one test case ID.
* Out-of-scope list is mandatory (prevents scope creep).

## 2.4 Input/Output specification (example, JSON Schema style)

```json
// Input
{ "tenant_id": "uuid", "user_id": "uuid", "conversation_id": "uuid",
  "message": "string (1..4000 chars)", "language": "en|hi|ta|auto", "attachments": [] }
// Output
{ "answer": "string", "citations": [{"doc_id":"uuid","version":3,"page":4,"quote":"string<=300"}],
  "confidence": "high|medium|low", "needs_human": false, "missing_info": ["string"],
  "disclaimer": "AI-generated — verify important information." }
```

## 2.5 Scope freeze

1. PM presents PRD in a 60-minute review (Architect, QA, Security, Finance must attend).
2. List of open questions closed or assigned.
3. Signatures (PM, Architect, QA, Security, Customer sponsor).
4. Tag the document `APPROVED v1.0`. After this, changes need a **Change Request** (impact on cost, time, risk) approved by PM + Head of AI Eng.

## 2.6 Stage 2 gate

- [ ] All 17 sections filled; out-of-scope list present
- [ ] Autonomy matrix complete; no class-D action autonomous
- [ ] Numeric accuracy/latency/cost targets set
- [ ] Privacy review done (data categories, retention, deletion)
- [ ] Requirements traceable to test IDs
- [ ] Signatures collected; scope frozen
