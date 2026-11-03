# Stage S09 — Testing and AI Evaluation (part of Gate G8)

## Purpose
Prove the system works, survives failures, and meets the accuracy targets from the scope freeze, **with numbers**.

## Who and when
- **Owner:** QA Engineer (everyone helps). **Approver:** QA lead and Technical Lead.
- **Time:** 1–2 weeks.

## Before you start
Feature-complete build; labelled real examples; acceptance criteria from S04.

## The five kinds of testing

| Kind | Question | Where |
|---|---|---|
| Unit | Does each small function work? | `tests/unit/` |
| Integration | Do the parts work together? | `tests/integration/` |
| Workflow (end to end) | Does a bill travel from arrival to ledger correctly? | `tests/workflow/` |
| AI evaluation | Is the AI step accurate enough? | `tests/evaluation/` |
| Reliability and security | Does it survive failures and attacks? | `tests/security/` |

## Step by step

1. **Write the test plan** (`05-testing/test-plan.md`): what is tested, how, with which data, pass criteria, who runs it. Map every requirement REQ-nnn to at least one test.
2. **Run all automated tests** and record the result. Fix failures; never "skip" a failing test to get green.
3. **Build the evaluation set.** At least 300 real, authorised examples, labelled by a person (the correct answer for each field). Split: 70% for tuning prompts, **30% held out** and never used for tuning, to give an honest score.
4. **Measure per use case (do not use one generic accuracy):**
   - *Extraction:* per-field exact match, precision, recall, share of bills where totals reconcile, JSON validity rate.
   - *Classification:* precision, recall, F1 per class.
   - *RAG assistants:* retrieval hit rate (measured alone), groundedness, citation correctness, correct refusal when the answer is unknown.
   - *Agents:* tool-choice accuracy, completion rate, steps per task, unsafe-action attempts (target: zero).
5. **Label every failure** by cause: OCR, prompt, missing rule, unclear source, wrong label. Fix the biggest cause first; re-run on the evaluation set.
6. **Compare with the target** from S04. Record pass or fail per metric in `05-testing/evaluation-report.md`.
7. **Reliability tests:** send the same webhook twice; stop the AI provider; return a 429; kill the service mid-posting; drop the network; return invalid JSON; fill the disk; stop the database. After each, check: no duplicate posting, no lost job, sensible alert, correct recovery.
8. **Performance test:** run the expected peak (e.g. 200 bills in 10 minutes). Record time per bill, queue growth, CPU/memory.
9. **Cost test:** record tokens, OCR pages and money per successful bill; compare with the S11 estimate; flag differences above 20%.
10. **User acceptance preparation:** write UAT scripts in the customer's words (step, expected result) for S13.
11. **Bug handling:** log every defect with steps to reproduce, expected vs actual, severity (P1–P3), owner. P1 and P2 must be fixed before sign-off.
12. **Regression rule:** every prompt, model or rule change re-runs the whole evaluation set. Block the release if a key metric drops beyond the agreed tolerance.

## Worked example (GST bill project)
Targets: key-field exact match ≥ 97%; zero unflagged tax-maths errors reaching the ledger; ≤ 20% of bills sent to review. Result on the held-out 90 bills: 97.8% exact match; 0 unflagged maths errors; 17% to review → **pass**. Biggest failure cause: low-light photos (OCR) → recommendation: add an "image too dark, please resend" message.
*(Figures here illustrate how to report; your project's numbers come from your own data.)*

## Deliverables (`05-testing/`)
- `test-plan.md`, `test-results.md`, `evaluation-report.md`, `reliability-report.md`, `performance-report.md`, `cost-report.md`
- Defect log; UAT scripts.

## Done checklist
- [ ] Every requirement has a test; all automated tests pass.
- [ ] Evaluation set ≥ 300 real examples with held-out portion.
- [ ] Metrics chosen per use case, thresholds agreed **before** testing.
- [ ] Failure causes classified and top causes addressed.
- [ ] Reliability tests done: duplicates, outages, timeouts, partial failures.
- [ ] No open P1/P2 defects.
- [ ] Cost per successful execution measured.

## Common mistakes
- Tuning the prompt on the same data used for the final score.
- Testing only clean examples.
- Reporting "95% accurate" without saying of what.
- Ignoring the cost of retries and failures.

## Gate sign-off
QA signs the evaluation and test reports; Technical Lead confirms. Final G8 approval needs S10 as well.

---

*Developed by **V. LOKESHWARAN** · VEL Software Solutions · velsoftwaresolutions.in@gmail.com*
