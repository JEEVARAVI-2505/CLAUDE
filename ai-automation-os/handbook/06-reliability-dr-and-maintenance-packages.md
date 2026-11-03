# Part 6 — Reliability, Disaster Recovery, Maintenance Packages and Commercial Terms

## 6.1 Disaster recovery (DR) in plain words

A disaster is anything that takes the system or its data away: a server failure, a deleted database, a cloud-region outage, a ransomware attack, a mistaken command. DR is the pre-agreed plan to get back.

- **RPO (recovery point objective):** the most data we can afford to lose, measured in time. RPO of 24 hours means we may lose up to a day of work.
- **RTO (recovery time objective):** the longest we can be down before it hurts.

| Production model | Proposed RPO | Proposed RTO | What must exist to meet it |
|---|---|---|---|
| Lean MVP | 24 hours | 1 working day | Nightly encrypted backups kept off-server; written restore steps |
| Professional | 4 hours | 8 hours | Managed database with point-in-time recovery or frequent backups; infrastructure scripts to rebuild; monitored backups |
| Enterprise | 15 minutes | 2 hours | Database replication, standby environment in a second location, automated failover, tested runbooks |

These are **design targets to agree with the customer**; each needs the matching infrastructure and cost. Never promise an RPO or RTO the setup cannot meet.

### DR procedure (write it as a runbook)
1. **Detect:** monitoring alert or customer report; declare an incident; name an incident lead.
2. **Contain:** stop the damage (disable affected workflows, rotate exposed secrets).
3. **Communicate:** tell the PM and the customer what happened, the impact and the next update time.
4. **Recover:** rebuild infrastructure from scripts; restore the latest good backup; replay queued events using idempotency keys so nothing is processed twice.
5. **Verify:** run smoke tests and compare record counts with the last known good state.
6. **Review:** within one week hold a blameless review; update the runbook and tests.

### DR checklist (quarterly)
- [ ] Backups run daily and alert on failure.
- [ ] A backup was restored on a clean server this quarter; time taken recorded.
- [ ] Encryption keys (including the workflow engine's key) are backed up separately and tested.
- [ ] Infrastructure can be rebuilt from the repository scripts.
- [ ] Contact list and escalation path are current.
- [ ] Spending limits and rate limits survive a rebuild.

## 6.2 Reliability controls (summary)

Retries with growing waits · idempotency keys on every external action · rate-limit handling · dead-letter queue reviewed daily · timeouts on every call · health checks · alerts to a named person · versioned workflows and prompts · rollback to the previous version.

## 6.3 Maintenance packages

Three monthly packages. The hours are proposals; **prices come from the calculator** (`maintenance_package_price_inr`) = `(hours × loaded hourly cost) ÷ (1 − target margin)`, using placeholder labour rates until you enter real ones.

| | **Basic** | **Professional** | **Enterprise** |
|---|---|---|---|
| Included hours per month | 6 (2 developer + 4 support) | 14 (6 developer + 8 support) | 40 (16 developer + 20 support + 4 architect) |
| Computed monthly price (placeholder rates) | ₹4,615 | ₹11,385 | ₹36,923 |
| Support hours | Mon–Fri, 10:00–18:00 IST | Mon–Fri extended hours | Extended hours plus on-call for P1 |
| Channels | Email/ticket | Email/ticket + chat | Email/ticket + chat + phone |
| P1 response target | 8 business hours | 4 business hours | 1 hour |
| P2 response target | 2 business days | 1 business day | 4 business hours |
| P3 | Planned, within 5 business days | Within 3 business days | Within 2 business days |
| Daily execution monitoring | Automated alerts | Automated + daily check | Daily check + weekly review call |
| Weekly failure review | — | Included | Included |
| Monthly report | Usage and cost summary | + quality metrics and cost vs plan | + review call and improvement plan |
| Prompt/workflow updates | By change request | Minor tweaks included | Included within hours, plus quarterly review |
| API and security updates | Critical only | Included | Included, tested on staging first |
| Backup verification | Quarterly | Monthly | Monthly + restore drill |
| Performance optimisation | — | Annual | Quarterly |
| Extra work | Billed at the extra-hour rate | Same | Same, priority scheduling |

**Extra-work rate (placeholder):** developer hourly cost ÷ (1 − target margin) ≈ ₹1,077 per hour, billed in 1-hour minimum blocks and only after a written estimate is approved.

**Escalation path:** L1 Customer Success → L2 Developer → L3 Architect/AI Engineer → Technical Lead → Founder. Time limits for moving up are set by severity (for example a P1 not resolved or without a plan in 1 hour goes straight to the Technical Lead).

**Important pricing check:** at a 35% margin the Professional retainer (₹11,385) is about 76% of the ₹14,999 Professional subscription. A subscription tier can therefore only include this much support if support is pooled across many customers or the subscription price is higher. Decide this at Gate G9 and show the working in the price sheet.

## 6.4 What a proposal and agreement must state

**Include:** scope (the frozen specification); what is **implementation** (one-time) and what is **recurring**; usage limits and overage prices; the maintenance package; response targets; customer responsibilities; assumptions; payment milestones; term and renewal; data ownership (customer owns their data and outputs); platform, code and prompt ownership (VEL retains unless the contract transfers it); licensing of third-party platforms; sub-processors (including the AI provider); confidentiality; limitation of liability; termination and data return/deletion; governing law. **A lawyer reviews every contract template.**

**Exclusions to list in every SOW:** work outside the frozen scope; third-party price changes; customer data-quality problems; changes in a third party's API that need rework (quoted separately); training beyond the agreed sessions; hardware; regulatory advice.

**Statements we never make:**

- "100% accurate" or any **guaranteed AI accuracy**. We state measured accuracy on a named test set and the review process that catches errors.
- "Unlimited" usage. Every plan has defined limits.
- That the AI "understands" a business or "replaces" a person. We say it **assists** and that people approve consequential actions.
- That time saved equals cash saved. We show realisable savings with stated assumptions.
- That the system is "secure" without evidence. We state what was tested.

## 6.5 Project approval history

Every project keeps `00-admin/approvals.md`: date, gate, decision, approver, evidence links, conditions. This, together with the project record (owner, budget, deadline, scope version, hours, production cost, selling price, expected margin, stage, risks) is the audit trail the Founder uses to see exactly what was agreed and when.

---

*Developed by **V. LOKESHWARAN** · VEL Software Solutions · velsoftwaresolutions.in@gmail.com*
