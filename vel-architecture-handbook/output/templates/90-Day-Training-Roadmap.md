# 90-Day Training Roadmap

_Thirteen weeks from fundamentals to a full architecture proposal_

Document VEL-SA-003 · Version 1.0 · 4 November 2026 · VEL Software Solutions · Confidential, internal use

# 90-Day Training Roadmap

Thirteen weeks, one gate every four weeks. Study about 50% and practise about 50% in the first month; move toward supervised real work in the last month.

| Week | Focus | Deliverable | Assessment / gate |
|---|---|---|---|
| 1 | Onboarding, tools, lifecycle overview | Setup done; first diagram started | Setup checklist |
| 2 | Level 1: software, networks, cloud, security basics | Notes; Postman collection; DNS trace | Oral quiz |
| 3 | Level 2: stakeholders, interviews, objectives | Stakeholder map; mock interview | Mentor review |
| 4 | Level 2: requirements, NFRs, acceptance, risks | Requirements for GreenLeaf | Project 1-2 reviewed; Gate: Levels 1-2 |
| 5 | Level 3: architecture vs design; components; coupling | Classification table | Mentor review |
| 6 | Level 3: layered, modular monolith, microservices, events | Project 4 draft | Peer review |
| 7 | Level 4: patterns (Chapter 4) | Pattern "when NOT to use" notes | Quiz |
| 8 | Level 4 continued; ADRs | Project 7 (ADR) | Gate: Levels 3-4 |
| 9 | Level 5: application, database, API | Projects 3 and 5 | Review |
| 10 | Level 5: cloud, network, security, DR | Project 6 | Security Architect review |
| 11 | Level 6: evaluation; cost; feasibility | Project 8 | Gate: Levels 5-6 |
| 12 | Level 7: governance; shadow a real engagement | Governance notes; shadow log | Mentor |
| 13 | Capstone: Project 9 proposal and presentation | Full proposal | ARB-style panel; final sign-off |

## Scorecard

| Dimension | Evidence | Rating (1-5) |
|---|---|---|
| Fundamentals | Quizzes, notes |  |
| Analysis quality | Requirements, interview write-ups |  |
| Design reasoning | Options, ADRs |  |
| Diagrams | Clarity and correctness |  |
| Communication | Presentation, emails |  |
| Professionalism | Honesty about assumptions, confidentiality |  |

# Practical Training Projects

Nine projects, each reviewed by a senior and signed in the trainee log. Use fictional or fully anonymised clients only.

| # | Project | Deliverable | Acceptance |
|---|---|---|---|
| 1 | Basic three-tier application | Diagram + 1-paragraph explanation | Rubric in Chapter 11 ≥ 75 |
| 2 | C4 system context diagram | Context diagram + interface list | Actors/systems complete; owner confirms |
| 3 | Database architecture | ERD, data dictionary, backup and retention plan, RPO/RTO | Normalised; keys defined; restore test described |
| 4 | Monolith vs microservices | Comparison matrix + recommendation + ADR | Criteria weighted before scoring; trade-offs honest |
| 5 | REST API architecture | OpenAPI skeleton, versioning, auth, error model, rate limit policy | Valid OpenAPI; consistent naming; secured |
| 6 | Cloud-hosted application | Deployment + network diagram, cost range with calculator date, DR plan | No single point of failure unaccepted; costs sourced |
| 7 | Architecture Decision Record | Complete ADR with 2+ options | Reviewer can understand it cold |
| 8 | Technical feasibility assessment | Report (Appendix F) with conclusion and conditions | Evidence per dimension; assumptions labelled |
| 9 | Full architecture proposal for a simulated client | Repository with all 20 deliverables (Chapter 8) + 20-minute presentation | ARB-style review passed |

## Project 9 assessment: full proposal

| Area | Weight |
|---|---|
| Requirements quality and traceability | 15% |
| Options and decision quality | 20% |
| Target architecture and diagrams | 20% |
| Security and operations | 15% |
| Cost and risk analysis | 10% |
| Documentation and repository hygiene | 10% |
| Presentation in business language | 10% |

> **Certification of competence** — Completion of the nine projects plus Levels 1-7 qualifies the trainee as **Junior Technical Analyst (cleared)**. Promotion to Solution Architect additionally needs two supervised real engagements and a Principal Architect sign-off.
