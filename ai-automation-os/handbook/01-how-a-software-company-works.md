# Part 1 — How a Software Company Really Works

Read this before Stage 0. It explains the world you are joining: the people, the paperwork, the rhythm of the week, and why each rule exists.

## 1.1 The big picture: how money and work flow

```
Customer has a problem
      ↓
Sales/Founder talks to customer  →  Proposal and price approved
      ↓
Contract (SOW) signed  →  Project created, Project ID issued
      ↓
Analysis → Design → Build → Test → Deploy      (this handbook)
      ↓
Customer accepts (UAT sign-off)  →  Invoice raised
      ↓
Support and maintenance  →  Monthly subscription or retainer = recurring revenue
```

A software company earns money only when the customer's problem is **solved and the customer says so**. Everything in this handbook exists to make that happen reliably and profitably.

## 1.2 Why companies use stages and gates

Building the wrong thing is the most expensive mistake. Finding a misunderstanding in Stage 2 costs an hour of talking. Finding it in Stage 13 costs weeks of rebuilding. **Gates** are cheap checks placed where mistakes are cheapest to catch.

## 1.3 The organisation, department by department

| Department | Purpose | Typical outputs |
|---|---|---|
| Management | Direction, money, hiring, contracts | Budgets, priorities, approvals |
| Sales and Business Development | Find and win customers | Proposals, demos, signed contracts |
| Business Analysis | Understand the customer's process | Process maps, requirements |
| Engineering | Build the product | Code, workflows, APIs |
| AI and Data | Make AI steps reliable | Prompts, evaluation reports |
| QA (Quality Assurance) | Find faults before customers do | Test plans, bug reports |
| DevOps and Infrastructure | Run software safely | Servers, deployment, monitoring |
| Security | Protect data and systems | Reviews, policies |
| Customer Success and Support | Keep customers happy and productive | Training, tickets, health checks |
| Finance and Admin | Invoices, tax, payroll, compliance | Invoices, books, filings |

## 1.4 Tickets: how work is tracked

Every piece of work is a **ticket** (also called a task or issue) on a shared board. No ticket, no work.

A good ticket has: **title** (verb first: "Validate GSTIN check character"), **description** (what and why), **acceptance criteria** (how we know it is done), **estimate** (hours), **assignee**, **priority**, **project ID**.

Board columns, left to right: **Backlog → Ready → In Progress → In Review → Testing → Done**.

- **Ready** means the ticket is clear enough to start (Definition of Ready: has acceptance criteria and an estimate).
- **Done** means finished to the team's standard (Definition of Done, section 1.9).

## 1.5 The weekly rhythm

| When | What | Length | Purpose |
|---|---|---|---|
| Every morning | Stand-up | 10–15 min | Each person says: done yesterday, doing today, blocked by |
| Monday | Sprint or week planning | 30–60 min | Pick the tickets for this week |
| Daily | Customer update by PM | short | Customer is never surprised |
| Wednesday | Technical review | 30 min | Design questions, code problems |
| Friday | Status report + demo to team | 30 min | Show what works; update status sheet |
| Monthly | Cost and quality review | 60 min | Check money, errors, customer health |
| Quarterly | Security and backup-restore drill | half day | Prove we can recover |

**Stand-up rule:** it is a status check, not a discussion. Long topics go to a separate meeting.

## 1.6 How code travels from your laptop to the customer

```
Your laptop (local)  →  Git branch  →  Pull request  →  Automatic tests (CI)
      →  Code review by Technical Lead  →  Merge to main
      →  Staging server (practice copy)  →  QA and customer test
      →  Approval  →  Production server (live)  →  Monitoring
```

Rules of this path:

- **Nobody edits the live system directly.** Ever.
- **Nobody pushes straight to `main`.** All changes go through a pull request and review.
- **Staging first.** Production gets only what already worked on staging.
- **Every release has a version number and a rollback plan** (how to go back if it goes wrong).

## 1.7 The three environments

| Environment | Used by | Data | Secrets |
|---|---|---|---|
| Local (your laptop) | You | Fake/sample only | Your own test keys in `.env` (never committed) |
| Staging | Team, QA, customer demo | Fake or customer-authorised test data | Staging keys |
| Production | The customer's staff | Real customer data | Production keys held in a secret manager, few people can see them |

A mistake on local costs nothing. A mistake on production costs trust. That is why they are separate.

## 1.8 Documents a company keeps (and who writes them)

| Document | Written by | Read by | Why it exists |
|---|---|---|---|
| Market/opportunity report | Analyst | Founder | Decide whether to pursue |
| Process document (AS-IS/TO-BE) | Business Analyst | Everyone | Shared understanding of the work |
| Requirements and scope freeze | BA + PM | Customer, team | What we promised |
| Architecture document | Technical Lead | Engineers | How it is built |
| Test plan and report | QA | PM, customer | Proof of quality |
| Security review | Security | Founder | Proof it is safe enough |
| Cost and price sheet | Founder + PM | Management | Profit control |
| Runbook | DevOps | Support | How to fix common problems |
| User and admin manuals | Customer Success | Customer | How to use and manage it |
| Status report | PM | Everyone | Weekly truth |
| Change request | PM | Customer, Founder | Controlled scope changes |

If a decision is only in a chat message, treat it as not decided. Put it in the project folder.

## 1.9 Definition of Done (for any ticket)

A ticket is Done only when **all** are true:

- [ ] The code or document does what the acceptance criteria say.
- [ ] Automated tests exist for it and pass.
- [ ] A second person has reviewed it.
- [ ] Documentation is updated (README, comments for public functions, this ticket).
- [ ] No secrets or customer data are in the change.
- [ ] It has run successfully on staging (for deployable work).
- [ ] The ticket links to the commit or pull request.

## 1.10 Code review: what it is and why

Code review means a colleague reads your change before it is merged. It catches bugs, spreads knowledge, and keeps quality consistent. It is **not** criticism of you. Rules:

- Keep pull requests **small** (under about 300 changed lines).
- Write a clear description: what, why, how to test.
- Respond to every comment: fix it or explain.
- Reviewers check the list in Appendix C.

## 1.11 Bugs and severity

| Severity | Meaning | Example | Reaction |
|---|---|---|---|
| P1 Critical | Wrong data posted or system down for everyone | Bills posted twice | Drop everything; tell PM and Founder at once |
| P2 Major | One feature broken, workaround exists | Reviewer screen will not load for one client | Fix within the agreed support time |
| P3 Minor | Small defect or cosmetic | Misspelt label | Schedule in normal work |

## 1.12 Customer communication rules

- The **PM** is the customer's main contact. Engineers do not promise dates or features directly.
- Never say "yes, we can add that" in a call. Say "I will note it and our Project Manager will come back with effort and cost." That is how scope creep is controlled.
- Be honest about problems early, with a plan.
- Never ask for or accept customer passwords by chat or email. Use the agreed secure method.

## 1.13 Professional habits that matter more than talent

- Name files and branches clearly (Appendix A).
- Keep your work visible: update tickets daily.
- Test your own work before asking for review.
- Write small, readable code; comments explain *why*, not *what*.
- Say "I do not know yet" and then find out.
- Protect confidentiality: customer names, data and prices stay inside the company.

## 1.14 How a project earns profit (and how it loses it)

Profit comes from: accurate estimates, frozen scope, reuse of components, low support load, and good customer fit. Profit disappears through: scope creep without change requests, unplanned rework, wrong customers, too much support, and unverified prices. Every rule in this handbook protects one of those.

---

*Developed by **V. LOKESHWARAN** · VEL Software Solutions · velsoftwaresolutions.in@gmail.com*
