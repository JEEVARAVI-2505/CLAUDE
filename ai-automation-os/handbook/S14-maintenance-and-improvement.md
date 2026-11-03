# Stage S14 — Maintenance and Continuous Improvement (Gate G12)

## Purpose
Keep the system working, accurate and affordable after delivery, and turn good service into recurring revenue.

## Who and when
- **Owner:** Customer Success (support), Developer (L2), Architect/AI Engineer (L3). **Approver (G12):** Customer Success head and Founder at handover to maintenance.
- **Time:** ongoing.

## Before you start
G11 passed; runbook; monitoring live; support terms in the contract.

## Step by step

### A. Handover to maintenance (G12)
1. Hold a handover meeting: project team → support team. Walk through architecture, runbook, known issues, customer contacts, thresholds, cost per execution.
2. Confirm who watches the dashboards, who receives alerts, who answers tickets, and the support hours.

### B. Support levels
| Level | Who | Handles |
|---|---|---|
| L1 | Customer Success | How-to questions, user access, reports |
| L2 | Developer | Failed runs, integration errors, small rule changes |
| L3 | Architect / AI Engineer | Model or prompt problems, new document types, redesign |

Severity: **P1** wrong data posted or system down for all (respond at once); **P2** one feature broken; **P3** minor/cosmetic. Response targets follow the contract.

### C. The operating rhythm
| When | Task | Owner |
|---|---|---|
| Daily | Check failed runs, queue size, alerts | Support |
| Weekly | Review failures and human corrections; add new failure cases to the evaluation set; update runbook | Support + AI Engineer |
| Monthly | Cost review (₹ per successful execution vs plan); success rate; human-override rate; token spend; customer usage and health call | PM + Customer Success |
| Monthly | Dependency and security updates; check the backup restores | DevOps |
| Quarterly | Restore drill; security review; test new model versions on the evaluation set; pricing review | DevOps, Security, AI Engineer, Founder |

### D. Metrics and action thresholds
| Metric | Action threshold |
|---|---|
| Workflow success rate | below 97% → incident review |
| Human-override rate | up 5 points → re-evaluate prompt/rules |
| Cost per successful execution | more than 20% above plan → investigate |
| Token spend | above 80% of cap → alert customer |
| Latency (95th percentile) | above the agreed limit → scale or optimise |
| Queue backlog | growing for 3 days → add capacity |
| Duplicate actions | any → P1 |

### E. Controlled change
1. **Never** change a production model or prompt without running the full evaluation set and getting release approval.
2. New feature or document type = change request: estimate, price, approve, then run S04 → S13 in miniature.
3. Provider API changes: watch announcements; test in staging; schedule the fix.
4. Keep a **changelog** of every release.

### F. Customer success and growth
1. Monthly review call: usage, savings achieved, issues, requests.
2. Track a health score: usage trend, complaints, override rate, payment status.
3. Offer upgrades when usage nears the plan limit; offer related automations when value is proven.
4. Collect a short testimonial and a referral when the customer is happy and the ROI report supports it.

### G. Learning loop
After each project hold a 1-hour retrospective: what went well, what went wrong, what to change in this handbook. Send changes to the Founder; update the book.

## Deliverables
- Handover record; monthly report template filled each month; changelog; updated evaluation set; retrospective notes.

## Done checklist
- [ ] Support team has accepted the handover.
- [ ] Alerts, dashboards and owners are named.
- [ ] Monthly review calendar set with the customer.
- [ ] Release/change process agreed; no unapproved production changes.
- [ ] Retrospective held; handbook updated.

## Common mistakes
- Treating delivery as the end.
- Ignoring small rising error rates until the customer complains.
- Upgrading a model on production "because it is newer".

## Gate G12 sign-off
Customer Success and Founder confirm the maintenance handover is complete.

---

*Developed by **V. LOKESHWARAN** · VEL Software Solutions · velsoftwaresolutions.in@gmail.com*
