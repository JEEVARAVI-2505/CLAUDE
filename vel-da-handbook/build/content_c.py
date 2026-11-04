"""Parts 8-13 and annexes: teaching format, commercial model, projects, sources, assignment A1."""
from pathlib import Path

from vel_pdf import (H1, H2, H3, P, bullets, callout, code, flow, numbered, pb, table)

A1 = Path("/home/user/CLAUDE/vel-da-handbook/assignments/A1")
SCRATCH = Path("/tmp/claude-0/-home-user-CLAUDE/2d74a425-3153-5e7a-a425-aa285ba68b64/scratchpad/a1_out.txt")


def part8():
    f = [pb()] + H1("Part 8 — Teaching Format and Repository Standard")
    f += H2("8.1 The 15-step format used for every topic")
    f += [P("Every lesson in every module is written in this order, so a beginner always knows where they are. "
            "A topic is <b>complete</b> only when steps 10, 14 and 15 are evidenced.")]
    f += table([
        ["#", "Step", "What it contains"],
        ["1", "Definition", "One plain sentence; no unexplained jargon"],
        ["2", "Business purpose", "Why a client would pay for this"],
        ["3", "Real-world example", "A small business story using realistic (synthetic) numbers"],
        ["4", "Required software", "Exact tools and versions"],
        ["5", "Installation", "Official download source and commands"],
        ["6", "Configuration", "Settings, folders, environments"],
        ["7", "Step-by-step implementation", "Numbered actions a beginner can follow"],
        ["8", "Code and commands", "Copy-ready, commented, tested"],
        ["9", "Expected output", "What success looks like, pasted from a real run"],
        ["10", "Verification", "A check that proves it worked (count, total, test)"],
        ["11", "Common errors", "Message, cause, fix"],
        ["12", "Security considerations", "Data handling, credentials, permissions"],
        ["13", "Documentation", "What to write in README / data dictionary"],
        ["14", "Practical assignment", "A task with a clear deliverable"],
        ["15", "Assessment", "Questions and rubric; pass condition"],
    ], [0.06, 0.26, 0.68])
    f += H2("8.2 Standard project repository")
    f += code("""
data-analytics-project/
  README.md  requirements.txt  .gitignore
  docs/        project-overview, requirements, data-dictionary, kpi-definitions,
               architecture, data-lineage, user-guide  (all .md)
  data/        sample/  raw/  processed/  reference/
  src/         extraction/ cleaning/ transformation/ analytics/ reporting/
  sql/         schemas/ queries/ views/ procedures/
  pipelines/   ingestion/ transformation/ orchestration/
  dashboards/  powerbi/ tableau/
  models/      training/ evaluation/ inference/
  tests/       data_quality/ unit/ integration/
  deployment/
""")
    f += [P("Adapt the structure to the project (a small Excel-only job does not need <font face='Courier'>models/</font>), but keep the names "
            "so any VEL engineer can find things. Document every change from the standard in <font face='Courier'>docs/project-overview.md</font>.")]
    f += H2("8.3 Data handling rules (mandatory)")
    f += table([
        ["Rule", "How it is enforced"],
        ["Client datasets, personal data, credentials and confidential exports <b>never</b> go into a public repository, and never into Git history at all", "<font face='Courier'>.gitignore</font> excludes <font face='Courier'>data/raw/</font>, <font face='Courier'>data/processed/</font>, <font face='Courier'>*.pbix</font> with embedded data, <font face='Courier'>.env</font>; run the secret scan before every push"],
        ["Secrets live in a password manager or environment variables", "Code reads <font face='Courier'>os.environ[\"DB_PASSWORD\"]</font>; the repo holds only <font face='Courier'>.env.example</font> with fake values"],
        ["Only the minimum personal data is collected", "Drop or hash columns such as phone, email, national ID unless the PRD requires them"],
        ["Retention", "Agree per project in the SOW (default: raw client extracts deleted or archived 90 days after final acceptance unless the contract says otherwise)"],
        ["Access", "Least privilege: read-only database users for reporting; named accounts; MFA; access reviewed monthly under managed service"],
        ["Training uses synthetic data", "Real client data is allowed in training only after written client consent and Head approval"],
    ], [0.45, 0.55])
    f += code("""
# .gitignore (minimum)
.venv/
__pycache__/
.env
data/raw/
data/processed/
*.csv.gz
*.xlsx#
~$*.xlsx
""")
    return f


def part9():
    f = [pb()] + H1("Part 9 — Commercial Model and Pricing")
    f += H2("9.1 The formulas")
    f += [P("<b>Total Project Cost</b> = Labour + Software licences + Infrastructure + Data engineering + Dashboard development + QA + "
            "Project management + Other direct costs. (Labour is the hours of each role at that role's fully loaded cost rate; listing "
            "engineering, dashboard, QA and PM separately keeps the estimate auditable.)"),
          P("<b>Selling price</b> = (Delivery cost + Risk contingency + Overhead) / (1 − Target profit margin)."),
          P("<b>Gross margin</b> = (Price − Direct delivery cost) / Price. &nbsp; <b>Break-even</b> (monthly) = Fixed monthly overhead / "
            "Contribution per recurring contract, where contribution = monthly fee − monthly cost to serve.")]
    f += H2("9.2 Worked example (ILLUSTRATIVE numbers, not VEL rates)")
    f += [P("Small retail client, one executive dashboard, three data sources, one nightly pipeline. Rates and percentages below are "
            "<b>assumptions</b> chosen to show the arithmetic; replace them with VEL's approved rate card and verified vendor prices.")]
    f += table([
        ["Line", "Hours", "Rate (INR/h, assumed)", "Cost (INR)"],
        ["Discovery and requirements", "24", "800", "19,200"],
        ["Data audit", "24", "800", "19,200"],
        ["Data engineering", "80", "800", "64,000"],
        ["Dashboard development", "60", "800", "48,000"],
        ["QA and reconciliation", "30", "800", "24,000"],
        ["Project management", "24", "800", "19,200"],
        ["<b>Labour subtotal</b>", "242", "", "<b>1,93,600</b>"],
        ["Dev/test cloud and tools (VEL cost)", "", "", "12,000"],
        ["Other direct costs", "", "", "5,000"],
        ["<b>Direct delivery cost</b>", "", "", "<b>2,10,600</b>"],
        ["Risk contingency 15%", "", "", "31,590"],
        ["Overhead 20% of (cost + contingency)", "", "", "48,438"],
        ["<b>Full cost</b>", "", "", "<b>2,90,628</b>"],
        ["Price at 30% target margin: 2,90,628 / 0.70", "", "", "4,15,183 (quote INR 4,15,000)"],
    ], [0.5, 0.1, 0.2, 0.2])
    f += [P("Check: gross margin = (4,15,000 − 2,10,600) / 4,15,000 = <b>49.3%</b>. If the contingency is fully used, profit is "
            "4,15,000 − 2,90,628 = INR 1,24,372, a net margin of <b>30.0%</b>.")]
    f += table([
        ["Recurring example", "Value"],
        ["Monthly maintenance fee (assumed)", "INR 25,000"],
        ["Monthly cost to serve (monitoring, checks, support hours; assumed)", "INR 9,000"],
        ["Contribution per contract", "INR 16,000"],
        ["Fixed monthly overhead of the department (assumed)", "INR 1,50,000"],
        ["Break-even contracts = 1,50,000 / 16,000", "9.4, so 10 contracts"],
    ], [0.7, 0.3])
    f += H2("9.3 Keep two things separate on every quote")
    f += table([
        ["VEL service fees (VEL invoices)", "Third-party subscriptions (client pays vendor, or VEL passes through at cost with written approval)"],
        ["Discovery, build, QA, training, managed service, support", "Power BI / Tableau licences, cloud database and storage, warehouse compute, API plans, backup storage"],
        ["Fixed price per SOW or monthly retainer", "Usage-based: can rise with data volume; state the assumption and a review date in the SOW"],
    ], [0.4, 0.6])
    f += [P("Also compute and record for each project: <b>cost per dashboard</b> (dashboard development + its share of QA and PM / number of "
            "dashboards), <b>cost per pipeline</b>, <b>cost per customer</b> (total delivery and support cost / active clients), <b>recurring "
            "revenue</b> (sum of monthly fees) and <b>licence and cloud expenses</b> as separate lines.")]
    f += callout("Pricing sources", "Cloud and BI prices must come from the vendors' official pricing pages (Part 11.3), captured with date, "
                 "region, plan and currency. Record exchange rates and tax treatment (for example GST for Indian clients; export-of-services rules for "
                 "overseas clients — confirm with the company's accountant).")
    return f


def part10():
    f = [pb()] + H1("Part 10 — Mandatory Training Projects")
    f += [P("All projects use <b>synthetic or explicitly authorised data</b>. Each has a repository, a README, a data dictionary and a "
            "reviewed deliverable. Acceptance is by the Head of Data &amp; Analytics against the rubric.")]
    f += table([
        ["#", "Project", "Skills proved", "Acceptance criteria", "Week"],
        ["1", "Excel business dashboard", "Formulas, XLOOKUP, PivotTables, charts, validation, Power Query", "KPIs reconcile to raw totals; refreshable; documented; no hard-coded numbers in charts", "3"],
        ["2", "SQL sales analysis", "Joins, aggregation, CTEs, windows, views", "30 queries, each with a business question and a verified result; schema script re-runs cleanly", "5"],
        ["3", "Python data cleaning", "pandas, validation, logging, tests", "Rule log; before/after counts; 10+ unit tests pass; raw file untouched", "7"],
        ["4", "Power BI executive dashboard", "Model, DAX, design, RLS, refresh", "Star schema; 10 measures documented; RLS tested with two roles; accessibility check", "9"],
        ["5", "API-to-database pipeline", "Auth, pagination, retries, loading", "Idempotent re-run; failure handled and logged; rate limits respected", "10"],
        ["6", "Automated monthly reporting", "Scheduling, templating, delivery", "Report generated with no manual step; reconciliation row included; failure alert works", "11"],
        ["7", "Predictive sales forecast", "Time-series split, baseline, evaluation", "Beats baseline on held-out data or honestly says it does not; limitations page; no leakage", "12–13+"],
        ["8", "Full simulated client project", "Stages 01–17", "All stage outputs produced; mock client acceptance record signed", "Days 91–150"],
    ], [0.04, 0.2, 0.24, 0.45, 0.07])
    return f


def part11():
    f = [pb()] + H1("Part 11 — Official Learning Sources")
    f += [P("Use <b>current official documentation</b> first. Third-party tutorials may supplement but never replace it. "
            "URLs below were supplied by the company brief or are the vendors' main documentation sites; if a link has moved, "
            "search from the vendor's home page and tell the Head so this list is corrected.")]
    f += H2("11.1 Core learning sources")
    f += table([
        ["Topic", "Source", "URL"],
        ["Microsoft training (all products)", "Microsoft Learn", "https://learn.microsoft.com/"],
        ["Power BI", "Microsoft Learn: Power BI", "https://learn.microsoft.com/power-bi/"],
        ["Tableau", "Tableau Learn", "https://www.tableau.com/learn"],
        ["Python", "Python documentation", "https://docs.python.org/3/"],
        ["pandas", "pandas documentation", "https://pandas.pydata.org/docs/"],
        ["PostgreSQL", "PostgreSQL documentation", "https://www.postgresql.org/docs/"],
        ["Google Cloud", "Google Cloud documentation", "https://cloud.google.com/docs"],
        ["AWS", "AWS documentation", "https://docs.aws.amazon.com/"],
        ["dbt", "dbt documentation", "https://docs.getdbt.com/"],
        ["scikit-learn", "scikit-learn user guide", "https://scikit-learn.org/stable/"],
    ], [0.27, 0.3, 0.43])
    f += H2("11.2 Supporting official sources by level")
    f += table([
        ["Level", "Also use", "URL"],
        ["1 Data fundamentals", "Microsoft Learn data-fundamentals paths; PostgreSQL tutorial", "https://learn.microsoft.com/ ; https://www.postgresql.org/docs/current/tutorial.html"],
        ["2 Excel", "Microsoft Support: Excel help and training", "https://support.microsoft.com/excel"],
        ["3 SQL", "PostgreSQL SQL language reference", "https://www.postgresql.org/docs/current/sql.html"],
        ["4 Python", "NumPy, Matplotlib, Plotly, Requests", "https://numpy.org/doc/ ; https://matplotlib.org/stable/ ; https://plotly.com/python/ ; https://requests.readthedocs.io/"],
        ["5 BI", "DAX reference; Looker Studio help", "https://learn.microsoft.com/dax/ ; https://cloud.google.com/looker-studio"],
        ["6 Data engineering", "dbt; Apache Airflow; cloud warehouse docs", "https://docs.getdbt.com/ ; https://airflow.apache.org/docs/ ; https://cloud.google.com/bigquery/docs ; https://docs.snowflake.com/ ; https://docs.aws.amazon.com/redshift/"],
        ["7 Predictive", "scikit-learn model evaluation and cross-validation", "https://scikit-learn.org/stable/model_selection.html"],
        ["Tools", "Git, VS Code, GitHub", "https://git-scm.com/doc ; https://code.visualstudio.com/docs ; https://docs.github.com/"],
        ["Azure", "Azure documentation", "https://learn.microsoft.com/azure/"],
    ], [0.17, 0.33, 0.5])
    f += H2("11.3 Official pricing and licensing pages (verify on the day you quote)")
    f += table([
        ["Vendor", "Pricing / licensing page"],
        ["Power BI", "https://powerbi.microsoft.com/pricing/"],
        ["Tableau", "https://www.tableau.com/pricing"],
        ["AWS", "https://aws.amazon.com/pricing/"],
        ["Microsoft Azure", "https://azure.microsoft.com/pricing/"],
        ["Google Cloud", "https://cloud.google.com/pricing"],
        ["Snowflake", "https://www.snowflake.com/pricing/"],
        ["Python, pandas, PostgreSQL, scikit-learn, dbt Core", "Open-source licences; read each project's LICENSE file and record it in the project's third-party register"],
    ], [0.3, 0.7])
    f += callout("Source discipline", "For every cost, rule or legal statement in a proposal, the cost sheet or document must record: source URL, date "
                 "accessed, plan or version, and who verified it. A number without a source is an assumption and must be labelled as one.")
    return f


def part12():
    f = [pb()] + H1("Part 12 — First Practical Assignment (A1): Audit a Sales Dataset")
    f += [P("<b>Level 1 · Week 1 · Estimated time 6–8 hours · Data: synthetic (generated by the script below)</b>")]
    f += [P("This assignment follows the 15-step teaching format (Part 8.1). It is deliberately small: the aim is to learn to "
            "<b>distrust data politely</b> and to prove, with evidence, what is wrong with it. This is Stage 04 in miniature.")]
    f += H2("1. Definition")
    f += [P("A <b>data audit</b> is a systematic, read-only inspection of a dataset to find missing values, duplicates, wrong formats, impossible values "
            "and inconsistent definitions, before anyone builds a report on it.")]
    f += H2("2. Business purpose")
    f += [P("A dashboard built on unaudited data shows confident wrong numbers. The audit is the cheapest place to find errors "
            "and the first thing a client sees that proves VEL is careful.")]
    f += H2("3. Real-world example")
    f += [P("\"Sunrise Retail\" (fictional) exports 505 order lines from its billing software. The owner says January–June sales were "
            "\"about right\". Before building anything we check: are there duplicate orders (double counting revenue)? Negative quantities? "
            "Dates in two formats (so some months sort wrongly)? Regions typed in different cases (so a pivot shows \"north\" and \"North\" "
            "as two regions)? Each defect changes the totals.")]
    f += H2("4–6. Software, installation and configuration")
    f += bullets(["Python 3.11+, pandas, VS Code, Git (Part 3.5). Excel or Google Sheets for the manual check.",
                  "Create the folder <font face='Courier'>a1-sales-audit/</font>, create and activate a virtual environment, run "
                  "<font face='Courier'>pip install pandas</font>, then save the two scripts below into the folder.",
                  "Use <font face='Courier'>git init</font> and add a <font face='Courier'>.gitignore</font> (Part 8.3). Even though this data is fake, practise the habit."])
    f += H2("7. Step-by-step implementation")
    f += numbered([
        "Run <font face='Courier'>generate_sales_data.py</font>. It writes <font face='Courier'>data/sales_raw.csv</font> (505 rows). The fixed seed 2505 means everybody gets the same file.",
        "Open the CSV in Excel. Without code, write down by eye 3 problems you can spot. (Most are hidden; this shows why code is needed.)",
        "Read <font face='Courier'>audit_data.py</font> line by line. For each of the six checks write one sentence: what does it test and why does it matter?",
        "Run the audit script. Compare its numbers with your Excel observations.",
        "Reproduce two of the checks in Excel: remove duplicates (note the count), and add a helper column <font face='Courier'>=F2*G2-H2</font> to find revenue mismatches.",
        "Write the audit report (template in Annex A): findings table, business impact of each, recommended fix, and the question you would ask the data owner.",
        "Commit with the message <font face='Courier'>L1.01: audit sales_raw.csv and report findings</font>.",
    ])
    f += H2("8. Code")
    f += H3("generate_sales_data.py (creates synthetic data with deliberate defects)")
    f += code((A1 / "generate_sales_data.py").read_text())
    f += H3("audit_data.py (read-only audit; never changes the source file)")
    f += code((A1 / "audit_data.py").read_text())
    f += H2("9. Expected output")
    f += [P("Run on the file above, the real output of the audit script is:")]
    f += code(SCRATCH.read_text())
    f += [P("The first rows of <font face='Courier'>sales_raw.csv</font> start with: <font face='Courier'>ORD0001, 2025-06-05, C066, North, P03, 8, 850.0, 6800.0</font>.", "small")]
    f += H2("10. Verification")
    f += table([
        ["Check", "How to prove it", "Expected"],
        ["Row count", "<font face='Courier'>len(df)</font> or Excel status bar", "505 data rows (500 orders + 5 duplicates)"],
        ["Duplicates", "Excel: Data > Remove Duplicates (all columns)", "5 rows removed"],
        ["Missing region", "Excel filter on blanks", "10 rows"],
        ["Revenue mismatch", "Helper column <font face='Courier'>=F2*G2-H2</font>", "9 non-zero rows"],
        ["Why 9, not the 4 injected?", "Inspect the mismatching rows", "4 rows had revenue inflated by 1.5x, and the 5 rows given a negative quantity no longer equal quantity x price, so both defect groups show up"],
    ], [0.22, 0.43, 0.35])
    f += H2("11. Common errors")
    f += table([
        ["Message or symptom", "Cause", "Fix"],
        ["<font face='Courier'>ModuleNotFoundError: No module named 'pandas'</font>", "Virtual environment not active or pandas not installed", "Activate <font face='Courier'>.venv</font>, run <font face='Courier'>pip install pandas</font>"],
        ["<font face='Courier'>FileNotFoundError ... sales_raw.csv</font>", "Generator not run, or run from another folder", "Run the generator first; the scripts use paths relative to their own folder"],
        ["Excel shows dates as <font face='Courier'>05/06/2025</font> for some rows and <font face='Courier'>2025-06-05</font> for others", "Genuine data defect (and Excel locale guessing)", "Report it; never fix by hand in the raw file"],
        ["Counts differ from this book", "A different seed or edited file", "Re-generate with seed 2505; do not edit the CSV"],
    ], [0.38, 0.32, 0.3])
    f += H2("12. Security considerations")
    f += bullets(["Treat the file as if it were real client data: do not email it or upload it to online tools.",
                  "The audit is <b>read-only</b>. Never overwrite the raw file; later cleaning writes a new file in <font face='Courier'>data/processed/</font>.",
                  "Real projects: a data-sharing agreement must exist before any file is received (Stage 02)."])
    f += H2("13. Documentation")
    f += [P("Create <font face='Courier'>README.md</font> (what the project is, how to run both scripts) and <font face='Courier'>docs/data-dictionary.md</font> "
            "with one row per column: name, meaning, type, allowed values, and known issues. For example: <i>region — text — North/South/East/West — 10 blanks, 8 wrong-case.</i>")]
    f += H2("14. Practical assignment (deliverables)")
    f += numbered([
        "<font face='Courier'>audit_report.md</font> or PDF (Annex A template), maximum 2 pages.",
        "Both scripts, plus README and data dictionary, committed to Git.",
        "<b>Extension (required):</b> add two new checks to <font face='Courier'>audit_data.py</font> — (a) customer_id values not matching the pattern C### and (b) order_date values later than today — and explain what results you get.",
        "A 2-minute spoken explanation to your buddy of why raw data must not be edited.",
    ])
    f += H2("15. Assessment")
    f += table([
        ["Rubric line", "5 = excellent", "3 = pass", "0 = fail"],
        ["Completeness of findings", "All six defect types found with counts", "At least five types", "Fewer than four"],
        ["Evidence", "Every finding backed by query output or screenshot", "Most backed", "Statements without evidence"],
        ["Business impact", "Each defect tied to a wrong number or decision", "Impact stated for most", "None"],
        ["Safe handling", "Raw file untouched; Git history clean", "Minor slip, corrected", "Raw file edited"],
        ["Documentation", "README and dictionary allow a stranger to repeat the work", "Understandable with gaps", "Missing"],
    ], [0.24, 0.31, 0.25, 0.2])
    f += [P("<b>Pass:</b> every line 3 or more, average 3.5 or more, and Quiz Q1 (Annex C) at 70% or higher. Do not start Level 2 until A1 is passed.")]
    return f


def part13():
    f = [pb()] + H1("Part 13 — Module Roadmap and Progress Tracker")
    f += [P("Modules are issued one at a time. A module is released after the previous one is reviewed and approved. Status is updated at each release.")]
    f += table([
        ["Module", "Content", "Status"],
        ["1", "Foundation: charter, organisation, onboarding, roadmap, 90-day plan, stack, lifecycle map, commercial model, sources, Assignment A1", "<b>Issued (this document)</b>"],
        ["2", "Level 1 full lesson: data fundamentals, keys, quality, governance, KPIs, with quizzes", "Next"],
        ["3", "Level 2 Excel, Project 1", "Planned"],
        ["4", "Level 3 SQL on PostgreSQL, Project 2", "Planned"],
        ["5", "Level 4 Python for analytics, Project 3", "Planned"],
        ["6", "Level 5 Power BI and Tableau, Project 4", "Planned"],
        ["7", "Level 6 data engineering, Projects 5–6", "Planned"],
        ["8", "Level 7 predictive analytics, Project 7", "Planned"],
        ["9–12", "Stages 01–17 in depth with templates; Project 8", "Planned"],
        ["13", "Internal management portal (employees, training, clients, projects, tasks, inventories, QA, costing, billing, tickets, reports)", "Planned"],
        ["14", "Sales kit: discovery questionnaires, proposals, demo scripts, SOW, pricing sheets, handover pack", "Planned"],
    ], [0.1, 0.65, 0.25])
    f += H2("Known limitations of this version")
    f += bullets(["Rate card and prices in Part 9 are illustrative only. The Founder must set VEL's real rates and verify vendor prices.",
                  "Legal topics (data protection, contracts, tax) are described at awareness level; the company's lawyer and accountant must review the templates.",
                  "Tool versions and menus change; each module lists the version it was written against."])
    return f


def annexes():
    f = [pb()] + H1("Annex A — Data Audit Report Template (Stage 04 / Assignment A1)")
    f += table([
        ["Section", "Content"],
        ["1. Scope", "Dataset name, source, date received, owner, row and column counts, file hash if available"],
        ["2. Method", "Tools and scripts used; confirmation that the raw file was not modified"],
        ["3. Findings table", "ID | Check | Count | Example rows | Business impact | Severity (High / Medium / Low)"],
        ["4. Questions for the data owner", "What does a blank region mean? Are negative quantities returns? Which total is the trusted one?"],
        ["5. Recommendation", "Fix at source / fix in clean layer / exclude / accept with note"],
        ["6. Verdict", "Fit for reporting: Yes / Yes after fixes / No"],
        ["7. Sign-off", "Author, reviewer, date"],
    ], [0.25, 0.75])
    f += H1("Annex B — Daily Log Template")
    f += code("""
Date:            Name:            Week/Day:
1. Done today:
2. Learned (one sentence per topic):
3. Blocked by / questions asked:
4. Commit(s) made (hash or message):
5. Plan for tomorrow:
Buddy initials:
""")
    f += H1("Annex C — Quiz Q1 (Level 1, Week 1) and Trainer Answer Key")
    f += [P("Trainer copy. Trainees receive the questions only. Pass mark 7 of 10.")]
    f += table([
        ["#", "Question", "Answer key"],
        ["1", "Give one example each of structured, semi-structured and unstructured data.", "Structured: sales table. Semi-structured: JSON from an API. Unstructured: customer email text, images."],
        ["2", "What is a primary key, and why must it be unique and not blank?", "Column(s) that identify one row; duplicates or blanks make rows ambiguous and break joins."],
        ["3", "What is a foreign key? Give an example.", "A column referencing another table's primary key, e.g. sales.customer_id to customers.customer_id."],
        ["4", "Name four data quality dimensions and test one for sales_raw.csv.", "Completeness, uniqueness, validity, consistency, accuracy, timeliness. E.g. completeness: 10 blank regions."],
        ["5", "Why do we never edit raw data?", "Traceability and repeatability; fixes happen in a clean layer so every number can be traced back."],
        ["6", "Define KPI and give a good and a bad example.", "A measurable value tied to a goal. Good: monthly revenue by region, defined precisely. Bad: \"sales\" with no definition or period."],
        ["7", "A row has quantity -3. List two possible explanations and what to do.", "Return/credit note or entry error. Do not guess; ask the owner and document the rule."],
        ["8", "What is data governance in one sentence?", "Agreed ownership, rules, quality standards and access controls for data."],
        ["9", "Why is it unsafe to put a database password in a Git repository?", "History keeps it forever and others can read it; use environment variables or a password manager."],
        ["10", "Name the stages of the data lifecycle.", "Collect, store, process/clean, analyse, share/report, retain/archive, delete."],
    ], [0.05, 0.5, 0.45])
    f += callout("Engineer must verify", "This handbook and the training outputs support analytics work. Financial, medical, legal and other "
                 "high-stakes conclusions drawn from data or models must be verified by a qualified professional before anyone acts on them.")
    return f
