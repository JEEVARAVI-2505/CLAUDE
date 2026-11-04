# QA Sign-off Checklist

Complete before every release recommendation (Stage 14). Reference evidence for each tick.

## Scope and plan

- [ ] Approved test strategy and plan are in the repository, versioned.
- [ ] Scope changes since approval are recorded and approved.
- [ ] Entry criteria were met before execution began (Gate G6).

## Execution

- [ ] All planned critical cases executed; counts reconcile (passed + failed + blocked + not run = planned).
- [ ] Every result has evidence; evidence contains no secrets or personal data.
- [ ] Regression exit criteria met (Gate G12).

## Defects

- [ ] All Severity 1 and 2 defects are fixed and verified, or waived in writing by the product owner.
- [ ] Every deferred defect is in the risk summary with an owner.
- [ ] No defect was closed without a verified retest.

## Coverage and automation

- [ ] Requirement coverage computed and uncovered requirements listed.
- [ ] Automation results attached; flaky tests reported separately.
- [ ] Known defects marked as expected failures reference defect IDs.

## Non-functional

- [ ] Performance report approved, with limitations stated (or explicitly out of scope).
- [ ] Accessibility findings listed against WCAG criteria (or out of scope).
- [ ] Security observations handed to the named coordinator.

## UAT and release

- [ ] UAT acceptance signed (or not required by contract).
- [ ] Production smoke list, accounts, window and rollback decision-maker agreed.
- [ ] Second reviewer checked numbers against raw data.

## Records

- [ ] Metrics have formulas, dates, builds and sources.
- [ ] Recommendation is Go / Conditional Go / No-Go and follows from the evidence.
- [ ] Evidence is archived per the retention rule; no real customer data exists in the repository.

## Approvals

| Role | Name | Signature | Date | Decision |
|---|---|---|---|---|
| QA Lead | | | | |
| Second reviewer (Test Manager) | | | | |
| Client release owner | | | | |
