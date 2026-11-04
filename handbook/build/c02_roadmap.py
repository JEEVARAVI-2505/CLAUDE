from lib import Chapter

HEAD = ["Topic", "What it is", "Why businesses need it", "Exercise & check"]
W = [14, 30, 26, 30]


def level(c, title, dur, tools, rows, mistakes, assess, approver, capstone):
    c.h2(title)
    c.p(f"**Duration:** {dur} · **Tools:** {tools}")
    c.table(HEAD, rows, widths=W)
    c.p(f"**Common mistakes:** {mistakes}")
    c.p(f"**Capstone:** {capstone}")
    c.p(f"**Assessment:** {assess} · **Approval:** {approver}")


def build():
    out = []
    c = Chapter("3", "Training Roadmap: Levels 1–6",
                "A beginner reaches supervised-project readiness in six levels (~ 90 days with the plan in Appendix B). "
                "Levels 1–2 are shared by everyone; from Level 3 the employee follows a role track (see Chapter 9).",
                search="level business systems analysis software engineering database enterprise architecture professional")
    c.pills(["L1 Business systems", "L2 Business analysis", "L3 Software engineering",
             "L4 Database", "L5 Enterprise architecture", "L6 Professional engineering"])

    level(c, "Level 1 — Business Systems Fundamentals", "2 weeks", "Excel/Sheets, draw.io, a notebook",
          [
              ["How businesses operate", "Organisations buy, make/serve, sell, collect cash and report.", "Software models these flows; if you don't understand them you build the wrong thing.", "Map how a local shop earns a rupee, from supplier to bank."],
              ["Departments & workflows", "Sales, purchase, stores, production, finance, HR, support — each with hand-offs.", "Most defects are hand-off gaps between departments.", "Draw the order-to-cash flow of a distributor."],
              ["Business processes", "A repeatable sequence of steps with inputs, outputs and an owner.", "Only a defined process can be automated or measured.", "Write one process as numbered steps with owner per step."],
              ["Users & roles", "Who uses the system and what each is allowed to do.", "Drives permissions, screens and training.", "List 6 roles for a clinic and their allowed actions."],
              ["Records & transactions", "A **master** record (customer) vs a **transaction** (invoice).", "Correct structure keeps reports and audits valid.", "Classify 15 fields as master / transaction / reference."],
              ["Approval workflows", "A request moves through approvers by rules (amount, role).", "Controls spend and fraud; the most requested ERP feature.", "Design a 3-level purchase approval with limits."],
              ["Reporting", "Turning records into decisions: lists, summaries, dashboards.", "Clients buy software for the *reports* they get.", "Sketch 5 reports a store owner needs and their data fields."],
              ["Business rules", "Conditions the business enforces (credit limit, GST slab).", "Rules live in configuration/code and change often.", "Write 10 rules in “IF … THEN …” form."],
              ["Functional vs non-functional requirements", "What the system does vs how well (speed, security, uptime).", "Non-functional gaps cause most production failures.", "Classify 20 sample requirements and add 5 missing NFRs."],
          ],
          "treating a spreadsheet column as a requirement; ignoring exceptions (returns, cancellations); mixing master and transaction data.",
          "30-question quiz ≥ 80 % + whiteboard the order-to-cash flow", "Business Analyst lead",
          "Document one real small business (shop/clinic/tuition centre): departments, 3 workflows, roles, 5 reports.")

    level(c, "Level 2 — Business Analysis", "3 weeks", "draw.io/BPMN, Excel, Markdown, Jira/GitHub Issues",
          [
              ["Client discovery", "Structured meetings to learn goals, pain and constraints.", "Wrong discovery = wrong product and disputed scope.", "Run a mock 45-min discovery; submit notes in the template."],
              ["Stakeholder interviews", "Questioning owners, users and IT using open, probing questions.", "Different stakeholders hold different truths.", "Interview a peer; list 3 conflicting statements."],
              ["Requirement gathering", "Capturing, numbering and validating needs.", "Traceable IDs link need → design → test.", "Produce 25 numbered requirements (FR-001…)."],
              ["Process mapping", "Visual flow with swimlanes, decisions and exceptions.", "Shows waste and automation points.", "Map a leave-approval process in BPMN."],
              ["AS-IS and TO-BE", "Current process vs improved process after the system.", "Proves value; fixes scope boundary.", "Produce both diagrams and a gap list."],
              ["User stories", "“As a <role> I want <goal> so that <benefit>.”", "Small testable slices of value.", "Write 12 stories, each ≤ 3 days of work (INVEST)."],
              ["Acceptance criteria", "Given/When/Then conditions that define “done”.", "Basis of UAT and payment milestones.", "3 criteria per story, all testable."],
              ["Use cases", "Actor–system interaction with main and alternate flows.", "Covers exceptions developers miss.", "Write 3 use cases with 2 alternate flows each."],
              ["Scope management", "In-scope / out-of-scope / assumptions / dependencies.", "Prevents unpaid scope creep.", "Write the scope section of a SOW."],
              ["Change requests", "Formal evaluation (impact on time/cost/risk) before changing scope.", "Protects margin and relationship.", "Price one change using the rate card (Ch 8)."],
          ],
          "solution-first thinking (“we'll use a dashboard”) before problem definition; unmeasurable words (“fast”, “user-friendly”); no owner for each requirement.",
          "Requirement-review defence in front of a panel (scored on a rubric)", "Business Analyst lead + Project Manager",
          "Full BRD + FRD (Appendix C/D templates) for a 4-module mini ERP with AS-IS/TO-BE maps.")

    level(c, "Level 3 — Software Engineering", "4 weeks (track-based)",
          "VS Code / Visual Studio, .NET 8 SDK **or** Python 3.12 + Node 20 LTS, Git, Postman/curl",
          [
              ["Programming fundamentals", "Variables, control flow, functions, collections, errors.", "Every later skill depends on it.", "Solve 20 graded problems (FizzBuzz → invoice totals)."],
              ["C# & .NET 8 (or Python / TypeScript)", "Typed language and runtime used for most projects.", "Strong typing and tooling suit long-lived business systems.", "Build a console invoice calculator with unit tests."],
              ["Object-oriented programming", "Classes, encapsulation, interfaces, inheritance vs composition.", "Models business entities and swaps implementations.", "Model Customer/Invoice/Payment with invariants."],
              ["APIs", "HTTP contracts (REST, JSON) between client and server.", "Web, mobile and integrations all use APIs.", "Design and call 5 endpoints; publish OpenAPI."],
              ["Backend architecture", "Layers: API → application → domain → infrastructure.", "Keeps rules testable and replaceable.", "Refactor a controller-heavy app into layers."],
              ["Frontend architecture", "Components, state, routing, forms, API client.", "Users judge the product by the UI.", "Build a list/detail/edit screen in React + TypeScript."],
              ["Authentication", "Proving identity (passwords, OIDC, MFA, JWT/cookies).", "Foundation of every control.", "Implement login with hashed passwords; no plain text."],
              ["Authorisation", "Deciding what an identity may do (roles, policies, ownership).", "Prevents data exposure (OWASP A01).", "Add policy tests for 3 roles."],
              ["Error handling", "Consistent, safe error responses and exceptions.", "Stable systems and safe messages.", "Return RFC 7807 problem details; no stack traces to clients."],
              ["Logging", "Structured events with correlation IDs, no secrets.", "Support and audit depend on logs.", "Trace one request across logs using a correlation ID."],
          ],
          "business logic in controllers/UI; catching exceptions and ignoring them; logging passwords/tokens; trusting client-side validation.",
          "Code review pass (≥ 80 % rubric) + 60 % automated test coverage on domain code", "Technical Lead",
          "Employee-management API + web UI (Project P1 in Chapter 10).")

    level(c, "Level 4 — Database Engineering", "3 weeks", "PostgreSQL 16+, SQL Server Developer/Express, MySQL 8, DBeaver/Azure Data Studio, Docker",
          [
              ["Relational databases", "Tables, rows, keys and relationships.", "The core of almost every business system.", "Design 6 tables for a school with PK/FK."],
              ["SQL", "SELECT/INSERT/UPDATE/DELETE, joins, grouping, window functions.", "Reports and troubleshooting need SQL fluency.", "Write 20 report queries from a sample schema."],
              ["PostgreSQL", "Open-source RDBMS with rich types and strong standards support.", "Default choice for cost and portability.", "Install, create roles, restore a dump."],
              ["SQL Server", "Microsoft RDBMS, common in Windows/.NET shops.", "Many clients already own licences.", "Port a schema; compare `identity` vs sequences."],
              ["MySQL", "Popular open-source RDBMS (InnoDB).", "Common on shared hosting and PHP legacy.", "Note differences in quoting, `AUTO_INCREMENT`, defaults."],
              ["Normalisation", "Removing duplication (1NF–3NF) so each fact is stored once.", "Prevents update anomalies and wrong totals.", "Normalise a denormalised invoice sheet to 3NF."],
              ["Transactions", "ACID units of work.", "Money and stock must never be half-updated.", "Rollback test (Chapter 2, Example A)."],
              ["Indexes", "Structures that speed lookups at write/storage cost.", "Slow screens are usually missing indexes.", "Read an `EXPLAIN` plan; add and measure an index."],
              ["Migrations", "Versioned, repeatable schema changes.", "Safe releases across dev/stage/prod.", "Add a column with EF Core/Flyway/Alembic; roll forward."],
              ["Data integrity", "PK, FK, UNIQUE, CHECK, NOT NULL, triggers sparingly.", "The database is the last line of defence.", "Break each constraint on purpose and show the error."],
              ["Backup & recovery", "Full/differential/log backups; tested restores; RPO/RTO.", "A backup never restored is only a hope.", "Backup, drop a table, restore to a point in time."],
          ],
          "no foreign keys “for speed”; storing money as float; `SELECT *` in code; running migrations manually on production; never testing restores.",
          "SQL practical (timed) + restore drill witnessed by the Database Engineer", "Database Engineer",
          "Normalised schema + migrations + seed data + restore drill for the Inventory system (P2).")

    level(c, "Level 5 — Enterprise Architecture", "3 weeks", "draw.io / C4 model, Docker Compose, RabbitMQ, Redis, OpenAPI",
          [
              ["Modular monoliths", "One deployable with strict internal module boundaries.", "Best default: simple to run, easy to split later.", "Split an app into 4 modules with enforced references."],
              ["Microservices & trade-offs", "Independently deployed services with own data.", "Scale/teams benefits vs cost, latency, consistency pain.", "Write an ADR justifying monolith vs microservices for 3 cases."],
              ["Multi-tenancy", "One system serving many customers with isolated data.", "SaaS economics — but a major leak risk.", "Only with explicit requirement: implement tenant filter + isolation tests."],
              ["Role-based access", "Roles → permissions → users, plus data scope (branch/department).", "Segregation of duties.", "Permission matrix for 6 roles × 12 actions."],
              ["Audit trails", "Immutable who/what/when/before/after records.", "Disputes, fraud checks, compliance support.", "Add audit on invoice edits; prove it cannot be altered by the app."],
              ["Workflow engines", "Configurable state machines for approvals.", "Business changes flows without redeploying.", "Model Draft→Submitted→Approved→Posted with guards."],
              ["Notifications", "Email/SMS/in-app/webhook events.", "Keeps users in the loop; reduces follow-ups.", "Outbox pattern: send reliably after commit."],
              ["Reporting", "Operational reports vs analytical (read replica, warehouse).", "Heavy reports must not slow transactions.", "Build 3 reports; show query plan and index."],
              ["Integration patterns", "REST, webhooks, queues, file exchange, idempotency, retries.", "Systems never live alone.", "Integrate a mock accounting API with retry + idempotency key."],
              ["Data migration", "Extract → clean → map → load → reconcile.", "Go-live fails more from bad data than bad code.", "Migrate a messy CSV with a reconciliation report."],
          ],
          "microservices on day one; tenant ID forgotten on one query; audit written by the same code that can be bypassed; migrating without reconciliation totals.",
          "Architecture review board presentation (C4 L1–L3 + 3 ADRs)", "Technical Lead + Enterprise Architect (external reviewer if none)",
          "Architecture pack for the multi-module ERP prototype (P6).")

    level(c, "Level 6 — Professional Engineering", "3 weeks", "Git, GitHub, GitHub Actions, Docker, a cloud sandbox with budget alert, OWASP ZAP/Dependency scanners",
          [
              ["Git & GitHub", "Version control, branches, pull requests.", "Traceability and safe collaboration.", "Feature branch → PR → review → squash merge."],
              ["Code reviews", "Peer inspection against a checklist.", "Cheapest defect-removal step.", "Review a seeded-bug PR and find ≥ 8 of 10 issues."],
              ["Automated testing", "Unit, integration, system, acceptance tests.", "Lets us change code without fear.", "Test pyramid for one module; coverage report."],
              ["Docker", "Packaging an app with its runtime.", "Same behaviour on every machine.", "Multi-stage image, non-root user, health check."],
              ["CI/CD", "Automatic build-test-deploy on every change.", "Fast, repeatable, auditable releases.", "GitHub Actions: build → test → scan → image → deploy to staging."],
              ["Cloud deployment", "Running on AWS/Azure/GCP (PaaS first).", "Clients need hosted, backed-up systems.", "Deploy to a sandbox with TLS and a custom domain."],
              ["Monitoring", "Metrics, logs, traces, alerts, uptime checks.", "Find outages before the client does.", "Dashboard + alert for error rate and latency."],
              ["Security", "OWASP Top 10, secrets, dependency and image scanning.", "A breach ends a startup's reputation.", "Fix 5 seeded OWASP issues; secret-scan passes."],
              ["Documentation", "README, ADRs, runbooks, API docs, user manuals.", "Handover and support depend on it.", "A new joiner runs the project from README in < 30 min."],
          ],
          "committing secrets; pushing directly to `main`; no rollback plan; alerts nobody owns; tests that pass only on the author's machine.",
          "Full pipeline demonstration + security checklist", "Technical Lead + Project Manager",
          "Take the Billing application (P4) from empty repo to monitored staging deployment.")
    out.append(c)

    # ---------------- Chapter 4 technology
    c = Chapter("4", "Technology Stack Evaluation",
                "Technology is chosen from **five factors**: client environment, target platform, budget, employee skills and long-term maintainability. "
                "We never choose a tool because it is fashionable.", search="stack react angular dotnet python fastapi postgres redis rabbitmq docker aws azure gcp wpf")
    c.h2("Evaluation of each option")
    c.table(["Technology", "Best fit", "Avoid / caution", "Skill & cost note"], [
        ["**C# / .NET 8 + ASP.NET Core**", "Long-lived enterprise back-ends, Windows/Microsoft shops, strong typing, excellent tooling and performance.", "Teams with no .NET skills and tiny budgets.", "Free, open source; Visual Studio Community/VS Code. Default for backend."],
        ["**WPF**", "Windows-only desktop apps: billing counters, shop-floor stations, offline-capable tools.", "Anything needing browser/mobile access; Linux/macOS users.", "Runs only on Windows; use MVVM; keep logic in UI-free libraries."],
        ["**React + TypeScript**", "Rich web UIs, large ecosystem, hiring pool; portals and dashboards.", "Over-engineering simple CRUD; teams without TS discipline.", "Free. Default for web frontend."],
        ["**Angular**", "Large teams wanting an opinionated, batteries-included framework.", "Small teams needing speed; steep learning curve.", "Free; choose when the client already runs Angular."],
        ["**TypeScript**", "Typed JavaScript for front-end and Node tooling.", "Mixing `any` everywhere defeats its value.", "Mandatory for all JS code."],
        ["**Python**", "Data work, automation, integrations, AI features, quick prototypes.", "Very large strictly-typed domains without type-checking discipline.", "Free; use type hints + mypy/ruff."],
        ["**FastAPI**", "Fast Python APIs with automatic OpenAPI and validation (Pydantic).", "Heavy admin-style apps better served by Django.", "Free; good for integration microservices."],
        ["**PostgreSQL**", "Default OLTP database: standards-compliant, JSON, extensions, no licence fee.", "Clients locked to SQL Server tooling.", "Free; managed services are inexpensive."],
        ["**SQL Server**", "Client already licensed; deep Microsoft integration; reporting tools.", "Licence cost for new, cost-sensitive clients.", "Licensing varies by edition — verify on Microsoft's site."],
        ["**MySQL**", "Existing LAMP/hosting estates; read-heavy simple workloads.", "Complex reporting/constraints vs PostgreSQL.", "Free community edition; know the storage-engine (InnoDB) behaviour."],
        ["**Redis**", "Caching, sessions, rate limiting, short-lived locks.", "Using it as the primary database for business records.", "Cache is disposable by design; never the system of record."],
        ["**RabbitMQ**", "Reliable background jobs, integration events, decoupling.", "Simple apps where a DB-backed job table suffices.", "Adds an operational component — justify in an ADR."],
        ["**REST APIs**", "Default API style for web/mobile/integration; OpenAPI documented.", "Chatty fine-grained calls; use batching or GraphQL only if justified.", "Version URLs/headers; document in OpenAPI."],
        ["**Docker**", "Reproducible packaging for dev, CI and deployment.", "Windows-GUI apps like WPF (not containerised).", "Free for small business per vendor terms — verify current licence terms."],
        ["**GitHub Actions**", "CI/CD where code is on GitHub.", "Long, expensive builds without caching.", "Free quota varies by plan — verify on GitHub's pricing page."],
        ["**AWS / Azure / GCP**", "Managed hosting, databases, storage, identity, monitoring.", "Over-engineering (Kubernetes) for small apps.", "Choose by client's existing relationship, region/latency and your team's skills."],
    ], widths=[17, 31, 26, 26])

    c.h2("Weighted decision matrix (use in the architecture ADR)")
    c.p("Score each candidate 1–5 per factor, multiply by weight, sum. The **weights below are the department default** and may be changed *only with written justification* in the ADR.")
    weights = [("Client environment fit", 25), ("Target platform", 20), ("Budget (licence + hosting)", 15),
               ("Team skills available", 20), ("Long-term maintainability", 20)]
    cand = {"A: .NET 8 + React + PostgreSQL": [4, 5, 5, 4, 5],
            "B: .NET 8 + WPF + SQL Server": [5, 2, 3, 4, 4],
            "C: FastAPI + React + PostgreSQL": [3, 5, 5, 3, 4]}
    rows = []
    for name, sc in cand.items():
        tot = sum(s * w for s, (_, w) in zip(sc, weights)) / 100
        rows.append([name] + [str(s) for s in sc] + [f"**{tot:.2f}**"])
    c.table(["Option"] + [f"{n} ({w}%)" for n, w in weights] + ["Weighted /5"], rows, widths=[26, 14, 12, 14, 12, 12, 10])
    c.p("*Worked check for A:* 4×0.25 + 5×0.20 + 5×0.15 + 4×0.20 + 5×0.20 = 1.00 + 1.00 + 0.75 + 0.80 + 1.00 = **4.55**. "
        "The scores are illustrative for a typical web ERP; a Windows-only counter application would score B higher on platform fit.")

    c.h2("Department default stacks")
    c.table(["Scenario", "Default stack", "Hosting default"], [
        ["Web ERP / CRM / HRMS / portal", "ASP.NET Core 8 modular monolith · React + TypeScript · PostgreSQL · Redis (cache only)", "Managed PaaS container service + managed PostgreSQL"],
        ["Microsoft-centric client", "ASP.NET Core 8 · Angular or React · SQL Server", "Azure App Service + Azure SQL, or client's data centre"],
        ["Windows counter/billing desktop", "WPF (MVVM) · UI-free domain libraries · SQL Server/PostgreSQL · optional sync API", "Local/branch server + cloud backup"],
        ["Integration & data automation", "Python 3.12 + FastAPI · RabbitMQ where needed · PostgreSQL", "Container on the same platform"],
        ["Legacy modernisation", "Strangler pattern: new .NET services/UI beside the old app, shared database first, then split", "Parallel run on staging"],
    ], widths=[26, 46, 28])
    c.callout("warn", "Rule: justify, don't presume",
              "Microservices, Kubernetes, message brokers and multi-tenancy are **not defaults**. Each needs an Architecture Decision Record "
              "stating the requirement that forces it, the alternatives, and the cost of operating it.")
    out.append(c)
    return out
