"""Part A: cover, intake, lifecycle/gates, Phases 1-7."""
from lib import *
import model as M

def cover():
    return f"""<div class="cover"><div class="tag">Company Operating System · Version 1.0 · 3 Nov 2026</div>
<h1>Mobile Application Development Company<br>Operating System</h1>
<p class="lead">One repeatable system to research, design, build, test, publish, price, sell and maintain
profitable mobile apps — with SOPs, templates, approval gates, costing formulas and a fully worked example.</p>
{table(["Document control",""],[["Owner","CTO / Managing Director"],["Approver","Founders (Gate owner list in §2)"],
 ["Status","Draft v1.0 for founder review"],["Currency / units","INR (₹), Indian digit grouping; hours; ex-GST unless stated"],
 ["Companion file","build/model.py recomputes every number in this document"]])}
{callout("warn","Read this first - what is verified and what is not",
 "Prices of cloud services, SaaS tools, SMS/email and store fees were <b>not</b> verified against live official pages while preparing this edition "
 "(the research environment could not reach them). Every such figure is labelled <b>Planning</b> or <b>Last known</b> and must be replaced from the official page "
 "or calculator, with the access date, at <b>Gate 8</b>. No market statistics are quoted; Phase 1 requires every statistic to carry a source URL, access date and confidence level.")}
</div>"""

def exec_summary():
    sc = {s["n"]: s for s in M.SC}
    k = [("₹" + M.num(M.L_E / 100000, 2) + " L", "Expected build labour (sample app)"),
         (M.num(M.H_E) + " h", "Expected effort (PERT)"),
         (M.inr(M.CP1["target"]), "Target custom-build price (client #1)"),
         (f"{M.BE_N} customers", "SaaS operating break-even"),
         (f"{M.GM_PCT*100:.0f}%", "Gross margin (cost to serve)"),
         (M.inr(sc[100]["op"]) + "/mo", "Operating profit at 100 customers")]
    kp = "".join(f"<div class=kpi><b>{a}</b><span>{b}</span></div>" for a, b in k)
    return f"""<div class=kpis>{kp}</div>
<p>This document is the standing instruction set for the whole company. It does five things:</p>
{ol(["<b>Intake</b> (§1): captures company facts and sets explicit defaults where facts are still missing.",
"<b>Lifecycle and 11 approval gates</b> (§2): nobody starts a phase without the gate signed.",
"<b>Phase SOPs 1-7, 10-13</b> (§3-§9, §12-§15): owner, inputs, procedure, output, exit criteria, templates.",
"<b>Costing and pricing engine</b> (§10-§11): formulas plus a complete worked example for one sample app, <i>SiteDiary</i>.",
"<b>People and portal</b> (§16-§17): role responsibilities, weekly report, and the spec for the internal project portal."])}"""

def intake():
    rows = [["Company name", "[To be provided]", "Placeholder: <i>Your Company Pvt Ltd</i>"],
            ["Business model", "Hybrid: own SaaS products + paid client builds", "Own products give recurring revenue; client work funds the runway"],
            ["Employees", "6 (PM, UX, Flutter, Backend, QA, DevOps/DB) + part-time support", "Used for rates in §10"],
            ["Technology budget (cash, non-labour)", "₹3,00,000 for first product", f"Sample app non-labour cash need: {M.inr(M.SETUP_COSTS)} (§10-D)"],
            ["Target industries", "Construction & field services (first); then retail, clinics", "Choose one wedge industry per product"],
            ["Target customers", "Small contractors / site supervisors in India (5-50 staff)", "Validate in Phase 1 interviews"],
            ["Preferred technology", "Flutter + NestJS/TypeScript + PostgreSQL + Firebase Cloud Messaging", "Decided per project by the §5 matrix, not by default"],
            ["Own vs client apps", "Both", "Pricing models for both are in §11"],
            ["Initial project", "SiteDiary - offline-first daily site report app", "Fictional sample used throughout"]]
    return (callout("ok", "Task 0 - Intake", "Fill the third column of the table at the kickoff meeting. Values shown are <b>working assumptions</b> so the system can be demonstrated end to end; "
            "replace them and re-run <code>model.py</code> / the calculator in §10.") +
            table(["Question", "Working assumption", "Note / where used"], rows) +
            "<h3>Intake questionnaire (copy for every new project)</h3>" +
            checklist(["Who pays: our own product, a client, or an enterprise buyer?", "Which of Android, iOS, web is genuinely required? (Default: ask if a PWA suffices.)",
                       "Is offline use required? Which operations? Conflict rule?", "Which personal / sensitive data is collected? (Triggers DPDP Act 2023 review.)",
                       "Payment, SMS, maps or AI features needed?", "Budget ceiling and deadline? Who signs each gate?", "Who owns source code and customer data after delivery?"]))

def lifecycle():
    flow = ["Market research", "G1", "Requirements + I/O freeze", "G2", "Tech + architecture", "G3", "UI/UX", "G4", "Repo + env setup", "G5",
            "Build (mobile + backend)", "G6", "QA + security", "G7", "Costing + pricing", "G8", "Production deploy", "G9", "Store publishing", "G10",
            "Showcase + sales", "Handover", "G11", "Maintenance"]
    f = '<div class="flow">' + "".join(f'<div class="g">{x}</div>' if x.startswith("G") and len(x) <= 3 else f"<div>{x}</div>" for x in flow) + "</div>"
    g = [["G1", "Market validation", "Business Researcher / PM", "Founder", "Research report with sourced evidence; ≥10 customer interviews or equivalent; go / pivot / stop decision"],
         ["G2", "Requirements & scope freeze", "PM", "Founder + customer (if client)", "PRD, screen I/O spec, API-DB map signed; change-control active"],
         ["G3", "Architecture & technology", "Tech lead", "CTO", "Decision matrix, 11 architecture docs, threat model, ADRs"],
         ["G4", "UI/UX approval", "UX Designer", "PM + customer", "Clickable prototype, usability test ≥5 users, accessibility check"],
         ["G5", "Repo & dev setup", "DevOps", "Tech lead", "Protected main, CI green, 3 environments, secrets policy, .env.example"],
         ["G6", "MVP development review", "Tech lead", "CTO + PM", "All frozen screens built; demo against spec; ≥80% unit coverage on business logic"],
         ["G7", "QA & security", "QA owner", "CTO", "Zero open Sev-1/2, OWASP MASVS checklist done, device matrix passed"],
         ["G8", "Costing & pricing", "PM + Finance", "Founders", "Verified provider prices (dated), cost model, price floor, break-even"],
         ["G9", "Production deployment", "DevOps", "CTO", "Restore drill passed, monitoring + alerts live, rollback tested"],
         ["G10", "Store submission & release", "Release manager", "CTO + PM", "Store checklists complete, privacy declarations match SDK audit"],
         ["G11", "Customer handover", "PM", "Founder + customer", "Manuals, training, credentials transfer, support contract signed"]]
    return (f + table(["Gate", "Name", "Evidence prepared by", "Approver", "Minimum exit criteria"], g) +
            callout("bad", "Rule", "No employee may start work belonging to a later phase until the preceding gate is recorded as <b>Approved</b> in the portal (§17) "
                    "with approver name, date and evidence links. Rejected or revision-requested gates return the work to the owner with written reasons."))

def phase1():
    body = sop("Business Researcher (PM backup)", "Idea one-pager; industry & customer hypotheses",
        ["Write the problem statement in one sentence and name the buyer, user and payer.",
         "Collect competitor data from Google Play, Apple App Store, Product Hunt, G2/Capterra, Reddit, Google Trends and official sites. Log each fact in the evidence register below.",
         "Read the 1- and 2-star reviews of the top five competitors; tag complaints (missing feature, price, bug, support).",
         "Run ≥10 customer interviews (script: problem, current workaround, willingness to pay, budget owner).",
         "Size the market <b>bottom-up</b>: TAM = number of potential customers × annual revenue per customer; SAM = reachable segment; SOM = realistic 3-year capture. Cite the source of every count.",
         "Choose the monetisation model: paid, subscription, freemium or enterprise; justify with competitor pricing and interview evidence.",
         "Rate customer-acquisition difficulty (channels, cost, sales cycle) and state a go / pivot / stop recommendation."],
        "Market Research Report (PDF)", "Gate 1")
    ev = table(["ID", "Finding", "Source URL", "Access date", "Confidence (H/M/L)", "Used for"],
               [["E-001", "<i>(example row)</i> Competitor X lists price ₹___/month", "https://…", "YYYY-MM-DD", "H - official page", "Pricing benchmark"],
                ["E-002", "<i>(example row)</i> 38 of 120 reviews mention no offline mode", "https://… (store page)", "YYYY-MM-DD", "M - own tagging", "Feature gap"]])
    bottom = formula("TAM = N(potential customers) × ARPA(₹/yr)\nSAM = TAM × share reachable via our channels and language/geography\nSOM = SAM × realistic 3-yr share (state the basis; ≤5% unless evidence says otherwise)")
    return (body + "<h3>Evidence register (mandatory format)</h3>" + ev + "<h3>Bottom-up sizing formula</h3>" + bottom +
            callout("warn", "No invented statistics", "If a number cannot be sourced, write <b>Unknown - research needed</b>. Reports with unsourced figures are rejected at Gate 1. "
                    "Use government sources (e.g. MeitY, ministry reports) and official competitor pages as primary evidence."))

def phase2():
    body = sop("Product Manager", "Approved market report", [
        "Write the PRD: objective, users, roles and permissions, modules, navigation, journeys, offline/online scope, integrations, SDKs, security, performance, device support, accessibility, maintenance.",
        "For <b>every screen</b> complete the Screen I/O sheet (below). One sheet per screen, stored in <code>docs/requirements/screens/</code>.",
        "Build the screen → API → database map and the failure-behaviour table.",
        "Review with UX, tech lead and QA (testability of each rule). Resolve all open questions.",
        "Freeze: tag the PRD <code>prd-v1.0-frozen</code>. Any later change requires a Change Request (CR)."],
        "PRD + Screen Specification + I/O Freeze document", "Gate 2")
    scr = table(["Field", "Content"], [["Screen ID / name", "S-014 Daily Report - Attendance"], ["Purpose / role", "Record who worked today / Supervisor"],
        ["Inputs & types", "worker_id (picker), status (enum: present, absent, half), hours (decimal 0-16), note (text ≤200)"],
        ["Validation", "status required; hours required if present; hours step 0.5; no duplicate worker per date"],
        ["Buttons → actions", "Save → local write + sync queue; Submit report → locks day, enqueues upload"],
        ["API → DB", "POST /v1/reports/{id}/attendance → INSERT attendance(…) ; GET same → SELECT"],
        ["Output / errors", "Toast 'Saved offline'; E-4101 'Hours must be 0-16'"],
        ["Loading / offline", "Skeleton rows; fully usable offline, badge 'Pending sync'"]])
    fail = table(["Situation", "Required behaviour (default - override in PRD)"], [
        ["Slow internet", "Show cached data immediately; spinner after 400 ms; timeout 15 s then 'Retry' without losing input"],
        ["No internet", "Offline banner; allowed offline ops list (§ PRD); writes queue locally"],
        ["API failure (5xx)", "Exponential backoff (2,4,8,16 s, max 5); user sees non-technical message + request id"],
        ["Session expiry (401)", "Silent refresh once; else save draft locally, route to login, restore draft after login"],
        ["Invalid login", "Generic message, no hint which field; lockout 5 attempts / 15 min"],
        ["Permission denied", "Explain why the permission is needed; offer settings link; degrade gracefully"],
        ["App restart / rotation", "Restore last route and unsaved form from local draft; layouts handle rotation where supported"],
        ["Interrupted upload", "Resumable chunked upload; resume on reconnect; checksum verify"],
        ["Failed notification", "Never the sole channel; in-app inbox mirrors every push; token refresh on rotation"]])
    cr = code("""CHANGE REQUEST  CR-<project>-<nnn>
Requested by / date:          Reason & business value:
Affected screens / APIs / tables:
Effort by role (hours) and cost (use §10 rates):
Schedule impact (days):       Risk:
Decision: Approved / Rejected / Deferred      Approver + date:""")
    return body + "<h3>Screen I/O sheet (example filled)</h3>" + scr + "<h3>Mandatory failure behaviours</h3>" + fail + "<h3>Change-request form</h3>" + cr

def phase3():
    rows = [["Dev cost", "20%", "Flutter 4 / Native 2 / RN 4 / PWA 5"], ["Performance", "10%", "Native 5 / Flutter 4 / RN 3 / PWA 3"],
            ["Platform-specific features", "10%", "Native 5 / Flutter 3 / RN 3 / PWA 2"], ["Maintenance cost", "15%", "Flutter 4 / Native 2 / RN 4 / PWA 5"],
            ["Offline capability", "10%", "Native 5 / Flutter 4 / RN 4 / PWA 3"], ["Team experience", "15%", "Score from actual skills"],
            ["Security", "5%", "Native 5 / others 4 (equal controls required)"], ["Scalability / vendor risk", "15%", "Score per backend/cloud choice"]]
    rows.append(["Weighted score", "100%", "Σ weight × score; ties → lowest maintenance cost"])
    decision = table(["Criterion", "Weight", "Example scoring guide (1-5)"], rows, total_last=True)
    stack = table(["Layer", "Default choice", "Choose something else when"], [
        ["Mobile", "Flutter (one codebase, Android+iOS)", "Heavy native APIs/AR/BLE → Native; team is React-only → React Native; no device features → PWA"],
        ["Backend", "NestJS (TypeScript) REST + OpenAPI", ".NET/Python if the team's depth is there; Supabase/Firebase for tiny MVPs"],
        ["Database", "PostgreSQL; SQLite (Drift) on device; Redis only if caching proven needed", "Firestore if data is document-shaped and offline-sync by vendor is acceptable"],
        ["Cloud", "Managed container + managed PG in an India region", "AWS/Azure/GCP by customer mandate or credits; check data-residency needs"],
        ["Notifications", "FCM (Android + iOS via APNs bridge)", "Direct APNs only if FCM limits hit"]])
    docs = ["Application architecture (C4 context + container)", "Frontend architecture (layers, state, navigation)", "Backend architecture (modules, queues)",
            "Database architecture (ER diagram, indexes, migrations)", "API architecture (REST, versioning, error model)", "Authentication (OTP/password, JWT + refresh, device binding)",
            "Notification architecture", "Offline sync architecture (outbox, conflict policy)", "File storage (signed URLs, lifecycle)", "Threat model (STRIDE; MASVS mapping)", "Cloud infrastructure diagram"]
    return (sop("Tech Lead", "Frozen PRD", ["Shortlist options; score with the matrix below; record the decision as an ADR.",
            "Challenge the premise: can a responsive web app / PWA meet the requirement? If yes, recommend it.",
            "Produce the 11 architecture documents; threat-model every trust boundary.", "Review cost implications with PM before approval."], "ADR + architecture pack", "Gate 3") +
            "<h3>Technology decision matrix</h3>" + decision + "<h3>Default stack and exceptions</h3>" + stack +
            "<h3>Required architecture documents</h3>" + checklist(docs) +
            callout("warn", "Offline is a design decision", "Define per entity: allowed offline operations, local retention, sync order, and conflict rule "
                    "(e.g. <i>server-wins for master data; append-only for reports; last-writer-wins only with timestamp + device id, and user-visible conflict screen for edits</i>). Do not sync everything by default."))

def phase4():
    return (sop("UX Designer", "Frozen PRD and architecture", [
        "Journey maps → information architecture → screen inventory (same IDs as PRD).", "Low-fi wireframes, review with PM; then high-fi in Figma using a token-based design system.",
        "Include loading, empty, error, permission-prompt and offline states for every screen.", "Build clickable prototype; test with ≥5 representative users; log issues and fix Sev-1/2.",
        "Accessibility pass: contrast ≥4.5:1, touch targets ≥48dp (Android) / 44pt (iOS), dynamic text, screen-reader labels.",
        "Export tokens and component specs for developers; get PM + customer sign-off."], "Figma prototype + design tokens + component library", "Gate 4") +
        table(["Standard", "Rule"], [["Platform conventions", "Material guidance on Android, Human Interface Guidelines on iOS (Flutter adaptive widgets where it matters)"],
        ["Tokens", "Colour, type scale, 4/8 pt spacing, radius, elevation defined once; no hard-coded values in screens"],
        ["Originality", "Never copy competitor UI; original brand name, colours, icon and tone"],
        ["Dark mode", "Only if in PRD; tokens must support it from day one"]]))

def phase5():
    tools = table(["Tool", "Check command", "Expected"], [["Git", "git --version", "≥ 2.40"], ["Flutter / Dart", "flutter --version && flutter doctor -v", "No ✗ for chosen targets"],
        ["Node.js (LTS)", "node -v && npm -v", "Active LTS"], ["Android Studio / SDK", "flutter doctor --android-licenses", "Licences accepted"],
        ["Xcode (macOS only)", "xcodebuild -version", "Needed for iOS build/sign"], ["PostgreSQL client", "psql --version", "Matches server major"], ["Postman / Bruno", "n/a", "Collection in docs/api"]])
    tree = code("""mobile-app/
  mobile/        lib/{core,config,services,models,repositories,features,widgets,navigation,state}
                 assets/  test/  integration_test/
  backend/       src/  tests/  migrations/
  admin-dashboard/        (only if PRD requires)
  docs/          requirements/ architecture/ api/ database/ ui-ux/ deployment/ testing/
  infra/         .github/workflows/   .env.example   .gitignore   README.md   CONTRIBUTING.md""")
    gh = checklist(["Private repository per product; org-level 2FA enforced", "main protected: PR required, ≥1 review, status checks required, no force-push",
                    "Branches: feature/<id>-<slug>, fix/…, release/x.y.z; squash-merge", "Commits: Conventional Commits (feat:, fix:, docs:, chore:) + task id",
                    "Actions: lint, unit tests, build APK/AAB/IPA debug, dependency & secret scan, CodeQL", "Dependabot enabled; release tags vX.Y.Z; CHANGELOG generated",
                    "Environments dev / staging / production with separate secrets, databases and API URLs", "Secrets only in GitHub/Cloud secret stores; <code>.env.example</code> lists names only",
                    "Pre-commit secret scan; signing keys stored offline + in vault, never in repo"])
    return (sop("DevOps Engineer", "Approved architecture", ["Install and verify tools on every workstation (table).", "Create repo from the company template; apply branch protection.",
            "Create three environments with separate credentials; wire CI/CD.", "Dry-run: fresh clone → build → test → deploy to dev in under 30 minutes (onboarding test)."], "Configured repo + env", "Gate 5") +
            "<h3>Tool installation checks</h3>" + tabletool(tools) + "<h3>Repository structure</h3>" + tree + "<h3>GitHub standards</h3>" + gh)

def tabletool(t): return t

def phase6():
    mods = ["Splash, onboarding, registration, login, forgot password", "Dashboard, navigation, core modules per PRD", "Forms, search/filter, reports", "Notifications, profile, settings, help, logout"]
    be = ["AuthN/AuthZ (RBAC), user management, business logic, REST endpoints + OpenAPI", "Database access via migrations only; audit trail on writes", "File management (signed URLs), notification service, payments if required", "Structured logging with request ids; central error model"]
    off = table(["Aspect", "Standard"], [["Local DB", "SQLite via Drift; schema versioned and migrated"], ["Outbox", "Every offline write = row in outbox (id, entity, op, payload, created_at, attempts)"],
        ["Retry", "Exponential backoff + jitter, max attempts, then 'needs attention' list"], ["Conflict", "Rule per entity (see §5 callout); conflicts logged and surfaced"],
        ["Indicators", "Offline banner, per-item 'pending' badge, 'last synced' timestamp"], ["Idempotency", "Client-generated UUID + Idempotency-Key header so retries never duplicate"]])
    return (sop("Tech Lead + Developers", "Frozen PRD, approved design, ready repo", ["Slice work by module; each task = one PR linked to PRD screen ID.",
            "Test-first for business rules; mandatory code review; demo to PM every sprint.", "Never build screens not in the PRD; raise a CR instead.",
            "Track estimate vs actual hours per task in the portal.", "At Gate 6, demo every frozen screen against its I/O sheet."], "Working MVP", "Gate 6") +
            "<h3>A. Mobile frontend scope</h3>" + ul(mods) + "<h3>B. Backend scope</h3>" + ul(be) + "<h3>C. Offline standard</h3>" + off +
            "<h3>D. Push notifications</h3>" + ul(["Ask permission in context (not at first launch); explain value", "Store device tokens per user-device; delete on logout; refresh on rotation",
            "Handle invalid-token errors by pruning; never rely on push as the only channel", "Per-category preferences and quiet hours; no personal data in payloads"]) +
            "<h3>E. Admin dashboard (if PRD requires)</h3>" + ul(["User management, roles, settings", "Analytics and reports", "Subscription management, notifications, support tools", "Audit-log viewer; every action by role-based access"]))

def phase7():
    mat = table(["Dimension", "Minimum matrix (replace with analytics-based list at G7)"], [
        ["Android", "API levels covering the top versions by your analytics, incl. one low-RAM (≤3 GB) device"], ["iOS", "Current and previous major iOS; smallest and largest iPhone"],
        ["Network", "Wi-Fi, 4G, 2G-throttled, airplane mode, network switching, request interruption"], ["Tablets", "Only if in PRD"]])
    perf = table(["Metric", "Target (default)"], [["Cold start", "≤ 3 s on mid-range device"], ["Screen load (cached)", "≤ 1 s"], ["API p95", "≤ 500 ms at planned load"],
        ["Crash-free sessions", "≥ 99.5%"], ["ANR rate (Android)", "< 0.47% (Play bad-behaviour threshold; verify current value)"], ["APK/AAB size", "Track; justify >50 MB"]])
    sec = checklist(["Auth & session handling (token expiry, revocation, brute-force)", "Tokens in Keystore/Keychain, not plain storage", "API authorisation per object (IDOR tests)",
        "TLS only, certificate validation, consider pinning with rotation plan", "No secrets / PII in logs, screenshots or crash reports", "Deep links & WebViews validated",
        "Third-party SDK review (data collected vs privacy declaration)", "Data deletion works end to end", "Permissions minimal", "Map results to OWASP MASVS levels; test with MASTG procedures"])
    return (sop("QA Engineer (owner)", "Build candidate + PRD", ["Write test cases from each I/O sheet (traceability matrix: requirement → test → result).", "Run functional, device, network, performance, security tests.",
            "Automate: unit, widget, integration, API and a smoke E2E suite in CI.", "Report defects by severity; Sev-1/2 must be zero to pass.", "Publish the QA & Security Report."], "QA & Security Report", "Gate 7") +
            "<h3>Device and network matrix</h3>" + mat + "<h3>Performance targets</h3>" + perf + "<h3>Security checklist (OWASP MASVS / MASTG)</h3>" + sec +
            table(["Severity", "Definition", "Gate rule"], [["Sev-1", "Crash, data loss, security hole, blocked core flow", "Zero allowed"], ["Sev-2", "Major feature wrong, no workaround", "Zero allowed"],
            ["Sev-3", "Minor with workaround", "Documented, scheduled"], ["Sev-4", "Cosmetic", "Backlog"]]))
