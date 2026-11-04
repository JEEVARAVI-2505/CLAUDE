"""Front matter: how to use the handbook, golden rules, service portfolio."""
from lib import *  # noqa: F401,F403


def how_to_use() -> str:
    return join(
        h1("How to Use This Handbook",
           "This document is the single operating manual for the Software Testing & QA Automation Department. "
           "It does two jobs: **(1)** it trains a beginner from zero to a professional who can join supervised "
           "client projects, and **(2)** it defines the mandatory 16-stage QA lifecycle every client project "
           "follows, with procedures, templates, evidence requirements and approval gates.",
           numbered=False, anchor="how-to-use"),
        callout("gold", "Golden rules", [
            "Do not skip stages. Do not start a stage until the previous stage's approval gate is signed.",
            "**Evidence or it did not happen.** Every pass, fail, blocked or skipped result needs reproducible "
            "evidence (screenshot, log, HAR file, report or recording) stored in the project repository.",
            "Test only systems you are **authorised in writing** to test. Never test production or third-party "
            "systems without the client's written approval.",
            "Security testing is limited to authorised assessment activity and is coordinated with qualified "
            "security personnel. Testers report suspected vulnerabilities; they do not exploit them.",
            "Never commit passwords, API keys, tokens, customer personal data or production database extracts.",
            "Never manipulate, round up, or selectively report metrics to make a project look successful. "
            "Report unresolved risk honestly.",
            "Never claim software is “bug-free”, “secure” or “guaranteed”. Testing reduces risk; it cannot prove "
            "the absence of defects.",
            "Every release recommendation needs a second reviewer (QA Lead) before it reaches the client.",
        ]),
        h3("Reading path for a beginner"),
        pills(["Part A: Learn (Levels 1–6)", "Part B: Tools", "Part C: Client lifecycle (16 stages)",
               "Part D: Repository, documents, metrics", "Part E: Costing & roles", "Part F: Projects, plan, assessment"]),
        h3("The teaching method used for every topic"),
        p("Each learning topic is delivered in the same ten parts so that nothing is left to chance. "
          "Trainers must not mark a topic complete until all ten parts are done."),
        table(["#", "Part", "What the trainee must receive or produce"], [
            ["1", "Definition", "A one- or two-sentence plain-language definition, plus the formal term (ISTQB glossary where one exists)."],
            ["2", "Purpose", "Why the topic matters to a client and what risk it reduces."],
            ["3", "Setup", "Tools, accounts, test data and environment needed before starting."],
            ["4", "Procedure", "Numbered steps the trainee follows exactly."],
            ["5", "Expected output", "What the finished work must look like (file, report, screenshot, command output)."],
            ["6", "Verification", "How the trainee proves the work is correct (self-check list or peer check)."],
            ["7", "Errors", "The most common mistakes and how to recognise and fix them."],
            ["8", "Reporting", "How and where the result is recorded (template, repo path, defect tracker)."],
            ["9", "Assignment", "A practical task, time-boxed, with deliverables."],
            ["10", "Assessment", "Pass criteria: a quiz, a demonstration, or a reviewed deliverable."],
        ], widths=["6%", "18%", "76%"]),
        "<!--TOC-->",
        h2("Document control"),
        table(["Item", "Value"], [
            ["Document", "VEL-QA-001 · Software Testing & QA Automation Department Handbook"],
            ["Version / status", "1.0 · draft for management approval"],
            ["Owner", "Head of QA (to be assigned by management)"],
            ["Approver", "Managing Director / Delivery Director"],
            ["Review cycle", "Every 6 months, and whenever a tool, licence or standard referenced here changes materially"],
            ["Companion files", "90-Day Training Plan (VEL-QA-002) · Templates Pack (VEL-QA-003) · Practical Workbook (VEL-QA-004) · Assessment Framework (VEL-QA-005) · `qa-project-template/` repository skeleton and practice targets"],
        ], widths=["22%", "78%"], first_bold=True),
        callout("info", "Verify before you rely",
                "Tool versions, licences, free-tier limits and prices change. Version numbers and licences quoted in "
                "this handbook were checked against the npm and PyPI registries on 4 November 2026 where marked "
                "“registry-verified”; everything else must be re-verified on the vendor's official page before use "
                "or before quoting a client."),
        end_chapter(),
    )


def portfolio() -> str:
    rows = [
        ["Manual software testing", "Risk-based test design, scripted and exploratory execution, evidence capture, defect reporting.", "“We release and hope.” / “Customers find our bugs.”", "Manual QA, Junior QA"],
        ["Web application testing", "Functional, usability, session, form and navigation testing across supported browsers and viewports.", "“The site breaks on some phones/browsers.”", "Manual QA, Automation QA"],
        ["Mobile application testing", "Android/iOS functional testing on real devices and emulators; OS-version, interruption and network-condition testing.", "“The app crashes on certain devices.”", "Manual QA, Automation QA"],
        ["Desktop application testing", "Installer, upgrade, UI, file-handling and OS-compatibility testing for Windows/macOS/Linux applications.", "“Our installer fails on some PCs.”", "Manual QA"],
        ["API testing", "Request/response, status-code, authentication, schema and negative testing; contract checks.", "“Integrations fail in production.”", "API Tester"],
        ["Database testing", "Data integrity, migration, constraints, transactions and report-query validation on test data only.", "“Numbers in reports do not match.”", "Manual QA, API Tester"],
        ["Functional testing", "Verification of each requirement and user story against acceptance criteria.", "“Does it do what we specified?”", "Manual QA"],
        ["Regression testing", "Repeatable suites (manual + automated) proving existing behaviour survives change.", "“Every release breaks something old.”", "Manual QA, Automation QA"],
        ["Integration testing", "Data exchange, interface contracts, error handling and dependency behaviour between modules/systems.", "“It works alone but not together.”", "API Tester, Automation QA"],
        ["End-to-end testing", "Business-critical journeys across UI, API, data and third-party services.", "“Checkout/booking/payroll must never fail.”", "Automation QA"],
        ["Cross-browser testing", "Chromium, Firefox, WebKit and agreed real-browser/OS matrix.", "“Looks fine on my machine.”", "Manual QA, Automation QA"],
        ["Performance testing", "Workload modelling, baseline, load, stress, soak and spike tests with agreed thresholds in controlled environments.", "“Will it survive launch day?”", "Performance Tester"],
        ["Load and stress testing", "Finds capacity limits and failure modes; reports throughput, latency percentiles, error rate and saturation.", "“How many users can we handle?”", "Performance Tester"],
        ["Accessibility testing", "WCAG-based automated scans plus manual keyboard and screen-reader checks.", "“We must meet accessibility requirements.”", "Manual QA, Automation QA"],
        ["Test automation", "Playwright/Selenium/Cypress UI suites, pytest/Postman API suites, CI integration, reporting.", "“Regression takes a week by hand.”", "Automation QA"],
        ["Release validation", "Entry/exit criteria checks, QA sign-off, risk summary, production smoke test.", "“Can we ship on Friday?”", "QA Lead, Release Coordinator"],
        ["QA consulting", "Process assessment, test strategy, tool selection, team coaching, metrics design.", "“We have no QA process.”", "QA Lead, Test Manager"],
    ]
    return join(
        h1("Company Service Portfolio",
           "Seventeen commercially deliverable services. Each can be sold alone or combined into a project, a fixed-scope "
           "package, a time-based engagement or a monthly retainer (see Costing and Pricing)."),
        table(["Service", "What we deliver", "Typical client trigger", "Primary roles"], rows,
              widths=["19%", "42%", "24%", "15%"], first_bold=True),
        callout("danger", "Security testing boundary",
                "We perform **authorised assessment activity only** and coordinate it with qualified security "
                "personnel (client security team or a certified penetration-testing partner). Testers may run "
                "agreed, non-destructive checks (for example verifying that an API rejects a missing token, or "
                "that error pages do not leak stack traces) and must escalate anything that looks like a "
                "vulnerability. We do not run exploit tools, brute-force, denial-of-service or scanning against "
                "systems without a signed scope, written authorisation and a named security coordinator."),
        callout("gold", "Wording we use with clients",
                "“Quality assurance support”, “evidence-based release recommendation”, “risk summary”. "
                "We never certify a product as defect-free, compliant or secure, and we never guarantee "
                "that a release will have no production incidents. Certification and compliance attestation "
                "are performed by authorised auditors."),
        h2("Service families and how they combine"),
        table(["Family", "Services", "Typical package"], [
            ["Core functional QA", "Manual, web, mobile, desktop, functional, regression", "Project-based or fixed-scope; 2–8 weeks"],
            ["Technical QA", "API, database, integration, end-to-end", "Fixed-scope package alongside core QA"],
            ["Automation engineering", "Test automation, cross-browser, CI integration", "Project + maintenance retainer"],
            ["Non-functional QA", "Performance, load/stress, accessibility", "Time-boxed engagement with a defined workload model"],
            ["Governance & advisory", "Release validation, QA consulting", "Retainer or day-rate"],
        ], widths=["24%", "44%", "32%"], first_bold=True),
        end_chapter(),
    )
