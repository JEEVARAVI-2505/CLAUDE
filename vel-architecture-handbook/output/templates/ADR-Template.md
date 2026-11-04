# Architecture Decision Record Template

_One decision, its options, reasons and consequences_

Document VEL-SA-007 · Version 1.0 · 4 November 2026 · VEL Software Solutions · Confidential, internal use

# Appendix D: Architecture Decision Record Template

Copy to `adr/NNNN-short-title.md`. One decision per record. Accepted ADRs are never edited; supersede them.

```markdown
# ADR-NNNN: <short noun phrase describing the decision>

Status: Proposed | Accepted | Deprecated | Superseded by ADR-XXXX
Date: YYYY-MM-DD
Deciders: <names / roles>     Reviewer: <name>     Impact: Low | Medium | High

## Context
<The problem, forces and constraints. Link to requirement IDs. Mark facts CONFIRMED / ASSUMED / UNKNOWN.>

## Options considered
1. <Option A>: summary, pros, cons, cost, risk
2. <Option B>: summary, pros, cons, cost, risk
3. <Do nothing / baseline>

## Decision
We will <choice>, because <reason tied to requirements and trade-offs>.

## Consequences
Positive: ...
Negative / risks: ...
Follow-up actions and owners: ...
Review trigger: <measurable condition that means we should revisit>

## Compliance and links
Related requirements: REQ-..., NFR-...   Related ADRs: ...   Evidence: <links>
```

## Quality checklist

- [ ] Two or more real options plus baseline
- [ ] Trade-offs include downsides
- [ ] Linked to requirements
- [ ] Assumptions labelled
- [ ] Review trigger defined
- [ ] Reviewed by a second person; approved by authority matching the impact level
