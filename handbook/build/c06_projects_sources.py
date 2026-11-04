from lib import Chapter

SOURCES = [
    # (group, label, url, used for)
    ("Microsoft / .NET", ".NET documentation", "https://learn.microsoft.com/dotnet/", "C#, .NET 8, EF Core, WPF entry point"),
    ("Microsoft / .NET", "ASP.NET Core documentation", "https://learn.microsoft.com/aspnet/core/", "APIs, authentication/authorisation, middleware, health checks"),
    ("Microsoft / .NET", "SQL Server documentation", "https://learn.microsoft.com/sql/", "T-SQL, indexes, backup/restore, security"),
    ("Microsoft / .NET", "WPF documentation", "https://learn.microsoft.com/dotnet/desktop/wpf/", "Windows desktop applications"),
    ("Microsoft / .NET", "Azure documentation", "https://learn.microsoft.com/azure/", "Azure hosting and services"),
    ("Databases", "PostgreSQL documentation", "https://www.postgresql.org/docs/", "SQL, transactions, indexes, row-level security, backup"),
    ("Databases", "MySQL reference manual", "https://dev.mysql.com/doc/", "MySQL / InnoDB behaviour"),
    ("Databases", "Redis documentation", "https://redis.io/docs/", "Caching, sessions, rate limiting"),
    ("Databases", "RabbitMQ documentation", "https://www.rabbitmq.com/docs", "Queues, exchanges, reliability"),
    ("Front-end & Python", "React — Learn", "https://react.dev/learn", "Components, state, forms"),
    ("Front-end & Python", "TypeScript documentation", "https://www.typescriptlang.org/docs/", "Typed front-end code"),
    ("Front-end & Python", "Angular documentation", "https://angular.dev/", "Angular option"),
    ("Front-end & Python", "FastAPI documentation", "https://fastapi.tiangolo.com/", "Python APIs, validation, OpenAPI"),
    ("Architecture & security", "Martin Fowler — articles & catalogue", "https://martinfowler.com/", "Patterns, modular monolith, microservice trade-offs"),
    ("Architecture & security", "Patterns of Enterprise Application Architecture catalogue", "https://martinfowler.com/eaaCatalog/", "Unit of Work, Repository, Domain Model"),
    ("Architecture & security", "OWASP", "https://owasp.org/", "Top 10, ASVS, Cheat Sheet Series"),
    ("Architecture & security", "OWASP Top 10", "https://owasp.org/www-project-top-ten/", "Web application risks"),
    ("Architecture & security", "OWASP API Security Top 10", "https://owasp.org/www-project-api-security/", "API risks"),
    ("Architecture & security", "OWASP ASVS", "https://owasp.org/www-project-application-security-verification-standard/", "Security verification checklist"),
    ("Architecture & security", "C4 model", "https://c4model.com/", "Architecture diagrams"),
    ("Architecture & security", "OpenAPI Specification", "https://spec.openapis.org/oas/latest.html", "API contracts"),
    ("Architecture & security", "WCAG 2.2", "https://www.w3.org/TR/WCAG22/", "Accessibility target"),
    ("Engineering platform", "GitHub Docs", "https://docs.github.com/", "Repositories, PRs, branch protection, Actions, security features"),
    ("Engineering platform", "GitHub Actions", "https://docs.github.com/actions", "CI/CD workflows"),
    ("Engineering platform", "Docker Docs", "https://docs.docker.com/", "Images, Compose, security"),
    ("Cloud", "AWS documentation", "https://docs.aws.amazon.com/", "AWS services"),
    ("Cloud", "Google Cloud documentation", "https://cloud.google.com/docs", "GCP services"),
    ("Cloud pricing (verify at quoting time)", "AWS Pricing Calculator", "https://calculator.aws/", "Quote hosting"),
    ("Cloud pricing (verify at quoting time)", "Azure Pricing Calculator", "https://azure.microsoft.com/pricing/calculator/", "Quote hosting"),
    ("Cloud pricing (verify at quoting time)", "Google Cloud Pricing Calculator", "https://cloud.google.com/products/calculator", "Quote hosting"),
    ("Cloud pricing (verify at quoting time)", "GitHub pricing", "https://github.com/pricing", "CI minutes and seats"),
]

REFS = {  # chapter number -> source labels cited at the chapter end
    "1": ["Martin Fowler — articles & catalogue", "OWASP"],
    "2": ["PostgreSQL documentation", "ASP.NET Core documentation", "OWASP Top 10"],
    "3": [".NET documentation", "ASP.NET Core documentation", "SQL Server documentation", "PostgreSQL documentation", "React — Learn",
          "TypeScript documentation", "Docker Docs", "GitHub Docs", "OWASP"],
    "4": [".NET documentation", "WPF documentation", "React — Learn", "Angular documentation", "FastAPI documentation", "PostgreSQL documentation",
          "MySQL reference manual", "Redis documentation", "RabbitMQ documentation", "Docker Docs", "GitHub Actions", "AWS documentation",
          "Azure documentation", "Google Cloud documentation"],
    "5": ["C4 model", "OpenAPI Specification", "OWASP ASVS", "Martin Fowler — articles & catalogue", "GitHub Docs"],
    "6": ["C4 model", "GitHub Docs", "Docker Docs", "Martin Fowler — articles & catalogue"],
    "7": ["OWASP Top 10", "OWASP API Security Top 10", "OWASP ASVS", "PostgreSQL documentation", "ASP.NET Core documentation", "WCAG 2.2"],
    "8": ["AWS Pricing Calculator", "Azure Pricing Calculator", "Google Cloud Pricing Calculator", "GitHub pricing"],
    "9": ["Martin Fowler — articles & catalogue"],
    "10": [".NET documentation", "ASP.NET Core documentation", "PostgreSQL documentation", "React — Learn", "OWASP Top 10"],
}


def project(c, title, goal, reqs, db, code, tests, handover, accept):
    c.h2(title)
    c.kv([("Goal", goal), ("Key requirements", "\n".join(f"• {r}" for r in reqs)), ("Database design", db),
          ("Code deliverables", code), ("Testing", tests), ("Handover pack", handover), ("Acceptance criteria", accept)], cls="brk")


def build():
    out = []
    c = Chapter("10", "Practical Training Projects and Sign-off",
                "Seven projects, completed **in order**, each in the employee's personal sandbox with a budget alert. Each is reviewed by the Technical Lead against its acceptance criteria. "
                "Evidence (repo link, test report, screenshots, handover pack) goes in the portfolio. Projects 1–6 are individual; Project 7 is a team simulation.",
                search="projects employee management inventory crm billing portal erp simulated client sign-off")
    c.table(["#", "Project", "Level gate", "Weeks", "Approver"], [
        ["P1", "Employee management system", "After L3", "2", "Technical Lead"],
        ["P2", "Inventory management system", "After L4", "2", "Technical Lead + DB Engineer"],
        ["P3", "CRM prototype", "After L2/L3", "1.5", "BA lead + Technical Lead"],
        ["P4", "Billing application", "After L4", "2", "Technical Lead"],
        ["P5", "Role-based enterprise portal", "After L5", "2", "Technical Lead"],
        ["P6", "Multi-module ERP prototype", "After L5", "3", "Technical Lead + PM"],
        ["P7", "Complete simulated client project", "After L6", "6", "Department Head"],
    ], widths=[6, 40, 18, 10, 26])

    project(c, "P1 — Employee Management System",
            "Master data, departments, documents and basic HR workflows; the first full-stack CRUD with RBAC.",
            ["Employee master (personal, job, contact), departments, designations, reporting manager.",
             "Roles: HR Admin (full), Manager (own team, read), Employee (own profile, read).",
             "Document upload (ID proof) with secure file handling; audit log of edits.",
             "Search/filter/pagination; export to CSV (HR Admin only)."],
            "`department`, `designation`, `employee` (FK dept, desig, manager_id self-FK), `employee_document`, `app_user`, `role`, `user_role`, `audit_log`. Unique `employee_code`; `CHECK (date_of_joining <= current_date)`.",
            "ASP.NET Core 8 API (layers per Ch 6), EF Core migrations, React + TypeScript screens (list/detail/edit), JWT/cookie auth, OpenAPI.",
            "≥ 60 % unit coverage on domain/application; integration tests with a PostgreSQL container; role × endpoint permission tests; upload abuse tests.",
            "README (run in < 30 min), ERD, API docs, 1-page admin guide, seed script.",
            "All tests green in CI; manager cannot read another team; audit row exists for each update; uploaded `.exe` renamed `.pdf` is rejected.")
    c.code("""create table employee (
  id              uuid primary key default gen_random_uuid(),
  employee_code   varchar(20)  not null unique,
  full_name       varchar(150) not null,
  email           varchar(254) not null unique,
  department_id   int          not null references department(id),
  designation_id  int          not null references designation(id),
  manager_id      uuid         references employee(id),
  date_of_joining date         not null check (date_of_joining <= current_date),
  status          varchar(12)  not null default 'Active' check (status in ('Active','Inactive','Exited')),
  row_version     bigint       not null default 0,
  created_at      timestamptz  not null default now()
);
create index ix_employee_department on employee(department_id);
create index ix_employee_manager on employee(manager_id);""", "sql")

    project(c, "P2 — Inventory Management System",
            "Stock accuracy through movements: the ledger is truth, balances are derived.",
            ["Items (SKU, UoM, reorder level), warehouses, suppliers.", "Goods receipt, issue, transfer, adjustment — each creates immutable movement rows.",
             "Stock on hand per warehouse, low-stock report, stock valuation (weighted average).", "Negative stock blocked unless permission granted and audited."],
            "`item`, `warehouse`, `supplier`, `stock_movement(id, item_id, warehouse_id, qty_signed, unit_cost, ref_type, ref_id, created_at)`, `grn`, `grn_line`, `issue`. Balance = Σ qty_signed (view/materialised table updated in the same transaction).",
            "Domain service `StockService` (pure, deterministic), application commands, API, simple UI. Golden tests below.",
            "Hand-calculated tests: receive 10 @ ₹100, receive 10 @ ₹120 → average ₹110; issue 5 → 15 units × ₹110 = ₹1,650. Concurrency test: two simultaneous issues cannot overdraw.",
            "ERD, stock-valuation rule note, restore drill record, user guide.",
            "Σ movements = reported balance for all items; transfer is atomic (both rows or none); concurrency test passes; restore drill witnessed.")
    c.code("""[Fact] // Weighted average — hand calculation: (10*100 + 10*120)/20 = 110; after issue 5 -> 15 units, value 1650
public void Weighted_average_cost_after_two_receipts_and_one_issue()
{
    var s = new StockLedger();
    s.Receive(qty: 10, unitCost: 100m); s.Receive(qty: 10, unitCost: 120m);
    s.AverageCost.Should().Be(110m);
    s.Issue(5);
    s.OnHand.Should().Be(15); s.Value.Should().Be(1650m);
}""", "csharp")

    project(c, "P3 — CRM Prototype",
            "Capture and progress leads to won/lost deals with a visible pipeline.",
            ["Lead capture (web form + manual), owner assignment, duplicate detection by phone/e-mail.", "Pipeline stages with configurable probabilities; activities (call, meeting, task); follow-up reminders.",
             "Convert lead → account + contact + opportunity; reports: conversion rate, pipeline value, activity by owner."],
            "`lead`, `account`, `contact`, `opportunity(stage_id, amount numeric(14,2), expected_close)`, `pipeline_stage`, `activity`, `note`, `user_owner`. Unique partial index on normalised phone.",
            "REST API, Kanban board UI (React), background reminder job (outbox pattern).",
            "Story-level acceptance tests (Given/When/Then); duplicate-detection test; forecast calculation golden test: weighted pipeline = Σ amount × probability.",
            "BA pack (AS-IS/TO-BE, stories), ERD, demo script, user guide.",
            "Sales user sees only own leads; manager sees team; forecast numbers match hand calculation on seed data.")

    project(c, "P4 — Billing Application",
            "Accurate, auditable invoicing with tax and payments.",
            ["Customers, items/services, tax rates (from reference table / rule pack — never hard-coded).", "Quote → invoice → payment → receipt; credit notes; numbering series per financial year without gaps.",
             "Invoice PDF, payment allocation, outstanding ageing report, reminders."],
            "`customer`, `item`, `tax_rate`, `invoice`, `invoice_line`, `payment`, `payment_allocation`, `credit_note`, `number_series`. Money `numeric(14,2)`; line total stored; invoice immutable after posting.",
            "Domain `InvoiceCalculator` (rounding policy documented), posting use case in one transaction with audit and number allocation.",
            "Golden test: 2 × ₹500 = ₹1,000 + 18 % tax ₹180 = ₹1,180. Rounding tests (half-up per line vs per invoice as stated in SOW). Gap-free numbering test under concurrency.",
            "Tax configuration guide, ERD, sample invoices, runbook for number-series repair (never manual SQL).",
            "Totals match hand calculations; posted invoice cannot be edited (only credited); ageing report reconciles to ledger.")
    c.code("""[Fact] // 2 x 500 = 1000; tax 18% = 180; total = 1180   (rate comes from tax_rate table, not code)
public void Invoice_total_with_single_tax_rate()
{
    var inv = Invoice.Draft(customerId);
    inv.AddLine(itemId, qty: 2, unitPrice: 500m, taxRate: 0.18m);
    inv.SubTotal.Should().Be(1000m); inv.TaxTotal.Should().Be(180m); inv.GrandTotal.Should().Be(1180m);
}""", "csharp")

    project(c, "P5 — Role-based Enterprise Portal",
            "A portal for customers/vendors/employees with fine-grained access, notifications and audit.",
            ["Login with MFA option; roles and permissions managed by admin; data scope (own company/branch).", "Self-service: view orders/invoices, raise tickets, upload documents, download statements.",
             "Notifications (e-mail/in-app) via outbox; full audit trail; admin activity report."],
            "`app_user`, `role`, `permission`, `role_permission`, `user_scope`, `ticket`, `notification_outbox`, `audit_log`. Row-level scope applied by a central query filter.",
            "Permission policies, scope filter, outbox dispatcher, admin UI. If tenant isolation is *required*: tenant filter + RLS (Ch 7) — otherwise not built.",
            "Permission-matrix test (role × endpoint); IDOR tests (user A requests user B's record id); session-expiry and lock-out tests; accessibility check (axe + keyboard).",
            "Threat model (STRIDE table), permission matrix, security checklist, user and admin guides.",
            "Every IDOR attempt returns 404/403 and is logged; no endpoint without authorisation attribute; ZAP baseline scan has no high findings.")

    project(c, "P6 — Multi-module ERP Prototype",
            "Integrate Masters, Sales, Purchase, Inventory, Finance-lite and HR-lite as a modular monolith.",
            ["Module boundaries with public interfaces; shared kernel (numbering, approval, attachments, audit).", "Order-to-cash and procure-to-pay flows end-to-end; approval workflow with limits.",
             "Posting to a simple ledger (double entry: debits = credits); dashboard and 5 reports."],
            "Per-module schemas (`sales.*`, `inventory.*`, `finance.*`), `ledger_entry` (debit, credit, account), `approval_request`, `approval_step`. Cross-module references by ID only.",
            "Architecture tests forbidding cross-module references; domain events (in-process) with outbox; CI pipeline to staging.",
            "End-to-end scenario tests: sales order → stock issue → invoice → payment → ledger balances; trial balance debits = credits; performance baseline.",
            "C4 diagrams, 3+ ADRs, module catalogue, runbook, release notes.",
            "Trial balance balances after the scenario suite; architecture tests pass; fresh clone runs in < 30 min.")

    project(c, "P7 — Complete Simulated Client Project",
            "A supervised team (BA, 2 developers, QA, PM) runs all 29 lifecycle stages for a fictional client (e.g. a 40-bed clinic or a 5-branch distributor).",
            ["Role-play client provided by the Technical Lead with realistic messy data and a change request injected in week 4.", "All templates (Appendices C–L) completed; all gates G1–G9 executed with sign-offs.",
             "Costing and SOW prepared by the team; margin reviewed by Department Head."],
            "Designed by the team (3NF ERD, migrations, seed + migrated legacy CSV with reconciliation).",
            "Deployed to staging and a production-like environment with CI/CD, monitoring and backups.",
            "Full test plan, UAT executed by the role-play client, security checklist, restore drill, performance baseline.",
            "Complete handover pack (Appendix K), training session delivered, lessons-learned report.",
            "Department Head's panel marks each gate evidence; any missing gate evidence = fail until fixed.")
    c.callout("safety", "Sign-off statement",
              "“I have reviewed the evidence for this project against its acceptance criteria. The employee has demonstrated the skills listed and may "
              "work on supervised client projects at this level.” — Technical Lead (name, date, signature). Sign-off is **supervised-practice readiness**, not independence or certification.")
    out.append(c)

    c = Chapter("11", "Official Learning Sources",
                "Use **official and current** documentation first. Every chapter ends with the references it relies on. Cite the page and access date in your own documents.",
                search="sources links documentation microsoft postgresql react typescript fastapi fowler owasp github docker")
    groups = []
    for g, *_ in SOURCES:
        if g not in groups:
            groups.append(g)
    for g in groups:
        c.h2(g)
        c.table(["Source", "Link", "Used for"], [[l, f"[{u}]({u})", use] for gg, l, u, use in SOURCES if gg == g], widths=[28, 44, 28])
    c.callout("warn", "Link and price verification status",
              "The links above are the canonical official addresses. In the build environment used to generate this edition, outbound access to these sites was restricted, so "
              "**links were not live-checked and no vendor price was copied from a vendor page**. The Department Head's office must run a link check and re-date "
              "every price-dependent figure before the next release (see Appendix B, week 12 task).")
    out.append(c)
    return out
