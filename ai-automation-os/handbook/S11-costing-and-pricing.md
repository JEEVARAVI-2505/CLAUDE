# Stage S11 — Costing and Pricing (Gate G9)

## Purpose
Know the true cost of building and running the automation, then set a price that is fair to the customer and profitable for us. **No quotation leaves the company before this gate.**

## Who and when
- **Owner:** PM with the Founder. **Approver (G9):** Founder.
- **Time:** 3 days (start an early draft in S04).

## Before you start
Measured effort (hours), architecture, verified provider prices, test measurements (tokens, OCR pages, cost per bill), customer baseline.

## The calculator
Everything below is implemented and tested in `ai-automation-os/calculator/`.
```bash
cd ai-automation-os/calculator
pip install pytest
python -m pytest -q          # 12 tests of the formulas
python report.py             # prints every figure
python sensitivity.py        # what-if analysis
```
**All prices live in `pricing_inputs.json`.** Each has a source link and `verified` flag. The function `assert_quotable()` **refuses** to produce a quote while any price is unverified. To verify: open the official pricing page, copy the value, put the URL and today's date, set `verified` to true.

## Step by step

1. **Development cost.** For each role: estimated hours × fully loaded hourly cost. Sum. Add overhead percentage. Make three versions: optimistic (×0.8), expected, pessimistic (×1.4). Do not count the same hours twice.
2. **Software and platform cost.** List every paid tool; separate open-source (free to use) from commercial services. Check licence terms before reselling anything.
3. **AI cost per execution.** `input tokens/1,000,000 × input price + output tokens/1,000,000 × output price`, plus retries and escalations, plus OCR. Compare three model tiers.
4. **Third-party API cost** (WhatsApp, email, SMS, OCR, accounting integrations): separate free, plan and usage-based charges; check rate limits and commercial-use restrictions.
5. **Monthly production cost.** Infrastructure + workflow platform + AI + APIs + storage + monitoring + backups + maintenance labour + support labour + domain/other.
6. **Human review cost.** `review minutes × share of items reviewed × reviewer hourly cost ÷ 60`. Include it; it is real cost.
7. **Volume simulation.** Compute at 100, 500, 1,000, 5,000, 10,000, 25,000, 50,000, 100,000 executions a month with low, medium and high token use. Include failures and retries.
8. **Three production models:** Lean (pilots), Professional (paying customers), Enterprise (only when a contract funds redundancy). Show setup cost, monthly cost, annual cost, limits and risks for each.
9. **Selling price.**
   - *Project:* `(development + third-party) × (1 + risk allowance) ÷ (1 − target margin)`.
   - *Subscription tiers:* Starter, Professional, Business, Enterprise with setup fee, monthly fee, included executions, overage price, support. Never promise unlimited AI.
10. **Contribution margin per customer:** `revenue − cost to serve` (includes review and maintenance labour).
11. **Customer ROI:** `hours saved × customer staff rate × realisable share − subscription`; payback = `their investment ÷ monthly net benefit`. Only claim savings the customer can actually realise.
12. **Break-even and profit:** customers needed to cover fixed costs; profit at 5, 10, 25, 50, 100, 250, 500 customers; taxes shown separately.
13. **Run the sensitivity checks:** volume ±; model price ×2; auto-approval rate; support hours ×0.5 and ×2; churn 3/6/10%. Note which assumption can flip the decision; measure it first in the pilot.
14. **Write the Cost and Price Pack** and present for Gate G9.

## Worked example (placeholder prices; see `02-sample-lifecycle-gst-bill-automation.md`)
AI ≈ ₹0.43 per bill; human review ≈ ₹1.67 per bill; build ≈ ₹3.79 lakh; Professional tier contribution ≈ ₹3,467 per customer per month; break-even ≈ 27 customers; support hours are the biggest margin lever. **These numbers are placeholders until prices are verified.**

## Deliverables (`07-commercial/`)
- `cost-sheet.xlsx` and calculator output, `price-sheet.md`, `roi-report.md`, `sensitivity.md`, `pricing_inputs.json` with all prices verified and dated.

## Done checklist
- [ ] Every price is verified from the official source and dated.
- [ ] Development, platform, AI, API, review and support costs all included.
- [ ] Volume table and three production models done.
- [ ] Price gives at least the target margin after risk allowance.
- [ ] Customer ROI uses realisable savings, not all saved time.
- [ ] Sensitivity done; riskiest assumption identified.
- [ ] `assert_quotable()` passes.

## Common mistakes
- Using free tiers as if they were permanent production.
- Forgetting support and review labour.
- Pricing from competitor lists without computing our cost.
- Promising unlimited usage.

## Gate G9 sign-off
Founder approves the price and margin. Without it, no proposal or quote is sent.

---

*Developed by **V. LOKESHWARAN** · VEL Software Solutions · velsoftwaresolutions.in@gmail.com*
