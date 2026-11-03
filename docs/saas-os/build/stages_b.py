"""Stage cards, phases 8-14 + final lifecycle stages."""
import calc
from calc import inr

def stages():
    P = calc.pricing()
    S = []
    S.append(dict(n=8, name="SaaS Pricing & Profitability", gate="G8 – Pricing Approval",
     why="Sets a price that covers full cost, CAC and churn with a target margin.",
     steps=[
      "Cost-based: (cost to serve + fixed share + target margin). Competitor-based: positioning within the observed range. Value-based: share of customer's quantified saving. Choose the price inside the intersection.",
      "Compute CAC, MRR, ARR, gross margin (state which costs sit in cost of revenue), contribution margin, break-even customers, operating profit; show tax separately.",
      "Define plans: Free Trial (if suitable), Starter, Professional, Business, Enterprise — price (monthly/annual), users, storage, API limits, features, support, overage, upgrade rule.",
      "Include payment-gateway fees, refunds, discounts, GST, failed payments, churn.",
      "Never promise 'unlimited' without a cost and abuse-control rule (fair-use limit, rate limiting).",
      "Recommend setup fee, subscription fees, minimum viable price, target gross margin, maximum CAC, break-even customers, monthly revenue target, renewal strategy."],
     roles=[("Business Owner","Decides price"),("Finance Analyst","Maintains the model"),("Sales Lead","Validates price with prospects")],
     tools="Pricing workbook / dashboard SaaS Pricing Calculator.",
     time="1 week", cost="Labour ≈ 30 h.",
     deliver=["Pricing Model","Plan matrix","Profitability table by customer count","Commercial recommendation"],
     checklist=["Three pricing approaches compared","Margin target stated","LTV/CAC ≥ 3 and payback < 12 months","Break-even customer count explained","Fair-use and overage defined"],
     approval="G8 signed by Business Owner. Prices are published only after G8.",
     sources=[("CBIC – GST","https://cbic-gst.gov.in/"),("Razorpay pricing (example gateway)","https://razorpay.com/pricing/"),("Stripe pricing (example gateway)","https://stripe.com/pricing")],
     mistakes=[("Pricing off competitors only","Triangulate with cost and value"),("Ignoring churn and refunds","Apply leakage factors"),("Calling revenue profit","Show operating profit and tax separately")]))
    S.append(dict(n=9, name="Domain, DNS & Production Deployment", gate="G9 – Release Approval",
     why="Takes the product from repository to a safe, monitored, rollback-able live service.",
     steps=[
      "Domain: generate options, check availability at reputable registrars (never claim availability without checking), compare first-year vs renewal price, check trademark conflicts, register in the COMPANY's name, enable MFA and auto-renew/renewal lock, record ownership and recovery procedure.",
      "DNS: copy exact records supplied by the hosting and email providers (A/AAAA/CNAME/MX/TXT/SPF/DKIM/DMARC/CAA). Keep existing email records. Never guess a value.",
      "Deploy: connect repo → build settings → production env vars → production DB → storage → domain → DNS → verify propagation → TLS → HTTPS redirect → migrations → backups → monitoring → error tracking → alerts → smoke tests → critical workflow tests → rollback test → release approval.",
      "Record every value in the Domain/DNS Report and Deployment Report."],
     roles=[("DevOps Engineer","Executes SOP"),("Tech Lead","Verifies and approves"),("Business Owner","Registrar account owner, release approval")],
     tools="Registrar console, DNS provider, hosting platform, dig/nslookup, SSL Labs, uptime monitor, error tracker.",
     time="2–4 days", cost="Domain + hosting per cost model; labour ≈ 36 h DevOps in sample.",
     deliver=["Domain/DNS Report","Deployment Report","Smoke-test evidence","Rollback test evidence"],
     checklist=["MFA on registrar, DNS and hosting accounts","Auto-renew on; renewal reminders in shared calendar","SPF, DKIM, DMARC verified","HTTPS-only with valid certificate","Backup restore tested","Alerts reach on-call","Rollback executed on staging"],
     approval="G9 signed by Tech Lead and Business Owner after smoke tests pass.",
     sources=[("ICANN","https://www.icann.org/"),("Cloudflare DNS docs","https://developers.cloudflare.com/dns/"),("Vercel domains","https://vercel.com/docs/domains"),("DMARC.org","https://dmarc.org/")],
     mistakes=[("Overwriting MX records","Export existing zone before edits"),("Registering domain in an employee's name","Company account only"),("No restore test","Restore backups to staging quarterly")]))
    S.append(dict(n=10, name="Customer Showcase, Sales & Delivery", gate="G10 – Commercial Approval",
     why="Turns a working product into signed, paying, correctly-onboarded customers.",
     steps=[
      "Identify and qualify leads against the ICP; log in the CRM; schedule discovery calls.",
      "Showcase script: introduction → customer problem → current workflow → solution → live demo → benefits → limitations → pricing → pilot proposal → next steps.",
      "Demo only completed features, with synthetic or authorised data. Get written consent before using any logo, name, screenshot or testimonial.",
      "Proposal → SOW → timeline → agreement → SaaS subscription terms → onboarding → user and admin manuals → training → acceptance form → support details.",
      "Contract must define ownership of source code, reusable components, third-party libraries, customer data and IP."],
     roles=[("Sales Lead","Pipeline and proposals"),("Product Owner","Demo and scoping"),("Support Lead","Onboarding and training"),("Legal advisor","Contract review")],
     tools="CRM / dashboard pipeline, demo environment, e-signature, proposal templates.",
     time="2–8 weeks per sale (depends on sales cycle)", cost="Included in CAC.",
     deliver=["Demo environment + script","Proposal and SOW","Agreement and subscription terms","Customer acceptance form","Handover pack"],
     checklist=["No unfinished feature shown as complete","Written consent for logos","IP clauses reviewed","Acceptance criteria tied to PRD","Support hours documented"],
     approval="G10: discount or special terms beyond policy need Business Owner approval.",
     sources=[("MeitY","https://www.meity.gov.in/"),("CBIC GST","https://cbic-gst.gov.in/")],
     mistakes=[("Promising roadmap items","State limitations explicitly"),("Verbal scope agreements","Everything in the SOW")]))
    S.append(dict(n=11, name="Maintenance & Post-Launch Operations", gate="G11 – Operational Readiness",
     why="Recurring revenue only survives if the service stays up, secure and supported.",
     steps=[
      "Monitoring: uptime, application errors, database health, queue depth, certificate and domain expiry.",
      "Monthly routine: security patching, dependency updates, backup restore verification, cost review, usage analytics, subscription/renewal review.",
      "Incident levels: Critical (service down or confirmed serious security incident), High (major function unavailable), Medium (limited impact), Low (minor/cosmetic). Each has owner, escalation, communication and recovery steps (see Incident Matrix).",
      "Maintenance agreement: service hours, response targets, exclusions, price. Only promise what the on-call rota can actually deliver."],
     roles=[("Support Lead","Tickets and customer comms"),("DevOps Engineer","Monitoring, patching, recovery"),("Tech Lead","Incident commander for Critical/High")],
     tools="Uptime monitor, error tracker, log platform, ticketing, status page.",
     time="Ongoing; budget %d maintenance hours/month in Professional model" % calc.MODELS["Professional"]["maint_h"],
     cost="≈ %s/month maintenance labour + support (see model)." % inr(calc.MODELS["Professional"]["maint_h"]*calc.UNIT["maint_rate"]),
     deliver=["Maintenance Agreement","Runbooks","Monthly Maintenance Report","Post-incident reviews"],
     checklist=["On-call rota exists","Backups verified monthly","Patches applied within agreed window","Customer comms templates ready"],
     approval="G11 signed by Tech Lead; SLA wording reviewed by Business Owner.",
     sources=[("Google SRE book","https://sre.google/sre-book/table-of-contents/"),("NIST SSDF","https://csrc.nist.gov/pubs/sp/800/218/final")],
     mistakes=[("Offering 99.99% without redundancy","SLA must match architecture and staffing"),("Never testing restores","Scheduled restore drill")]))
    S.append(dict(n=12, name="Employee Management & Company SOP", gate="Continuous",
     why="Makes every project run the same way regardless of who works on it.",
     steps=[
      "Every project record holds: project ID, product owner, tech lead, developers, QA owner, customer contact, budget, timeline, approved scope, cost estimate, sales price, expected margin, current stage, risk register.",
      "Weekly review: each employee reports completed, pending, blockers, time spent, quality issues, upcoming deliverables.",
      "All 20 standard templates are used unchanged unless the Tech Lead approves a variant."],
     roles=[("Managing Director","Final authority"),("Project manager / Product Owner","Weekly review"),("Employees","Timely accurate reporting")],
     tools="Internal dashboard, timesheets, shared drive.", time="30 min weekly review per project", cost="Overhead.",
     deliver=["Project record","Weekly status report","Risk register"],
     checklist=["Every active project has all 14 fields","Timesheets filled daily","Risks reviewed weekly"],
     approval="Reviewed weekly by management.",
     sources=[("Atlassian agile guides","https://www.atlassian.com/agile")],
     mistakes=[("Status by chat only","Dashboard is the source of truth")]))
    S.append(dict(n=13, name="Internal Management Dashboard", gate="G13 – Dashboard Release",
     why="Gives management live visibility of pipeline, delivery, cost, revenue and approvals.",
     steps=["Build the portal as a standard SaaS project using this same process (it is Project 0).","Implement the 20 modules and role-based access (see Dashboard Specification)."],
     roles=[("Tech Lead","Architecture"),("Developers","Modules"),("Managing Director","Product owner")],
     tools="Next.js/React + TypeScript + PostgreSQL (suggested, subject to G3).", time="8–12 weeks", cost="Internal project; track like any other.",
     deliver=["Dashboard v1 with all management KPIs"],
     checklist=["Role-based access enforced server-side","Approvals recorded with timestamp and user","KPIs reproducible from raw data"],
     approval="G13 by Managing Director.", sources=[("OWASP ASVS","https://asvs.owasp.org/")],
     mistakes=[("Vanity metrics","Every KPI has a definition and owner")]))
    S.append(dict(n=14, name="Source Documentation Discipline", gate="Continuous",
     why="Every technical or financial claim is traceable.",
     steps=["Prefer official docs, pricing pages, registrar, DNS, cloud calculators, gateways, security standards, government tax/legal sources.","Store each source as: title, URL, publisher, access date, what it supports.","Re-verify pricing and regulation at every G7/G8 and before every contract."],
     roles=[("Everyone","Cite sources"),("Tech Lead","Spot-checks")], tools="Shared source register.", time="Continuous", cost="Overhead.",
     deliver=["Source register per project"],
     checklist=["No uncited price","No uncited legal claim","Access date present"],
     approval="Checked at each gate.", sources=[("See Source Index appendix","#sources")],
     mistakes=[("Using blog posts for prices","Use the official page; blogs only as leads")]))
    return S
