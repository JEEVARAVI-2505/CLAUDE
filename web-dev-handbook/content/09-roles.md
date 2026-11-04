## 9.1 Roles and responsibilities

| Role | Core responsibilities | Authority and limits | How success is measured |
|---|---|---|---|
| **Junior Frontend Developer** | Implement UI components and pages from approved designs; write component tests; fix UI bugs; follow accessibility and responsive standards; open small PRs | Works from assigned tickets; no architecture changes; no production access; every PR reviewed | Design match, accessibility score, review rework rate, tickets delivered vs estimate |
| **Junior Backend Developer** | Implement endpoints, validation, service-layer rules and tests from the API spec; write migrations under review; fix defects | No schema or auth changes without TL review; no production access; no secrets in code | Test coverage of rules, defect rate, spec conformity |
| **Full-stack Developer** | Deliver features end to end - UI, API, database; integrate APIs; own a module; mentor juniors | Can propose design changes; merges only after review; limited staging access | Feature lead time, quality at UAT, documentation completeness |
| **UI/UX Designer** | Research, sitemap, wireframes, design system, hi-fi screens, prototypes, accessibility and hand-off; review the built UI | Owns design decisions within the approved brief; client approves | Client approval cycles, usability findings, design-implementation match |
| **QA Engineer** | Test plan from requirements; manual and automated tests (Playwright, Postman); defect management; UAT support; release-readiness report | Can block a release by raising a critical defect to the TL | Defect escape rate, automation coverage of critical journeys |
| **DevOps Engineer** | Environments, CI/CD, hosting, infrastructure as code, monitoring, backups, DNS/TLS, patching, cost alerts | Production changes only through the pipeline or a documented change; least privilege | Deployment success rate, mean time to restore, backup-restore test results |
| **Technical Lead** | Architecture and ADRs, standards, code review of sensitive areas, security sign-off, estimation review, mentoring, go/no-go for releases | Approves technical decisions and releases; escalates budget/scope to PM | Quality at release, security findings, team growth, estimate accuracy |
| **Project Manager** | Scope, SOW, schedule, budget, risks, change control, client communication, status reports, UAT and handover coordination | Owns client relationship and plan; cannot change technical decisions alone | On-time and on-budget delivery, client satisfaction, margin vs plan |

!!! note "Career ladder"
    Junior -> Developer -> Senior -> Lead. Promotion requires demonstrated gates: independent delivery, quality of reviews given, mentoring, documentation, and security awareness - not tenure alone.

## 9.2 Daily work procedure

| Time | Activity | Output |
|---|---|---|
| Start of day | Read messages; check assigned tickets and CI status; plan the day (3 priorities) | Daily plan in the tracker |
| Stand-up (15 min) | Each person: yesterday, today, blockers - no problem-solving in the meeting | Blockers list with owners |
| Focus blocks | Build, test, review - phone and notifications off | Commits, PRs |
| Mid-day | Review at least one teammate's PR; respond to review comments | Review given |
| End of day | Push work-in-progress to your branch; update tickets and timesheet; note next steps | Daily log (3 lines) |

Working agreements: commit at least daily; keep PRs small; never leave a branch un-pushed overnight; respond to review requests within one working day; ask early (stuck rule in Section 2).

## 9.3 Task assignment

| Stage | Rule |
|---|---|
| Ticket quality ("ready") | Has a clear title, user story or problem, acceptance criteria, design/API links, estimate, priority, owner, requirement id |
| Assignment | Technical Lead / PM assigns by skill and learning need; a junior receives work with a named reviewer |
| Sizing | Estimate in hours or points; any ticket over 2 days is split |
| Work-in-progress limit | Maximum 2 active tickets per person |
| Workflow | Backlog -> Ready -> In progress -> In review -> In staging -> Accepted -> Done |
| Blocked | Mark as blocked with the reason and owner; mention in stand-up; escalate per 9.8 |
| Done | Meets the definition of done (Section 6) |

## 9.4 Code review (reviewer checklist)

- [ ] Understands the purpose (ticket, requirement id); change is small and focused
- [ ] CI green; tests added and meaningful; edge cases covered
- [ ] Authentication and authorisation checks present on the server
- [ ] Input validated; output encoded; SQL parameterised; no injection or XSS path
- [ ] No secrets, tokens or personal data in code, logs, fixtures or screenshots
- [ ] Error handling and logging consistent with standards
- [ ] Accessibility and responsive behaviour checked (UI changes)
- [ ] Performance: no N+1 queries, no unbounded lists, images optimised
- [ ] Migrations safe (locks, rollback), backward compatible
- [ ] Documentation and CHANGELOG updated
- [ ] Comments labelled **blocker**, **suggestion**, **question**, **nit**; tone respectful

## 9.5 Timesheets

| Rule | Detail |
|---|---|
| Frequency | Daily entry; weekly submission by Friday 18:00 |
| Granularity | By project and task (ticket id); 15-minute increments |
| Categories | Billable (project work), Non-billable (training, internal, sales support), Leave |
| Honesty | Record actual time - estimates are improved only by accurate actuals |
| Approval | PM approves weekly; discrepancies above 10% against the plan are explained |
| Use | Invoicing (time-and-materials), margin tracking, estimate calibration. Never used to measure keystrokes or punish honest reporting |

## 9.6 Leave and handover

Before leave of more than one day, complete and share a **handover note** with your mentor/TL:

```text
HANDOVER NOTE
Name / dates away:
Project / tickets in progress (links, state, next step):
Branches and PRs open (links, review state):
Environments touched (what is deployed where, anything fragile):
Pending client communication / deadlines:
Where the notes and credentials access are (never the credentials themselves):
Who to contact for what while I am away:
Known risks:
```

Rules: push all work; no one should need your laptop; production access is never given to "cover" a colleague - the TL arranges authorised cover.

## 9.7 Documentation rules

- Documentation lives in the repository (`docs/`) and in the shared knowledge base; **no knowledge only in chat or in one person's head**.
- Every module owner updates docs in the same PR as the code.
- Decisions are recorded as ADRs; incidents as post-incident reviews; meetings as short minutes with action owners.
- Diagrams are stored as editable source files.

## 9.8 Escalation

{{diagram: escalation | Figure 9.1 - Escalation ladder (start at L1; move up when the trigger is met)}}

| Severity | Definition | Example | Initial response (internal target - set per SLA) | Escalate to |
|---|---|---|---|---|
| **P1 Critical** | Production down, data loss or security breach | Site returns 500 for all users; leaked credentials | Immediately; page the on-call and TL | TL + PM at once; Director and client sponsor within 1 hour |
| **P2 High** | Major feature broken, no workaround | Payments failing for some users | Within 1 hour | TL, PM |
| **P3 Medium** | Feature degraded, workaround exists | Search slow | Same working day | Mentor / TL |
| **P4 Low** | Cosmetic or request | Typo, minor layout issue | Within 2 working days | Ticket owner |

**How to escalate:** state *what happened, impact, since when, what you tried, what you need*. Do not wait for certainty. Keep a timeline of actions during incidents.

!!! danger "SECURITY INCIDENTS"
    If you suspect a leaked secret, a compromised account or a data breach: **(1)** stop - do not delete evidence, **(2)** tell the TL within one hour, **(3)** revoke/rotate exposed credentials with the TL's direction, **(4)** the PM and Director decide client and legal notifications. Never discuss an incident in public channels or social media.

## 9.9 Communication and conduct

- Client communication goes through the PM or the agreed channel; developers do not agree to scope or price changes in chat.
- Write short, specific messages; summarise decisions in the ticket; confirm agreements in writing.
- Be honest about status and risk - bad news early is cheaper than bad news late.
- Respect confidentiality: no client details on public forums, AI tools or personal accounts without approval.
- Credit sources; never present copied code or text as your own; respect open-source licences.
