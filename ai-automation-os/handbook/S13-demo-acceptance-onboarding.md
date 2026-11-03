# Stage S13 — Demo, Acceptance and Onboarding (Gate G11)

## Purpose
Show the customer that it works, let them prove it on their real work, train their people, and obtain their written acceptance.

## Who and when
- **Owner:** PM and Customer Success. **Approver (G11):** the customer's owner (signature).
- **Time:** 2–4 weeks.

## Before you start
G10 passed; UAT scripts; training material; the customer's test data permissions; the commercial documents from S11.

## Step by step

### A. Demonstration
1. **Prepare the demo script** (30 minutes): the customer's problem in their numbers → the old way → the new way live → a failure being handled → a human approval → the measured result → the limits → the price.
2. **Use fake or authorised data only.** Never use another customer's information.
3. **Rehearse twice** including a recovery plan if the internet or a provider fails.
4. **Be honest about limits.** State what the system does not do.

### B. Onboarding (week by week)
| Week | Activity |
|---|---|
| 0 | Contract, data processing agreement and SOW signed; kickoff; named customer owner and reviewer |
| 1 | Connect their channels and accounts through secure methods; import master data (vendors, ledgers, clients); create user accounts and roles |
| 2 | **Shadow mode:** the system runs but humans still do the work; compare results daily |
| 3 | **Supervised go-live:** the system works, humans review everything |
| 4 | Tune thresholds (what is auto-approved); fix issues; user acceptance test; sign-off |

### C. Training
1. Reviewer training (1 hour, hands-on): the review queue, approve, edit, reject, what the reasons mean.
2. Admin training (1 hour): users, settings, reports, what to do when an alert appears.
3. Give the **User Manual**, **Admin Manual**, a one-page quick guide and the support contact and hours.

### D. User acceptance test (UAT)
1. The customer runs the UAT scripts on real work.
2. Record every result and issue. Fix defects (P1/P2 before sign-off).
3. Measure results against the targets from S04 and the baseline from S02: time per item, error rate, share handled automatically.
4. Prepare the **ROI report** from measured results, not estimates.
5. The customer signs the **Acceptance Certificate** stating what was delivered and that it meets the agreed criteria.

### E. Commercial close
1. Confirm what the customer owns (their data and outputs) and what we own (platform code and prompts); confirm third-party licences.
2. Raise the invoice per the milestone plan; start the subscription/support clock.

## Deliverables (`09-handover/`)
- `demo-script.md`, `uat-results.xlsx`, `acceptance-certificate.pdf` (signed)
- `user-manual.pdf`, `admin-manual.pdf`, `quick-guide.pdf`, `training-attendance.xlsx`
- `roi-report.md` (measured), `onboarding-checklist.xlsx`

## Done checklist
- [ ] Demo done with fake/authorised data; limits stated.
- [ ] Shadow mode and supervised go-live completed.
- [ ] Staff trained; manuals delivered.
- [ ] UAT passed; no open P1/P2.
- [ ] Measured results compared with baseline and targets.
- [ ] Acceptance Certificate signed.
- [ ] Ownership and licence terms confirmed in writing.

## Common mistakes
- Skipping shadow mode and going live on day one.
- Demoing features that are not built.
- No named customer owner, so nobody accepts.

## Gate G11 sign-off
The customer owner's signature on the Acceptance Certificate. Without it the project is not delivered.

---

*Developed by **V. LOKESHWARAN** · VEL Software Solutions · velsoftwaresolutions.in@gmail.com*
