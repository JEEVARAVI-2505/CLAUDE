Every client project - whether a one-page site or a SaaS platform - follows these **27 stages in order**. Small projects run stages quickly and combine documents; they never skip a gate. For each stage this section defines the **purpose, responsible employee, inputs, procedure, outputs, tools, risks, verification, documentation and approval authority**.

{{diagram: lifecycle | Figure 5.1 - The 27-stage client project lifecycle}}

## Phases and gates

| Phase | Stages | Main output | Key gate |
|---|---|---|---|
| 1 - Win and define | 1-6 | Signed SOW, frozen scope | **Gate 05:** SOW approved - work may start |
| 2 - Design | 7-10 | Sitemap, approved UI/UX, architecture, database design | **Gate 08:** design approved; **Gate 10:** DB design review |
| 3 - Build | 11-18 | Working, integrated product in staging | Code review on every pull request |
| 4 - Verify and release | 19-25 | Tested, secured, deployed, live with HTTPS | **Gate 22:** UAT sign-off before production |
| 5 - Operate | 26-27 | Handover, maintenance, support | **Gate 26:** signed handover certificate |

## Role abbreviations used in the stage tables

| Abbreviation | Role |
|---|---|
| PM | Project Manager |
| TL | Technical Lead |
| BD | Business Development executive |
| UX | UI/UX Designer |
| FE / BE / FS | Frontend / Backend / Full-stack developer |
| QA | QA engineer |
| DO | DevOps engineer |

## Approval authority matrix

| Decision | Approves | Informed |
|---|---|---|
| Accept the lead | PM | BD, TL |
| Scope and SOW | Authorised commercial representative + client signatory | PM, TL |
| Design | Client sponsor | UX, TL, FE |
| Architecture and database design | TL | PM, DO, client IT |
| Merge to `main` | A reviewer who is **not** the author (TL for sensitive modules) | Team |
| Release to staging | TL | PM, QA |
| UAT acceptance | Client sponsor | PM, QA |
| Production deployment (go/no-go) | TL | PM, client |
| Handover | Client sponsor | PM, TL, DO |
| Change request | PM (impact) + client sponsor (cost/time) | TL |

!!! gate "DO NOT START IMPLEMENTATION"
    without (1) an approved scope and SOW, (2) agreed commercial terms and (3) written access authorisation. Access is granted to named individuals with individual accounts - never shared admin credentials.

## Change-request procedure (applies from Stage 6 onwards)

1. The client or a team member raises the change **in writing** (ticket or e-mail).
2. The PM and TL assess impact on cost, time, risk and other features.
3. The client approves the revised quote and timeline.
4. An SOW addendum is signed **before** work begins; the change log is updated.

## Stage definitions

{{stages: stages}}
