# Templates — copy a block into a new file; never edit this file for a project

## T1. PRD (Product Requirements Document)
```
# PRD: <product> — v<semver> — Status: DRAFT|APPROVED — Owner: <PM> — Date
1 Target user · 2 Business problem (baseline number) · 3 AI use case (why not rules?)
4 User journeys (happy/failure/hand-off) · 5 Inputs · 6 Outputs (schema) · 7 Knowledge sources
8 AI capabilities · 9 Tools (name, risk class) · 10 Permissions · 11 Human approval
12 Data privacy (categories, residency, retention, deletion) · 13 Performance · 14 Accuracy thresholds
15 Cost limits · 16 Usage limits · 17 Integrations
Out of scope: …
Requirements table: ID | Statement (testable) | Priority | Test ID
Open questions: …
Approvals: PM / Architect / QA / Security / Customer — date
```

## T2. Agent specification
```
Agent ID / version · Purpose · Instructions (prompt id+version) · Model tier + pinned versions
Input schema · Output schema · Tools (name, args, risk class) · Permissions (roles, data scope)
Memory (which stores) · Max steps · Timeout · Token budget · Error handling · Human-approval conditions
Eval suite & latest result · Owner · Reviewer · Change history
```

## T3. Prompt file (`src/prompts/<id>/<version>.yaml`)
```yaml
id: <id>
version: 0.1.0
status: draft            # draft|review|approved|production|deprecated
author: <name>
reviewer: <name>
model_tier: balanced
sections: {role: , objective: , context: , allowed_inputs: , instructions: , tool_restrictions: ,
           output_schema: , quality: , failure_handling: , safety: }
eval: {dataset: golden-v3, report: docs/evaluation/<file>.md, passed: false}
changelog: [{date: , change: , by: }]
```

## T4. ADR (Architecture Decision Record, `docs/adr/NNNN-title.md`)
```
# ADR-NNNN <decision> — Status: proposed|accepted|superseded — Date — Deciders
Context · Options considered (with pros/cons and sources) · Decision · Consequences · Review date
Prices/licences verified on: <date> from <URL>
```

## T5. Tool specification
```
Name · Purpose · Args schema · Returns schema · Risk class A|B|C|D · Roles allowed · Rate limit
Timeout · Idempotent? · Approval rule · Failure modes · Security notes · Tests
```

## T6. Evaluation report
```
Candidate (git SHA, prompt versions, model versions, index version) · Dataset versions
Metrics table: metric | threshold | result | pass? · Per-slice results · Failure analysis
Human review sample (n, raters, kappa) · Security results · Cost/latency · Known limitations
Decision: GO | NO-GO | GO-with-conditions — signatures
```

## T7. Customer interview script (30 min)
```
1 Tell me about your role. 2 Describe the last time you needed information/answers and couldn't find them.
3 What did you do? How long did it take? 4 What does it cost you (time, money, mistakes)?
5 What tools do you use today? What do you dislike? 6 Who decides and who pays for tools like this?
7 What would make you trust or distrust an AI answer? 8 What data can never leave your company?
9 If this existed at $X/month would you pilot it? What would stop you? 
Rules: do not pitch; ask for specifics and stories; record exact quotes.
```

## T8. Pull-request template (`.github/PULL_REQUEST_TEMPLATE.md`)
```
## What & why (ticket/requirement ID)
## How tested (tests added, eval run attached)
## AI impact (prompt/model/retrieval changed? metrics before→after)
## Security & privacy (secrets? personal data? permissions? new tools?)
## Screenshots / traces
- [ ] Tests pass  - [ ] Docs updated  - [ ] No secrets  - [ ] Reviewer assigned
```

## T9. Change request
```
ID · Requested by · Description · Reason · Impact (scope, cost $, time, risk, accuracy) · Alternatives
Decision (PM + Head of AI Eng) · Date · Linked ADR/PRD version
```

## T10. Incident post-mortem
```
Summary · Severity · Timeline (UTC) · Impact (customers, data) · Root cause · What went well/badly
Action items (owner, due) · Customer communication · Lessons for runbooks
```

## T11. Monthly cost review
```
Month · Actual AI spend vs model · Actual infra vs model · Cost/answer · Cost/successful task
Top tenants by cost · Tenants over budget · Provider price changes · Actions
```
