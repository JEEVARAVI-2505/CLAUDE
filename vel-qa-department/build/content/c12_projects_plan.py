"""Practical employee projects (1–8) and the 90-day training plan."""
from lib import *  # noqa: F401,F403

PROJECTS = [
    dict(n=1, name="Test a business website manually", days="5 days", level="Level 3",
         target="The company's own website, or a client site **with written authorisation**, or a public practice site. No authorisation, no testing.",
         tasks=["Write a one-page scope and test approach (what is in/out, browsers, devices, data).",
                "Build a sitemap; list user journeys and risks.",
                "Design and execute at least **40 test cases**: navigation, links, forms, content, responsive layout (360/768/1280 px), cross-browser (two browsers), first-line accessibility.",
                "Log every defect with evidence; rate severity and priority.",
                "Write the test execution report with an honest risk statement."],
         deliverables="`docs/test-plan.md` (lite), `test-cases/functional/website.csv`, `evidence/`, `defects/`, `docs/test-execution-report.md`.",
         accept="Every case executed with evidence; ≥ 5 reproducible defects or documented zero-defect reasoning; counts reconcile; report states what was **not** tested."),
    dict(n=2, name="Prepare a complete test case suite", days="5 days", level="Level 2",
         target="VEL Practice Shop, shop/cart/session/logout modules (REQ-010 to REQ-014) plus registration regression.",
         tasks=["Create the requirements traceability matrix.",
                "Apply EP, BVA, decision table, state transition and error guessing; name the technique on each case.",
                "Write at least **60 test cases** across functional, negative, boundary, session and accessibility types, each linked to a requirement.",
                "Tag cases: smoke, regression, UAT, automation candidate.",
                "Arrange a four-eyes review with a colleague; record and resolve every comment."],
         deliverables="`test-cases/functional/shop.csv`, `docs/requirements-traceability.md`, review log.",
         accept="100 % requirement coverage; no duplicate cases; ≥ 25 % of cases are negative or boundary; review comments closed."),
    dict(n=3, name="Test a REST API", days="5 days", level="Level 4",
         target="VEL Practice API (`qa-project-template/practice-api`) running locally.",
         tasks=["Read the contract and build a Postman collection with ≥ **40 requests**.",
                "Cover positive, negative, authentication, boundary, schema and method tests for every endpoint.",
                "Add assertions on status, headers, body, schema and response time.",
                "Raise a defect for each deviation, with request/response evidence (tokens removed).",
                "Hand over any access-control observation as a **security escalation** note, not a public defect."],
         deliverables="`automation/api/postman/collection.json`, defects, `docs/api-test-report.md`.",
         accept="≥ 8 correct defects; no false defects against a documented behaviour; all endpoints covered; escalation note present."),
    dict(n=4, name="Automate browser tests with Playwright", days="5 days", level="Level 5",
         target="VEL Practice Shop UI.",
         tasks=["Set up Playwright with pytest; configure base URL through an environment variable.",
                "Create page objects for Register, Login and Shop.",
                "Automate ≥ **15 tests** including parametrised boundary tests; mark known defects `xfail(strict=True)` with the defect ID.",
                "Run on Chromium and Firefox; enable trace on first retry/failure.",
                "Run each test 5 times; fix or remove any flaky test; document how to run."],
         deliverables="`automation/ui/`, `automation/page-objects/`, `automation/fixtures/`, HTML/JUnit report.",
         accept="Suite green on 5 consecutive runs; no sleeps; no secrets; code review passed."),
    dict(n=5, name="Automate API tests", days="4 days", level="Level 5",
         target="VEL Practice API.",
         tasks=["Create session fixtures for base URL and token (credentials from environment variables).",
                "Write ≥ **15 pytest tests**, including JSON-schema validation of products.",
                "Mark seeded defects as strict `xfail` with defect IDs.",
                "Generate JUnit XML; run in CI.",
                "Report automation coverage against the Level-4 manual cases."],
         deliverables="`automation/api/`, `automation/api/schemas/`, CI run link, coverage note.",
         accept="Independent tests; zero shared state; schema checks present; CI green."),
    dict(n=6, name="Create a regression suite", days="3 days", level="Level 2 + 5",
         target="Practice Shop build 1.0 vs build 1.1 (`?build=1.1`).",
         tasks=["Classify tests into tiers: **smoke** (≤ 15 min), **regression** (nightly), **full** (weekly).",
                "Write selection rules and a change-impact table.",
                "Run the suite on build 1.0 (baseline) and build 1.1.",
                "Compare results; identify fixed defects, **new regressions**, and obsolete tests.",
                "Report with a go/no-go style recommendation for build 1.1."],
         deliverables="`test-cases/regression/`, `docs/regression-report.md`, updated RTM.",
         accept="At least one regression found and explained; every skipped test justified; recommendation supported by evidence."),
    dict(n=7, name="Perform a controlled performance test", days="4 days", level="Level 6",
         target="VEL Practice API on **localhost only** (never a shared or third-party system).",
         tasks=["Write a one-page performance test plan: objectives, workload model (use the worked formula), thresholds (e.g. p95 < 500 ms, errors < 1 %), environment, data, stop conditions.",
                "Script `GET /api/products` and `GET /api/slow?delay=200` in k6, JMeter or Locust; validate with one user.",
                "Run baseline (1 VU), then load at 10, 25 and 50 VUs with ramp-up, steady state and ramp-down.",
                "Record client metrics and the server's CPU/memory (e.g. `top`).",
                "Report p50/p95/p99, throughput, errors, bottleneck notes and **limitations** (local machine, synthetic data)."],
         deliverables="`performance/` scripts, results summary, `docs/performance-report.md`.",
         accept="Plan approved before running; steady-state only is measured; limitations stated; no claim beyond measured data."),
    dict(n=8, name="Prepare a complete client QA report", days="5 days", level="All levels",
         target="Run the full lifecycle on the Practice Shop + Practice API as if it were a paying client.",
         tasks=["Produce strategy, plan, RTM, test cases, execution report, defect summary, automation coverage report, performance and accessibility summaries.",
                "Compute all metrics with the formulas in this handbook.",
                "Write the release QA report with the recommendation (Go / Conditional Go / No-Go) and the unresolved-risk summary.",
                "Complete the QA sign-off checklist; the QA Lead acts as second reviewer.",
                "Present for 20 minutes to a mock client panel."],
         deliverables="Complete `qa-project/` repository, `docs/release-report.md`, `docs/qa-signoff.md`, presentation.",
         accept="Numbers reconcile with source data; recommendation is consistent with evidence; risks are plain; panel questions answered honestly."),
]


def _project(p: dict) -> str:
    return join(
        h2(f"Project {p['n']} — {p['name']}"),
        kv([["Level / time-box", f"{p['level']} · {p['days']}"], ["Target system", p["target"]],
            ["Tasks", ("ol", p["tasks"])], ["Deliverables (repository paths)", p["deliverables"]],
            ["Acceptance criteria", p["accept"]],
            ["Marking (100 points)", "Planning and approach 15 · Technical quality 30 · Evidence and traceability 20 · Reporting honesty 20 · Professional conduct and timeliness 15. **Pass: 70.**"]]),
    )


def projects() -> str:
    return join(
        h1("Practical Employee Projects",
           "Eight projects take the trainee from manual testing to a complete client QA report. Each project is marked on the same "
           "rubric and must be reviewed by a person other than its author. Targets are the local practice systems or an explicitly "
           "authorised site."),
        table(["#", "Project", "Level", "Time", "Primary deliverable"], [
            [str(p["n"]), p["name"], p["level"], p["days"], p["deliverables"].split(",")[0]] for p in PROJECTS
        ], widths=["5%", "38%", "13%", "10%", "34%"]),
        callout("danger", "Authorisation first",
                "Only test the Practice Shop and Practice API (running on your own machine) or a site whose owner has authorised testing in writing. "
                "Never run automation, load or scanning tools against public websites, third-party APIs or shared systems without that authorisation."),
        *[_project(p) for p in PROJECTS],
        h2("Project sign-off sheet"),
        table(["Project", "Date", "Score /100", "Reviewer", "Pass (≥ 70)", "Reviewer comments"],
              [[f"{p['n']}. {p['name']}", "", "", "", "☐", ""] for p in PROJECTS], widths=["30%", "9%", "9%", "13%", "9%", "30%"]),
        end_chapter(),
    )


WEEKS = [
    ("1", "Onboarding · Level 1a", ["Day 1 agenda: policies, accounts, buddy, Golden Rules", "Workstation set-up; run Practice Shop and API", "SDLC, STLC, QA vs QC, verification vs validation", "Errors, defects, failures; seven testing principles", "Read the A0 specification; write questions and the traceability skeleton"],
     "Set-up evidence; learning journal; questions list", "Buddy check Friday"),
    ("2", "Level 1b", ["Test cases and scenarios", "Test plans and test data", "Bug lifecycle; severity and priority", "Write first 10 test cases and the first defect report", "Level 1 quiz and exit-gate review"],
     "10 reviewed cases; 1 defect report; quiz", "**Level 1 gate** (QA Lead)"),
    ("3", "Level 2a", ["Requirement analysis and testability checklist", "Equivalence partitioning", "Boundary-value analysis", "Decision tables, state transitions, error guessing", "A0 test design: 30+ cases and RTM"],
     "A0 suite and RTM", "Peer review"),
    ("4", "Level 2b", ["A0 execution on Chrome and Firefox", "A0 execution at 360 px; evidence and defects", "Retest and A0 report", "Exploratory session; smoke, sanity, regression, UAT concepts", "A0 review presentation to QA Lead"],
     "A0 report; defect log; exploratory sheet", "**Milestone 1 (~Day 30): A0 ≥ 70; Level 2 quiz**"),
    ("5", "Level 3a", ["Browser matrix; developer tools", "Responsive testing", "Form testing", "Navigation and session testing", "Start Project 1 (manual website test)"],
     "Browser matrix; session checklist", "Buddy check"),
    ("6", "Level 3b", ["Mobile testing approaches; emulation vs real devices", "OS compatibility; accessibility first-line checks", "Project 1 execution", "Project 1 defects and report", "Level 3 gate review"],
     "Project 1 package", "**Level 3 gate**; Project 1 review"),
    ("7", "Project 2 · Level 4a", ["Project 2: traceability and design", "Project 2: 60+ cases", "Project 2: review and fixes", "HTTP methods, status codes, curl", "Authentication, JSON, Postman basics"],
     "Project 2 suite; Postman collection start", "Project 2 review"),
    ("8", "Level 4b", ["Schema validation and negative API tests", "Project 3: collection build", "Project 3: defects and escalation note", "Project 3 report", "Level 4 gate; Milestone 2 review"],
     "Project 3 package", "**Level 4 gate; Milestone 2 (~Day 56)**"),
    ("9", "Level 5a", ["Python fundamentals 1", "Python fundamentals 2; files and JSON", "pytest basics; running and reading reports", "Fixtures and parametrisation", "Start Project 5 (API automation)"],
     "Python exercises; first API tests", "Code review"),
    ("10", "Level 5b", ["Project 5 completion and schema checks", "Playwright basics, locators, auto-wait", "Page Object Model", "Project 4: automate 15 UI tests", "Cross-browser run; traces; flakiness check"],
     "Projects 4 and 5 code", "Code review (independent)"),
    ("11", "Level 5c", ["GitHub Actions CI", "Project 6: regression tiers and selection rules", "Project 6: run on build 1.0 and 1.1", "Project 6 report", "Level 5 gate"],
     "CI link; regression report", "**Level 5 gate**"),
    ("12", "Level 6", ["Performance concepts; workload model; plan for Project 7", "k6 / JMeter / Locust scripts; validation", "Project 7 runs and analysis", "Accessibility automation; database validation", "Flaky management; security coordination; coverage"],
     "Performance report; a11y scan; SQL checks", "**Level 6 gate**"),
    ("13", "Capstone · Assessment", ["Project 8: assemble repository and documents", "Project 8: metrics and recommendation", "Project 8: sign-off checklist and rehearsal", "Final assessment: written paper + practical", "Presentation to management; role placement decision"],
     "Complete client QA report; assessment results", "**Milestone 3 (Day 90): final review**"),
]


def training_plan() -> str:
    rows = []
    for wk, focus, days, deliv, gate in WEEKS:
        rows.append([f"**Week {wk}**\n{focus}", "\n".join(f"**{d}** {t}" for d, t in zip(["Mon", "Tue", "Wed", "Thu", "Fri"], days)), deliv, gate])
    return join(
        h1("90-Day Employee Training Plan",
           "Thirteen weeks (65 working days, about 90 calendar days) take a beginner from zero to a supervised, productive QA professional. "
           "Each week has daily focus areas, a required deliverable and a review gate. Use it with the Practical Workbook."),
        table(["Milestone", "When", "Pass requirements", "Decision"], [
            ["**M1 — Foundations**", "End of week 4 (~Day 30)", "Level 1 and Level 2 gates passed; A0 ≥ 70; one exploratory session reported.", "Continue / extend by one week with coaching"],
            ["**M2 — Technical testing**", "End of week 8 (~Day 56)", "Level 3 and 4 gates; Projects 1–3 ≥ 70; can write a defect that a developer reproduces first time.", "Continue / re-test weak areas"],
            ["**M3 — Automation & advanced QA**", "Week 13 (Day 90)", "Level 5 and 6 gates; Projects 4–8 ≥ 70; final assessment ≥ 75.", "Place as Junior QA, Manual QA, or specialist track"],
        ], widths=["25%", "20%", "40%", "15%"]),
        h2("Weekly schedule"),
        table(["Week and level", "Daily focus", "Deliverable", "Review gate"], rows, widths=["16%", "46%", "20%", "18%"]),
        h2("Weekly rhythm"),
        table(["When", "Activity", "Output"], [
            ["Every day 09:45", "Stand-up with buddy (10 min): yesterday, today, blockers.", "Blockers resolved or escalated."],
            ["Every day 17:30", "Learning-journal entry: what I did, learned, what I do not understand.", "`journal/day-nn.md` committed."],
            ["Every Friday", "Review with buddy (30 min): deliverable demo, feedback, next week.", "Feedback recorded in the trainee file."],
            ["End of each level", "Gate review with the QA Lead.", "Signed gate record."],
            ["Every 2 weeks", "One-to-one with the Test Manager about progress and wellbeing.", "Notes; adjustments to plan."],
        ], widths=["20%", "55%", "25%"], first_bold=True),
        h2("If the trainee falls behind"),
        ol(["Identify the gap with the buddy within two working days; check understanding, not just output.",
            "Add a focused coaching block (maximum 5 working days) with a specific exercise and a re-test.",
            "If the gap remains after the re-test, the QA Lead and Test Manager agree a revised plan in writing.",
            "Never lower an assessment pass mark to keep a schedule."]),
        h2("Final evidence checklist (Day 90)"),
        checklist(["Learning repository with 90 daily journal entries.", "All level-gate records signed.",
                   "Projects 1–8 with scores ≥ 70 and reviewer names.", "Capstone repository complete and reproducible.",
                   "Final assessment papers and practical marksheets filed.", "Management decision and development plan recorded."]),
        end_chapter(),
    )
