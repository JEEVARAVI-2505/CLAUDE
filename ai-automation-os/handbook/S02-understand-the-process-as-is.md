# Stage S02 — Understand the Customer's Process (AS-IS) (Gate G2)

## Purpose
Document exactly how the customer does the work today, with real numbers, so we automate the **real** process and not our imagination of it.

## Who and when
- **Owner:** Business Analyst. **Approver (G2):** the customer's process owner (signature) and the PM.
- **Time:** 1 week.

## Before you start
G1 passed, a signed NDA (non-disclosure agreement) with the customer, a named contact who does the work daily.

## Step by step

1. **Create the project folder** `P-<year>-<nnn>_<Short-Name>` from the template and fill `project.yaml` (customer, PM, technical lead, dates).
2. **Shadow the work.** Sit with the person doing the task for at least two sessions. Watch; do not interrupt. Note every click, document, system and decision.
3. **Fill the Process Discovery Sheet (T2)**: process name, objective, owner, people, trigger, inputs, steps with minutes, business rules, decisions, exceptions with frequency, outputs, destination system, approvals, failures, data sensitivity.
4. **Collect 20–50 real examples** of the inputs (with written permission) — the clean ones and the ugly ones. Store them **outside Git** in the restricted customer folder. These later become your test set.
5. **Measure the baseline.** For one week count: items processed, minutes per item, errors found, rework time. Write: `baseline cost per month = hours × hourly cost + rework cost`.
6. **Draw the AS-IS workflow** as a swimlane diagram: one lane per person or system, arrows for hand-offs, a marker on every wait and every decision. Draw it on paper first, then in a diagram tool.
7. **Draw three more pictures:** data flow (what data moves where, mark personal/financial data), system map (which software talks to which), exception map (what happens when things go wrong).
8. **Find bottlenecks and error sources:** for each step write minutes, waiting time, error rate, why.
9. **List business rules as numbered, testable sentences.** Bad: "check the tax". Good: "Rule 4: a bill has either CGST+SGST or IGST, never both."
10. **Validate with the customer.** Walk the customer through the diagrams and the rule list. Fix every misunderstanding. Get the process owner to sign the AS-IS document.

## Worked example (GST bill project)
AS-IS: bill arrives by WhatsApp/email/paper → clerk opens it → types vendor name, GSTIN, invoice number, date, taxable value, tax amounts, total → picks the ledger → checks the tax maths → enters in accounting software → month-end reconciliation against the government's purchase statement.
Measured: 6 minutes per bill, 3% typing errors, peak workload in the last week of each month, about 4% unreadable photos.
Rules found: GSTIN has 15 characters with a valid check character; tax parts must add to the total within ₹1 rounding; CGST equals SGST; invoice number at most 16 characters; the same vendor + invoice number must never be posted twice.

## Deliverables (`02-analysis/`)
- `process-discovery-sheet.docx`
- `as-is-swimlane.png` (and source file), `data-flow.png`, `system-map.png`, `exception-map.png`
- `baseline-measurements.xlsx`
- `business-rules-v1.md` (numbered)
- `bottleneck-analysis.md`
- `as-is-signoff.pdf` (signed)

## Done checklist
- [ ] I watched the work being done at least twice.
- [ ] Baseline is measured with real numbers, not opinions.
- [ ] 20+ real examples collected with permission and stored safely.
- [ ] Every business rule is numbered and testable.
- [ ] Exceptions are listed with how often they happen.
- [ ] The customer process owner signed the AS-IS document.

## Common mistakes
- Trusting what people *say* they do instead of watching.
- Documenting only the happy path.
- Collecting data with no written permission.
- Skipping the signature, then arguing later about what was agreed.

## Gate G2 sign-off
Customer process owner signs the AS-IS. PM checks the baseline numbers exist. If the customer cannot sign, the process is not understood yet; repeat steps 2–10.

---

*Developed by **V. LOKESHWARAN** · VEL Software Solutions · velsoftwaresolutions.in@gmail.com*
