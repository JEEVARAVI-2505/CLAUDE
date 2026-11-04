# Architecture Review Checklist

_Review standards for Stages 08-12 and 14_

Document VEL-SA-010 · Version 1.0 · 4 November 2026 · VEL Software Solutions · Confidential, internal use

# Appendix J: Architecture Review Checklist

| Field | Entry |
|---|---|
| Project / document under review | __ |
| Reviewer (not the author) | __ |
| Date | __ |
| Outcome: Approved / Approved with conditions / Rejected | __ |

## Requirement coverage

- [ ] Every MUST requirement traced to design
- [ ] NFRs quantified
- [ ] Acceptance criteria exist

## Architecture consistency

- [ ] Diagrams, ADRs and specs agree
- [ ] Terminology consistent
- [ ] C4 levels not mixed

## Security boundaries

- [ ] Trust boundaries drawn
- [ ] Authentication and authorisation designed
- [ ] Secrets management defined
- [ ] Threat model reviewed

## Data protection

- [ ] Data classified
- [ ] Encryption in transit and at rest decided
- [ ] Retention and deletion defined
- [ ] Privacy owner consulted

## Scalability and performance

- [ ] Load model and targets
- [ ] Bottleneck identified
- [ ] Test plan

## Availability and failure handling

- [ ] Single points of failure removed or accepted
- [ ] Timeouts, retries, idempotency
- [ ] Graceful degradation

## Observability

- [ ] Logs, metrics, traces
- [ ] Alerts with owners
- [ ] Dashboards

## Backup and recovery

- [ ] RPO/RTO defined
- [ ] Backups separate from primary
- [ ] Restore tested or test planned

## Operational complexity

- [ ] Client can operate it
- [ ] Runbooks planned
- [ ] Support model defined

## Cost

- [ ] Estimate has sources and dates
- [ ] Independent review
- [ ] Cost-control measures

## Maintainability and vendor dependency

- [ ] Module boundaries clear
- [ ] Lock-in assessed with exit plan
- [ ] Technical debt visible

## Implementation feasibility

- [ ] Skills available
- [ ] Timeline realistic
- [ ] Dependencies identified

## Findings log

| # | Finding | Severity (Critical/High/Med/Low) | Owner | Due | Resolution |
|---|---|---|---|---|---|
|  |  |  |  |  |  |
|  |  |  |  |  |  |
|  |  |  |  |  |  |
|  |  |  |  |  |  |
|  |  |  |  |  |  |
|  |  |  |  |  |  |

> **Rule** — No approval while a Critical or unaccepted High finding is open. Document accepted risks with the sponsor's name and date.
