from lib import Chapter


def build():
    out = []
    # ------------------------------------------------------------ Chapter 6
    c = Chapter("6", "Enterprise Project Folder Structure",
                "Every client repository starts from the same template so any engineer can find anything in seconds. "
                "The structure follows **Clean/Onion layering** (domain at the centre, infrastructure at the edge) inside a **modular monolith** by default.",
                search="folder structure repository src docs tests deployment security monitoring github workflows")
    c.code("""enterprise-software-project/
├── README.md                    # what it is, how to run in <30 min, links to docs
├── CONTRIBUTING.md              # branching, commits, PR rules, Definition of Done
├── CODEOWNERS                   # mandatory reviewers per path
├── docs/
│   ├── business-requirements/   # BRD, stakeholder register, AS-IS / TO-BE maps
│   ├── functional-requirements/ # FRS, user stories, field dictionary, traceability
│   ├── workflows/               # BPMN / state diagrams, approval matrices
│   ├── architecture/            # C4 diagrams, ADRs (adr-0001-*.md), NFRs
│   ├── database/                # ERD, data dictionary, migration & backup plan
│   ├── api/                     # OpenAPI files, integration contracts
│   └── user-manuals/            # role-based guides, training material, FAQ
├── src/
│   ├── frontend/                # React/Angular app (or WPF shell)
│   ├── backend/                 # API host: controllers/endpoints, DI, middleware
│   ├── domain/                  # entities, value objects, domain rules (no I/O)
│   ├── application/             # use cases, commands/queries, validators, interfaces
│   ├── infrastructure/          # EF Core/SQL repositories, e-mail, storage, clock
│   └── integrations/            # adapters to accounting, payment, SMS, government APIs
├── database/
│   ├── migrations/              # versioned, forward-only scripts
│   ├── seeds/                   # reference data (roles, tax codes, statuses)
│   └── scripts/                 # backup, restore, reporting views, maintenance
├── tests/
│   ├── unit/                    # fast tests: domain + application
│   ├── integration/             # real DB (container) + API tests
│   ├── system/                  # end-to-end, performance, security checks
│   └── acceptance/              # UAT scripts mapped to acceptance criteria
├── deployment/                  # Dockerfiles, compose, IaC, runbooks, release notes
├── security/                    # threat models, security checklist, scan config, policies
├── monitoring/                  # dashboards, alert rules, health-check definitions, SLOs
└── .github/
    └── workflows/               # ci.yml, security.yml, deploy-staging.yml, deploy-prod.yml""", "tree")
    c.h2("What each directory is for")
    c.table(["Directory", "Purpose", "Rules"], [
        ["`docs/business-requirements`", "The *why*: BRD, stakeholders, AS-IS/TO-BE.", "Signed versions are immutable; changes create a new version."],
        ["`docs/functional-requirements`", "The *what*: FRS, stories, field dictionary, traceability matrix.", "Every FR ID appears in at least one test."],
        ["`docs/workflows`", "Approval flows and state machines.", "Diagrams stored as source (`.bpmn`, `.drawio`) plus exported PNG."],
        ["`docs/architecture`", "C4 diagrams, ADRs, NFRs.", "One ADR per significant decision; never delete, supersede."],
        ["`docs/database`", "ERD, data dictionary, backup/restore plan.", "Updated in the same PR as any migration."],
        ["`docs/api`", "OpenAPI specs and integration contracts.", "Generated or validated in CI; breaking changes need version bump."],
        ["`docs/user-manuals`", "End-user and admin guides.", "Written for roles, with screenshots from staging."],
        ["`src/domain`", "Entities, value objects, domain services and rules.", "**No** references to database, HTTP or UI frameworks."],
        ["`src/application`", "Use cases, DTOs, validators, ports (interfaces).", "Defines transactions boundaries; depends only on domain."],
        ["`src/infrastructure`", "Implementations of ports: EF Core, storage, e-mail, clock.", "Replaceable; contains no business rules."],
        ["`src/backend`", "Composition root: endpoints, auth, DI, logging, health checks.", "Thin — maps HTTP to use cases."],
        ["`src/integrations`", "Anti-corruption adapters to external systems.", "One folder per external system; contract tests with recorded samples."],
        ["`src/frontend`", "UI application.", "Talks to the backend only through the generated API client."],
        ["`database/migrations`", "Versioned schema changes (`V0001__create_party.sql`).", "Applied migrations are never edited."],
        ["`database/seeds`", "Reference data only; no personal data.", "Idempotent scripts."],
        ["`database/scripts`", "Backup/restore/maintenance scripts.", "Dry-run flag and confirmation prompt on destructive scripts."],
        ["`tests/*`", "Test pyramid layers.", "Unit tests run < 2 minutes; integration tests use disposable containers."],
        ["`deployment`", "Dockerfiles, compose, IaC, runbooks.", "No secrets; configuration via environment/secret store."],
        ["`security`", "Threat models, checklist, scan configs, risk register.", "Reviewed every release."],
        ["`monitoring`", "Dashboards-as-code, alert rules, SLOs.", "Every alert has an owner and a runbook link."],
        ["`.github/workflows`", "CI/CD pipelines.", "Pinned action versions; least-privilege tokens."],
    ], widths=[26, 40, 34])

    c.h2("Adapting the structure to the chosen architecture")
    c.table(["Architecture", "Adaptation"], [
        ["**Modular monolith (default)**", "Inside `src/`, give each business module its own folder per layer or its own project: `src/modules/inventory/{domain,application,infrastructure}`. Modules talk through public interfaces or in-process events only; an architecture test fails the build on forbidden references."],
        ["**Microservices (justified by ADR)**", "One repository per service **or** a mono-repo with `services/<name>/` each having its own `src/`, `database/`, `deployment/`. Add `contracts/` (OpenAPI/AsyncAPI) and `platform/` (gateway, broker, observability). Each service owns its database — no shared tables."],
        ["**WPF desktop**", "`src/frontend` becomes `src/desktop-wpf` (MVVM). Keep `domain`/`application` UI-free class libraries so the same rules can serve a future web API. `deployment/` holds the installer (MSIX/WiX) and update channel."],
        ["**React + FastAPI**", "`src/backend` is the Python package (`app/api`, `app/services`, `app/models`, `alembic/`); `domain/application/infrastructure` map to `app/domain`, `app/services`, `app/repositories`."],
        ["**Legacy modernisation**", "Add `legacy/` (read-only snapshots, schema dumps) and `docs/migration/` (mapping sheets, reconciliation reports, strangler routing table)."],
    ], widths=[26, 74])
    c.callout("tip", "Definition of a ready repository (Stage 17)",
              "A new engineer can clone, run `docker compose up`, run the tests and open the app in **under 30 minutes** using only the README.")

    out.append(c)
    # ------------------------------------------------------------ Chapter 7
    c = Chapter("7", "Mandatory Enterprise Engineering Standards",
                "These sixteen standards are enforced in code review and CI. Each has a **rule**, an **implementation pattern** and a **verification**. "
                "A standard may be waived only by the Technical Lead, in writing, with a risk note in the ADR log.",
                search="standards modular separation integrity transaction rbac audit tenant validation file backup api security performance accessibility versioning documentation")
    c.table(["#", "Standard", "Rule", "How we verify"], [
        ["1", "**Modular architecture**", "Business capabilities are modules with explicit public APIs.", "Architecture tests block forbidden dependencies."],
        ["2", "**Separation of concerns**", "UI, API, application, domain and persistence are separate layers; no business logic in controllers or SQL triggers.", "Code review checklist; layering tests."],
        ["3", "**Reusable business components**", "Shared concerns (numbering series, tax, approval, attachments, audit) are built once in a shared kernel.", "No duplicate logic in PR review."],
        ["4", "**Database integrity**", "PK/FK/UNIQUE/CHECK/NOT NULL on every table; money as `numeric/decimal`; UTC timestamps.", "DB checklist (Appendix F)."],
        ["5", "**Transaction management**", "One use case = one transaction; explicit boundaries; optimistic concurrency on editable records.", "Rollback and concurrency tests."],
        ["6", "**Role-based access control**", "Deny by default; permission checks server-side at endpoint **and** object level.", "Permission-matrix test per role."],
        ["7", "**Audit logs**", "Who/what/when/old/new for every create/update/delete/approve; append-only.", "Audit tests; DB account cannot update/delete audit rows."],
        ["8", "**Multi-tenant isolation**", "Only if the client requires it. Tenant ID on every row, enforced centrally (global filter + DB row-level security where available).", "Mandatory cross-tenant tests (below)."],
        ["9", "**Data validation**", "Validate at the edge (shape/type/range) **and** in the domain (business rules) **and** in the DB (constraints).", "Negative tests for each rule."],
        ["10", "**Secure file handling**", "Whitelist type by content, size limit, random storage names, outside web root, malware scan hook, signed download links.", "Upload abuse tests."],
        ["11", "**Backup & restore**", "Automated backups, off-site copy, encrypted, **restore drill each quarter**; RPO/RTO documented.", "Drill report."],
        ["12", "**API security**", "TLS only; authN on every endpoint; rate limits; input limits; CORS allow-list; no sensitive data in URLs; OWASP API Top 10 review.", "ZAP/automated scan + manual checklist."],
        ["13", "**Performance**", "Budgets in NFRs (e.g. p95 < 500 ms for list screens); pagination everywhere; indexes justified by query plans; no N+1.", "Load test + query-plan review."],
        ["14", "**Accessibility**", "WCAG 2.2 AA target: keyboard operable, labels, contrast, focus order, error text.", "axe/Lighthouse + manual keyboard test."],
        ["15", "**Versioning**", "Semantic versioning for releases; API versions; DB migration versions; documented deprecations.", "Release checklist."],
        ["16", "**Documentation**", "README, ADRs, API docs, runbooks, user manuals updated in the same PR as the change.", "PR template checkbox; docs build in CI."],
    ], widths=[4, 20, 46, 30])

    c.h2("Implementation patterns")
    c.h3("Transaction + audit in one unit of work (standards 5 and 7)")
    c.code("""public sealed class ApproveInvoiceHandler(AppDbContext db, ICurrentUser user, IClock clock)
{
    public async Task Handle(Guid invoiceId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var inv = await db.Invoices.SingleAsync(i => i.Id == invoiceId, ct);      // tenant filter applied globally
        if (inv.CreatedBy == user.Id) throw new DomainException("Maker cannot approve own invoice.");
        var before = inv.Status;
        inv.Approve(user.Id, clock.UtcNow);                                       // domain rule inside entity
        db.Audit.Add(new AuditEntry("Invoice", inv.Id, "Approve", before, inv.Status, user.Id, clock.UtcNow));
        await db.SaveChangesAsync(ct);                                            // RowVersion concurrency token checked
        await tx.CommitAsync(ct);
    }
}""", "csharp")
    c.h3("Multi-tenant isolation (standard 8) — only with explicit requirement")
    c.callout("rule", "Never design multi-tenancy without explicit tenant-isolation requirements and tests",
              "Before any tenant column is added, the SOW/FRS must state: how many tenants, isolation level (shared schema / schema-per-tenant / database-per-tenant), "
              "who may see cross-tenant data (nobody by default), and what happens on tenant offboarding. If these are missing, **stop and ask** — build single-tenant.")
    c.code("""// EF Core global query filter — every tenant-owned entity implements ITenantOwned
modelBuilder.Entity<Invoice>().HasQueryFilter(e => e.TenantId == _tenant.Id);

// PostgreSQL defence in depth (row-level security)
alter table invoice enable row level security;
create policy tenant_isolation on invoice
  using (tenant_id = current_setting('app.tenant_id')::uuid);""", "csharp / sql")
    c.code("""// Mandatory isolation tests (xUnit) — a build with any failing test cannot be released
[Fact] public async Task TenantA_cannot_read_TenantB_invoice() { /* seed B, query as A -> empty/404 */ }
[Fact] public async Task TenantA_cannot_update_TenantB_invoice_by_guessing_id() { /* expect 404/403, row unchanged */ }
[Fact] public async Task Reports_and_exports_filter_by_tenant() { }
[Fact] public async Task Background_jobs_run_with_explicit_tenant_context() { }
[Fact] public async Task Missing_tenant_context_fails_closed() { /* no tenant -> exception, not 'all rows' */ }""", "csharp")
    c.h3("Secure file upload (standard 10)")
    c.code("""var allowed = new Dictionary<string, byte[]> { [".pdf"] = "%PDF"u8.ToArray(), [".png"] = [0x89,0x50,0x4E,0x47] };
if (file.Length is 0 or > 10_000_000) return Results.BadRequest("Invalid size");
var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
if (!allowed.TryGetValue(ext, out var magic)) return Results.BadRequest("Type not allowed");
await using var s = file.OpenReadStream(); var head = new byte[magic.Length]; await s.ReadExactlyAsync(head);
if (!head.SequenceEqual(magic)) return Results.BadRequest("Content mismatch");
var stored = $"{Guid.NewGuid():N}{ext}";             // never trust the client file name
await storage.SaveAsync(stored, s, ct);               // outside web root; scan hook; DB keeps metadata""", "csharp")
    c.h3("Validation at three levels (standard 9)")
    c.table(["Level", "Example", "Why"], [
        ["Edge (API)", "Required, length ≤ 100, valid e-mail, amount > 0", "Fail fast with 400 and field-level messages."],
        ["Domain", "Credit limit not exceeded; cannot ship more than ordered", "Rules hold no matter which client calls."],
        ["Database", "`CHECK (qty >= 0)`, `UNIQUE (tenant_id, invoice_no)`", "Last line of defence against bugs and direct SQL."],
    ], widths=[18, 48, 34])
    c.callout("safety", "Structural/regulatory honesty",
              "Standards reduce risk; they do not guarantee security or legal compliance. Security reports carry the line "
              "“Independent engineer/auditor review required before production reliance.”")
    out.append(c)
    return out
