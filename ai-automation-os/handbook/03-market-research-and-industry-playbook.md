# Part 3 — Market Research, Market Size and Industry Playbook

**Evidence status:** this Part gives the **method** and **where to look**. It was written without live internet research, so every market fact, price and competitor feature below is a **hypothesis to verify**. A figure becomes usable only when you attach its source link and date.

## 3.1 How to research an industry (a repeatable one-week routine)

| Day | Task | Output |
|---|---|---|
| 1 | List the industry's daily work: jobs, documents, systems, messages | process list (30–50 lines) |
| 2 | Read 5 job advertisements and 5 software vendor pages for the industry; note repeated tasks and tools | tool and task list with links |
| 3 | Interview 3 insiders (owner, operations person, accountant) using template T1 | interview notes |
| 4 | Estimate volume, minutes and error cost per process | savings estimate (formula below) |
| 5 | Check competitors and alternatives; find who pays and how much today | competitor table with sources |
| 6 | Score the top 3 processes (opportunity score) | ranked shortlist |
| 7 | Write the industry page (Section 3.5 format) and review with the Founder | industry page |

**Savings formula (use everywhere):**
`monthly hours saved = items per month × minutes per item ÷ 60`
`monthly value = hours saved × staff hourly cost × realisable share` (realisable share is usually 0.5–0.7; only count time the customer can really redeploy or cut).

## 3.2 Market size: transparent bottom-up TAM, SAM and SOM

Never use a "top-down" claim such as "the automation market is worth X billion, we need 1%". Count customers instead.

| Term | Question | Formula |
|---|---|---|
| **TAM** (total addressable market) | If every possible buyer bought, what would the yearly revenue be? | `Σ segments: firms × annual contract value (ACV)` |
| **SAM** (serviceable addressable market) | How much can we reach with our channels, region, language and product fit? | `Σ segments: firms × geography filter × fit filter × ACV` |
| **SOM** (serviceable obtainable market) | What can we realistically win and onboard in 3 years? | `min(SAM customers × share won, onboarding capacity per month × months) × ACV` |

**Rules**

1. Every number has a **source** (registry, government statistics, directory count, your own sampled lead list) and a date.
2. The **fit filter** is the share of firms that actually meet our qualification rule (for bills: at least 2,000 a month).
3. The **geography filter** is the share we can serve with our current language, support and sales reach.
4. SOM is limited by **our capacity** to onboard, not only by market share. The calculator shows which limit binds.
5. Show three cases: low, medium, high.

**Worked example (illustrative placeholder inputs, not market data):**

| Segment | Firms | Geography | Fit | Customers in SAM |
|---|---|---|---|---|
| CA / accounting firms at 2,000+ bills a month | 20,000 | 30% | 15% | 900 |
| SMEs with in-house accounts team at 2,000+ bills a month | 40,000 | 30% | 5% | 600 |

ACV = ₹14,999 × 12 = ₹1,79,988 (Professional tier).

- TAM = (20,000 + 40,000) × ₹1,79,988 = **₹1,079.9 crore a year**
- SAM = 1,500 customers × ₹1,79,988 = **₹27.0 crore a year**
- SOM = 5% × 1,500 = 75 customers (capacity 3 a month × 36 = 108, so not binding) = **₹1.35 crore ARR**

Run it yourself: `python calculator/market_sizing.py` (inputs in `market_inputs.json`; the tests check the arithmetic by hand). **The firm counts are placeholders**; replace them with sourced figures before using this in any pitch.

## 3.3 Competitor comparison matrix

Fill one row per tool and **verify every cell on the vendor's official page, with date**. The starting hypotheses:

| Tool | Type and licence | Pricing structure (verify) | Integrations | AI features | Best for | Limitations for us | Typical customer |
|---|---|---|---|---|---|---|---|
| Zapier | Hosted no-code | Usage-based (tasks); tiers | Very large catalogue | Built-in AI steps and agents | Quick app-to-app flows | Cost rises with volume; hosted only; limited data-residency control | Small teams, marketing, ops |
| Make | Hosted visual builder | Usage-based (operations/credits); tiers | Large catalogue | AI modules | Branching scenarios at mid cost | Hosted only; complex scenarios hard to version | SMEs, agencies |
| n8n | Workflow engine, source-available licence; self-host or cloud | Cloud by execution tiers; self-host licence terms apply | Large + custom code nodes | AI nodes, agent nodes | Custom builds, self-hosting, code when needed | You operate and secure it; licence limits reselling it as a service | Technical teams, agencies |
| Activepieces | Open-source automation; self-host or cloud | Open-source core; paid cloud/enterprise (verify editions) | Growing catalogue | AI pieces | Permissive embedding | Smaller catalogue | Startups, developers |
| Microsoft Power Automate | Enterprise iPaaS + desktop RPA | Per-user/per-flow licensing (verify) | Deep Microsoft 365, many connectors | Copilot features | Microsoft-centred companies | Licence complexity; Microsoft-centric | Mid/large enterprises |
| UiPath | Enterprise RPA platform | Enterprise licensing (verify) | Strong UI automation, enterprise connectors | Document understanding, agents | Legacy UI automation at scale | Heavy and costly for small firms | Large enterprises |
| Workato | Enterprise iPaaS | Enterprise pricing (verify) | Enterprise apps | AI features | Governed enterprise integration | Enterprise pricing and sales cycle | Mid/large enterprises |
| Industry-specific vendors | Vertical software | Per user/module | Their own ecosystem | Varies | Out-of-the-box vertical workflows | Closed, customisation limited | Industry-specific |
| Manual process | — | Staff time | — | — | Zero software cost | Errors, delay, peaks | Everyone today |

**How we position:** horizontal tools are our **components**, not our competitors. We sell a **finished, accountable outcome for one vertical** with service levels, audit trail, and local rules.

## 3.4 Willingness to pay: five signals (score 0–2 each)

1. The customer **already pays** for the pain (staff, outsourcing, software).
2. They can **quantify** the cost without help.
3. A named **budget owner** is in the meeting.
4. They accept a **paid pilot**.
5. They name a **deadline or event** (audit, peak season, new hire needed).
Total 7 or more = qualified.

## 3.5 Industry playbook (15 industries)

Use this page format for every industry you research. Items 1–10 are the questions from Phase 1; each entry lists **hypotheses to test**, not facts.

### 1. Healthcare (clinics, hospitals, diagnostics)
1. **Repetitive processes:** appointment booking, reminders, patient registration, billing, insurance pre-authorisation follow-up, report delivery.
2. **Manual admin:** scheduling changes, referral letters, claim status calls.
3. **Data entry:** patient details, insurance forms, lab results into records.
4. **Communication bottlenecks:** missed-appointment calls, test-result enquiries, after-hours messages.
5. **Documents:** prescriptions, discharge summaries, insurer forms, scanned reports.
6. **Reports:** daily appointment load, no-show rate, collections.
7. **Integrations:** hospital/clinic management software, WhatsApp Business, payment gateway, calendar.
8. **Savings:** `appointments per day × reminder minutes`, plus no-shows reduced × average visit revenue (measure baseline first).
9. **Willingness to pay:** clinic owners and administrators; strong when no-shows are costly.
10. **Existing solutions:** clinic/hospital management systems with reminder modules, appointment apps (verify).
**Watch-outs:** medical data is highly sensitive; consent, retention and access rules matter; AI must never give diagnosis or treatment advice.
**First offer to test:** appointment reminders and rescheduling over WhatsApp with human handover.

### 2. Construction
1. **Repetitive:** tender document review, bill-of-quantities preparation, subcontractor follow-up, site progress reports, material requests.
2. **Manual admin:** attendance, purchase requests, RFIs, invoice matching.
3. **Data entry:** quantities from drawings and schedules into spreadsheets; measurement sheets.
4. **Communication:** site-to-office updates, subcontractor payment queries.
5. **Documents:** drawings, specifications, purchase orders, delivery challans, running bills.
6. **Reports:** daily progress, cost vs budget, procurement status.
7. **Integrations:** accounting, project-management tools, WhatsApp groups, spreadsheets, drawing/BIM exports.
8. **Savings:** `tenders per month × hours per tender`; bills processed × minutes.
9. **Willingness to pay:** contractors and consultants with many bids or bills; pays when a missed item costs money.
10. **Existing solutions:** construction-management suites, estimating software (verify).
**Watch-outs:** extracted quantities must be checked by an engineer; never present AI output as structural or design approval.
**First offer to test:** tender/BOQ extraction and comparison with a mandatory engineer review step.

### 3. Real estate
1. **Repetitive:** lead capture, first reply, qualification, site-visit scheduling, follow-ups, document collection.
2. **Manual admin:** listing updates, brochure sending, agreement drafts, payment reminders.
3. **Data entry:** enquiry details into CRM, KYC data.
4. **Communication:** slow response to portal leads, repeated questions about price and availability.
5. **Documents:** KYC, agreements, receipts, allotment letters.
6. **Reports:** lead source performance, site-visit conversion, collections ageing.
7. **Integrations:** property portals, CRM, WhatsApp Business, calendar, payment links.
8. **Savings:** faster response lifts conversion; measure `leads per month × response time × conversion change × average commission`.
9. **Willingness to pay:** agents, developers and brokers with paid-ad leads; pays when leads are wasted.
10. **Existing solutions:** real-estate CRMs and lead-management tools (verify).
**Watch-outs:** messaging consent and template rules; honest property information.
**First offer to test:** WhatsApp and form lead qualification with CRM entry and site-visit booking.

### 4. Manufacturing
1. **Repetitive:** purchase orders, goods receipt, quality records, maintenance requests, production reports.
2. **Manual admin:** shift reports, inventory counts, supplier follow-up.
3. **Data entry:** delivery notes, inspection sheets, stock movements.
4. **Communication:** shop-floor to office, supplier delay notices.
5. **Documents:** POs, invoices, test certificates, material certificates.
6. **Reports:** daily production, rejection rate, stock ageing.
7. **Integrations:** ERP, inventory, spreadsheets, machine data (sometimes via MQTT).
8. **Savings:** `documents per day × minutes`; avoided stock-outs.
9. **Willingness to pay:** plant managers and finance teams; pays when errors stop production or delay billing.
10. **Existing solutions:** ERP modules, MES, document-capture tools (verify).
**Watch-outs:** never let AI control machines or safety systems; recommendations only.
**First offer to test:** PO and delivery-document extraction into ERP with exception queue.

### 5. Retail
1. **Repetitive:** stock updates, supplier orders, price changes, daily sales reports, customer queries.
2. **Manual admin:** reconciliation of cash and card, vendor invoices.
3. **Data entry:** purchase invoices, stock counts.
4. **Communication:** order-status and availability questions.
5. **Documents:** supplier invoices, delivery notes.
6. **Reports:** daily sales, stock alerts, margin by item.
7. **Integrations:** point-of-sale, accounting, WhatsApp, payment gateway.
8. **Savings:** invoice minutes × volume; stock-out reduction.
9. **Willingness to pay:** owners of multi-store or high-SKU shops.
10. **Existing solutions:** retail POS and inventory software (verify).
**First offer to test:** supplier-invoice capture and low-stock alerts.

### 6. E-commerce
1. **Repetitive:** order confirmation, shipping updates, returns and refunds, product-listing updates, review replies.
2. **Manual admin:** failed-delivery handling, refund approvals, marketplace reconciliation.
3. **Data entry:** listings, courier details, settlement files.
4. **Communication:** "where is my order" queries, return reasons.
5. **Documents:** invoices, settlement reports, return forms.
6. **Reports:** returns rate, courier performance, daily revenue.
7. **Integrations:** store platform, marketplaces, couriers, WhatsApp, payments.
8. **Savings:** `tickets per month × minutes`; returns/failed-delivery cost avoided.
9. **Willingness to pay:** store owners with growing support load.
10. **Existing solutions:** helpdesk tools, store-platform apps (verify).
**Watch-outs:** refunds are financial actions and need approval rules.
**First offer to test:** order-status and return-request automation with human approval for refunds.

### 7. Education
1. **Repetitive:** admission enquiries, fee reminders, timetable notices, attendance, certificates.
2. **Manual admin:** document verification, report cards, parent communication.
3. **Data entry:** admission forms, marks, attendance.
4. **Communication:** repeated parent questions, announcements.
5. **Documents:** admission forms, transfer certificates, mark sheets.
6. **Reports:** fee collection, attendance, enrolment funnel.
7. **Integrations:** school/college management software, WhatsApp, payment links, spreadsheets.
8. **Savings:** office-staff hours; fee-collection delays reduced.
9. **Willingness to pay:** private institutions and coaching centres; budgets are seasonal.
10. **Existing solutions:** school ERPs and fee-management apps (verify).
**Watch-outs:** minors' data protection and consent.
**First offer to test:** admission-enquiry handling and fee reminders on WhatsApp.

### 8. Finance and accounting
1. **Repetitive:** bill entry, bank reconciliation, ledger coding, GST checks, payment reminders, report packs.
2. **Manual admin:** chasing missing documents, month-end checklists.
3. **Data entry:** invoices, expenses, statements into accounting software.
4. **Communication:** client reminders, query handling.
5. **Documents:** invoices, bank statements, receipts, tax notices.
6. **Reports:** MIS packs, ageing, tax summaries.
7. **Integrations:** accounting software, banks (statements), email, WhatsApp, government portals through supported methods only.
8. **Savings:** the sample project (`1,500 bills × 6 min`).
9. **Willingness to pay:** accounting firms and finance teams with volume; strong at month-end peaks.
10. **Existing solutions:** accounting software with capture features, expense tools (verify).
**Watch-outs:** do not execute payments or file returns without approval; keep a full audit trail.
**First offer to test:** the sample project: purchase-bill capture with validation and review.

### 9. Logistics
1. **Repetitive:** shipment status updates, proof-of-delivery handling, freight invoice checking, driver and customer notifications.
2. **Manual admin:** e-way bill and document preparation, claims, route reports.
3. **Data entry:** consignment details, delivery receipts.
4. **Communication:** "where is my shipment" calls, delay notices.
5. **Documents:** lorry receipts, delivery challans, invoices, customs paperwork.
6. **Reports:** on-time delivery, vehicle utilisation, freight cost.
7. **Integrations:** transport-management software, GPS/telematics, WhatsApp, accounting.
8. **Savings:** shipments per day × minutes of status work.
9. **Willingness to pay:** transporters and freight forwarders; pays when disputes delay payment.
10. **Existing solutions:** transport-management and tracking software (verify).
**First offer to test:** proof-of-delivery capture and automatic customer status messages.

### 10. Recruitment
1. **Repetitive:** CV screening, interview scheduling, candidate follow-up, offer paperwork.
2. **Manual admin:** shortlisting spreadsheets, reference checks.
3. **Data entry:** candidate details from CVs into tracking systems.
4. **Communication:** status updates to candidates, scheduling back-and-forth.
5. **Documents:** CVs, offer letters, ID documents.
6. **Reports:** pipeline stage counts, time to hire.
7. **Integrations:** applicant tracking systems, email, calendar, job boards.
8. **Savings:** `CVs per role × minutes per CV`; scheduling hours.
9. **Willingness to pay:** recruitment agencies and HR teams with volume.
10. **Existing solutions:** applicant tracking systems with parsing (verify).
**Watch-outs:** AI may assist, but hiring decisions stay with qualified people; bias and fairness checks required.
**First offer to test:** CV parsing into a structured shortlist sheet plus interview scheduling.

### 11. Marketing agencies
1. **Repetitive:** client reports, lead handling, content scheduling, ad-performance summaries, approvals.
2. **Manual admin:** collecting numbers from platforms, chasing client approvals.
3. **Data entry:** metrics into report templates.
4. **Communication:** client status updates, feedback rounds.
5. **Documents:** reports, briefs, invoices.
6. **Reports:** weekly and monthly client dashboards.
7. **Integrations:** ad platforms, analytics, CRM, spreadsheets, messaging.
8. **Savings:** `clients × report hours per month`.
9. **Willingness to pay:** agencies with many retainer clients.
10. **Existing solutions:** reporting dashboards and agency-management tools (verify).
**First offer to test:** automated monthly client report drafts with human review before sending.

### 12. Hospitality
1. **Repetitive:** booking confirmations, guest messages, check-in details, feedback collection, supplier orders.
2. **Manual admin:** reservations from multiple channels, rate updates.
3. **Data entry:** guest details, bookings from emails.
4. **Communication:** repeated guest questions, special requests.
5. **Documents:** invoices, guest forms, supplier bills.
6. **Reports:** occupancy, revenue per room, review summaries.
7. **Integrations:** property-management systems, booking channels, WhatsApp, payments.
8. **Savings:** front-desk hours; review-driven revenue.
9. **Willingness to pay:** small hotels and restaurants with high enquiry volume.
10. **Existing solutions:** property-management and reservation software (verify).
**First offer to test:** guest enquiry handling and automatic booking-confirmation messages.

### 13. Professional services (legal, consulting, engineering, architecture)
1. **Repetitive:** client intake, document collection, time capture, invoicing, status updates, report drafting.
2. **Manual admin:** matter/project setup, deadline tracking.
3. **Data entry:** client details, time sheets.
4. **Communication:** client progress questions.
5. **Documents:** contracts, drawings, reports, engagement letters.
6. **Reports:** utilisation, billing, project status.
7. **Integrations:** practice-management, accounting, email, calendar, document storage.
8. **Savings:** billable hours recovered.
9. **Willingness to pay:** partners who see unbilled time; strong when staff are costly.
10. **Existing solutions:** practice-management software (verify).
**Watch-outs:** confidentiality and privilege; AI drafts need professional review.
**First offer to test:** client intake and document-collection workflow with reminders.

### 14. Small and medium enterprises (general)
1. **Repetitive:** invoicing, payment reminders, enquiry replies, order tracking, daily reports.
2. **Manual admin:** spreadsheets copied between tools.
3. **Data entry:** the same data typed into several systems.
4. **Communication:** owner is the bottleneck for approvals.
5. **Documents:** invoices, quotations, purchase orders.
6. **Reports:** weekly owner summary.
7. **Integrations:** accounting, WhatsApp, email, spreadsheets, payment gateways.
8. **Savings:** owner and admin hours.
9. **Willingness to pay:** price-sensitive; wants quick payback and simple terms.
10. **Existing solutions:** accounting suites, no-code tools they set up themselves (verify).
**First offer to test:** quotation and invoice automation with payment reminders.

### 15. Enterprise businesses
1. **Repetitive:** approvals, onboarding, ticket triage, report assembly, data synchronisation between systems.
2. **Manual admin:** compliance evidence collection, vendor onboarding.
3. **Data entry:** re-keying between legacy systems.
4. **Communication:** cross-department hand-offs.
5. **Documents:** contracts, policies, forms in large volume.
6. **Reports:** compliance, finance, operations packs.
7. **Integrations:** ERP, HR, CRM, identity systems, data warehouses.
8. **Savings:** large volume × many teams; also risk reduction.
9. **Willingness to pay:** budgets exist but cycles are long; procurement and security review required.
10. **Existing solutions:** enterprise iPaaS and RPA platforms (verify).
**Watch-outs:** security questionnaires, audit trails, data residency, single sign-on, service levels.
**First offer to test:** one contained departmental workflow as a pilot, then expand.

## 3.6 Deliverable: Market Research and Automation Opportunity Report

For each industry researched, file the industry page (Section 3.5 format), the sourced competitor rows (3.3), the TAM/SAM/SOM sheet (3.2) with low/medium/high cases, the opportunity score and a go/no-go recommendation, in `03-Research/` and `04-Projects/<ID>/01-discovery/`.

---

*Developed by **V. LOKESHWARAN** · VEL Software Solutions · velsoftwaresolutions.in@gmail.com*
