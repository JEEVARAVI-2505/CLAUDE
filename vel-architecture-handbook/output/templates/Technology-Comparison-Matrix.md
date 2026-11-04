# Technology Comparison Matrix

_Stage 07 weighted comparison of options_

Document VEL-SA-009 · Version 1.0 · 4 November 2026 · VEL Software Solutions · Confidential, internal use

# Appendix G: Technology Comparison Matrix

**Method:** (1) agree criteria and weights with the client **before** scoring; (2) score each option 1-5 with evidence; (3) weighted score = Σ(weight x score); (4) run sensitivity: change weights ±20% and see if the winner changes; (5) the highest score is an input to the recommendation, not the recommendation itself.

| Field | Entry |
|---|---|
| Decision to be made | __ |
| Options (min. two + baseline) | __ |
| Date, authors, reviewer | __ |

| Criterion | Weight (sum = 100) | Option A score | Option B score | Option C / baseline | Evidence / notes |
|---|---|---|---|---|---|
| Requirement fit |  |  |  |  |  |
| Security |  |  |  |  |  |
| Scalability |  |  |  |  |  |
| Availability / reliability |  |  |  |  |  |
| Maintainability and skills |  |  |  |  |  |
| Total cost (3 years) |  |  |  |  |  |
| Vendor lock-in (5 = low) |  |  |  |  |  |
| Operational complexity (5 = low) |  |  |  |  |  |
| Delivery time / risk |  |  |  |  |  |
| Ecosystem and support |  |  |  |  |  |
| **Weighted total** | **100** |  |  |  |  |

## Example scoring (training)

| Criterion | Weight | Modular monolith | Microservices | Low-code SaaS |
|---|---|---|---|---|
| Requirement fit | 20 | 4 | 5 | 3 |
| Total cost (3 years) | 20 | 4 | 2 | 4 |
| Operational complexity | 15 | 5 | 1 | 5 |
| Delivery time / risk | 15 | 4 | 2 | 5 |
| Scalability | 10 | 3 | 5 | 3 |
| Security | 10 | 4 | 4 | 3 |
| Vendor lock-in | 10 | 4 | 3 | 2 |
| **Weighted total (max 500)** | 100 | **405** | **305** | **370** |

Calculation, modular monolith: 20x4 + 20x4 + 15x5 + 15x4 + 10x3 + 10x4 + 10x4 = 80+80+75+60+30+40+40 = **405**. Microservices = **305**; low-code = **370**.

> **Reminder** — Low-code scores close to the monolith here. If the client values speed over customisation, or lock-in weight drops, it could win. State this in the recommendation.
