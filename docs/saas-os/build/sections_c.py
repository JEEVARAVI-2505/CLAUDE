"""Sample product, templates, dashboard, appendices."""
import calc
from calc import inr
from sections_a import table, callout

def sample():
    P = calc.pricing(); _, t = calc.labour()
    h = f'''<p><b>Product:</b> SlotWise – online appointment booking, reminders and no-show reduction for small clinics in India. <b>Status: worked example for training.</b> Market figures below are illustrative placeholders that demonstrate the method; they are not market facts and the competitor research has deliberately not been invented.</p>'''
    h += "<h3>S1. Market research (method + placeholders)</h3>" + table(["Item","Worked example"],[
      ("Segment / ICP","Independent clinics with 1–5 doctors, a receptionist, phone-based booking today"),
      ("Problem","Missed calls, double bookings, 15–30% no-shows (ASSUMPTION – must be validated in interviews)"),
      ("Buyer / user / decider","Buyer & decider: clinic owner-doctor; users: receptionist and patients"),
      ("Competitor list to research (≥5)","<i>Researcher to fill:</i> name, URL, pricing page, features, reviews, access date. Candidate search routes: G2/Capterra category 'Medical scheduling', Product Hunt, Google Trends, Reddit r/india, clinic-owner groups."),
      ("TAM formula","N clinics in India × ACV. ACV = ARPA ₹2,274 × 12 = ₹27,288. N must come from a government/industry source – <b>do not guess</b>."),
      ("SAM formula","N reachable (target cities, 1–5 doctors, smartphone-using) × ACV"),
      ("SOM formula","Tenants winnable in 3 years = sales capacity × conversion; with 1 sales FTE closing 4 per month ≈ 48/yr → ~100–150 in 3 years after churn"),
      ("Recurring revenue","Subscription tiers, WhatsApp reminder bundle, onboarding fee, premium support")], "") + callout("ok","G1 decision rule","Proceed only if ≥3 of 10 clinic owners rank missed appointments among top-3 problems and ≥3 agree to a paid pilot at ≥ ₹999/month.")
    h += "<h3>S2. PRD summary and I/O freeze (excerpt)</h3>" + table(["Module","Key features","Roles"],[
      ("Booking","Public booking page, slot rules, buffer times, holidays, double-booking prevention","Patient, Receptionist, Doctor"),
      ("Reminders","Email + SMS/WhatsApp reminders T-24h/T-2h, confirm/cancel link","System, Receptionist"),
      ("Patients","Minimal patient record (name, phone, email), consent flag","Receptionist, Doctor"),
      ("Billing","Subscription via payment gateway; invoices with GST","Owner"),
      ("Reports","Daily schedule PDF, no-show rate CSV","Owner, Doctor"),
      ("Admin","Tenant management, plan limits, audit log","Platform admin")]) + \
      "<h4>Input specification (excerpt)</h4>" + table(["Field","Type","Unit","Req.","Default","Validation","Min/Max","Source","DB mapping"],[
      ("slot_duration","integer","minutes","Yes","15","multiple of 5","5 / 120","Doctor settings form","doctors.slot_minutes"),
      ("patient_phone","string","E.164","Yes","–","regex ^\\+[1-9]\\d{7,14}$","8 / 15 digits","Booking form","patients.phone"),
      ("appointment_start","timestamptz","UTC (shown in Asia/Kolkata)","Yes","–","future; inside working hours; slot free","now / +180 days","Booking form","appointments.start_at"),
      ("consent_reminders","boolean","–","Yes","false","must be true to send reminders","–","Booking form","patients.consent_reminders")]) + \
      "<h4>Output specification (excerpt)</h4>" + table(["Output","Format","Calculation","Precision","Display","Export","Error handling"],[
      ("No-show rate","percentage","no_shows ÷ (completed + no_shows) in period","1 decimal","Reports dashboard","CSV, PDF","Return 0.0% with note if denominator is 0"),
      ("Daily schedule","list","appointments for doctor/day ordered by start","–","Doctor dashboard","PDF","Empty-state message if none")]) + \
      "<h4>Sample scenario + acceptance criterion</h4><p><b>Input:</b> slot_duration 15, doctor works 09:00–13:00, patient requests 10:15 on a free day. <b>Output:</b> appointment created 10:15–10:30; reminders queued for T-24h and T-2h. <b>Given/When/Then:</b> Given slot 10:15 is already booked, when another patient requests it, then the API returns 409 and the UI offers the next three free slots.</p>"
    h += "<h3>S3. Architecture decision</h3>" + table(["Criterion (weight)","A: Next.js + Node/TS modular monolith + PostgreSQL on PaaS","B: React SPA + FastAPI + PostgreSQL on a cloud VM"],[
      ("Development cost (20%)","4 – one language end-to-end","3 – two languages"),("Hosting/ops cost (15%)","4 – managed, minimal ops","3 – VM patching is ours"),("Scalability (10%)","3 – fine to ~5,000 users per model","4"),("Security (15%)","4 – managed TLS, patching","3 – more surface to own"),("Performance (10%)","4","4"),("Developer availability (10%)","5","4"),("Vendor lock-in (10%)","3 – PaaS-specific features avoided","5"),("Long-term operating cost (10%)","3 – PaaS premium at scale","4"),
      ("<b>Weighted score</b>","<b>%.2f</b>" % (4*.20+4*.15+3*.10+4*.15+4*.10+5*.10+3*.10+3*.10),"<b>%.2f</b>" % (3*.20+3*.15+4*.10+3*.15+4*.10+4*.10+5*.10+4*.10))]) + \
      "<p><b>Decision (illustrative):</b> Option A; modular monolith (modules: auth, booking, reminders, billing, reports, admin) with background worker for reminders; PostgreSQL; object storage for exports. Microservices rejected – no independent scaling need. Threat-model highlights: tenant isolation (row-level checks on every query), phone/email PII protection, webhook signature verification, rate limiting on booking page.</p>"
    h += "<h3>S4. Repository setup</h3><pre>slotwise/\n├─ apps/web  apps/api\n├─ packages/ui  packages/shared\n├─ docs/{requirements,architecture,database,api,deployment,testing}\n├─ tests/{unit,integration,e2e}\n├─ infra/\n├─ .github/{workflows,ISSUE_TEMPLATE,pull_request_template.md}\n├─ .env.example  .gitignore  README.md  CONTRIBUTING.md</pre>"
    h += "<h3>S5. Production cost (computed)</h3><p>See section 'Financial Model'. Expected development labour for SlotWise is <b>%s</b> (optimistic %s, pessimistic %s) over %.0f expected hours.</p>" % (inr(t["exp"]), inr(t["opt"]), inr(t["pes"]), t["hrs_e"])
    h += "<h3>S6. Pricing</h3><p>ARPA %s, contribution per tenant %s, break-even <b>%d tenants</b>, LTV:CAC %.1f. Full plan table in 'Financial Model'.</p>" % (inr(P["arpa"]), inr(P["contrib"]), P["be"], calc.ltv(P)["ratio"])
    h += "<h3>S7. Deployment record (template filled with PLACEHOLDERS)</h3>" + table(["Step","Value","Status"],[("Domain","[to be checked at registrar – availability NOT assumed]","Pending"),("DNS","[exact records copied from hosting/email provider]","Pending"),("TLS","Provider-issued certificate; HTTPS redirect on","Pending"),("Migrations","Run on production before traffic switch","Pending"),("Backup restore test","Restored to staging, row counts match","Pending"),("Smoke tests","Sign-up, book, remind, pay, export","Pending"),("Rollback test","Previous release re-deployed < 15 min","Pending")]) + callout("warn","Honesty rule","No claim of 'production-ready' is made for the sample: every deployment row is Pending until evidence is attached.")
    h += "<h3>S8. Customer delivery</h3><ol><li>Pilot with 3 clinics for 60 days at 50% price; written consent for case study.</li><li>SOW covering: configuration, data import, training (2 × 90 min), acceptance tests.</li><li>Subscription terms: monthly/annual billing, fair-use limits, data export on exit within 30 days, data deletion within 90 days, customer owns its data; company owns platform code and reusable components; third-party licences remain with licensors.</li><li>Handover pack: user manual, admin manual, support contacts and hours, escalation matrix.</li></ol>"
    return h

TEMPLATES = [
 ("Market Research Report","Opportunity evidence","Segment, problem, TAM/SAM/SOM workbook, sources with access dates, recommendation, kill criteria"),
 ("Competitor Analysis","Compare 5–10 competitors","Name, URL, access date, pricing, features, tech signals, reviews, positioning, gaps"),
 ("Customer Discovery","Interview log","Interviewee role, date, current workflow, pain rank, spend today, willingness to pay, quotes, consent"),
 ("PRD","Product definition","Purpose, customers, roles/permissions, modules, journeys, NFRs, backup, maintenance"),
 ("Input/Output Specification","Data contract","Per field: type, unit, required, default, validation, min/max, source, DB map; per output: format, logic, precision, display, export, API shape, errors"),
 ("Scope Freeze","Baseline + change control","Version, approvers, out-of-scope, CR log (ID, justification, tech/cost/timeline/security impact, approvals)"),
 ("Architecture Document","Technical design","Options scored, diagrams, ERD, API, auth, threat model, infrastructure, ADRs"),
 ("UI/UX Approval","Design sign-off","Sitemap, flows, hi-fi links, design tokens, usability findings, approvals"),
 ("GitHub Setup","Repo checklist","Repo URL, branch rules, CI checks, secrets storage, environments, release tags"),
 ("Development Report","Weekly progress","Modules done, requirement refs, hours vs estimate, demo notes, blockers"),
 ("Code Review","PR review record","Scope, correctness, security, tests, readability, decision"),
 ("QA Report","Test evidence","Test plan, cases run/passed/failed, defects by severity, regression, UAT sign-off"),
 ("Security Report","Pre-launch review","ASVS checklist, dependency scan, secrets scan, auth/RBAC tests, findings, residual risk"),
 ("Cost Estimation","Phase 7 workbook","Labour three-point, setup, monthly, capacity, three models, source URLs + dates"),
 ("Pricing Model","Phase 8 workbook","Three pricing approaches, plans, margins, break-even, CAC/LTV, assumptions"),
 ("Domain/DNS Report","Domain record","Registrar, owner, MFA, renewal date, DNS records (exact values), propagation proof"),
 ("Deployment Report","Release record","Build, env vars (names only), migrations, smoke tests, rollback proof, approver"),
 ("Customer Showcase","Demo pack","Script, dataset, limitations list, pricing, pilot proposal, next steps"),
 ("Project Handover","Delivery close","Deliverables, credentials transfer procedure, manuals, training, acceptance form"),
 ("Maintenance Report","Monthly ops","Uptime, incidents, patches, backup test, cost vs budget, support stats, actions"),
]

def templates():
    rows = [(i+1, n, p, f) for i, (n, p, f) in enumerate(TEMPLATES)]
    return table(["#","Template","Purpose","Mandatory fields"], rows) + \
     "<p class='note'>Each template is stored in the dashboard Documents module, versioned, and linked to its gate. The Change Request, Weekly Status and Risk Register forms are below.</p>"

def forms():
    cr = table(["Field","Content"],[("Change Request ID","CR-<project>-<nnn>"),("Requested by / date",""),("Description",""),("Business justification",""),("Technical impact",""),("Cost impact (₹)",""),("Timeline impact (days)",""),("Security impact",""),("Customer approval","Name / date / signature"),("Product owner approval","Name / date / signature"),("Decision","Approved / Rejected / Deferred")])
    wk = table(["Weekly status field","Content"],[("Employee / week",""),("Completed tasks",""),("Pending tasks",""),("Blockers",""),("Time spent (h) by project ID",""),("Quality issues",""),("Upcoming deliverables","")])
    rk = table(["ID","Risk","Likelihood (1–5)","Impact (1–5)","Score","Mitigation","Owner","Status"],[("R1","Key developer unavailable","3","4","12","Cross-training, docs","Tech Lead","Open"),("R2","Provider price change","3","3","9","Re-run cost model each quarter","Finance","Open"),("R3","Scope creep","4","4","16","Change-request gate","Product Owner","Open"),("R4","Security incident","2","5","10","ASVS review, backups, incident drill","Tech Lead","Open")])
    pr = table(["Project record field","Value"],[(x,"") for x in ["Project ID","Product owner","Technical lead","Assigned developers","QA owner","Customer contact","Budget","Delivery timeline","Approved scope (version)","Production cost estimate","Sales price","Expected margin","Current stage / gate","Risk register link"]])
    return f"<h3>Change Request form</h3>{cr}<h3>Project record (14 mandatory fields)</h3>{pr}<h3>Weekly employee report</h3>{wk}<h3>Risk register (example rows)</h3>{rk}"

def dashboard():
    mods = [("1","Company Overview","KPIs, alerts, pending approvals","All"),("2","Project Management","Project record, stage tracker, timeline","PM, Tech Lead"),("3","Market Research","Reports, competitor sheets, sources","Product"),("4","Product Requirements","PRD/I-O versions","Product"),("5","Scope Approval","Freeze + CR workflow","Product, Owner"),("6","Architecture Approval","Docs + ADR + approval","Tech Lead"),("7","UI/UX Approval","Design links + sign-off","Design, Product"),("8","Employee Assignments","Roster, capacity","PM"),("9","Development Tracking","Tasks, hours vs estimate","Dev"),("10","GitHub Repository Tracking","Repos, PRs, CI status, releases","Tech Lead"),("11","QA & Testing","Test runs, defects","QA"),("12","Production Cost Calculator","Phase 7 model","Finance"),("13","SaaS Pricing Calculator","Phase 8 model","Finance, Owner"),("14","Domain & DNS Tracker","Domains, renewals, records","DevOps"),("15","Deployment Tracker","Releases, rollbacks","DevOps"),("16","Customer Management","Accounts, contacts, consents","Sales, Support"),("17","Sales Pipeline","Stages, forecasts","Sales"),("18","Subscription Management","Plans, renewals, MRR","Finance, Support"),("19","Maintenance Management","Tickets, incidents, SLA","Support, DevOps"),("20","Reports & Analytics","Exports, trend charts","Managers")]
    k = [("Total / active / delayed projects","Count from project records; delayed = current date > planned date of current stage"),("Project budgets vs actual expenditure","Budget field vs timesheet hours × loaded rate + invoices"),("Estimated vs actual production cost","Phase 7 model vs provider invoices"),("Expected revenue","Σ weighted pipeline value (stage probability × ACV)"),("MRR","Σ active subscription price (ex-GST, net of discounts)"),("Gross margin","(Revenue − cost of revenue) ÷ Revenue; cost of revenue defined in pricing model"),("Employee productivity","Delivered story points ÷ hours; never used alone for appraisal"),("Customer satisfaction","CSAT/NPS from post-onboarding and quarterly surveys"),("Pending approvals","Open gate/CR requests per approver")]
    perm = table(["Role","Can do"],[("Employee","Update tasks, log time, upload deliverables, request approvals"),("Lead","Review, approve technical gates, assign work"),("Manager / Owner","Approve, reject or request changes at all gates; see finance"),("Finance","Edit cost/pricing models, view revenue"),("Admin","Users, roles, audit log")])
    return "<h3>Modules</h3>"+table(["#","Module","Purpose","Primary roles"],mods)+"<h3>Management KPIs</h3>"+table(["KPI","Definition"],k)+"<h3>Roles and permissions</h3>"+perm+"<p>Each project shows a stage tracker G1 → G12. Dashboard is responsive, role-based (server-side checks), and audit-logged. It is built as Project 0 using this same operating system.</p>"

def ops():
    inc = table(["Level","Definition","Owner","Response target*","Escalation","Communication","Recovery"],[
      ("Critical","Entire service down or confirmed serious security incident","Tech Lead (incident commander)","Acknowledge ≤ 30 min, 24×7 only if staffed","Managing Director immediately","Status page + email to affected customers; regulator/customer notification per law/contract","Rollback or failover; preserve evidence; post-incident review ≤ 5 days"),
      ("High","Major business function unavailable","DevOps on-call","≤ 2 business hours","Tech Lead after 2 h","Email update every 4 h","Hotfix branch, expedited review"),
      ("Medium","Limited functionality affected, workaround exists","Support Lead → Developer","≤ 1 business day","Tech Lead if > 3 days","Ticket updates","Fix in next release"),
      ("Low","Minor or cosmetic","Support Lead","≤ 3 business days","None","Ticket","Backlog")])
    sla = table(["Maintenance plan item","Starter","Business"],[("Service hours","Mon–Fri 10:00–18:00 IST","Mon–Sat 09:00–20:00 IST"),("Channels","Email","Email + chat + phone"),("Included work","Bug fixes, patching, backups","Plus ≤ 8 h/month minor changes"),("Exclusions","New features, customer-caused data issues, third-party outages, force majeure","Same"),("Price","Included in subscription","Included / add-on per quote")]) + "<p class='note'>*Targets are examples. Never publish an SLA (uptime %, response times) until on-call staffing, monitoring and architecture can demonstrably meet it.</p>"
    dns = table(["Record","Purpose","Rule"],[("A / AAAA","Maps name to IPv4 / IPv6 address","Use exact values from hosting provider"),("CNAME","Alias to another hostname","Cannot coexist with other records on same name; apex needs ALIAS/flattening if supported"),("MX","Mail routing","Copy from email provider; never overwrite existing MX"),("TXT","Verification, SPF, DKIM, DMARC","Only one SPF record per domain; merge includes"),("SPF","Authorised sending servers","Start with provider include; end with ~all then -all after testing"),("DKIM","Message signing","Publish selector record exactly as given"),("DMARC","Policy and reporting","Begin p=none with rua reporting; move to quarantine/reject after review"),("CAA","Restricts certificate issuers","Must include the CA your host uses")])
    return "<h3>Incident matrix</h3>"+inc+"<h3>Maintenance agreement outline</h3>"+sla+"<h3>DNS record reference</h3>"+dns

SRC = [("VS Code","https://code.visualstudio.com/docs"),("GitHub","https://docs.github.com/"),("Next.js","https://nextjs.org/docs"),("React","https://react.dev/"),("Node.js","https://nodejs.org/docs/"),("PostgreSQL","https://www.postgresql.org/docs/"),("Vercel","https://vercel.com/docs"),("Cloudflare","https://developers.cloudflare.com/"),("AWS","https://docs.aws.amazon.com/"),("Google Cloud","https://cloud.google.com/docs"),("Microsoft Azure","https://learn.microsoft.com/azure/"),("OWASP","https://owasp.org/"),("OWASP ASVS","https://asvs.owasp.org/"),("NIST SSDF SP 800-218","https://csrc.nist.gov/pubs/sp/800/218/final"),("ICANN","https://www.icann.org/"),("MeitY","https://www.meity.gov.in/"),("CBIC (GST)","https://cbic-gst.gov.in/"),("WCAG 2.2","https://www.w3.org/TR/WCAG22/"),("Google SRE book","https://sre.google/sre-book/table-of-contents/"),("Apple Developer Program – what's included","https://developer.apple.com/programs/whats-included/"),("Apple App Store Small Business Program","https://developer.apple.com/app-store/small-business-program/"),("Google Play service fees","https://support.google.com/googleplay/android-developer/answer/112622"),("Google Play lower service fees (2026)","https://support.google.com/googleplay/android-developer/answer/16954621"),("Android Developers Blog – expanded billing","https://android-developers.googleblog.com/2026/06/play-expanded-billing.html")]

VER = {"https://developer.apple.com/programs/whats-included/":"<b>2026-11-03</b> – page fetched",
       "https://developer.apple.com/app-store/small-business-program/":"<b>2026-11-03</b> – page fetched",
       "https://support.google.com/googleplay/android-developer/answer/112622":"2026-11-03 – search summary only; page blocked, re-verify",
       "https://support.google.com/googleplay/android-developer/answer/16954621":"2026-11-03 – search summary only; page blocked, re-verify",
       "https://android-developers.googleblog.com/2026/06/play-expanded-billing.html":"2026-11-03 – search summary only; re-verify"}

def sources():
    return table(["Source","URL","Verified on (fill)"],[(a,f'<a href="{u}">{u}</a>',VER.get(u,"____/____/______")) for a,u in SRC]) + callout("warn","Verification required","Links are authoritative starting points. Pricing, tax rates and regulations change; whoever uses a figure must open the page, confirm it, and record the date. Only the two Apple pages marked above were fetched directly; Google Play figures are search-reported and flagged as such; no cloud-provider price is quoted as fact.")

def assumptions():
    return table(["ID","Assumption","Where used","Replace with"],[("A1","Fully loaded hourly rates","Labour cost","HR/finance actuals"),("A2","Hours per activity (O/M/P)","Labour cost","Past project timesheets"),("A3","Provider unit prices","Infrastructure model","Official pricing pages + dates"),("A4","Usage per user (DAU, requests, MB)","Capacity","Analytics from pilot"),("A5","Plan prices & mix","Pricing","Customer discovery + competitor pages"),("A6","Churn, refunds, failed-payment rates","LTV, profit","Billing data after 3 months"),("A7","Gateway fee","Contribution","Gateway contract"),("A8","GST 18%, corporate tax 25.17%","Pricing, profit","CA / CBIC / Income-tax dept"),("A9","CAC ₹6,000","LTV:CAC","Actual sales & marketing spend")]) 
