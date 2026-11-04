"""Parts 0-3: orientation, department charter, organisation, onboarding."""
from vel_pdf import (H1, H2, H3, P, bullets, callout, code, flow, numbered, orgchart, pb, table)


def part0():
    f = H1("Part 0 — How to Use This Handbook")
    f += [P("<b>Data Analytics &amp; Business Intelligence Department — Training, Engineering and Client Delivery "
            "Operating System</b>")]
    f += table([
        ["Document control", ""],
        ["Document ID", "VEL-DA-001 (companion to VEL-DH-001, the company Delivery Handbook)"],
        ["Version", "1.0 — Module 1 (Foundation). Later modules are issued one at a time (see Part 13)."],
        ["Owner", "V. LOKESHWARAN, Founder"],
        ["Audience", "New employees with basic computer knowledge, plus the Head of Data &amp; Analytics who trains them"],
        ["Review cycle", "Every quarter, and after every client project closes"],
        ["Contact", "velsoftwaresolutions.in@gmail.com"],
    ], [0.22, 0.78])
    f += H2("0.1 What this handbook is")
    f += [P("This is the single operating manual for the Data Analytics &amp; BI department. It does two jobs at once: "
            "it <b>trains</b> a beginner into a professional analytics engineer, and it defines the <b>corporate "
            "process</b> used to deliver analytics projects to clients, including international clients, from the "
            "first enquiry (0%) to a signed-off, maintained solution (100%)."),
          P("Nothing here assumes you already know the subject. Every technical word is explained the first time it "
            "appears. Where a number depends on a vendor price or a law, this handbook tells you to <b>verify it from "
            "the official source on the day you quote</b> instead of printing a figure that will go out of date.")]
    f += H2("0.2 What is in Module 1, and what comes next")
    f += table([
        ["Delivered now (Module 1)", "Delivered later, one module at a time"],
        ["Department charter and services (Part 1)<br/>Organisation structure and roles (Part 2)<br/>"
         "Employee onboarding plan (Part 3)<br/>Training roadmap, Levels 1–7 (Part 4)<br/>"
         "<b>90-day training schedule (Part 5)</b><br/>Technology stack and tool selection (Part 6)<br/>"
         "17-stage project lifecycle summary (Part 7)<br/>Teaching format and repository standard (Part 8)<br/>"
         "Commercial model (Part 9)<br/>Training projects and learning sources (Parts 10–11)<br/>"
         "<b>First practical assignment A1 with real code (Part 12)</b><br/>Module roadmap (Part 13), Annexes A–C",
         "Module 2: Level 1 Data Fundamentals (full lesson)<br/>Module 3: Level 2 Excel<br/>Module 4: Level 3 SQL<br/>"
         "Module 5: Level 4 Python<br/>Module 6: Level 5 Business Intelligence<br/>Module 7: Level 6 Data Engineering<br/>"
         "Module 8: Level 7 Predictive Analytics<br/>Modules 9–12: Stages 01–17 in full depth, with templates<br/>"
         "Module 13: Internal portal<br/>Module 14: Sales kit (questionnaires, proposals, SOW)"],
    ], [0.5, 0.5])
    f += callout("Honest scope note",
                 "A 90-day plan can take a motivated beginner to <b>supervised junior-analyst level</b> across "
                 "Levels 1–5, with a working introduction to Levels 6–7. It cannot produce an independent "
                 "senior engineer; that takes roughly 6–12 months of real project work. This handbook plans for "
                 "that reality instead of promising otherwise.")
    f += H2("0.3 The twelve golden rules of the department")
    f += numbered([
        "<b>Never assume the client's data is correct.</b> Audit it first (Stage 04). Every project starts with a data-quality assessment.",
        "<b>Sell business outcomes, not charts.</b> A dashboard is a means; a trusted, verified management report is the product.",
        "<b>One definition per KPI.</b> If two people calculate \"revenue\" differently, the dashboard is wrong even if the maths is right.",
        "<b>Reconcile before you publish.</b> Important numbers must match a trusted source record, with evidence saved.",
        "<b>Never commit client data, personal data, passwords or API keys to Git.</b> Not once, not temporarily.",
        "<b>Predictions are estimates, not promises.</b> Always show a baseline, the error, and the limitations.",
        "<b>Choose the simplest tool that works.</b> Excel before Power BI, a SQL view before a pipeline, a chart before a model.",
        "<b>Write it down.</b> If it is not in a document or ticket, it did not happen.",
        "<b>Do not move forward until the previous level is understood.</b> Gates are real; a failed gate means repeat, not skip.",
        "<b>Small steps, saved often.</b> Commit whenever something works; keep every transformation reproducible.",
        "<b>Ask early.</b> Being stuck for 30 minutes without asking is a mistake, not a virtue.",
        "<b>Client data belongs to the client.</b> Treat it as if it were your own bank details; use the minimum needed, for the agreed purpose only.",
    ])
    f += [pb()]
    f += H2("0.4 How to use the handbook day to day")
    f += numbered([
        "Find today's week in the 90-day schedule (Part 5). Read the week's objectives before touching a tool.",
        "Follow the 15-step teaching format (Part 8) for each topic. Do the practical assignment, not just the reading.",
        "Save every deliverable in the named folder. Update your daily log (template in Annex B).",
        "At the end of each level, take the gate assessment. Pass mark and rules are in Part 4.",
        "If anything in this book is wrong, unclear or out of date, tell the Head of Data &amp; Analytics; they will fix the book. Improving it is part of everyone's job.",
    ])
    return f


def part1():
    f = [pb()] + H1("Part 1 — Department Charter and Commercial Services")
    f += H2("1.1 Mission")
    f += [P("To turn clients' scattered, unverified business data into <b>trusted decisions</b>: one verified reporting "
            "environment per client, delivered on time, documented, secure, and maintained on a monthly service.")]
    f += H2("1.2 The five service lines")
    f += table([
        ["Line", "What the client receives", "Typical outputs", "Recurring revenue?"],
        ["<b>A. Data Analytics</b>", "Answers to business questions: where is revenue leaking, who are our best customers, why did margin fall?",
         "Analysis report, KPI pack, customer segments, trend and root-cause findings", "Sometimes (retainer)"],
        ["<b>B. Business Intelligence</b>", "Interactive dashboards and automated reports that leaders use every week",
         "Power BI / Tableau / Looker Studio dashboards, scheduled reports, KPI monitor", "Yes (maintenance)"],
        ["<b>C. Data Engineering</b>", "A reliable, clean, integrated data foundation the dashboards sit on",
         "ETL/ELT pipelines, database or warehouse, data model, quality monitors", "Yes (pipeline support)"],
        ["<b>D. Predictive Analytics</b>", "Forecasts and risk scores where history and volume justify them",
         "Sales/demand/revenue/inventory forecast, churn model, anomaly alerts, maintenance prediction", "Yes (model monitoring)"],
        ["<b>E. Managed Analytics</b>", "VEL runs the analytics function for the client month after month",
         "Monthly reporting, dashboard upkeep, pipeline monitoring, KPI tracking, consulting", "Yes (core recurring)"],
    ], [0.17, 0.33, 0.33, 0.17])
    f += H2("1.3 Selling outcomes, not dashboards")
    f += table([
        ["Do not sell", "Sell instead"],
        ["\"A Power BI dashboard\"", "\"A management reporting solution that consolidates sales, finance and operations data into one verified reporting environment, so the owner sees the same numbers as the accountant.\""],
        ["\"A churn model\"", "\"An early-warning list of customers likely to leave next quarter, so the account team can act, measured against what happens without the model.\""],
        ["\"ETL pipelines\"", "\"Reports that refresh themselves every morning, so nobody spends two days a month copying Excel sheets.\""],
    ], [0.28, 0.72])
    f += H2("1.4 Target industries and typical first questions")
    f += table([
        ["Industry", "Typical systems", "Typical first KPIs"],
        ["Healthcare", "HIS/EMR, billing, lab systems (<b>health data is highly sensitive</b>)", "Bed occupancy, wait time, claim rejection rate, revenue per department"],
        ["Manufacturing", "ERP, MES, machine logs, Excel", "OEE, scrap rate, on-time delivery, cost per unit, downtime"],
        ["Retail", "POS, inventory, loyalty, Excel", "Sales per store, basket size, stock-outs, margin by category"],
        ["E-commerce", "Shopify/WooCommerce, ad platforms, payment gateways", "Conversion rate, CAC, repeat-purchase rate, return rate, AOV"],
        ["Logistics", "TMS, GPS, warehouse system", "On-time-in-full, cost per delivery, route utilisation, dwell time"],
        ["Finance", "Core banking/ledger, CRM, collections", "Portfolio at risk, collection efficiency, cost-to-income, delinquency"],
        ["SaaS", "Product database, billing, CRM, support", "MRR/ARR, churn, expansion revenue, activation, support load"],
        ["Professional services", "Time tracking, invoicing, CRM", "Utilisation, realisation, project margin, pipeline, debtor days"],
    ], [0.17, 0.38, 0.45])
    f += callout("Regulated industries",
                 "Healthcare and finance data carry legal duties (for example India's Digital Personal Data Protection "
                 "Act 2023, and for overseas clients GDPR or HIPAA). Never accept a regulated-data project until the "
                 "Founder and the client's data owner have agreed the legal basis, storage location and access rules in "
                 "writing. When unsure, escalate; do not decide alone.")
    return f


def part2():
    f = [pb()] + H1("Part 2 — Organisation Structure")
    f += H2("2.1 Department chart")
    f += orgchart()
    f += [P("A startup rarely has all of these as separate people. The chart shows the <b>roles</b>; Part 2.3 shows how "
            "to staff them in phases. One person may hold several roles in Phase 1, but the <b>duties must still be "
            "performed</b>, especially QA, which should never be marked by the person who built the work.", "small")]
    f += H2("2.2 Roles in plain words")
    f += table([
        ["Role", "What they really do", "Key outputs", "You ask them when"],
        ["Head of Data &amp; Analytics", "Owns delivery quality, architecture decisions and training; reviews everything before the client sees it", "Architecture, estimates, code review, gate sign-off", "You are technically stuck or unsure about a design"],
        ["Business Analyst / Client Lead", "Talks to the client; turns business problems into written requirements and KPI definitions", "Discovery report, PRD, KPI definitions, UAT plan", "You do not understand what the client wants"],
        ["Data Engineer", "Gets data from sources, cleans it, loads it into a database or warehouse reliably", "Pipelines, schemas, data model, monitoring", "Data is missing, late, duplicated or slow"],
        ["BI Developer", "Builds dashboards and semantic models; writes DAX and measures", "Dashboards, measures, RLS, refresh schedule", "A number on a dashboard looks wrong"],
        ["Data Analyst", "Explores data, answers questions, writes SQL, finds insights", "Analysis notes, KPI results, ad-hoc queries", "You need an answer from the data"],
        ["Data Scientist / ML Engineer", "Builds and evaluates forecasts and classifiers; says when a model is <b>not</b> justified", "Baseline, model, evaluation report, monitoring", "Someone asks \"can we predict X?\""],
        ["QA &amp; Data Quality Analyst", "Reconciles dashboards to source, tests pipelines, checks access rules", "Test evidence, reconciliation sheet, defect log", "You want to know how to test something"],
        ["Project Manager", "Plan, risk, scope control, status, client communication", "Plan, weekly status, change log, invoices trigger", "Priorities, deadlines, scope questions"],
        ["Trainee", "Learns through the 90-day plan; works only on synthetic or authorised data until cleared", "Daily log, assignments, gate results", "—"],
    ], [0.17, 0.34, 0.25, 0.24])
    f += H2("2.3 Phased staffing for a startup")
    f += table([
        ["Phase", "Headcount (indicative)", "Who covers what", "Trigger to move on"],
        ["1. Foundation (months 0–6)", "Founder + 1 Head/Lead + 2–3 trainees", "Lead is analyst + engineer + BI developer; Founder does sales and project management; a <b>different trainee QA-checks</b> each deliverable", "2 paid projects delivered and accepted"],
        ["2. Delivery team (months 6–12)", "+ 1 BI developer, + 1 data engineer, + part-time QA", "Roles split; Lead reviews, does not build", "Recurring revenue covers team salaries"],
        ["3. Practice (year 2+)", "+ Data scientist, + BA/Client lead, + support analyst", "Managed analytics team runs monthly cycles", "Defined by Founder with finance"],
    ], [0.18, 0.2, 0.42, 0.2])
    f += H2("2.4 Responsibility matrix for the standard delivery")
    f += [P("R = does the work, A = accountable (one person only), C = consulted, I = informed.", "small")]
    f += table([
        ["Activity", "Founder", "Head", "BA", "Eng.", "BI Dev", "Analyst", "QA", "PM"],
        ["Lead qualification", "A", "C", "R", "—", "—", "—", "—", "I"],
        ["Discovery and requirements", "I", "A", "R", "C", "C", "C", "I", "C"],
        ["Data audit", "I", "A", "C", "R", "—", "R", "C", "I"],
        ["Architecture and estimate", "A (price)", "R", "C", "R", "C", "—", "—", "C"],
        ["Pipelines and data model", "—", "A", "C", "R", "C", "C", "C", "I"],
        ["Dashboards", "—", "A", "C", "C", "R", "C", "C", "I"],
        ["Predictive model", "—", "A", "C", "C", "—", "C", "C", "I"],
        ["QA and reconciliation", "I", "A", "C", "C", "C", "C", "R", "I"],
        ["Deployment approval", "A", "R", "I", "R", "R", "—", "C", "I"],
        ["Handover and acceptance", "A", "R", "R", "C", "C", "C", "C", "R"],
    ], [0.27, 0.1, 0.09, 0.08, 0.09, 0.09, 0.1, 0.08, 0.1])
    f += H2("2.5 Career ladder")
    f += table([
        ["Level", "Title", "Proves by", "Typical time"],
        ["T", "Trainee", "Passes 90-day gates 1–6", "0–3 months"],
        ["J1", "Junior Analyst / Junior BI Developer", "Delivers a supervised module of a real project; passes QA with few defects", "3–9 months"],
        ["J2", "Analyst / BI Developer / Data Engineer", "Owns one dashboard or pipeline end to end; writes the data dictionary and test evidence", "9–24 months"],
        ["S", "Senior (Architect, Lead)", "Designs solutions, estimates cost, leads client discovery, reviews others", "24+ months"],
    ], [0.08, 0.3, 0.46, 0.16])
    return f


def part3():
    f = [pb()] + H1("Part 3 — Employee Onboarding Plan")
    f += H2("3.1 Principles")
    f += bullets([
        "Every new employee gets a <b>buddy</b> (a colleague who answers questions) and a <b>mentor</b> (the Head of Data &amp; Analytics).",
        "The employee works <b>only on synthetic or explicitly authorised data</b> until the security briefing is signed.",
        "Equipment and accounts are ready <b>before day 1</b>. A new joiner should never wait on IT.",
        "Success is measured by gates passed, not hours sat.",
    ])
    f += H2("3.2 Before day 1 (Head of Data &amp; Analytics and Founder)")
    f += table([
        ["#", "Action", "Owner", "Done"],
        ["1", "Employment agreement, NDA and data-protection undertaking signed", "Founder", "[ ]"],
        ["2", "Laptop prepared: Windows 10/11 or Linux, 16 GB RAM recommended (8 GB minimum), 256 GB free storage, disk encryption on", "Head", "[ ]"],
        ["3", "Company email account created; multi-factor authentication (MFA) enforced", "Founder", "[ ]"],
        ["4", "GitHub account invited to the company organisation (private repos only)", "Head", "[ ]"],
        ["5", "Buddy assigned; first-week calendar sent", "Head", "[ ]"],
        ["6", "Training repository created from the template (Part 8) with synthetic data only", "Head", "[ ]"],
        ["7", "Password manager seat assigned (shared secrets live only there)", "Founder", "[ ]"],
    ], [0.05, 0.65, 0.18, 0.12])
    f += H2("3.3 Day 1 — Welcome and safety")
    f += table([
        ["Time", "Activity"],
        ["09:30–10:30", "Welcome by the Founder: company mission, services, the 12 golden rules (Part 0)"],
        ["10:30–11:30", "Tour of this handbook and the 90-day plan; meet buddy and mentor"],
        ["11:30–13:00", "<b>Information-security briefing</b>: MFA, password manager, no client data on personal devices or personal cloud, no secrets in Git, how to report an incident. Employee signs the acknowledgement."],
        ["14:00–17:00", "Workstation setup using the checklist in 3.5; verify every tool with the listed command"],
        ["17:00–17:30", "First daily log written (Annex B) and reviewed by buddy"],
    ], [0.2, 0.8])
    f += H2("3.4 Week 1 outline (detail in Part 5)")
    f += table([
        ["Day", "Focus", "Output"],
        ["1", "Orientation, security briefing, workstation", "Verified setup log"],
        ["2", "Level 1: what data is; databases vs spreadsheets; rows, columns, data types", "Notes + glossary (20 terms)"],
        ["3", "Level 1: keys, relationships, data quality dimensions", "Hand-drawn table diagram of a shop database"],
        ["4", "Level 1: data lifecycle, KPIs, governance. Start Assignment A1 (Part 12)", "A1 files generated"],
        ["5", "Finish A1; Level 1 gate quiz; Friday review with mentor", "A1 audit report + quiz result"],
    ], [0.08, 0.6, 0.32])
    f += H2("3.5 Workstation setup checklist")
    f += [P("Install in this order. After each tool, run the <b>verify command</b> and paste the output into your setup log. "
            "Download tools only from the official sites listed; never from third-party \"download\" pages.")]
    f += table([
        ["Tool", "Official source", "Verify command / action", "Needed from"],
        ["Git", "git-scm.com", "<font face='Courier'>git --version</font>", "Week 1"],
        ["VS Code", "code.visualstudio.com", "Open VS Code; install the Python and SQLTools extensions", "Week 1"],
        ["Python 3.11 or newer", "python.org/downloads", "<font face='Courier'>python --version</font> (on Linux/macOS: <font face='Courier'>python3 --version</font>)", "Week 1"],
        ["Microsoft Excel (licensed) or Google Sheets", "company licence / sheets.google.com", "Open a workbook; check that Power Query (Data tab, Get Data) exists", "Week 2"],
        ["PostgreSQL 16 or newer + pgAdmin", "postgresql.org/download", "<font face='Courier'>psql --version</font>; connect locally", "Week 4"],
        ["Power BI Desktop (Windows only)", "Microsoft Store or Microsoft download page", "Open and sign in with the work account", "Week 8"],
        ["Draw.io (diagrams.net)", "diagrams.net", "Create and save a test diagram as .drawio", "Week 1"],
    ], [0.22, 0.24, 0.42, 0.12])
    f += callout("Power BI on Mac or Linux", "Power BI Desktop runs on Windows only. A trainee on another system uses a company "
                 "Windows virtual machine or loaner laptop for Weeks 8–9. Plan this in advance; check Microsoft Learn for current options.")
    f += H3("Python project environment (do this once per project)")
    f += code("""
# Windows (PowerShell)
python -m venv .venv
.venv\\Scripts\\Activate.ps1
python -m pip install --upgrade pip
pip install pandas numpy matplotlib openpyxl requests

# Linux / macOS
python3 -m venv .venv
source .venv/bin/activate
python -m pip install --upgrade pip
pip install pandas numpy matplotlib openpyxl requests

# Record exactly what you installed, so the next person can repeat it
pip freeze > requirements.txt
""")
    f += [P("<b>Why a virtual environment?</b> It keeps each project's libraries separate, so upgrading one project never breaks another. "
            "<b>Common error:</b> <i>\"python is not recognised\"</i> means Python was installed without \"Add python.exe to PATH\"; "
            "re-run the installer and tick that box.")]
    f += H2("3.6 First-month checkpoints")
    f += table([
        ["When", "Checkpoint", "Pass condition"],
        ["End of week 1", "Orientation review", "Security acknowledgement signed; setup verified; A1 submitted"],
        ["End of week 3", "Gate 1 (Levels 1–2)", "Written 70%+ and Excel practical rated 3 of 5 or better on every rubric line"],
        ["Day 30", "Mentor 1-to-1", "Daily logs complete; buddy feedback; plan adjustments agreed in writing"],
        ["Day 90", "Probation review", "Gates 1–6 passed; Projects 1–6 reviewed; recommendation to Founder"],
    ], [0.18, 0.28, 0.54])
    return f
