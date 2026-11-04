from lib import Chapter


def sign(c, roles):
    c.h2("Approval")
    c.table(["Role", "Name", "Signature", "Date"], [[r, "", "", ""] for r in roles], widths=[30, 28, 28, 14])


def build():
    out = []
    # ---------------- C BRD
    c = Chapter("Appendix C", "Business Requirements Template (BRD)", kind="appendix", search="brd business requirements template")
    c.table(["Document control", ""], [["Project / client", ""], ["Document ID / version", "BRD-<PROJECT>-v0.1"], ["Author / reviewer", ""], ["Status", "Draft / Review / Approved"], ["Date", ""]], widths=[30, 70])
    for h, body in [
        ("1. Executive summary", ["Business problem in 3–5 sentences.", "Objectives and **measurable success criteria** (e.g. invoice time from 15 → 3 min; stock accuracy ≥ 98 %)."]),
        ("2. Business context", ["Organisation, locations, departments, headcount, transaction volumes.", "Current systems and tools; pain points with evidence."]),
        ("3. Scope", ["**In scope** (modules/processes).", "**Out of scope** (explicit).", "Assumptions · Constraints · Dependencies."]),
        ("4. Stakeholders", ["Sponsor, decision-maker, process owners, users, IT, finance, external parties (link register T04)."]),
        ("5. Process summary", ["AS-IS and TO-BE diagrams for each process (attach); gap list; expected benefit."]),
        ("6. High-level business requirements", ["BR-001… each with business owner, priority (Must/Should/Could/Won't), rationale, success measure."]),
        ("7. Business rules and calculations", ["BRL-001… with worked examples from real data."]),
        ("8. Reporting needs", ["Report name, audience, frequency, data source, sample."]),
        ("9. Risks and issues", ["Risk, likelihood, impact, mitigation, owner."]),
        ("10. Compliance & data considerations", ["Personal/sensitive data handled; retention; consent; regulatory areas to consider with the client's own legal advisers."]),
        ("11. Glossary and references", ["Terms used by the client; documents consulted."]),
    ]:
        c.h2(h)
        c.ul(body)
    sign(c, ["Client sponsor", "Business Analyst", "Technical Lead", "Department Head"])
    out.append(c)

    # ---------------- D FRD
    c = Chapter("Appendix D", "Functional Requirements Template (FRD / FRS)", kind="appendix", search="frd frs functional requirements template user story acceptance criteria")
    c.table(["Document control", ""], [["Project", ""], ["Baseline version", "FRS-v1.0 (after G2)"], ["Traceability", "BR → FR → Design → Test"]], widths=[30, 70])
    c.h2("1. Module overview")
    c.p("One section per module: purpose, actors, preconditions, main flow, alternate/exception flows, postconditions.")
    c.h2("2. Requirement table")
    c.table(["ID", "Module", "Requirement (shall …)", "Priority", "Source (BR / interview)", "Acceptance criteria (Given/When/Then)", "Test ID"], [
        ["FR-001", "Sales", "The system shall allow a Sales user to create a draft invoice for an active customer.", "Must", "BR-004", "Given an active customer, when the user saves lines, then a draft invoice with number blank is stored.", "TC-001"],
        ["FR-002", "Sales", "The system shall prevent posting if the credit limit would be exceeded unless a Manager approves.", "Must", "BRL-002", "Given outstanding ₹90,000 and limit ₹1,00,000, when posting ₹20,000, then approval is required.", "TC-002"],
        ["FR-003", "All", "The system shall record an audit entry for each approval.", "Must", "NFR-AUD-1", "Given an approval, then audit row has user, time, old/new status.", "TC-003"],
    ], widths=[7, 8, 28, 7, 10, 32, 8])
    c.h2("3. User-story format")
    c.code("""As a <role>
I want <capability>
So that <business benefit>

Acceptance criteria
  Given <context>  When <action>  Then <observable result>
Priority: Must/Should/Could   Estimate: <days>   Depends on: <IDs>""", "text")
    c.h2("4. Non-functional requirements (each measurable)")
    c.table(["ID", "Category", "Requirement", "Measure / test"], [
        ["NFR-PERF-1", "Performance", "List screens respond for 95 % of requests", "p95 < 500 ms at 50 concurrent users (load test)"],
        ["NFR-AVAIL-1", "Availability", "Business-hours availability", "≥ 99.5 % monthly (monitoring report)"],
        ["NFR-SEC-1", "Security", "No critical/high findings at release", "Scan + manual review"],
        ["NFR-BKP-1", "Backup", "RPO 24 h (or as agreed), RTO 4 h", "Quarterly restore drill"],
        ["NFR-ACC-1", "Accessibility", "WCAG 2.2 AA target for staff screens", "axe + keyboard test"],
        ["NFR-AUD-1", "Audit", "Append-only audit of create/update/delete/approve", "Audit tests"],
    ], widths=[14, 14, 40, 32])
    c.h2("5. Input/output specification")
    c.table(["Field", "Type", "Length", "Mandatory", "Validation", "Source/Default"], [["customer_name", "text", "150", "Yes", "trim; no control chars", "—"], ["invoice_date", "date", "—", "Yes", "≤ today + 0; within open period", "today"]], widths=[20, 12, 10, 12, 28, 18])
    c.h2("6. Roles and permissions matrix")
    c.table(["Action", "Clerk", "Manager", "Accountant", "Admin", "Auditor"], [["Create invoice", "✔", "✔", "—", "—", "—"], ["Approve invoice", "—", "✔", "—", "—", "—"], ["View audit log", "—", "—", "—", "✔", "✔"]], widths=[32, 12, 14, 14, 14, 14])
    sign(c, ["Client SPOC", "Business Analyst", "Technical Lead"])
    out.append(c)

    # ---------------- E SOW
    c = Chapter("Appendix E", "Statement of Work (SOW) Template", kind="appendix", search="sow statement of work template contract commercial")
    c.callout("warn", "Legal review", "This template is a business structure, not legal advice. Legal counsel must review the final contract wording before signature.")
    for h, items in [
        ("1. Parties and project", ["Client legal name/address; VEL Software Solutions; project name; SOW number; effective date."]),
        ("2. Background and objectives", ["Reference the signed BRD/FRS baseline (version, date)."]),
        ("3. Scope of services", ["Modules and features by FR ID range.", "Environments (dev/staging/production) and number of users/branches.", "Integrations listed by name and direction."]),
        ("4. Deliverables", ["Software, source code (per IP clause), documentation, user manuals, training, handover pack."]),
        ("5. Out of scope", ["Anything not listed in Section 3, including hardware, third-party licences, data entry/cleansing beyond the agreed allowance, regulatory certification."]),
        ("6. Assumptions and client responsibilities", ["Timely access to stakeholders and data; named SPOC; 5-working-day feedback SLA; client-owned licences/accounts; test data availability."]),
        ("7. Timeline and milestones", ["Milestone table with dates tied to gates G2–G9; delay caused by late client input extends the schedule day-for-day."]),
        ("8. Commercials", ["Price (ex-tax) by module; payment schedule (default 30/30/30/10 at G3/G4/G7/G9); recurring hosting/support fees; taxes; invoice terms; late-payment terms."]),
        ("9. Acceptance", ["UAT procedure, criteria (Ch 5 Stage 23), deemed acceptance after N working days of no response or on production use."]),
        ("10. Change control", ["Written change requests priced per Ch 8; no work on changes without approval."]),
        ("11. Warranty and support", ["Defect warranty period; severity definitions P1–P4 and response/resolution targets; support hours; exclusions."]),
        ("12. Intellectual property and confidentiality", ["Ownership of bespoke code, retained rights in reusable components and tools, licence to client, NDA."]),
        ("13. Data protection and security", ["Roles (controller/processor), data handled, security measures summary, breach notification process, data return/deletion at exit. No guarantee of absolute security or certification is given."]),
        ("14. Limitation of liability, termination, governing law", ["As approved by Legal."]),
    ]:
        c.h2(h)
        c.ul(items)
    sign(c, ["Client authorised signatory", "VEL authorised signatory (Department Head/Director)"])
    out.append(c)

    # ---------------- F DB checklist
    c = Chapter("Appendix F", "Database Design Checklist", kind="appendix", search="database design checklist normalisation index migration backup")
    for h, items in [
        ("Modelling", ["Entities and relationships trace to FR IDs.", "3NF unless a documented, measured reason to denormalise.", "Naming convention applied (snake_case, singular tables, `_id` keys).", "Master vs transaction vs reference tables separated."]),
        ("Integrity", ["Every table has a primary key.", "Foreign keys exist for every relationship (with defined ON DELETE behaviour).", "UNIQUE constraints for natural keys (e.g. invoice number per period).", "CHECK constraints for ranges/statuses; NOT NULL by default.", "Money in `numeric/decimal(14,2)`, never float; timestamps in UTC (`timestamptz`/`datetime2`)."]),
        ("Concurrency & transactions", ["Transaction boundary per use case documented.", "Row-version/optimistic concurrency on editable records.", "Gap-free number series use a locked counter row, not `MAX()+1`."]),
        ("Performance", ["Indexes on FKs and common filters; each justified by a query plan.", "No `SELECT *` in application code; pagination on all list queries.", "Reporting queries tested with production-scale data."]),
        ("Security", ["Least-privilege DB accounts (app ≠ migration ≠ admin).", "Sensitive columns identified; encryption at rest/in transit; masking in non-production.", "Audit table append-only (no UPDATE/DELETE grants).", "If multi-tenant is *explicitly required*: tenant_id + isolation tests."]),
        ("Migrations", ["Forward-only versioned scripts; applied scripts never edited.", "Rehearsed on production-sized copy; lock duration measured.", "Rollback = restore or compensating migration documented."]),
        ("Backup & recovery", ["Backup schedule and retention documented; off-site encrypted copy.", "RPO/RTO agreed with client.", "**Restore tested** and recorded (date, duration, result)."]),
        ("Documentation", ["ERD current; data dictionary lists each column's meaning, type and source."]),
    ]:
        c.h2(h)
        c.check(items)
    sign(c, ["Database Engineer", "Technical Lead"])
    out.append(c)

    # ---------------- G security & review
    c = Chapter("Appendix G", "Code Review and Security Checklist", kind="appendix", search="code review security checklist owasp pull request")
    c.h2("Pull-request review")
    c.check(["Linked to a story/defect; scope is small and focused.", "Tests added/updated and meaningful; CI green.", "No business logic in controllers/UI; layering respected.",
             "Validation at edge, domain and DB where relevant.", "Errors handled; no swallowed exceptions; safe messages to clients.", "Logging has correlation ID and **no secrets/PII**.",
             "Docs/ADR/OpenAPI updated.", "Migrations reviewed by Database Engineer."])
    c.h2("Security (OWASP Top 10 aligned)")
    c.check(["Access control: server-side checks at endpoint and object level (A01).", "Cryptography: TLS everywhere; passwords hashed with a modern adaptive algorithm (Argon2id/bcrypt/PBKDF2); keys in a secret store (A02).",
             "Injection: parameterised queries; encoded output (A03).", "Insecure design: threat model and abuse cases exist (A04).", "Misconfiguration: debug off, headers set, default accounts removed, CORS allow-list (A05).",
             "Vulnerable components: dependency/SBOM scan and update plan (A06).", "AuthN failures: MFA option, lock-out, session expiry, no credential stuffing paths (A07).",
             "Integrity: signed builds/pinned actions; no unsafe deserialisation (A08).", "Logging & monitoring: security events logged and alerted (A09).", "SSRF: outbound allow-list for URL fetchers (A10).",
             "Secret scan passes; no secrets in repo history.", "File upload rules (Ch 7) applied."])
    sign(c, ["Reviewer", "Technical Lead"])
    out.append(c)
    return out
