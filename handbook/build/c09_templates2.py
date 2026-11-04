from lib import Chapter
from c08_templates import sign


def build():
    out = []
    # ---------------- H QA
    c = Chapter("Appendix H", "QA Checklist", kind="appendix", search="qa checklist testing regression performance")
    c.h2("Test planning")
    c.check(["Test plan approved: scope, levels, environments, data, tools, entry/exit criteria, risks.", "Every FR and NFR traced to ≥ 1 test case (traceability matrix).", "Defect severity scale agreed (Critical/High/Medium/Low) with fix targets."])
    c.h2("Execution")
    c.check(["Unit tests pass; coverage target met on domain/application code (default ≥ 70 %).", "Integration tests pass against a real database container.",
             "System tests cover each end-to-end business scenario incl. exception paths (cancel, reverse, reject).", "Regression suite executed on the release candidate.",
             "Boundary, negative and invalid-input tests for every form and API.", "Permission tests: every role × sensitive action; unauthenticated access rejected.",
             "If multi-tenant: cross-tenant read/write/report/job tests pass.", "Concurrency tests: double-submit, simultaneous edits, number-series.",
             "Calculations verified against hand-calculated golden values from the client's real examples.", "Data-integrity: totals, counts and ledger balances reconcile after test scenarios.",
             "Performance test meets NFRs (p95, concurrency, report duration).", "Security scan (SAST/dependencies/secrets/container) clean of critical/high.",
             "Accessibility: keyboard-only walk-through; axe/Lighthouse results recorded.", "Cross-browser/device matrix tested as per NFR.", "Backup restore tested; log/alert samples verified.",
             "Print/PDF/export outputs match approved samples."])
    c.h2("Exit criteria for Release Candidate (G6)")
    c.check(["Zero open Critical/High defects; Medium defects have agreed workaround or date.", "All test evidence stored in `tests/` or QA tool; report signed.", "Release notes and known-issues list prepared."])
    c.table(["Test run", "Date", "Build/tag", "Total", "Passed", "Failed", "Blocked", "Tester"], [["", "", "", "", "", "", "", ""]] * 3, widths=[16, 12, 14, 10, 10, 10, 10, 18])
    sign(c, ["QA Engineer", "Technical Lead"])
    out.append(c)

    # ---------------- I UAT
    c = Chapter("Appendix I", "User Acceptance Testing (UAT) Checklist", kind="appendix", search="uat user acceptance testing checklist sign-off")
    c.h2("Preparation")
    c.check(["Release candidate deployed to **staging** with production-like configuration and realistic (masked) data.", "UAT scripts derived from acceptance criteria; each has steps, test data and expected result.",
             "UAT testers named by the client (one per role) and trained for 1–2 hours; UAT window agreed.", "Defect logging channel and triage rhythm (daily, within 24 h) agreed.", "Entry criterion: G6 passed."])
    c.h2("Execution")
    c.check(["Each script executed by a real user, not by the project team.", "Results recorded: Pass / Fail / Blocked with evidence (screenshot, record ID).", "Defects triaged: *defect* (fix now) vs *change request* (price) vs *training issue*.",
             "Fixes retested by the original tester; regression by QA.", "Business reports and printed documents checked against approved samples.", "Role permissions verified by the process owners."])
    c.h2("UAT script format")
    c.table(["ID", "Story/FR", "Role", "Steps", "Test data", "Expected result", "Actual", "Status", "Tester/date"], [
        ["UAT-001", "FR-002", "Manager", "1. Open invoice INV-D-1042  2. Click Post", "Customer C-17, limit ₹1,00,000, outstanding ₹90,000", "System asks for approval; audit row created", "", "", ""],
        ["UAT-002", "", "", "", "", "", "", "", ""]], widths=[8, 8, 8, 20, 16, 18, 8, 6, 8])
    c.h2("Exit and sign-off (G7)")
    c.check(["100 % of Must scripts passed; no open Critical/High defects.", "Accepted-defect list (Medium/Low) signed with fix dates.", "Training completion ≥ 90 % of users scheduled.", "Go-live readiness meeting held."])
    c.callout("rule", "Acceptance statement",
              "“The undersigned confirm that the delivered system has been tested against the agreed acceptance criteria in SOW <no.> and is accepted for production use, subject to the listed defects.”")
    sign(c, ["Client sponsor", "Client SPOC", "Project Manager", "Business Analyst"])
    out.append(c)

    # ---------------- J Deployment
    c = Chapter("Appendix J", "Deployment Checklist (Staging and Production)", kind="appendix", search="deployment checklist production staging rollback release")
    c.h2("Pre-deployment (T-3 to T-1 days)")
    c.check(["Change record approved; window agreed with client; stakeholders informed.", "Release candidate tagged; artifact/image digest recorded; SBOM and scans attached.",
             "Environment configuration reviewed (no secrets in repo; values in secret store); configuration diff vs staging checked.", "Database migrations rehearsed on a copy of production; duration measured.",
             "**Verified backup** taken and restore tested; backup location and time recorded.", "Rollback plan written with decision point and time limit; data-rollback approach defined.",
             "Monitoring dashboards and alerts ready; on-call contacts confirmed.", "Runbook reviewed by a second engineer."])
    c.h2("Deployment")
    c.check(["Freeze: no other changes to the environment.", "Maintenance notice shown if downtime is expected.", "Deploy via pipeline (no manual file copy).", "Run migrations; confirm version number.",
             "Smoke tests: login, one transaction per module, reports, integrations, scheduled jobs.", "Check logs/metrics for errors; compare latency/error rate to baseline."])
    c.h2("Post-deployment")
    c.check(["Client SPOC confirms smoke test on production.", "Hyper-care schedule active (24–72 h); issue channel open.", "Release notes sent; documentation updated.", "Change record closed with result and timings.", "Backups scheduled and first scheduled backup verified.", "Retrospective logged if any deviation."])
    c.h2("Rollback triggers (example)")
    c.ul(["Smoke test failure in a Must function; error rate > 2× baseline for 10 minutes; data-integrity discrepancy; security issue discovered."])
    sign(c, ["Deployer", "Approver (Technical Lead)", "Client sponsor (production)"])
    out.append(c)

    # ---------------- K Handover
    c = Chapter("Appendix K", "Client Handover Checklist", kind="appendix", search="handover checklist client closure documentation training support")
    c.h2("Deliverables")
    c.check(["Source code repository ownership/licence per SOW; branch protection and access transferred.", "Architecture document, ADRs, ERD and data dictionary.", "API documentation (OpenAPI) and integration contracts.",
             "Deployment guide, runbooks (start/stop, backup/restore, rollback, incident), monitoring guide.", "User manuals per role; admin manual; training recordings.", "Security checklist, scan report, accepted-risk register.",
             "Test evidence summary and UAT sign-off.", "Configuration list (environment variables by name — **no secrets in the document**)."])
    c.h2("Access and ownership")
    c.check(["Cloud accounts, domains, certificates and DNS in the client's name; renewal dates recorded.", "Credentials transferred via approved secret vault; our temporary access removed or converted to ticket-based support access.",
             "Admin users created for client; default/test accounts disabled.", "Third-party licences/API keys registered to the client."])
    c.h2("Knowledge transfer")
    c.check(["Admin/IT training session held and recorded.", "Super-users identified and trained.", "Support process explained: how to raise tickets, severity levels, contacts, hours.", "Known issues and roadmap shared."])
    c.h2("Commercial closure")
    c.check(["Final invoice raised per milestones.", "Warranty start and end dates recorded.", "Support/maintenance contract signed or declined in writing.", "Customer satisfaction survey sent; lessons-learned session held internally."])
    c.callout("rule", "Handover certificate", "“The client acknowledges receipt of the deliverables listed above and accepts responsibility for operating the system, subject to the warranty and support terms in SOW <no.>.”")
    sign(c, ["Client sponsor", "Client IT owner", "Project Manager", "Technical Lead", "Department Head"])
    out.append(c)

    # ---------------- L Costing spec
    c = Chapter("Appendix L", "Project Costing Model Specification", kind="appendix", search="costing model specification spreadsheet formulas inputs outputs")
    c.p("Specification for the department's estimating workbook (spreadsheet or small web tool). Implementation must reproduce the worked examples in Chapter 8 to the rupee — those are the acceptance tests.")
    c.h2("Sheets")
    c.table(["Sheet", "Purpose", "Key columns / cells"], [
        ["`Inputs`", "Global assumptions (versioned, dated).", "Cost/day by role · target margin · PM ratio · phase ratios · contingency by size · overhead/month · management fee % · support/upgrade % · tax note"],
        ["`RateCard`", "Billing rates derived from cost.", "Role · cost/day · rate/day = cost ÷ (1 − margin)"],
        ["`Modules`", "Three-point estimates.", "Module · FR range · O · M · P · expected = (O+4M+P)/6 · risk flag"],
        ["`Phases`", "Effort by lifecycle component.", "Component · basis · days · cost/day · cost"],
        ["`Integrations`", "Per interface.", "Name · complexity (3/6/12 d) · vendor · sandbox availability"],
        ["`Migration`", "Per data source.", "Source · tables · quality · days"],
        ["`Hosting&Licences`", "Quoted from **official calculators/vendor quotes**.", "Service · SKU · region · unit price · qty · months · source URL · date verified · screenshot ref"],
        ["`Summary`", "Price, margin, schedule.", "Labour · contingency · licences · hosting · direct · price · margin % · payment schedule"],
        ["`Recurring`", "Annual support, hosting, upgrades.", "Support % · hosting × 12 × (1+fee) · upgrade block"],
        ["`BreakEven`", "Department economics.", "Overhead/month · contribution per project · projects/year"],
        ["`ChangeRequests`", "CR log and pricing.", "CR ID · roles/days · assessment fee · post-freeze premium · status"],
    ], widths=[16, 30, 54])
    c.h2("Formulas (authoritative)")
    c.code("""LabourCost      = Σ ( Days[c] × CostPerDay[c] )                    over all components c incl. Project Management
PM_Days         = PM_Ratio × Σ Days[c ≠ PM]
Contingency     = LabourCost × Risk%                              (10 % small, 15 % medium, 20 % enterprise)
DirectCost      = LabourCost + Contingency + Licences + BuildHosting
Price           = DirectCost ÷ (1 − TargetMargin)                   (TargetMargin default 0.40; floor 0.30)
GrossMargin%    = (Price − DirectCost) ÷ Price
ModulePrice[m]  = (Price − SharedPrice − LicHostPrice) × DevDays[m] ÷ Σ DevDays
SharedPrice     = (IntegrationCost + MigrationCost) × (1 + Risk%) ÷ (1 − TargetMargin)
AnnualSupport   = Support% × Price ;  AnnualHosting = Monthly × 12 × (1 + MgmtFee)
BreakEven       = 12 × OverheadPerMonth ÷ (Price − DirectCost)       projects per year
ChangeRequest   = 0.5×BA_rate + Σ(days × rate) × (1 + 0.20 if after freeze)""", "text")
    c.h2("Controls")
    c.ul(["Inputs sheet is **versioned and dated**; changing a rate requires Department Head approval.", "Every price in `Hosting&Licences` carries source URL, SKU, region, date verified — no row without evidence.",
          "Technical Lead reviews module days; Finance reviews margin; both initial the Summary sheet.", "Model must fail validation if: margin < floor, contingency missing, any of the 17 cost components has zero days without a written reason.",
          "Export a PDF quote snapshot stored with the SOW; never overwrite a quoted version."])
    c.h2("Acceptance tests for the model")
    c.table(["Case", "Expected (ex-tax)"], [
        ["Small example (Ch 8)", "Price ₹6,82,567 · margin ₹2,73,027 (40 %)"],
        ["Medium example", "Price ₹27,08,833 · margin ₹10,83,533"],
        ["Enterprise example", "Price ₹93,87,033 · margin ₹37,54,813"],
        ["Change request (Medium, 1 BA + 4 Dev + 2 QA + 0.5 DB)", "₹64,333"],
    ], widths=[56, 44])
    sign(c, ["Technical Lead", "Finance", "Department Head"])
    out.append(c)

    # ---------------- M Glossary
    c = Chapter("Appendix M", "Glossary", kind="appendix", search="glossary terms acid api rbac uat sow")
    G = [("ACID", "Atomicity, Consistency, Isolation, Durability — properties of reliable transactions."), ("ADR", "Architecture Decision Record — short document of a decision, options and consequences."),
         ("API", "Application Programming Interface — contract for software to talk to software."), ("AS-IS / TO-BE", "Current process / designed future process."),
         ("Audit trail", "Append-only record of who did what and when."), ("BRD / FRS", "Business / Functional Requirements Specification."),
         ("CI/CD", "Continuous integration / delivery: automated build, test and deploy."), ("CR", "Change request after requirements freeze."),
         ("Idempotency", "Repeating an operation has the same effect as doing it once."), ("Modular monolith", "One deployable application with strict internal module boundaries."),
         ("Multi-tenancy", "One system serving multiple isolated customers (tenants)."), ("NFR", "Non-functional requirement: performance, security, availability etc."),
         ("Outbox pattern", "Write event to a table in the same transaction, publish afterwards for reliability."), ("RACI", "Responsible, Accountable, Consulted, Informed."),
         ("RBAC", "Role-based access control."), ("RPO / RTO", "Maximum acceptable data loss / maximum acceptable downtime."), ("SOW", "Statement of Work — contractual scope, price and terms."),
         ("SPOC", "Single point of contact."), ("UAT", "User acceptance testing by the client."), ("WCAG", "Web Content Accessibility Guidelines.")]
    c.table(["Term", "Meaning"], [[f"**{a}**", b] for a, b in G], widths=[22, 78])
    out.append(c)
    return out
