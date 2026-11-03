# Appendices

## Appendix A — Git cheat sheet

| I want to… | Command |
|---|---|
| See what changed | `git status` and `git diff` |
| Get the newest main | `git switch main && git pull` |
| Start work on a ticket | `git switch -c feature/GBA-12-short-name` |
| Choose files for a commit | `git add path/to/file` |
| Save with a message | `git commit -m "GBA-12: what changed"` |
| Send my branch | `git push -u origin feature/GBA-12-short-name` |
| Update my branch with main | `git fetch origin && git merge origin/main` |
| Undo changes in one file (not yet committed) | `git restore path/to/file` |
| See history | `git log --oneline -10` |

**Never:** `git push --force`, `git reset --hard`, committing `.env`, working directly on `main`. If something goes wrong, stop and ask; Git almost always can recover.

Good commit message: `GBA-12: reject bills whose CGST and IGST are both present`. Bad: `fix`, `changes`, `final`.

## Appendix B — Who to ask, and when to escalate

| Situation | First ask | Then |
|---|---|---|
| Stuck more than 30 minutes | Teammate | Technical Lead |
| Do not understand a customer rule | Business Analyst | PM |
| Customer asks for something new | PM (do not promise) | Founder via change request |
| Suspected leak, lost laptop, key committed | Technical Lead **immediately** | Founder; Security Specialist |
| Production problem | Support/DevOps on call | Technical Lead, PM |
| Price or contract question | PM | Founder |

**Escalation rule:** bad news early beats bad news late. A problem reported on Monday is a plan; the same problem reported on Friday is a crisis.

## Appendix C — Code review checklist

Reviewer ticks each item:

- [ ] Does it do what the ticket's acceptance criteria say?
- [ ] Are there tests, and do they test failures as well as success?
- [ ] Are names clear; are functions short and focused?
- [ ] No secrets, personal data or debug leftovers?
- [ ] Money uses `Decimal`; dates handled correctly?
- [ ] Every database query filters by customer?
- [ ] External writes are idempotent; errors handled deliberately?
- [ ] AI output validated before use; no AI-controlled write access?
- [ ] Documentation and README updated?
- [ ] Pull request is small enough to understand (under about 300 lines)?

## Appendix D — Frequently asked beginner questions

**Q: I do not understand the customer's business. Is that a problem?**
No, if you ask. Read the process sheet, ask the Business Analyst, and sit in on one customer session.

**Q: The AI gave a wrong answer. What do I do?**
Save the example, find the cause (OCR, prompt, missing rule), add it to the evaluation set, fix, re-run the full set. Never fix by editing one answer by hand.

**Q: May I paste customer data into a chat tool to get help?**
No, unless the Founder has approved that tool for that data. Use fake data that has the same shape.

**Q: The deadline is close. Can I skip the tests?**
No. Tell the PM early so scope or dates can change. Skipped tests become production bugs.

**Q: The customer asked me directly for a small extra feature.**
Reply: "I will note it and our Project Manager will come back with effort and cost." Tell the PM the same day.

**Q: I found a bug in someone else's code.**
Create a ticket with steps to reproduce; mention the owner; do not silently rewrite their work.

**Q: How do I know when I am done?**
When every item in the Definition of Done (Part 1, section 1.9) is ticked.

## Appendix E — Index of templates and tools

| Need | Where |
|---|---|
| Interview guide, process sheet, feasibility, I/O freeze, change request, agent spec, evaluation plan, security checklist, risk register, commercial documents, status report, readiness checklist, maintenance review, gate sign-off | `03-sop-and-template-pack.md` (T1–T14) |
| Technology and costing formulas | `04-technology-competitors-and-costing-reference.md` |
| Roles, responsibility matrix, portal, support | `05-people-portal-and-support.md` |
| Cost, price and ROI calculator | `calculator/` |
| Working example code and tests | `reference-project/` |

## Appendix F — Learning path for a beginner (first 90 days)

**Days 1–10 (foundation):** finish S00; read Parts 0–2; run and read `reference-project`; learn Python basics (variables, functions, lists, dictionaries, files, errors); learn Git basics; shadow a project meeting.
**Days 11–30 (contributor):** fix two small tickets (a new rule plus its tests); write one pull request per week; learn JSON and what an API is; try the API in Postman; read one process sheet and one scope freeze.
**Days 31–60 (builder):** build one endpoint end to end with tests; learn SQL basics and PostgreSQL; build a small n8n workflow with a webhook trigger; learn how to read provider API documentation; join an evaluation run.
**Days 61–90 (owner of a part):** own one integration adapter or one review screen; write its documentation; present it in the Friday review; take part in a security test and a deployment to staging.

**Topics to study, in order of value:** Python; Git; testing with pytest; JSON and APIs (HTTP methods, status codes); SQL; Docker basics; how large language models behave (they can be confidently wrong); prompt basics; security basics (secrets, least privilege, injection); how to read documentation.

**Self-check at day 90:** can you (1) take a ticket to merged pull request without help, (2) explain the 15 stages and 12 gates in your own words, (3) explain why AI output must be validated, (4) deploy to staging using the runbook?

## Appendix G — One-page summary of the 12 gates

| Gate | Approves | Approver |
|---|---|---|
| G1 | The problem is real and paid | Founder |
| G2 | We understand the customer's process | Customer owner |
| G3 | The chosen automation approach is feasible and the simplest | Technical Lead, Founder |
| G4 | Scope is frozen | Customer, Founder |
| G5 | Architecture and threats are covered | Tech Lead, Security, Founder |
| G6 | Workflow and screens accepted | Customer |
| G7 | Code reviewed | Technical Lead |
| G8 | Quality and security proven | QA, Security |
| G9 | Cost and price approved | Founder |
| G10 | Safe to go live | DevOps, Tech Lead, Founder |
| G11 | Customer accepted delivery | Customer owner |
| G12 | Maintenance handover done | Customer Success, Founder |

A gate is **passed** only with the evidence files named in its stage and the approver's signature recorded in `00-admin/gate-signoffs.md`. A skipped gate needs a written waiver from the Founder with the reason and the risk accepted.

---

*Developed by **V. LOKESHWARAN** · VEL Software Solutions · velsoftwaresolutions.in@gmail.com*
