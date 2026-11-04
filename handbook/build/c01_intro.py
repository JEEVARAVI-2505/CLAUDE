from lib import Chapter


def build():
    out = []

    # ---------------- How to use
    c = Chapter("", "How to Use This Handbook", search="golden rules reading path module")
    c.p("This handbook is the single operating manual of the **Custom Software & Enterprise Solutions Department**. "
        "It does two jobs: **(1)** it trains a beginner into a professional who can join supervised commercial software "
        "projects, and **(2)** it fixes the mandatory lifecycle, standards, templates, costing model and approval gates "
        "that every client project follows, from lead qualification to production support.")
    c.callout("rule", "Golden rules", [
        "Do not skip stages. A stage starts only when the previous stage's approval gate is signed.",
        "“It works on my laptop” is not “production-ready”. Production-ready means secure, tested, monitored, backed up, restorable, documented and handed over.",
        "Requirements are frozen in writing before design and development. Anything after the freeze is a **change request**.",
        "Never put passwords, keys or tokens in Git, chat, documents or tickets.",
        "Never run destructive commands (`DROP`, `DELETE` without `WHERE`, `rm -rf`) without understanding the effect and a verified backup.",
        "Never claim a system is “secure”, “compliant” or “guaranteed”. We offer *readiness support*; certification belongs to authorised auditors.",
        "Every production change has a second reviewer. Nobody approves their own work.",
        "Never design multi-tenancy unless the client has explicit tenant-isolation requirements — and then never ship it without isolation tests.",
    ])
    c.h2("Reading path for a beginner")
    c.pills(["Part A · Foundations (Ch 1–3)", "Part B · Technology & lifecycle (Ch 4–5)",
             "Part C · Architecture & standards (Ch 6–7)", "Part D · Business & people (Ch 8–9)",
             "Part E · Projects & sources (Ch 10–11)", "Appendices · Templates"])
    c.h2("How the module series works")
    c.p("The department curriculum is delivered **one module at a time**. This document is **Module 0 — the Department Handbook "
        "and Onboarding Plan**: it defines the whole system and gives each topic a summary pack. Each later module takes one "
        "level or lifecycle area and expands it into full lessons using the 14-point teaching structure in Chapter 2.")
    c.table(["Module", "Scope", "Status"], [
        ["**M0**", "Department Handbook, onboarding plan, lifecycle, standards, costing, templates (this document)", "Delivered — v1.0"],
        ["M1", "Level 1 — Business Systems Fundamentals (full lesson pack)", "Next"],
        ["M2", "Level 2 — Business Analysis (discovery, process mapping, user stories)", "Planned"],
        ["M3", "Level 3 — Software Engineering (C#/.NET 8 track and Python/TypeScript track)", "Planned"],
        ["M4", "Level 4 — Database Engineering (PostgreSQL, SQL Server, MySQL)", "Planned"],
        ["M5", "Level 5 — Enterprise Architecture", "Planned"],
        ["M6", "Level 6 — Professional Engineering (Git, CI/CD, Docker, cloud, monitoring)", "Planned"],
    ], widths=[10, 70, 20])
    c.callout("warn", "Currency of information",
              "Tool versions, vendor prices and platform limits change. This handbook gives **methods and formulas**; "
              "every vendor price used in a quote must be re-verified on the vendor's official pricing page or calculator on "
              "the day of quoting and recorded with the date. Prices in worked examples are **assumptions, not vendor quotes**.")
    out.append(c)

    # ---------------- Chapter 1 portfolio
    c = Chapter("1", "Department Service Portfolio",
                "Sixteen commercially deliverable service lines. Each can be sold alone or combined into a multi-module programme.",
                search="erp crm hrms inventory billing hospital school logistics workflow portal legacy modernisation")
    c.table(["#", "Service line", "What we deliver", "Typical client trigger"], [
        ["1", "**ERP development**", "Finance, purchase, sales, stock, production and HR modules on one data model with approvals.", "“Every department keeps its own Excel sheet.”"],
        ["2", "**CRM development**", "Leads, accounts, pipeline, quotes, follow-ups, activity timeline, sales reports.", "“We lose leads and don't know who called whom.”"],
        ["3", "**HRMS development**", "Employee master, attendance, leave, payroll inputs, appraisals, documents, self-service.", "“Leave and payroll are managed on paper.”"],
        ["4", "**Inventory management**", "Items, warehouses, batches/serials, GRN, issue, transfer, reorder, stock valuation.", "“Stock in the system never matches the shelf.”"],
        ["5", "**Billing & invoicing**", "Quotes, GST-ready invoices, receipts, credit notes, payment tracking, reminders.", "“Invoices are made in Word and payments are missed.”"],
        ["6", "**Accounting integrations**", "Two-way sync with Tally, Zoho Books, QuickBooks etc.; ledger mapping; reconciliation.", "“We key every bill twice.”"],
        ["7", "**Hospital & clinic systems**", "Registration, appointments, OPD/IPD, billing, pharmacy, lab, records with strict access control.", "“Patient files are lost; billing leaks revenue.”"],
        ["8", "**School & college systems**", "Admissions, fees, attendance, timetable, exams, results, parent portal.", "“Fee collection and results take weeks.”"],
        ["9", "**Project management systems**", "Projects, tasks, timesheets, resource plans, milestones, client approvals, profitability.", "“We cannot see project profit until it ends.”"],
        ["10", "**Manufacturing management**", "BOM, work orders, routing, shop-floor entry, quality checks, material planning.", "“We don't know true cost or WIP.”"],
        ["11", "**Logistics management**", "Orders, trips, drivers, tracking, proof of delivery, freight billing.", "“Dispatch runs on phone calls.”"],
        ["12", "**Booking & appointments**", "Slots, resources, reminders, payments, cancellations, calendar sync.", "“No-shows and double bookings.”"],
        ["13", "**Document management**", "Versioned storage, metadata, search, access rights, retention, e-sign hooks.", "“Nobody can find the latest contract.”"],
        ["14", "**Workflow management**", "Configurable multi-step approvals, SLAs, escalations, audit trail.", "“Approvals travel by email and stall.”"],
        ["15", "**Custom enterprise portals**", "Customer, vendor, employee and partner portals over existing systems.", "“Customers keep phoning for status.”"],
        ["16", "**Legacy modernisation**", "Assessment, strangler-pattern migration, re-platforming, data migration, parallel run.", "“Our VB6/Access/FoxPro app is a single point of failure.”"],
    ], widths=[4, 20, 46, 30])
    c.callout("safety", "Regulated domains",
              "Hospital, school, payroll and financial systems hold sensitive personal data. We build the controls "
              "(access control, audit trails, encryption, consent capture) and say “**compliance readiness support**”. "
              "We never state a system is compliant or certified; the client's own legal/compliance owner signs that off.")
    c.h2("Which service line first?")
    c.p("For a new department, sell in this order of delivery risk: **billing/inventory/booking** (small, clear scope) → **CRM/HRMS/project** "
        "(medium) → **ERP/manufacturing/hospital** (large, multi-module). Do not accept an ERP-scale programme until "
        "the team has delivered two medium projects and the Technical Lead has signed off the reference architecture.")
    out.append(c)

    # ---------------- Chapter 2 teaching standard
    c = Chapter("2", "Employee Teaching Standard",
                "Every topic in every module is taught with the same 14-point structure. A topic is **not complete** until all fourteen points exist and the employee has been approved on it.",
                search="teaching structure what why how tools installation verification exercise assessment approval")
    c.table(["#", "Point", "What the lesson must contain"], [
        ["1", "What it is", "A plain-language definition in two sentences, with one real-world example."],
        ["2", "Why businesses need it", "The business problem solved and the cost of not having it."],
        ["3", "How it works", "A diagram or step list of the mechanism — not just the vocabulary."],
        ["4", "Tools required", "Exact tools, versions policy and licence (free/permissive first)."],
        ["5", "Installation", "Copy-paste setup on the company standard workstation, with a check command."],
        ["6", "Implementation", "A guided, working example built step by step."],
        ["7", "Expected result", "What the finished output looks like (screen, output, file, test report)."],
        ["8", "Verification", "How the employee *proves* it works: commands, tests, queries."],
        ["9", "Common errors", "The five most frequent mistakes with symptom → cause → fix."],
        ["10", "Security", "What can go wrong securely and the control that prevents it."],
        ["11", "Documentation", "What must be written down (README, ADR, runbook, diagram) and where it is stored."],
        ["12", "Practical exercise", "A task with no solution shown, scoped to 2–4 hours."],
        ["13", "Assessment", "Quiz + demonstration with a pass mark (default ≥ 80 %)."],
        ["14", "Approval", "Named approver signs the topic in the employee's training record."],
    ], widths=[5, 22, 73])
    c.callout("tip", "Pass rules",
              "A **level** is passed only when every exercise is demonstrated live to the Technical Lead, every quiz is ≥ 80 %, and the "
              "level's capstone is reviewed. Re-sit allowed once per quiz after 3 working days of remedial study.")

    c.h2("Worked example A — Database transactions (all 14 points)")
    c.table(["Point", "Lesson content"], [
        ["1. What", "A **transaction** is a group of database changes that either *all* succeed or *all* are undone (atomicity). Example: moving ₹1,000 from account A to B."],
        ["2. Why", "Without it, a crash between “debit A” and “credit B” loses money. ERP/billing data must always balance."],
        ["3. How", "`BEGIN` → statements → `COMMIT` (make permanent) or `ROLLBACK` (undo). ACID: Atomic, Consistent, Isolated, Durable. Isolation levels control what concurrent users see."],
        ["4. Tools", "PostgreSQL 16+ (or SQL Server / MySQL InnoDB), `psql` or Azure Data Studio/DBeaver, .NET 8 with EF Core or Npgsql."],
        ["5. Install", "`docker run --name pg -e POSTGRES_PASSWORD=<local-dev-only> -p 5432:5432 -d postgres:16` → check: `docker exec pg psql -U postgres -c \"select version()\"`."],
        ["6. Implement", "See SQL and C# below: create `accounts`, write a transfer, force a failure, observe rollback."],
        ["7. Result", "After a forced failure, both balances are unchanged; after success, the total is unchanged."],
        ["8. Verify", "Run `select sum(balance) from accounts` before/after: must be identical. Add an automated integration test."],
        ["9. Errors", "Forgetting `COMMIT` (locks held); swallowing exceptions so rollback never happens; long transactions holding locks; mixing two connections in one logical unit; retrying non-idempotent work."],
        ["10. Security", "Use least-privilege DB accounts; parameterised queries only; never build SQL by string concatenation."],
        ["11. Docs", "Record the unit-of-work boundary per use case in the architecture doc; note isolation level and retry policy."],
        ["12. Exercise", "Implement “issue stock to production order” that decrements stock, inserts a movement row and writes an audit row atomically."],
        ["13. Assess", "10-question quiz + live demo of a rollback test."],
        ["14. Approve", "Technical Lead / Database Engineer signs “Transactions” in the training record."],
    ], widths=[16, 84])
    c.code("""-- PostgreSQL
create table accounts(id int primary key, owner text not null, balance numeric(14,2) not null check (balance >= 0));
insert into accounts values (1,'A',5000),(2,'B',1000);

begin;
  update accounts set balance = balance - 1000 where id = 1;
  update accounts set balance = balance + 1000 where id = 2;
commit;           -- use ROLLBACK; to undo

select sum(balance) from accounts;   -- must stay 6000""", "sql")
    c.code("""// C# 12 / .NET 8 + EF Core — one business operation = one transaction
public async Task TransferAsync(int from, int to, decimal amount, CancellationToken ct)
{
    await using var tx = await _db.Database.BeginTransactionAsync(ct);
    try
    {
        var a = await _db.Accounts.SingleAsync(x => x.Id == from, ct);
        var b = await _db.Accounts.SingleAsync(x => x.Id == to, ct);
        if (a.Balance < amount) throw new DomainException("Insufficient balance");
        a.Balance -= amount; b.Balance += amount;
        _db.AuditLog.Add(AuditEntry.For("Transfer", from, to, amount));
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
    catch { await tx.RollbackAsync(ct); throw; }   // never swallow
}""", "csharp")

    c.h2("Worked example B — Role-based access control (summary)")
    c.table(["Point", "Lesson content"], [
        ["What / Why", "RBAC grants *permissions* to *roles* and roles to *users*. Businesses need it so clerks cannot approve payments and auditors can read but not edit."],
        ["How", "Authenticate (who) → load roles/claims → authorise each action (`invoice.approve`) on the **server**. UI hiding is cosmetic only."],
        ["Implement", "ASP.NET Core policies: `[Authorize(Policy = \"invoice.approve\")]`; permission table `role_permission`; deny by default."],
        ["Verify", "A test per role × sensitive endpoint: allowed roles get 200, others 403; unauthenticated gets 401."],
        ["Errors", "Checking permission only in the frontend; role names hard-coded across code; forgetting object-level checks (user A reading user B's record — OWASP “Broken Access Control”)."],
        ["Security", "Deny by default; log denials; review privileged roles quarterly; separation of duties (maker ≠ checker)."],
    ], widths=[16, 84])
    c.callout("tip", "Convention",
              "In Modules M1–M6 each topic appears as a full 14-point lesson file. In this handbook (M0) the roadmap in Chapter 3 gives the "
              "*summary pack* (definition → why → exercise → check) for every topic so that the whole system can be planned and approved first.")
    out.append(c)
    return out
