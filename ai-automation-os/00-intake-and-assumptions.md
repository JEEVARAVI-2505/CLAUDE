# 00 — Company Intake and Working Assumptions

Status: **DRAFT for founder confirmation (Gate 0)**. Anything marked `ASSUMED` was not told to us and must be confirmed or replaced.

## 1. Intake answers received

| Item | Answer |
|---|---|
| Business model | **Both**: custom automation projects + subscription products |
| Team size | 2–15, scaling with available resources |
| Budget | ₹1 lakh to ₹25 lakh+, scaling with resources |
| Target industries | All listed; sample chosen on evidence of trend and low competition (see §3) |
| Output | Repo folder + published summary page |

## 2. Items still missing (ASSUMED until answered)

| Item | Working assumption | Why it matters |
|---|---|---|
| Company name | `[Company]` | Contracts, branding |
| Target customer size | 10–200 employee Indian SMEs, and accounting/CA firms as a channel | Price points, security level |
| Preferred stack | Python 3.12 + FastAPI, n8n (self-hosted), PostgreSQL + pgvector, Docker | Hiring, cost |
| Geography / currency | India, INR; GST 18% on services shown separately | Pricing, DPDP Act duties |
| Lead model | Founder-led sales first, partner channel later | CAC |

## 3. Which opportunity first (the "trending and low competition" question)

Method: 10 candidates scored against the 10 weighted criteria from Phase 1.4 (`calculator/opportunity_score.py`). Weights sum to 100. Favourable score 1–5 (complexity, error risk and competition are inverted so 5 = good).

| Rank | Opportunity | Score /100 |
|---|---|---|
| 1 | **GST purchase-bill processing (CA firms / SMEs)** | **82** |
| 2 | Real-estate WhatsApp lead qualification | 73 |
| 2 | E-commerce returns / RTO handling | 73 |
| 2 | Construction tender / BOQ extraction | 73 |
| 2 | Clinic appointment + follow-up on WhatsApp | 73 |
| 6 | Customer-support RAG assistant | 72 |
| 7 | Logistics POD / e-way bill documents | 71 |
| 8 | Recruitment CV screening | 67 |
| 9 | School fee reminders + enquiries | 64 |
| 10 | Legal admin document drafting | 61 |

**Honest limits of this ranking**
- The 1–5 inputs are the author's hypotheses, not market data. Live web research was **not possible** in this session (network allowlist blocked provider and vendor sites). Gate 1 requires replacing them with interview evidence from at least 10 target customers.
- "Low competition" is not claimed for the winner. Accounting-software vendors and GST tools already cover parts of this space. The wedge is **CA firms processing many clients' bills**: multi-client volume, a recurring need and a human reviewer already in the loop.
- **Construction tender/BOQ extraction** (rank 2, best competition score, 4/5) is the strongest second product because it matches your existing building-engineering domain. Recommended as Wave 2.
- Customer-support RAG and WhatsApp lead bots score well on demand but are crowded. Treat them as commodity offerings, not differentiators.

## 4. Gate 0 checklist (founder signs)

- [ ] Company name, entity type, GSTIN
- [ ] Monthly budget ceiling and cash runway
- [ ] Confirm India-first scope
- [ ] Confirm sample automation (GST bill processing) or substitute
- [ ] Name a person for each role in `01-operating-framework.md` §5 (one person may hold several)
