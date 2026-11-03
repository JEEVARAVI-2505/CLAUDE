# Stage S03 — Feasibility and TO-BE Design (Gate G3)

## Purpose
Decide **what** to automate and with **which kind** of automation, choosing the simplest approach that works reliably.

## Who and when
- **Owner:** Business Analyst with the Technical Lead. **Approver (G3):** Technical Lead and Founder.
- **Time:** 3–5 days.

## Before you start
Signed AS-IS, business rules, baseline, sample data.

## Step by step

1. **Mark every AS-IS step** with one of five tags:
   **A** fully automatic · **R** rule-based (a fixed rule decides) · **AI** needs AI to read or judge unstructured content · **H** needs a human decision · **E** exception handled by a person.
2. **Pick the automation class** for the whole process using the ladder. Take the **first** answer that is "yes":
   1. Input is structured and the rule is explicit → *rule-based or API automation*.
   2. No API exists but the screen is stable and automation is allowed → *RPA / browser automation* (never bypass CAPTCHAs or security controls).
   3. One step is a document or free text but the flow is fixed → *AI-assisted workflow*.
   4. Answers must come from our private documents → *RAG* (retrieval-augmented answers with sources).
   5. The path depends on what is found along the way and tools can be tightly limited → *tool-using agent*.
   6. Truly separate jobs needing separate permissions → *multi-agent*.
   Write down which simpler options you rejected and why.
3. **Decide the human role.** List every action that must have human approval: payments, refunds, sending external emails, deleting data, legal filings, anything costly if wrong. For the rest, define **confidence thresholds** (e.g. below 85% confidence → human review).
4. **Assess the risk of a wrong answer:** low (cosmetic), medium (rework), high (money/legal). High-risk steps always get human approval.
5. **Draw the TO-BE workflow** as a flow chart with the A/R/AI/H/E tags, including every failure path: retry, dead-letter (a holding area for jobs that keep failing), human exception queue.
6. **Estimate the benefit again** with the TO-BE: expected share handled automatically, expected review volume, expected time saved.
7. **Quick feasibility test:** take 10 real samples and try the AI step by hand in a chat tool (using **only approved, non-sensitive or authorised data**). Record how many came out right. If fewer than about 7 of 10, reconsider.
8. **Write the Feasibility Report** and present it for Gate G3.

## Worked example (GST bill project)
- Receive bill & de-duplicate: **R**. Read bill into fields: **AI** (OCR + language model). Check GSTIN, tax maths, dates: **R**. Suggest ledger: **R** from vendor history, **AI** only if none. Low confidence or any rule failure: **H**. Post to accounting software: **A** using an API, **only after** validation. Monthly missing-bill report: **R**.
- Class chosen: **AI-assisted workflow**. *Agent rejected* because the steps are fixed; a fixed flow is cheaper, testable and safer.
- Risk: wrong posting is medium; hence validation + review for doubtful bills.

## Deliverables (`02-analysis/`)
- `to-be-workflow.png` (+ source), `feasibility-report.md` (class, rejected options, risks, human steps, thresholds, benefit)
- `feasibility-sample-test.md` (10 samples, results)

## Done checklist
- [ ] Every step has a tag (A / R / AI / H / E).
- [ ] The chosen class is the **simplest** that can work; rejected options are explained.
- [ ] All high-risk actions need human approval.
- [ ] Failure paths are drawn.
- [ ] A sample test on real examples was done and recorded.
- [ ] Benefit estimate updated.

## Common mistakes
- Using AI or an "agent" because it sounds modern.
- Forgetting what happens when the automation is wrong.
- Letting the AI act on money or external messages without approval.

## Gate G3 sign-off
Technical Lead confirms feasibility and the chosen class; Founder confirms the benefit still justifies the work.

---

*Developed by **V. LOKESHWARAN** · VEL Software Solutions · velsoftwaresolutions.in@gmail.com*
