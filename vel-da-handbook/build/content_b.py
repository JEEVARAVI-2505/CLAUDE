"""Parts 4-7: training roadmap, 90-day schedule, technology stack, lifecycle."""
from vel_pdf import (H1, H2, H3, P, bullets, callout, code, flow, numbered, pb, table)


def part4():
    f = [pb()] + H1("Part 4 — Training Roadmap: Levels 1 to 7")
    f += [P("Seven levels, taught strictly in order. Each level ends with a <b>gate</b>: a written test and a practical "
            "task marked against a rubric. Do not start the next level until the gate is passed.")]
    f += flow(["L1\nData\nfundamentals", "L2\nExcel", "L3\nSQL", "L4\nPython", "L5\nBusiness\nIntelligence",
               "L6\nData\nengineering", "L7\nPredictive\nanalytics"], box_h=40)
    f += table([
        ["Level", "Topics (all taught from zero)", "Hours", "Proof of learning"],
        ["<b>1 Data Fundamentals</b>", "What is data; structured, semi-structured, unstructured; databases vs spreadsheets; tables, rows, columns; data types; primary and foreign keys; data quality; data lifecycle; business KPIs; data governance", "25", "Assignment A1 (data audit) + quiz"],
        ["<b>2 Excel</b>", "Entry and formatting; sorting, filtering; IF, SUMIF, COUNTIF; XLOOKUP; PivotTables; charts; data validation; Power Query; dashboard basics", "45", "Project 1: Excel business dashboard"],
        ["<b>3 SQL</b>", "Database basics; SELECT, INSERT, UPDATE, DELETE; WHERE, ORDER BY; GROUP BY, HAVING; JOINs; subqueries; CTEs; window functions; views; indexes; optimisation; database security", "55", "Project 2: SQL sales analysis (30 queries)"],
        ["<b>4 Python</b>", "Fundamentals; variables, lists, dicts; functions; file handling; pandas; NumPy; Matplotlib; cleaning; transformation; API extraction; error handling; automated reporting", "60", "Project 3: cleaning project with tests"],
        ["<b>5 Business Intelligence</b>", "Power BI Desktop; Power Query; data modelling; relationships; DAX; measures vs calculated columns; dashboard design; filters; row-level security; scheduled refresh; Power BI Service; Tableau fundamentals and alternatives", "60", "Project 4: executive dashboard"],
        ["<b>6 Data Engineering</b>", "ETL vs ELT; pipelines; API integration; PostgreSQL; warehouses; star and snowflake schemas; fact and dimension tables; incremental loading; validation; scheduling; failure handling", "55", "Projects 5 and 6: API-to-DB pipeline; automated monthly report"],
        ["<b>7 Predictive Analytics</b>", "Statistics; probability; correlation; regression; classification; time-series; clustering; feature engineering; evaluation; overfitting; data leakage; monitoring; <b>when NOT to predict</b>", "50", "Project 7: forecast vs baseline; then Project 8"],
    ], [0.18, 0.5, 0.07, 0.25])
    f += [P("Hours are guided learning plus practice. Add review and assessment time. Total about 350 hours, or about 9 weeks of full-time "
            "study; the rest of the 13 weeks is projects, gates, revision and client-process training.", "small")]
    f += H2("4.1 Gate rules")
    f += bullets([
        "<b>Written test:</b> pass mark 70%. Open-book for syntax, closed-book for concepts.",
        "<b>Practical task:</b> rubric of 5 lines scored 0–5. Every line must score 3 or more and the average must be 3.5 or more.",
        "<b>Fail:</b> mentor writes a gap list; trainee repeats only the weak topics for up to one week; one retake. A second failure triggers a mentor and Founder review of the plan.",
        "Assessors mark the <b>evidence</b> (files, queries, screenshots) not the explanation given in person.",
    ])
    f += H2("4.2 When predictive analytics is appropriate, and when it is not")
    f += [P("Predictive work is the most over-sold service in analytics. Use this decision table before proposing it:")]
    f += table([
        ["Question", "If yes", "If no"],
        ["Is there a clear decision that a prediction will change? (e.g. order more stock, call a customer)", "Continue", "Do a dashboard or analysis instead"],
        ["Is there enough clean history? (guide: at least 2 full seasonal cycles for forecasting; hundreds of examples of the rare class for classification)", "Continue", "Collect data first; offer descriptive analytics now"],
        ["Can a simple baseline (last year's value, moving average, average rate) be beaten by a clear margin?", "Model is justified", "Use the baseline and say so honestly"],
        ["Will the pattern stay stable? (not disrupted by price changes, new products, one-off events)", "Continue", "Warn the client; add monitoring and shorter horizons"],
        ["Can the client act on it and measure the result?", "Deploy after approval", "Do not deploy; document findings only"],
    ], [0.55, 0.22, 0.23])
    f += callout("Never promise outcomes", "Predictions are probabilistic estimates with error. Every deliverable must show the baseline, the "
                 "error measure on unseen data, the assumptions, and the sentence: \"Forecasts are estimates and not guaranteed outcomes.\"")
    return f


def part5():
    f = [pb()] + H1("Part 5 — The 90-Day Training Schedule")
    f += [P("<b>Rhythm:</b> 13 weeks (65 working days). Each day: <b>09:30–10:00</b> stand-up and plan; <b>10:00–13:00</b> "
            "learn (read, watch, type along); <b>14:00–16:30</b> practice and assignment; <b>16:30–17:30</b> review with buddy, "
            "daily log, commit work to Git. <b>Friday afternoon:</b> mentor review and next-week preview.")]
    f += table([
        ["Wk", "Level and focus", "Key activities", "Deliverable", "Gate"],
        ["1", "Orientation + <b>L1 Data Fundamentals</b>", "Security briefing; workstation; data concepts; keys; data quality; lifecycle; KPIs; governance", "<b>Assignment A1</b> audit report; setup log", "Quiz"],
        ["2", "<b>L2 Excel I</b>", "Entry, formats, sorting, filtering; IF, SUMIF, COUNTIF; XLOOKUP; data validation", "Cleaned sales workbook", "—"],
        ["3", "<b>L2 Excel II</b>", "PivotTables; charts; Power Query; dashboard layout and slicers", "<b>Project 1</b> Excel dashboard", "<b>Gate 1</b> (L1–2)"],
        ["4", "<b>L3 SQL I</b>", "Install PostgreSQL; create tables; SELECT/WHERE/ORDER BY; INSERT/UPDATE/DELETE; GROUP BY/HAVING; JOINs", "15 queries + schema script", "—"],
        ["5", "<b>L3 SQL II</b>", "Subqueries; CTEs; window functions; views; indexes; EXPLAIN; roles and permissions", "<b>Project 2</b> SQL sales analysis (30 queries)", "<b>Gate 2</b> (L3)"],
        ["6", "<b>L4 Python I</b>", "Syntax; lists/dicts; functions; files; errors; VS Code; Git basics", "20 practice scripts in repo", "—"],
        ["7", "<b>L4 Python II</b>", "pandas; NumPy; cleaning; merging; Matplotlib; REST API with requests; reports", "<b>Project 3</b> data-cleaning pipeline + unit tests", "<b>Gate 3</b> (L4)"],
        ["8", "<b>L5 BI I</b>", "Power BI Desktop; Power Query; star-schema model; relationships; first report", "Model + 3-page report", "—"],
        ["9", "<b>L5 BI II</b>", "DAX measures; time intelligence; design and accessibility; RLS; Service; scheduled refresh; Tableau tour", "<b>Project 4</b> executive dashboard (published to test workspace)", "<b>Gate 4</b> (L5)"],
        ["10", "<b>L6 Data Eng I</b>", "ETL vs ELT; warehouse design; fact and dimension tables; PostgreSQL loading; validation rules", "<b>Project 5</b> API-to-database pipeline", "—"],
        ["11", "<b>L6 Data Eng II</b>", "Incremental loads; scheduling; retries; logging; alerts; failure drills", "<b>Project 6</b> automated monthly report", "<b>Gate 5</b> (L6)"],
        ["12", "<b>L7 Predictive</b>", "Statistics; baselines; regression; time-series split; evaluation; leakage; limitations", "Project 7 started: forecast vs baseline", "—"],
        ["13", "Client process + review", "Stages 01–17 walkthrough; mock discovery call; SOW reading; mock handover; portfolio review", "Project 7 report; Project 8 plan approved", "<b>Gate 6</b> panel + probation review"],
    ], [0.05, 0.17, 0.4, 0.24, 0.14])
    f += callout("Days 91–150 (Phase 2)", "Project 7 (forecasting) is finished and Project 8, the full simulated client engagement, is run "
                 "across Stages 01–17 under supervision. A trainee is not scheduled onto a paid client project before Project 8 is accepted, "
                 "except as a shadow.", "ok")
    f += H2("5.1 Daily habits that make the plan work")
    f += bullets([
        "<b>Daily log</b> (template in Annex B): what I did, what I learned, what blocked me, what is next. Five lines, no more.",
        "<b>Commit every day</b>: at least one commit with a clear message. Message format: <font face='Courier'>L3.05: add window-function queries</font> (level.task: message).",
        "<b>Explain it aloud</b>: end each topic by explaining it to your buddy in two minutes. If you cannot, you do not yet understand it.",
        "<b>Break it on purpose</b>: after something works, change an input and see how it fails. This is how errors become familiar, not frightening.",
    ])
    f += H2("5.2 Weekly review (Friday, 45 minutes, mentor)")
    f += numbered([
        "Show the week's deliverable running, not slides.",
        "Mentor asks three \"why\" questions about the work; answers are noted.",
        "Defects found are logged with owner and fix date.",
        "Plan for next week confirmed; any slippage handled by cutting scope, not by skipping tests.",
    ])
    return f


def part6():
    f = [pb()] + H1("Part 6 — Technology Stack and Selection")
    f += H2("6.1 The stack VEL teaches")
    f += table([
        ["Category", "Tools", "Used for"],
        ["Spreadsheet", "Microsoft Excel, Google Sheets", "Small clients, prototypes, requirement workshops, Level 2"],
        ["BI", "Power BI, Tableau, Looker Studio", "Dashboards and reports; Power BI is the primary tool"],
        ["Programming", "Python", "Cleaning, API extraction, automation, modelling"],
        ["Analysis", "pandas, NumPy", "Tabular data processing, numerical work"],
        ["Visualisation", "Matplotlib, Plotly", "Static charts for reports, interactive charts"],
        ["Database", "PostgreSQL, MySQL", "Operational stores, staging and small warehouses"],
        ["Warehouse", "BigQuery, Snowflake, Redshift", "Larger volume and many users; usage-based cost"],
        ["ETL / transformation", "dbt, Airflow, Power Query", "SQL transformations; scheduled pipelines; light self-service shaping"],
        ["APIs", "REST APIs", "Pulling data from SaaS, ERP and CRM systems"],
        ["Version control", "Git, GitHub", "History, review, collaboration (private repositories)"],
        ["Development", "VS Code", "Editor for Python, SQL, Markdown"],
        ["Cloud", "AWS, Azure, GCP", "Hosting databases, storage, schedulers"],
        ["Machine learning", "scikit-learn", "Classical models: regression, classification, clustering"],
        ["Documentation", "Markdown, Draw.io", "Data dictionary, architecture and lineage diagrams"],
    ], [0.18, 0.32, 0.5])
    f += H2("6.2 How to choose tools for a client")
    f += [P("Choose by evidence, in this order, and write the reasons into the architecture document (Stage 05):")]
    f += table([
        ["Factor", "Question to ask the client", "Effect on choice"],
        ["Existing ecosystem", "Do they already use Microsoft 365, Google Workspace, or neither?", "Microsoft 365 users: Power BI is natural. Google-centric: Looker Studio is strong and cheap."],
        ["Budget", "What can they spend per month on licences and cloud?", "Low budget: Excel + Looker Studio + PostgreSQL. Verify licence prices on the day you quote."],
        ["Data volume", "Rows per table now and in 2 years? Refresh frequency?", "Under a few million rows: PostgreSQL is enough. Tens of millions and growing: consider a cloud warehouse."],
        ["Users", "How many viewers and creators? Mobile? External sharing?", "Per-user licences scale with viewers; check Power BI licensing rules for sharing and capacity."],
        ["Licensing", "Is any tool restricted by their policy or by data-residency law?", "Record licence terms; open-source preferred where equal."],
        ["Maintenance", "Who looks after it after handover? Skills they have?", "Prefer fewer moving parts; a simple stack the client can own beats a clever one only VEL can run."],
        ["Security", "Sensitive data? Required hosting region? Audit needs?", "May force on-premises or a specific cloud region, and row-level security."],
    ], [0.17, 0.38, 0.45])
    f += callout("Verify before quoting", "Vendor prices, licence tiers and free-tier limits change often. Before any proposal, open the vendor's "
                 "official pricing page, record the date, URL and the plan name in the cost sheet, and attach a screenshot. Never copy a price from "
                 "memory, a blog or this handbook.")
    f += H2("6.3 Reference architecture (default)")
    f += flow(["Sources\nCSV, Excel, DB,\nAPI, ERP, CRM", "Ingest\nPython / ETL", "Raw layer\nunchanged copy", "Clean layer\nvalidated",
               "Model layer\nfacts + dims", "BI layer\nmeasures, RLS", "Users\ndashboards"], box_h=40, fs=7)
    f += [P("Each arrow is a logged, repeatable step. <b>Raw data is never edited</b>; fixes are made in the clean layer so you can always trace "
            "a dashboard number back to the original record (<i>data lineage</i>).", "small")]
    return f


def part7():
    f = [pb()] + H1("Part 7 — The 17-Stage Client Project Lifecycle")
    f += [P("Every client project passes through these stages in order. Each stage ends with a named output and a sign-off. "
            "Full templates and step-by-step lessons are released in Modules 9–12; this is the map.")]
    f += flow(["01-03\nQualify, discover,\nscope freeze", "04-06\nAudit, architect,\nestimate", "07-11\nSetup, collect,\nclean, model",
               "12-14\nDashboards, predict,\nQA", "15-17\nDeploy, hand over,\nmaintain"], box_h=40, fs=7.4)
    f += table([
        ["Stage", "Name", "Main output", "Typical owner", "Sign-off"],
        ["01", "Lead qualification", "Lead qualification report (industry, problem, data, users, budget, decision-maker)", "BA / Founder", "Founder"],
        ["02", "Client discovery", "Discovery report: sources, ERP/CRM, processes, KPIs, data owners, quality and security issues", "BA", "Head"],
        ["03", "Requirements and scope freeze", "Approved PRD and Statement of Work with deliverables, exclusions, timeline, acceptance criteria", "BA + PM", "Client + Founder"],
        ["04", "Data audit", "Data quality assessment (completeness, duplicates, formats, outliers, definitions, freshness, access)", "Analyst / Eng.", "Head"],
        ["05", "Analytics architecture", "Architecture and data-flow diagrams; tool selection rationale", "Head", "Founder"],
        ["06", "Cost estimation", "One-time and recurring cost model with sourced prices and assumptions", "Head + PM", "Founder"],
        ["07", "Development environment", "Configured tools, cloud workspace, access", "Eng.", "Head"],
        ["08", "Repository structure", "Standard repo (Part 8), retention and access rules", "Eng.", "Head"],
        ["09", "Collection and integration", "Working connectors with authentication, pagination, retries, rate-limit handling, logging", "Eng.", "Head"],
        ["10", "Cleaning and transformation", "Cleaned data with traceability to originals; rule log", "Eng. / Analyst", "QA"],
        ["11", "Data modelling", "Fact and dimension tables, measures, KPI definitions, dictionary; client validates calculations", "Eng. / BI", "Client"],
        ["12", "Dashboard development", "Dashboards with filters, drill-down, comparisons, export, user-specific access", "BI Dev", "QA + Client"],
        ["13", "Predictive analytics (if justified)", "Objective, baseline, evaluation, limitations; deploy only after approval", "Data Sci.", "Head + Client"],
        ["14", "QA and validation", "Test evidence; reconciliation to trusted source records", "QA", "Head"],
        ["15", "Deployment", "Production database, pipelines, published dashboards, refresh, monitoring, backup", "Eng. + BI", "Client + Founder"],
        ["16", "Demonstration and handover", "User guide, refresh and access guides, training, final acceptance record", "PM + BA", "Client"],
        ["17", "Ongoing maintenance", "Monthly checks, updates, monitoring, reporting, support", "Managed team", "Monthly report"],
    ], [0.07, 0.19, 0.4, 0.17, 0.17])
    f += callout("Hard stops", "No Stage 05 before Stage 04 is complete. No Stage 15 without written production approval. "
                 "No client dataset leaves the approved environment at any stage. No dashboard is published until key metrics are reconciled (Stage 14).")
    return f
