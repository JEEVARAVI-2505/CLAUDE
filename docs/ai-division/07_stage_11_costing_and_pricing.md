# STAGE 11 — Complete Costing, Production Models and SaaS Pricing

**Owner:** PM + Finance. **Output:** `AI Product Costing and Subscription Pricing Model`.
**Reproduce every number:** `python3 docs/ai-division/tools/cost_model.py` (edit `ASSUMPTIONS` at the top, re-run).
**WARNING:** model prices in the script are *placeholders* (`price_checked_on: NOT YET VERIFIED`). Before quoting a customer: open each provider's official pricing page, copy the numbers, write the date.

## 11.1 Cost families (your checklist)

| Family | Items | Formula |
|---|---|---|
| **A. One-time development** | Research, requirements, architecture, UI/UX, backend, frontend, agents, prompts, RAG, data prep, integrations, evaluation, testing, security, deployment, docs | `Hours × Fully-loaded hourly cost`, per role |
| **B. AI model (monthly)** | Input, output, cached tokens, embeddings, reranking, image/audio, retries, fallback | `Σ over every model operation: units × unit price` |
| **C. Infrastructure (monthly)** | Hosting, DB, vector DB, storage, bandwidth, Redis, jobs, monitoring, logs, backups, security tooling, maintenance | Quotes from cloud calculators + 20% contingency |
| **D. Third-party (monthly)** | OCR, speech, email, WhatsApp, external APIs, payment gateway, data providers, monitoring SaaS | Price sheets; WhatsApp is per-conversation/message — check Meta's current rate card |
| **E. People for operating** | Support, AI-ops, human QA sampling | Hours × rate |
| **F. Sales & marketing** | CAC | Spend ÷ new customers |

**Fully-loaded hourly cost** = (salary + benefits + taxes + equipment + software + office share + management overhead) ÷ productive hours (≈ 1,600–1,800/yr, not 2,080).

## 11.2 A — One-time development cost (reference product, from the script)

| Role | $/h loaded | Hours | Cost |
|---|---|---|---|
| Lead / AI Architect | 30 | 600 | 18,000 |
| AI / LLM Engineers ×2 | 22 | 1,800 | 39,600 |
| Backend Engineer | 20 | 700 | 14,000 |
| Frontend Engineer | 18 | 500 | 9,000 |
| Product Manager | 22 | 450 | 9,900 |
| QA / Evaluation Engineer | 14 | 450 | 6,300 |
| DevOps (part-time) | 22 | 200 | 4,400 |
| Security (part-time) | 28 | 120 | 3,360 |
| Support / Docs | 10 | 120 | 1,200 |
| **Total** | | **4,940 h** | **$105,760** |

Rates are assumptions (India-based team); replace with your payroll data. Add 15–20% contingency in the budget request.

## 11.3 B — AI model cost: how it is calculated

Per answered question:

```
blended input price  = Σ(mix_share × model_input_price)      mix: 70% economical / 25% balanced / 5% high
effective input price = blended × (1 − cached_share + cached_share × cached_factor)
LLM cost = calls_per_answer × (tokens_in × eff_in_price + tokens_out × blended_out_price) / 1,000,000
```

Assumptions: 3,000 input + 400 output tokens per call, 1.3 calls/answer (retries/fallback/validator), 30% of input cached at 10% price.

| Component (per answer) | USD |
|---|---|
| LLM calls (routed mix) | 0.0027 |
| Reranker (≈ $2 / 1,000 searches) | 0.0020 |
| Human QA sampling (1% of answers × 3 min × $12/h) | 0.0060 |
| **Total variable cost per answer** | **≈ 0.0107** |

Insight: **our own quality-assurance labour is the biggest cost line, not the LLM.** That is typical early on; automate gradually while keeping the human sample.
Ingestion + storage per user per month ≈ $0.004 (20 docs × 6,000 tokens embedded once; 2 MB/doc at $0.10/GB-month).

## 11.4 E — Usage scenarios (monthly; from the script)

Assumptions: 60 answers per user per month; peak concurrency = 5% of users; infra tier switches at 500 / 5,000 / 100,000 users.

| Users | Answers/mo | Peak conc. | AI (LLM+rerank) | Human QA | Ingest | Infra | **Total** | **$/user** | Production model |
|---|---|---|---|---|---|---|---|---|---|
| 100 | 6,000 | 5 | 28 | 36 | 0 | 180 | **245** | 2.45 | Lean MVP |
| 500 | 30,000 | 25 | 141 | 180 | 2 | 180 | **503** | 1.01 | Lean MVP |
| 1,000 | 60,000 | 50 | 282 | 360 | 4 | 1,100 | **1,746** | 1.75 | Professional |
| 5,000 | 300,000 | 250 | 1,409 | 1,800 | 21 | 1,100 | **4,329** | 0.87 | Professional |
| 10,000 | 600,000 | 500 | 2,818 | 3,600 | 41 | 7,500 | **13,959** | 1.40 | Enterprise |
| 25,000 | 1,500,000 | 1,250 | 7,044 | 9,000 | 103 | 7,500 | **23,647** | 0.95 | Enterprise |
| 50,000 | 3,000,000 | 2,500 | 14,088 | 18,000 | 205 | 7,500 | **39,794** | 0.80 | Enterprise |
| 100,000 | 6,000,000 | 5,000 | 28,177 | 36,000 | 411 | 7,500 | **72,087** | 0.72 | Enterprise |

Step jumps (e.g. 5,000 → 10,000 users) are real: you buy infrastructure in tiers. Per scenario you must also write down: requests/user, tokens/request, model calls, retrieval operations, storage, concurrency, human review (script columns cover these; extend the script when your assumptions change). Beyond ~50,000 users infra at the "Enterprise" tier must be re-quoted — treat rows ≥ 25,000 as estimates ±40%.

## 11.5 F — Three production models

| | **Lean MVP** | **Professional** | **Enterprise** |
|---|---|---|---|
| Users supported | ≤ 500 | ≤ 5,000 | ≤ 100,000 (re-quote) |
| Setup (one-time) | $3,000 | $12,000 | $60,000 |
| Monthly infra | $180 | $1,100 | $7,500 |
| Annual infra | $2,160 | $13,200 | $90,000 |
| Architecture | 1 app container + managed Postgres(pgvector) + small Redis + object storage; single region | 2+ app replicas behind load balancer, HA Postgres + read replica, worker pool, managed monitoring, WAF | Multi-AZ, autoscaling, dedicated vector engine if measured need, SSO/SCIM, private networking, DR region |
| SLA you may promise | Best effort (99.0%) | 99.5% | 99.9% (contract credits) |
| **Scale-up triggers** | p95 latency > 3 s for 3 days; CPU > 70% sustained; DB > 70% storage; > 400 users | Same + vector query p95 > 300 ms; queue wait > 60 s; > 4,000 users | Customer demands dedicated tenancy / region |

## 11.6 G — SaaS plans (from the script)

| | Starter | Professional | Business | Enterprise |
|---|---|---|---|---|
| Subscription | $99/mo | $399/mo | $1,199/mo | from $3,500/mo (custom) |
| Included users | 5 | 25 | 100 | negotiated |
| AI answers included ("AI credits") | 1,500 | 10,000 | 50,000 | negotiated pool |
| Storage | 2 GB | 20 GB | 100 GB | negotiated |
| API access | – | 60 req/min | 300 req/min | custom |
| Support | Email, 2 business days | Email, 1 business day | Priority 8 h, onboarding call | Named contact, SLA |
| Overage | $0.05/answer | $0.04/answer | $0.03/answer | contract |
| Abuse control | Per-user daily cap, per-tenant monthly budget, file-size & rate limits, auto-pause at 120% usage with notice |

Annual prepay: 2 months free. **Never advertise "unlimited"**; use fair-use caps stated in the contract.

### Gross margin per plan at full included usage

| Plan | Price | COGS | Gross profit | GM % |
|---|---|---|---|---|
| Starter | $99 | $19.03 | $79.97 | 80.8% |
| Professional | $399 | $119.03 | $279.97 | 70.2% |
| Business | $1,199 | $571.19 | $627.81 | 52.4% |

COGS includes AI, reranker, human QA sample, ingestion/storage, 3% payment fee; **excludes shared infra and staff** (those are in opex below). Business margin is lowest because the heavy-use plan has a fixed price — raise its price, lower its allowance, or add more caching before selling it widely.

## 11.7 Unit metrics — definitions and values

| Metric | Formula | Value (Professional plan, assumptions) |
|---|---|---|
| Cost per active user | Total monthly cost ÷ active users | $0.72–$2.45 (table §11.4) |
| Cost per successful task | Total cost ÷ tasks passing evaluation | ≈ $0.0107 ÷ success rate 0.85 ≈ **$0.0126** |
| Cost per document | Embedding + OCR + storage ≈ $0.0002 embed + OCR (per page, provider price) | add OCR price |
| Cost per conversation | answers/conversation (≈ 4) × cost per answer | ≈ $0.043 |
| Cost per generated report | calls (≈ 6) × larger context (≈ 20k tokens in) at balanced tier | calculate with script by changing tokens; typically $0.05–$0.30 |
| Gross margin | (Revenue − COGS) ÷ Revenue | 70.2% |
| Contribution margin | Revenue − COGS − customer-specific support & onboarding amortised | gross − support (assume $25/mo) ≈ $255/mo |
| CAC | Sales+marketing spend ÷ new customers | $900 (assumed) |
| LTV | Gross margin per month ÷ monthly churn | $279.97 ÷ 0.03 ≈ **$9,332** |
| LTV : CAC | | **10.4** (target ≥ 3; assumption likely optimistic — validate with real churn) |
| CAC payback | CAC ÷ monthly gross margin | 3.2 months |
| Onboarding (one-time, separate) | Staff time | $400 per customer; charge a setup fee for large tenants |
| Break-even | Monthly opex ÷ gross margin per customer | $14,000 ÷ $279.97 ≈ **50 Professional customers** |

Opex assumption $14,000/month = ~4 FTE loaded ($10,400) + tools, legal, accounting, minimal marketing ($3,600). It does **not** include CAC.

### Operating profit by customer count (Professional-plan equivalent)

| Customers | Revenue/mo | Gross profit/mo | Operating profit/mo |
|---|---|---|---|
| 10 | 3,990 | 2,800 | −11,200 |
| 25 | 9,975 | 6,999 | −7,001 |
| 50 | 19,950 | 13,998 | −2 (break-even) |
| 100 | 39,900 | 27,997 | +13,997 |
| 200 | 79,800 | 55,993 | +41,993 |

Honest reading: at these assumptions, **the company loses money until ~50 customers**. Cash needed before break-even is roughly Σ monthly losses; with sales ramping ~5 customers/month the cumulative burn is about $60–70k beyond development — which is why the $170k budget (dev $105.8k + setup + runway) is tight. Re-run with your real ramp.

## 11.8 Separate the money streams (never mix)

1. **One-time development** (capex-like): $105,760 → amortise over 36 months for management reporting (≈ $2,938/mo).
2. **Customer onboarding** (one-time per customer): $400 cost; setup fee revenue optional.
3. **Monthly production cost**: §11.4.
4. **Subscription revenue**: §11.6.
Profit statements show all four on separate lines.

## 11.9 Controls that protect margin

* Per-tenant monthly token budget with alerts at 50/80/100%; hard stop or throttle at 120%.
* Provider-side spend caps and daily alert to Head of AI Eng.
* Cache embeddings and frequent answers (only where permissions allow), route easy questions to the economical tier.
* Monthly reconciliation: our metered usage vs provider invoices; investigate >5% gap.
* Re-run this model whenever a provider changes price, we change model, or actual usage deviates >15% from assumption.

## 11.10 Stage 11 gate

- [ ] All prices re-verified from official pages, dated, committed
- [ ] Scenarios 100 → 100,000 computed; assumptions listed
- [ ] Three production models and scaling triggers defined
- [ ] Plans priced with margins ≥ 60% gross (or explicit exception approved)
- [ ] Break-even and cash-runway reviewed by Finance
- [ ] No "unlimited" wording anywhere; overage & abuse rules in contract
