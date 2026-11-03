"""Part D: elaborated, precise add-ons appended to the phase sections."""
from lib import *
import model as M
I = M.inr

# ------------------------------------------------------------------ Phase 1
def p1_extra():
    score = [("Problem severity (pain, frequency)", 20), ("Willingness to pay (stated + observed)", 20), ("Market size reachable (SAM)", 15),
             ("Competitive gap (missing features/complaints)", 15), ("Acquisition ease (channels, sales cycle)", 15), ("Fit with our team and stack", 10), ("Regulatory / platform risk (inverse)", 5)]
    rows = [[n, w, "0-5", "w × score ÷ 5"] for n, w in score] + [["Total", 100, "", "Score out of 100"]]
    gate = table(["Decision", "Rule"], [["GO", "Score ≥ 70 and ≥ 10 interviews with ≥ 3 stating budget or paying a deposit/pilot"], ["PIVOT", "50-69, or strong pain but weak payment evidence: change segment, price or scope and re-test"],
                                         ["STOP", "< 50, or any red-flag item (e.g. platform bans the use case, unavoidable compliance cost > 6 months of revenue)"]])
    outline = ol(["Executive summary and recommendation (go / pivot / stop)", "Problem and customer definition (buyer, user, payer)", "Method and evidence register (every fact: ID, URL, access date, confidence)",
                  "Competitor teardown: features, rating, review count, price, last update, top complaints (Play Store, App Store, Product Hunt, G2, Capterra, Reddit, own sites)", "Demand signals: Google Trends, search terms, community threads, interview findings",
                  "Bottom-up TAM / SAM / SOM with formulas and inputs sourced", "Monetisation options and chosen model with price test results", "Acquisition plan and difficulty rating", "Risks, assumptions, open questions", "Appendix: interview notes, raw review tags"])
    script = ol(["Warm-up: role, team size, how a normal day runs", "Last time the problem happened: what, when, cost, who was affected", "Current workaround and tools; what they pay today",
                 "Show nothing yet; ask for their ideal solution", "Price anchor: 'what would be too cheap to trust, what too expensive?' (Van Westendorp)", "Who approves purchases; how long it took last time", "Close: would you pilot for 60 days? Can we collect a deposit?"])
    tree = table(["If…", "Choose"], [["Single user, low price, no recurring value", "Paid app (one-time) or ads - usually weak; avoid"], ["Recurring value, individual or small team", "Subscription (monthly/annual)"],
                ["Broad free usage, small paying share", "Freemium - only if free tier has low cost-to-serve (see §10 per-user cost)"], ["Process-critical, multi-user, integrations, SLA", "Enterprise / B2B contract with pilot"]])
    return ("<h3>Go / pivot / stop scorecard</h3>" + table(["Criterion", "Weight", "Scale", "Formula"], rows, num_cols=(1,), total_last=True) + gate +
            "<h3>Market research report outline</h3>" + outline + "<h3>Interview script (30 minutes)</h3>" + script + "<h3>Monetisation decision guide</h3>" + tree)

# ------------------------------------------------------------------ Phase 2
def p2_extra():
    prd = table(["#", "PRD section", "Minimum content", "Reviewer"], [
        [1, "Objective", "Problem, success metrics (e.g. reports submitted per site per week), non-goals", "Founder"], [2, "Target users", "Personas, devices, languages, connectivity", "PM"],
        [3, "Android / iOS requirements", "Min OS versions from analytics; tablets yes/no", "Tech lead"], [4, "Roles & permissions", "RBAC matrix below", "PM + security"], [5, "Modules", "Module list with priority (MoSCoW)", "PM"],
        [6, "Screens & navigation", "Screen IDs, nav graph, deep links", "UX"], [7, "User journeys", "Happy path + 3 failure paths each", "UX + QA"], [8, "Inputs / outputs", "Per-screen I/O sheets", "Tech lead"],
        [9, "Reports", "Layout, filters, export formats", "PM"], [10, "Notifications", "Matrix below", "PM"], [11, "Offline vs online", "Entity-level offline rules + conflict policy", "Tech lead"],
        [12, "Backend & DB", "Entities, APIs, retention", "Tech lead"], [13, "Integrations & SDKs", "Each SDK: purpose, data collected, licence", "Security"], [14, "Security", "MASVS L1 baseline; L2 where sensitive", "Security"],
        [15, "Performance", "Targets in §9", "QA"], [16, "Device compatibility", "Support matrix", "QA"], [17, "Accessibility", "WCAG 2.1 AA targets, TalkBack/VoiceOver pass", "UX"], [18, "Maintenance", "Support package, update policy", "PM"]])
    rbac = table(["Capability", "Owner (Admin)", "Manager", "Supervisor", "Worker (view)"], [["Create project / site", "✔", "✔", "-", "-"], ["Create daily report", "✔", "✔", "✔", "-"], ["Edit submitted report", "✔", "Request edit", "-", "-"],
                 ["View reports", "All", "Own sites", "Own site", "Own attendance"], ["Manage users / billing", "✔", "-", "-", "-"], ["Export PDF", "✔", "✔", "✔", "-"], ["View audit log", "✔", "-", "-", "-"]])
    notif = table(["Event", "Channel", "Recipient", "Timing", "User can disable?"], [["Report submitted", "Push + email PDF", "Owner/Manager", "Immediately", "Push yes, email no"], ["Report not submitted by 7 pm", "Push", "Supervisor", "7:00 pm local", "Yes"],
                  ["Sync failed > 1 h", "In-app + push", "Supervisor", "After 1 h", "No (data safety)"], ["Plan renewal due", "Email", "Owner", "7 days before", "No"], ["New device login", "Email", "User", "Immediately", "No"]])
    mp = table(["Screen", "Action", "API", "DB operation", "Offline?"], [
        ["S-001 Login", "Verify OTP", "POST /v1/auth/otp/verify", "SELECT user; INSERT session", "No"], ["S-010 Site list", "Open", "GET /v1/sites?updated_after=", "SELECT sites (scoped by org)", "Read cache"],
        ["S-014 Attendance", "Save", "POST /v1/reports/{id}/attendance", "UPSERT attendance", "Yes (outbox)"], ["S-016 Photos", "Add photo", "POST /v1/uploads (chunked)", "INSERT file_meta; object-store PUT", "Yes (queued)"],
        ["S-020 Submit", "Submit day", "POST /v1/reports/{id}/submit", "UPDATE report.status; INSERT audit", "Yes (outbox)"], ["S-031 Export", "PDF", "GET /v1/reports/{id}/pdf", "SELECT report + children", "Online only"]])
    return ("<h3>PRD contents (all 24 required headings map to these sections)</h3>" + prd + "<h3>Role and permission matrix (SiteDiary)</h3>" + rbac + "<h3>Notification matrix</h3>" + notif +
            "<h3>Screen → API → database map (extract)</h3>" + mp)

# ------------------------------------------------------------------ Phase 3
OPT = ["Flutter", "Native (Kotlin + Swift)", "React Native", "PWA"]
CRIT = [("Development cost", 20, (4, 2, 4, 5)), ("Performance", 10, (4, 5, 3, 3)), ("Platform-specific capability", 10, (3, 5, 3, 2)), ("Maintenance cost", 15, (4, 2, 4, 5)),
        ("Offline capability", 10, (4, 5, 4, 3)), ("Team experience", 15, (5, 2, 3, 4)), ("Security controls", 5, (4, 5, 4, 4)), ("Scalability / vendor independence", 15, (4, 4, 4, 4))]
def scores():
    return [sum(w * s[i] for _, w, s in CRIT) / 5 for i in range(4)]
def p3_extra():
    sc = scores()
    rows = [[n, w] + list(s) for n, w, s in CRIT] + [["Weighted score (out of 100)", 100] + [f"{x:.0f}" for x in sc]]
    best = OPT[sc.index(max(sc))]
    adr = code("""ADR-001  Mobile framework for <product>            Status: Proposed | Accepted | Superseded
Context:      requirement, constraints, team skills, budget
Options:      A, B, C (with scores from the matrix)
Decision:     <chosen option> because …
Consequences: positive / negative / risks / what would make us revisit
Approved by:  CTO, date""")
    cmp = table(["Layer", "Options evaluated", "Strengths", "Watch-outs"], [
        ["Mobile", "Kotlin/Compose; Swift/SwiftUI; Flutter; React Native", "Native: best platform access. Flutter: one codebase, consistent UI. RN: web-team reuse", "Native doubles cost; Flutter large binary; RN bridge/upgrade churn"],
        ["Backend", "NestJS, FastAPI, ASP.NET Core", "NestJS: TypeScript shared skills, OpenAPI; FastAPI: fast data/AI work; .NET: strong typing & tooling", "Pick the one the team can operate at 2 a.m."],
        ["Database", "PostgreSQL, MySQL, Firestore, SQLite, Redis", "PostgreSQL: relational integrity, JSON, PITR; SQLite: on-device; Firestore: simple realtime", "Firestore pricing scales with reads; Redis adds ops burden"],
        ["Cloud", "AWS, Azure, Google Cloud, Firebase, Supabase, managed PaaS", "Managed PaaS: fastest to operate; Supabase/Firebase: fast MVP", "Vendor lock-in, egress cost, free-tier changes, data residency"]])
    return ("<h3>Worked decision - SiteDiary</h3>" + table(["Criterion", "Weight", *OPT], rows, num_cols=(1, 2, 3, 4, 5), total_last=True) +
            f"<p>Result: <b>{best}</b> scores highest ({max(sc):.0f}). Scores are the team's judgement on a 1-5 scale and must be re-scored per project with evidence; here the offline requirement and the 6-person team's Dart/TypeScript skills drive the outcome. A PWA was rejected because reliable background sync, photo queueing and push on iOS are weaker than needed for site use.</p>" +
            "<h3>Technology comparison notes</h3>" + cmp + "<h3>Architecture Decision Record template</h3>" + adr)

# ------------------------------------------------------------------ Phase 4
def p4_extra():
    states = table(["State", "Required on every screen", "Example copy"], [["Loading", "Skeleton or spinner after 400 ms", "Loading today's report…"], ["Empty", "Illustration, reason, one primary action", "No reports yet - start today's report"],
                   ["Error", "What happened, what to do, retry; request id in details", "Couldn't load. Check connection and try again"], ["Offline", "Banner + pending badge", "Offline - changes will sync automatically"],
                   ["Permission", "Pre-prompt explaining the need", "Camera is needed to attach site photos"], ["Success", "Non-blocking confirmation", "Report submitted"]])
    deliver = checklist(["Journey maps (3 personas)", "IA sitemap and navigation graph", "Screen inventory with PRD IDs", "Low-fi wireframes reviewed with PM", "Design tokens (colour, type, spacing, radius, elevation)",
                         "Component library with states", "High-fi screens, light (+dark if in PRD)", "Clickable prototype for the 5 core journeys", "Usability test report (≥5 users, task success rate, SUS score)", "Accessibility report", "Developer handoff notes + asset export"])
    return "<h3>Mandatory states</h3>" + states + "<h3>Design deliverables (Gate 4 evidence)</h3>" + deliver + callout("ok", "Usability pass mark", "≥ 80% task success on core tasks, SUS ≥ 68, zero Sev-1 usability issues open.")

# ------------------------------------------------------------------ Phase 5
def p5_extra():
    yml = code("""name: ci
on: { pull_request: { branches: [main] }, push: { branches: [main] } }
jobs:
  mobile:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: subosito/flutter-action@v2
        with: { channel: stable }
      - run: flutter pub get
      - run: flutter analyze --fatal-infos
      - run: flutter test --coverage
      - run: flutter build apk --debug
  backend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version: 'lts/*' }
      - run: npm ci && npm run lint && npm test -- --coverage
  security:
    runs-on: ubuntu-latest
    permissions: { security-events: write }
    steps:
      - uses: actions/checkout@v4
      - uses: github/codeql-action/init@v3
      - uses: github/codeql-action/analyze@v3""")
    prot = table(["Setting (main branch)", "Value"], [["Require pull request", "Yes, ≥ 1 approval; dismiss stale approvals"], ["Required status checks", "mobile, backend, security"], ["Force pushes / deletions", "Blocked"], ["Linear history", "Squash merge"], ["Signed commits", "Recommended"]])
    commits = code("""feat(S-014): attendance form with offline outbox        (PRD S-014)
fix(sync): retry with jitter after 429                   (BUG-231)
docs: update API v1 error codes
chore(deps): bump flutter_riverpod
Release tags: v1.0.0 (MAJOR.MINOR.PATCH); hotfix = PATCH""")
    envs = table(["Environment", "Purpose", "Data", "Who deploys"], [["dev", "Daily integration", "Synthetic", "Auto on merge"], ["staging", "Release candidate, QA, demos", "Anonymised/synthetic", "Auto on tag rc"], ["production", "Customers", "Real", "Manual approval (Gate 9)"]])
    return "<h3>Sample CI workflow (.github/workflows/ci.yml)</h3>" + yml + "<h3>Branch protection</h3>" + prot + "<h3>Commit convention</h3>" + commits + "<h3>Environment separation</h3>" + envs

# ------------------------------------------------------------------ Phase 6
def p6_extra():
    dod = checklist(["Matches the screen I/O sheet (fields, validation, errors, offline)", "Unit tests for business rules; widget/integration test for the flow", "Code review approved (not by author); static analysis clean",
                     "Accessibility labels and 48dp targets", "No secrets or PII in logs", "Analytics events per tracking plan", "Demo-able on staging; QA verified; docs updated"])
    api = table(["API standard", "Rule"], [["Versioning", "/v1 prefix; breaking change = new version; deprecate with 6-month notice"], ["Errors", "{code:'E-4101', message, details[], request_id}; never leak stack traces"],
                 ["Pagination", "cursor-based; `updated_after` for sync"], ["Idempotency", "Idempotency-Key header on all POST that create"], ["Auth", "Short-lived access token (15 min) + rotating refresh token, device-bound"],
                 ["Rate limit", "Per user + per IP; 429 with Retry-After"], ["Docs", "OpenAPI generated and published to docs/api"]])
    cad = table(["Ceremony", "Cadence"], [["Sprint", "2 weeks; plan Monday, demo + retro last Friday"], ["Daily stand-up", "15 min"], ["Backlog refinement", "Weekly, PM + tech lead"], ["Gate 6 review", "End of build: demo of all frozen screens"]])
    return "<h3>Definition of Done</h3>" + dod + "<h3>API standards</h3>" + api + "<h3>Delivery cadence</h3>" + cad

# ------------------------------------------------------------------ Phase 7
def p7_extra():
    masvs = table(["MASVS category", "What we test", "Evidence"], [["MASVS-STORAGE", "No sensitive data in plain files/logs/backups; Keystore/Keychain for tokens", "Static review + device file inspection"],
        ["MASVS-CRYPTO", "Standard algorithms, no hard-coded keys", "Code scan, review"], ["MASVS-AUTH", "Session expiry, refresh rotation, biometric optional, brute-force limits", "API tests"],
        ["MASVS-NETWORK", "TLS only, valid certificates, no cleartext traffic exceptions", "Proxy test (mitm)"], ["MASVS-PLATFORM", "Deep links, intents, WebViews, permissions", "Manual + automated"],
        ["MASVS-CODE", "Dependencies updated, obfuscation/minify, no debug flags in release", "SCA scan, build audit"], ["MASVS-RESILIENCE", "Tamper/root detection only if risk justifies", "Threat model decision"],
        ["MASVS-PRIVACY", "Data minimisation, consent, deletion, SDK disclosures", "Privacy review vs store declarations"]])
    pyr = table(["Layer", "Target", "Tooling (typical)"], [["Unit", "≥ 80% on business logic", "flutter test / Jest"], ["Widget/component", "All reusable widgets, key screens", "flutter widget tests"],
                 ["API", "Every endpoint, auth + validation paths", "Supertest / Postman-Newman"], ["Integration", "Sync engine, DB migrations", "integration_test"], ["E2E smoke", "5 core journeys on 2 devices", "Patrol / Maestro / Appium"], ["Regression", "Entire suite on every release candidate", "CI"]])
    trace = table(["Requirement", "Screen", "Test case", "Result", "Defects"], [["REQ-014 Attendance hours 0-16", "S-014", "TC-0141 boundary values", "Pass", "-"], ["REQ-031 Offline submit", "S-020", "TC-0311 airplane mode submit then reconnect", "Pass", "-"]])
    return "<h3>MASVS category mapping</h3>" + masvs + "<h3>Test pyramid and targets</h3>" + pyr + "<h3>Traceability matrix (sample rows)</h3>" + trace

# ------------------------------------------------------------------ Phase 10/11
def p10_extra():
    tl = table(["Step", "Owner", "Lead time (plan, not guaranteed)"], [["Developer account + identity / D-U-N-S verification", "Founder", "Start at Phase 3; can take days to weeks"],
               ["Internal testing build", "Release manager", "Same day"], ["Closed / beta testing (TestFlight, Play testing tracks)", "QA", "1-2 weeks"], ["Store review", "Store", "Varies; never promise a date"],
               ["Staged rollout 10% → 50% → 100%", "Release manager", "3-7 days with crash monitoring"]])
    return "<h3>Release timeline</h3>" + tl

def p11_extra():
    dns = table(["Record", "Name", "Value", "Purpose"], [["A / AAAA or CNAME", "api", "<i>exact value shown by hosting provider</i>", "API endpoint"], ["CNAME", "admin", "<i>from provider</i>", "Admin dashboard"],
              ["TXT", "@", "<i>from provider (domain verification)</i>", "Ownership proof"], ["MX / TXT (SPF) / CNAME (DKIM)", "@, _domainkey", "<i>from email provider</i>", "Transactional email deliverability"], ["TXT (DMARC)", "_dmarc", "<i>policy chosen by security lead</i>", "Spoofing protection"]])
    env = table(["Variable (names only)", "Where stored", "Rotation"], [["DATABASE_URL", "Cloud secret manager", "On staff change / 90 days"], ["JWT_SIGNING_KEY", "Secret manager", "90 days"], ["FCM_SERVICE_ACCOUNT", "Secret manager", "Annually"],
                 ["SMTP / EMAIL_API_KEY", "Secret manager", "90 days"], ["PAYMENT_GATEWAY_SECRET", "Secret manager", "On suspicion / annually"], ["STORAGE_BUCKET", "Env config (not secret)", "-"]])
    rb = ol(["Declare incident; freeze deploys; notify on-call + tech lead", "Identify last good release tag", "Redeploy previous container image", "If a migration ran: apply compatible down-migration or deploy compatibility patch (expand/contract rule)",
             "Verify smoke tests; monitor error rate 30 min", "Post-incident review within 3 working days"])
    return "<h3>DNS record plan</h3>" + dns + "<p class=small>Always copy values exactly from the hosting or email provider's dashboard; do not guess.</p><h3>Environment variable catalogue</h3>" + env + "<h3>Rollback runbook</h3>" + rb

# ------------------------------------------------------------------ Phase 12
def p12_extra():
    demo = table(["Minute", "Step", "Message"], [["0-1", "Problem story", "A supervisor loses a day of site records when the network drops"], ["1-3", "Login + site list", "Setup in under 2 minutes"], ["3-6", "Daily report offline", "Airplane mode: attendance, materials, photos"],
                 ["6-7", "Reconnect", "Automatic sync; owner gets PDF"], ["7-8", "Admin dashboard", "Users, plans, audit log"], ["8-9", "Limits", "State what is not included"], ["9-10", "Pricing + pilot offer", "60-day pilot, success metrics, conversion price"]])
    sow = ol(["Parties, effective date", "Objectives and in-scope modules (frozen PRD reference)", "Out of scope and assumptions", "Deliverables and acceptance criteria per milestone", "Timeline and customer responsibilities",
              "Fees, payment schedule (e.g. 30% advance, 40% on Gate 6, 30% on handover), GST", "Change control (CR form)", "IP, licensing and data ownership", "Warranty, support package, SLA", "Confidentiality, termination, liability cap, governing law"])
    return "<h3>10-minute demo script</h3>" + demo + "<h3>Statement of Work outline</h3>" + sow

# ------------------------------------------------------------------ Phase 14/15
def p14_extra():
    ra = [["G1 Market", "R/A", "C", "", "", "", "", "", "I"], ["G2 Scope", "A", "R", "C", "C", "C", "", "", "I"], ["G3 Architecture", "C", "C", "", "R/A", "", "", "C", ""], ["G4 UI/UX", "A", "R", "C", "", "", "", "", ""],
          ["G5 Setup", "", "", "", "A", "", "", "R", ""], ["G6 MVP", "A", "", "R", "R", "C", "", "", ""], ["G7 QA/Security", "I", "", "C", "C", "R/A", "", "C", ""], ["G8 Pricing", "R", "", "", "C", "", "A (Finance)", "", "C"], ["G9 Deploy", "I", "", "", "C", "C", "", "R/A", ""],
          ["G10 Release", "A", "", "C", "R", "C", "", "R", ""], ["G11 Handover", "A", "", "", "", "C", "", "", "R"]]
    return ("<h3>RACI by gate (R responsible, A accountable, C consulted, I informed)</h3>" + table(["Gate", "PM", "UX", "Mobile dev", "Tech lead / Backend", "QA", "Finance / Founder", "DevOps", "Sales / Support"], ra) +
            "<h3>KPIs tracked weekly</h3>" + table(["KPI", "Formula", "Target"], [["Estimate accuracy", "Actual hours ÷ expected hours", "0.9 - 1.15"], ["Schedule variance", "(Actual - planned days) ÷ planned days", "≤ 10%"], ["Defect escape rate", "Prod defects ÷ total defects", "< 10%"],
             ["Rework ratio", "Hours on reopened items ÷ total hours", "< 15%"], ["Budget variance", "(Actual cost - budget) ÷ budget", "≤ 10%"], ["Customer satisfaction", "NPS / CSAT from pilot users", "NPS ≥ 30"]]))

def p15_extra():
    st = table(["Gate", "Deliverables the employee must upload"], [["1", "Market report PDF, evidence register, interview notes"], ["2", "PRD, screen sheets, API/DB map, risk log"], ["3", "ADR, 11 architecture docs, threat model"], ["4", "Figma link, tokens, usability report"],
                 ["5", "Repo URL, CI run, env checklist"], ["6", "Demo recording, test summary, build artefact"], ["7", "QA report, MASVS checklist, device matrix results"], ["8", "Cost model, price sources (URL + date), pricing sheet"],
                 ["9", "Restore drill log, monitoring screenshots, rollback test"], ["10", "Store checklists, screenshots, privacy declarations, review notes"], ["11", "Manuals, training record, handover signature"]])
    return "<h3>Required uploads per gate</h3>" + st

# ------------------------------------------------------------------ project plan + compliance
def plan():
    rows = [["Phase 1 Market research", "W1-W2", "PM"], ["Phase 2 PRD + I/O freeze", "W3-W4", "PM, UX, Tech lead"], ["Phase 3 Architecture", "W4-W5", "Tech lead"], ["Phase 4 UI/UX", "W5-W8", "UX"], ["Phase 5 Setup", "W6", "DevOps"],
            ["Phase 6 Build (Sprints 1-5)", "W7-W16", "Dev team"], ["Phase 7 QA + security", "W15-W18", "QA, security"], ["Phase 8-9 Costing & pricing", "W17-W18", "PM, Finance"], ["Phase 11 Production deploy", "W18-W19", "DevOps"],
            ["Phase 10 Store submission", "W19-W20 (+ review time)", "Release manager"], ["Phase 12 Pilot start", "W20+", "Sales"]]
    comp = table(["Topic (India)", "Why it matters", "Action - verify with counsel/CA"], [["Digital Personal Data Protection Act, 2023", "Worker/site-photo data may be personal data", "Consent notices, retention limits, deletion path, breach process"],
               ["GST on SaaS and services", "18% collected on subscriptions", "GST registration; invoices; confirm rates with CA / CBIC"], ["SMS (TRAI DLT) registration", "Transactional SMS/OTP needs registered sender + templates", "Register through chosen SMS provider"],
               ["Store policies", "Privacy declarations must match SDK behaviour", "Re-read policies on submission day"], ["Contracts & IP", "Source-code ownership, data processing terms", "Lawyer-reviewed templates"]])
    return "<h3>Indicative project plan - SiteDiary (20 weeks)</h3>" + table(["Phase", "Weeks", "Lead"], rows) + "<h3>Compliance checklist (India)</h3>" + comp
