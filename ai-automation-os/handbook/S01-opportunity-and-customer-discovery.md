# Stage S01 — Opportunity and Customer Discovery (Gate G1)

## Purpose
Prove that a real customer has a real, costly, repeated problem and will pay to solve it, **before** we spend any engineering time.

## Who and when
- **Owner:** Business Analyst or Founder. **Approver (G1):** Founder.
- **Time:** 1–2 weeks.

## Before you start
A target industry and a one-line idea, e.g. "bill processing for accounting firms".

## Step by step

1. **Write the problem statement** in one sentence: *"[Who] wastes [how much time/money] on [what task] because [why]."*
2. **List 10 possible customers** (names, size, contact route). Put them in `02-Sales-and-Customers/Leads/`.
3. **Research the market** for 2–3 hours: how people solve this today, what software exists, what it costs. Write findings in `01-discovery/market-notes.md` and mark every claim with its source link and date. Anything you could not verify is marked **UNVERIFIED**.
4. **List competitors and alternatives** in a table: name, what it does, price (if verified), weakness for our customer. Include "do it manually" as a competitor.
5. **Book 10 customer interviews** (45 minutes). Use template **T1** in `03-sop-and-template-pack.md`. Ask for consent before recording.
6. **Run the interviews.** Rules: listen more than you talk; ask "show me the last time you did this"; do not pitch until the end; write down numbers (how many, how long, how often wrong).
7. **Score each interview** 0–2 on five questions (pain stated, volume measurable, spends money today, buyer present, accepts pilot). 7 or more of 10 = qualified lead.
8. **Score the opportunity** with the weighted table (severity, frequency, time saved, money value, feasibility, AI necessity, integration difficulty, error risk, demand, competition). Weights are in `calculator/opportunity_score.py`. Replace guesses with interview facts.
9. **Estimate value:** `monthly value = items per month × minutes per item ÷ 60 × hourly cost`. Keep only the share the customer can really save (usually 50–70%).
10. **Write the report** `01-discovery/opportunity-report.md`: problem, evidence, market, competitors, score, value estimate, risks, recommendation (go / no-go / need more evidence).
11. **Present to the Founder** for Gate G1.

## Worked example (GST bill project)
- Problem statement: *"Small CA firms waste about 150 staff-hours a month typing 1,500 supplier bills, with about 3% typing errors."*
- 10 interviews planned with CA firms; 6 qualified (score ≥ 7).
- Value: 1,500 bills × 6 min ÷ 60 = 150 hours; × ₹200/hour = ₹30,000; counting 60% realisable = ₹18,000 per month.
- Recommendation: **go**, but only for firms processing 2,000+ bills a month, because below about 1,000 bills the product does not pay for itself.

## Deliverables (folder `04-Projects/<ID>/01-discovery/`)
- `problem-statement.md`, `market-notes.md`, `competitor-table.md`
- `interview-notes/` (one file per interview) and `interview-scores.xlsx`
- `opportunity-report.md`
- `00-admin/gate-signoffs.md` updated with G1

## Done checklist
- [ ] Problem statement is one sentence with numbers.
- [ ] At least 10 interviews done and notes filed.
- [ ] At least 3 qualified leads who would run a paid pilot.
- [ ] Competitor table includes the manual alternative.
- [ ] Every market claim has a source or is marked UNVERIFIED.
- [ ] Value estimate shows the formula and assumptions.
- [ ] Founder decision recorded.

## Common mistakes
- Pitching the product instead of listening.
- Counting friendly encouragement as demand. Demand is **willingness to pay or commit time**.
- Believing all saved time becomes saved money.
- Using made-up prices or numbers.

## Gate G1 sign-off
The Founder checks: real pain, measurable volume, willing buyers, no fatal competitor. Decision: **Go**, **Go with conditions**, or **Stop**. Stopping is a success if it saves months.

---

*Developed by **V. LOKESHWARAN** · VEL Software Solutions · velsoftwaresolutions.in@gmail.com*
