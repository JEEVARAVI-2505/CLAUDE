"""Part C: Phases 10-16, employee system, portal, sources."""
from lib import *
import model as M
I = M.inr

def release():
    a = ["Application ID / package name fixed (reverse-domain, never changes)", "versionName + monotonically increasing versionCode", "Icon, adaptive icon, splash", "Declare only needed permissions",
         "Generate upload key; enrol in Play App Signing; back up keystore offline (2 locations)", "Build signed AAB in CI", "Listing: title, short/full description, screenshots, feature graphic",
         "Data safety form matches SDK audit", "Privacy policy URL live", "Internal testing → closed/open testing as account type requires", "Submit for review; release staged (e.g. 10%→50%→100%)"]
    i = ["Bundle identifier registered; certificates + provisioning managed (Xcode managed signing or fastlane match)", "Version + build number; entitlements (push, associated domains)",
         "Usage-description strings for every permission", "Archive in Xcode / CI macOS runner; upload via Xcode, Transporter or altool replacement", "App Store Connect record, screenshots per required device size, description, keywords",
         "App Privacy ('nutrition label') answers match SDK audit", "Demo account + review notes for App Review", "TestFlight internal then external beta", "Submit; respond to review within 24 h; phased release"]
    post = ["Install from store on clean devices", "Register / login / password reset", "Main workflow end-to-end on production", "API connectivity to production only", "Payments (real small transaction, refunded)",
            "Push received on Android + iOS", "Crash reporting + analytics receiving events", "Support link and privacy policy open", "Store declarations reviewed against actual data flows"]
    return (sop("Release Manager + DevOps", "Gate 7 and Gate 9 approvals", ["Run Android and iOS checklists in parallel.", "Re-verify current store requirements on official pages on the submission day (policies, target API level, privacy rules change).",
            "Submit only after Gate 10 sign-off. Do not promise an approval date to customers.", "Run post-release verification within 2 hours of going live."], "Published, verified app", "Gate 10") +
            "<h3>Android release</h3>" + checklist(a) + "<h3>iOS release</h3>" + checklist(i) + "<h3>Post-release verification</h3>" + checklist(post) +
            callout("warn", "Common rejections", "Missing demo credentials, privacy declarations that contradict SDK behaviour, broken links, crashes at launch, requesting permissions without clear purpose, in-app purchase rules for digital goods. Verify current store policy text before submission."))

def deploy():
    steps = ["Register domain (ICANN-accredited registrar); enable registrar lock + 2FA", "Add DNS records <b>exactly as the hosting provider displays them</b> (A/AAAA/CNAME/TXT); never guess records",
             "Deploy backend container; set environment variables from secret manager", "Provision managed PostgreSQL; restrict to private network; enable automated backups + PITR",
             "Object storage with private buckets + signed URLs", "TLS certificate auto-renew; HSTS", "Run migrations through CI only (forward-only, reversible plan)",
             "Monitoring: uptime, error rate, p95 latency, queue depth, disk; alert routes to on-call", "Backup restore drill (documented RTO/RPO)", "Rollback: previous image tag + DB migration compatibility rule (expand/contract)",
             "Separate dev / staging / prod URLs, DBs and keys", "Production smoke test checklist signed by QA"]
    return (sop("DevOps Engineer", "Gate 8 and Gate 7", ["Follow the list in order; record evidence (screenshot or log) for each item.", "Never put production secrets in the mobile app: anything shipped in an app can be extracted. Use per-user tokens; keep third-party keys server-side or restricted by package/bundle ID and quota."], "Production environment", "Gate 9") + checklist(steps))

def sales():
    pack = ["Working demo build + synthetic accounts (no real customer data)", "Seed data script", "Demo script (10 min): problem → 3 key workflows → admin view → limits → pricing",
            "Sales deck, one-page brochure, pricing sheet", "Proposal, Statement of Work, contract", "Onboarding guide, user manual, admin manual, training slides, handover document"]
    own = table(["Topic", "Default position (legal review required)"], [["Source code", "Client builds: ownership of customer-specific code transfers on full payment; reusable components remain company IP, licensed to the client"],
        ["Third-party licences", "Listed in THIRD_PARTY register with licence type; client receives the list"], ["Customer data", "Belongs to the customer; processor terms (DPA) define use, retention, deletion on exit"],
        ["Limits & warranty", "30-60 day defect warranty on frozen scope; no warranty on third-party outages or store decisions"], ["Pilot", "Paid pilot (e.g. 60 days) with success metrics and conversion price agreed upfront"]])
    return (sop("Sales Executive + PM", "Gate 8 pricing, working build", ["Prepare demo environment; rehearse; state limitations honestly.", "Present pricing from the approved sheet only; deviations need founder approval.", "Convert to pilot or subscription; execute SoW.", "Handover and onboarding at Gate 11."], "Signed customer + onboarding", "Gate 11") +
            "<h3>Showcase kit</h3>" + checklist(pack) + "<h3>Ownership and licensing</h3>" + own)

def maintenance():
    tasks = ["Crash and ANR monitoring (daily review)", "Performance monitoring vs targets", "Security patches and dependency updates (monthly)", "New Android / iOS release compatibility (each beta season)",
             "Store compliance updates (target API level, privacy rules)", "API and DB maintenance (indexes, vacuum, migration hygiene)", "Backup verification monthly; restore drill quarterly",
             "Bug fixes by severity SLA", "Feature updates via CR", "Subscription renewals and version management (min supported version, forced-update flag)"]
    return ("<p>Prices and cost build-up are in §11 (9.7). Operational duties:</p>" + checklist(tasks) +
            table(["Severity", "Basic", "Professional", "Enterprise"], [["P1 outage / data risk", "Next business day", "4 business hours", "1 hour, 24x7 on-call"], ["P2 major defect", "3 days", "1 day", "4 hours"], ["P3 minor", "Backlog", "Monthly release", "Weekly release"]]))

def people():
    roles = [["Business Researcher", "Evidence register, market report", "Unsourced numbers"], ["Product Manager", "PRD, CRs, budget, schedule, customer comms", "Scope creep"],
             ["UI/UX Designer", "Prototype, tokens, usability results", "Unapproved UI changes"], ["Android / iOS / Flutter-RN Dev", "Mobile code, unit/widget tests", "Merging without review"],
             ["Backend Developer", "APIs, business logic, OpenAPI", "Breaking API changes without version"], ["Database Engineer", "Schema, migrations, backups, performance", "Manual prod changes"],
             ["QA Engineer", "Test plan, automation, release verdict", "Passing with open Sev-1/2"], ["DevOps Engineer", "CI/CD, infra, monitoring, secrets", "Secrets in repo"],
             ["Sales Executive", "Pipeline, demos, proposals", "Off-price discounts"], ["Customer Support Executive", "Tickets, first-line triage, feedback", "Promising unscoped work"]]
    rec = table(["Project record field", "Example"], [["Project ID", "PRJ-2026-001"], ["Project manager / tech lead / QA owner", "named people"], ["Developers assigned", "named, with % allocation"],
        ["Budget (approved)", I(M.L_E + M.SETUP_COSTS)], ["Approved scope", "PRD v1.0-frozen link"], ["Estimated hours / actual hours", f"{M.H_E:,.0f} / (from time log)"],
        ["Production cost", "labour actual + non-labour actual"], ["Selling price / expected margin", f"{I(M.CP1['target'])} / {M.TARGET_M*100:.0f}%"], ["Deadline / status", "date / stage tracker value"]])
    wk = code("""WEEKLY PERFORMANCE REPORT  (week ending ____)  Project: ______
Tasks planned / completed / carried over:   __ / __ / __
Hours estimated vs actual (variance %):     ____ / ____ ( __ %)
Defects opened / closed / reopened (by severity):
Delays (cause, days) and recovery plan:
Cost overrun to date (₹ and % of budget):
Customer feedback / NPS / tickets:
Risks and decisions needed from managers:""")
    return (table(["Role", "Accountable for", "Common failure to prevent"], roles) + "<p>In a small team one person may hold several roles; <b>hours are logged once</b> against the role actually performed, and the QA owner must not approve their own code.</p>" +
            "<h3>Project record (required for every project)</h3>" + rec + "<h3>Weekly report template</h3>" + wk)

def portal():
    mods = ["Company dashboard", "Project creation", "Market research", "Product requirements", "Scope approval", "UI/UX approval", "Employee assignment", "Development tracking",
            "GitHub integration", "Build management", "QA tracking", "Device testing", "Production cost calculator", "App pricing calculator", "Cloud cost calculator",
            "Android publishing tracker", "iOS publishing tracker", "Domain & DNS tracker", "Customer management", "Sales management", "Subscription management",
            "Maintenance tracker", "Employee performance", "Financial reports"]
    mm = "".join(f"<div>{i+1}. {m}</div>" for i, m in enumerate(mods))
    ent = table(["Entity", "Key fields"], [["Project", "id, name, type, stage, manager, tech_lead, qa_owner, budget, price, deadline"], ["StageGate", "project_id, gate (1-11), status (pending/approved/rejected/revision), approver, date, evidence[]"],
        ["Deliverable", "project_id, gate, file, uploaded_by, version"], ["TimeEntry", "project_id, task, role, user, hours, date"], ["CostLine", "project_id, category, estimated, actual, source_url, checked_on"],
        ["Customer / Subscription", "plan, MRR, start, renewal, churn_flag"], ["Incident", "project_id, severity, opened, resolved, root_cause"], ["PriceSource", "provider, page URL, value, currency, access date, reviewer"]])
    dash = ul(["Estimated vs actual project cost and variance", "Estimated vs actual monthly infrastructure", "Selling price, gross margin, MRR, active customers", "Pending tasks, delayed projects, production incidents"])
    return ("<p>Build the portal as the company's own product using this OS: PRD first, then Gate-by-Gate. Roles: Employee (upload deliverables, log time), Manager (approve / reject / request revision), Finance, Admin.</p>" +
            f'<div class="flow">{mm}</div><h3>Stage tracker rules</h3>' + ol(["Each stage has required deliverables; the stage cannot close until all are uploaded.", "Manager decision is one of Approve / Reject / Request revision, with comment; all decisions are audit-logged.", "A rejected gate re-opens the stage and notifies the owner."]) +
            "<h3>Core data model</h3>" + ent + "<h3>Dashboard KPIs</h3>" + dash +
            callout("ok", "Suggested build", "Phase-1 portal MVP in 6-8 weeks: projects, gates, deliverables, time entries, cost lines, price-source register. Calculators reuse the formulas in §10-§11."))

SRC = [("Android developer docs", "https://developer.android.com/docs"), ("Kotlin", "https://kotlinlang.org/docs/home.html"), ("Android Studio", "https://developer.android.com/studio"),
       ("Flutter", "https://docs.flutter.dev/"), ("React Native", "https://reactnative.dev/docs/getting-started"), ("Apple Developer documentation", "https://developer.apple.com/documentation/"),
       ("Apple Human Interface Guidelines", "https://developer.apple.com/design/human-interface-guidelines/"), ("App Store Connect help", "https://developer.apple.com/help/app-store-connect/"),
       ("Google Play Console help", "https://support.google.com/googleplay/android-developer/"), ("Firebase", "https://firebase.google.com/docs"), ("Supabase", "https://supabase.com/docs"),
       ("AWS", "https://docs.aws.amazon.com/"), ("Microsoft Azure", "https://learn.microsoft.com/azure/"), ("Google Cloud", "https://cloud.google.com/docs"), ("GitHub", "https://docs.github.com/"),
       ("OWASP MASVS", "https://mas.owasp.org/MASVS/"), ("OWASP MASTG", "https://mas.owasp.org/MASTG/"), ("NIST CSRC", "https://csrc.nist.gov/"), ("ICANN", "https://www.icann.org/"),
       ("MeitY", "https://www.meity.gov.in/"), ("CBIC GST", "https://cbic-gst.gov.in/")]

def sources():
    rows = [[n, f'<a href="{u}">{u}</a>'] for n, u in SRC]
    ver = table(["Figure used in this document", "Status", "Action at Gate 8"], [
        ["Google Play registration fee", "Last known - not verified", "Read official Play Console help; record fee, currency, date"], ["Apple Developer Program fee", "Last known - not verified (page fetch did not show price)", "Read developer.apple.com programs page"],
        ["Cloud, SMS, email, storage unit prices", "Planning assumptions", "Use official calculators; paste output + date in PriceSource"], ["SaaS tools (GitHub, Figma, Sentry…)", "Planning", "Check pricing pages"],
        ["Income tax & GST rates", "Planning - verify", "Confirm with CA / CBIC notifications"], ["Android vitals thresholds, store policies", "Reference only", "Re-read policy on submission day"]])
    return ("<h3>Official sources (primary references)</h3>" + table(["Source", "URL"], rows) + "<h3>Verification log - items that must be refreshed</h3>" + ver +
            callout("bad", "Pricing rule", "Every pricing calculation must cite the official provider page and the date checked. A price without source and date is not allowed into a quotation."))

def appendix():
    return ("<h3>Glossary</h3>" + table(["Term", "Meaning"], [["PERT", "Three-point estimate (O + 4M + P) ÷ 6"], ["ARPA", "Average revenue per account per month"], ["MAU/DAU", "Monthly / daily active users"],
        ["CAC / LTV", "Customer acquisition cost / lifetime value (gross margin ÷ churn)"], ["RTO / RPO", "Recovery time / data-loss window objective"], ["PWA", "Progressive web app"],
        ["MASVS / MASTG", "OWASP Mobile verification standard / testing guide"], ["CR", "Change request"]]) +
            "<h3>Day-1 action plan</h3>" + ol(["Fill intake (§1) and confirm roles/rates (§10.1).", "Create portal repo from template; apply Gate rules.", "Start Phase 1 for SiteDiary (10 interviews in 2 weeks).",
            "Open Google Play and Apple developer accounts early (identity checks take time).", "Replace every Planning figure by verified source + date before Gate 8."]) +
            callout("warn", "Structural / safety note", "This OS contains no engineering-safety claims. For apps in regulated domains (health, finance, construction safety) obtain domain-expert and legal review; AI-generated output must be verified by a qualified person."))
