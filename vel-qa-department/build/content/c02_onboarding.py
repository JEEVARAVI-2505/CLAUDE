"""Beginner onboarding and the first manual-testing assignment (Assignment A0)."""
from lib import *  # noqa: F401,F403

# Requirements of the VEL Practice Shop. Must stay in sync with qa-project-template/practice-app/SPEC.md.
REQS = [
    ["REQ-001", "Registration: **Full name** is required, 2–40 characters after trimming spaces."],
    ["REQ-002", "Registration: **Email** is required and must be a valid address (`name@domain.tld`)."],
    ["REQ-003", "Registration: **Age** is required, a whole number from **18 to 60 inclusive**."],
    ["REQ-004", "Registration: **Password** 8–20 characters with at least one digit and at least one uppercase letter."],
    ["REQ-005", "Registration: **Confirm password** must match the password exactly (case-sensitive)."],
    ["REQ-006", "Registration: the **Terms** checkbox must be ticked."],
    ["REQ-007", "A successful registration shows “Registration successful” and opens the login view."],
    ["REQ-008", "Login with registered credentials opens the shop. Any failure shows the generic message “Invalid email or password”."],
    ["REQ-009", "On the **3rd consecutive failed login** the account locks for 30 seconds and the message “Account locked. Try again in 30 seconds.” is shown; attempts during the lock show the same message."],
    ["REQ-010", "Shop: quantity per item is a whole number **1–10**. Cart total = sum(price × quantity) shown to 2 decimals; GST 18 % shown to 2 decimals (standard rounding)."],
    ["REQ-011", "Session expires after **90 seconds of inactivity**: “Session expired. Please log in again.” and the login view opens."],
    ["REQ-012", "Logout ends the session. Using the browser Back button afterwards must **not** show shop content."],
    ["REQ-013", "The application is usable at **360 px** viewport width with no horizontal scrolling."],
    ["REQ-014", "Accessibility: every input has a programmatic label; every image has alt text; all functions work by keyboard with a visible focus indicator."],
]


def onboarding() -> str:
    return join(
        h1("Beginner Onboarding",
           "Day-one programme for a new QA trainee. By the end of week one the trainee has a working "
           "machine, a learning repository, has run the practice targets, and has met every working agreement."),
        h2("Day 1 agenda"),
        table(["Time", "Activity", "Output / evidence"], [
            ["09:30", "Welcome, department purpose, services, who is who (QA Lead, Test Manager, Release Coordinator)", "Trainee knows their buddy and escalation path"],
            ["10:30", "Read and sign: confidentiality, acceptable use, test-data and evidence-integrity policy", "Signed policy acknowledgements filed by HR"],
            ["11:30", "Accounts: company e-mail, GitHub organisation invite, issue tracker, test-management tool", "Access checklist ticked (see below)"],
            ["13:30", "Workstation setup (table below)", "Screenshot of every verification command"],
            ["15:00", "Clone the learning repository; run the VEL Practice Shop and Practice API locally", "Two screenshots: app in browser, `GET /api/health` response"],
            ["16:30", "Read the Golden Rules and the lifecycle overview; first 1-to-1 with the buddy", "Three questions written in the learning journal"],
        ], widths=["10%", "55%", "35%"]),
        h2("Access checklist"),
        checklist([
            "Company e-mail and calendar working; two-factor authentication enabled.",
            "GitHub account added to the company organisation with the **QA-Trainee** team role (read on client repos, write only to the learning repository).",
            "Issue tracker (Jira or equivalent) account; can open the **QA-TRAINING** project.",
            "Test-management tool (TestRail, Zephyr, Xray or Kiwi TCMS) account in the training project.",
            "Password manager installed. **No** shared passwords, ever.",
            "Access to client systems: **none** until the Test Manager issues written authorisation naming the person, the system and the dates.",
        ]),
        h2("Workstation setup and verification"),
        p("Install each tool from the official site, then run the verification command. Paste the output into "
          "`journal/day-01-setup.md`. Exact version numbers will differ; the major version must satisfy the “Expected” column."),
        table(["Tool", "Verification command", "Expected output", "Used for"], [
            ["Git", "`git --version`", "`git version 2.x`", "Version control for all QA assets"],
            ["Python", "`python3 --version`", "`Python 3.11` or newer", "pytest, API automation, utilities"],
            ["Node.js (LTS)", "`node --version`", "`v20.x` or newer LTS", "Playwright, Newman, Lighthouse"],
            ["VS Code (or equivalent)", "`code --version`", "A version number", "Editing Markdown, code, YAML"],
            ["Chrome / Edge + Firefox", "Open `about:version` / `about:support`", "Browser version page", "Manual and cross-browser testing"],
            ["Browser developer tools", "Press `F12`, open **Network** tab", "Requests listed when you reload a page", "Evidence, API inspection"],
        ], widths=["20%", "27%", "29%", "24%"], first_bold=True),
        code("""# From the qa-project-template/ folder of your learning repository
python3 -m http.server 8000 --directory practice-app    # terminal 1
python3 practice-api/server.py --port 8080               # terminal 2

# Verify (terminal 3)
curl -s http://localhost:8080/api/health
# Expected: {"status":"ok","service":"vel-practice-api"}
# Open http://localhost:8000 in the browser: the VEL Practice Shop login view appears.""", "bash"),
        h2("Working agreements"),
        table(["Topic", "Rule", "Why"], [
            ["Evidence integrity", "Never edit a screenshot, log or recording. If you must redact personal data, keep the original in the restricted evidence store and note the redaction.", "Evidence that cannot be trusted is worthless to the client."],
            ["Honest reporting", "Report what happened, including your own mistakes and tests you could not finish.", "A hidden gap is a production risk."],
            ["Confidentiality", "No client data in personal e-mail, chat tools, public AI services or public repositories.", "Contractual and legal exposure."],
            ["Test data", "Use synthetic data only. Production extracts and real personal data are forbidden.", "Privacy law and client contract."],
            ["Authorisation", "Test only what is written in the scope. Out-of-scope observations are reported, not explored.", "Unauthorised testing can cause outages and legal harm."],
            ["Asking for help", "Spend a maximum of 30 minutes stuck, then ask your buddy. Write down what you tried.", "Speed with learning."],
            ["Daily rhythm", "09:45 stand-up, 17:30 learning-journal entry (what I did, what I learned, what blocks me).", "Visible progress and early warning."],
        ], widths=["18%", "52%", "30%"], first_bold=True),
        h2("Naming conventions used from day one"),
        table(["Item", "Pattern", "Example"], [
            ["Requirement", "`REQ-<nnn>`", "`REQ-003`"],
            ["Test scenario", "`TS-<area>-<nn>`", "`TS-REG-01`"],
            ["Test case", "`TC-<area>-<nnn>`", "`TC-REG-004`"],
            ["Defect", "`DEF-<nnn>`", "`DEF-017`"],
            ["Evidence file", "`<yyyy-mm-dd>_<TC-id>_<PASS|FAIL|BLOCKED>_<n>.<ext>`", "`2026-11-06_TC-REG-004_FAIL_1.png`"],
            ["Git branch", "`qa/<initials>/<topic>`", "`qa/ab/registration-tests`"],
            ["Commit message", "`<type>(<scope>): <summary>`", "`test(registration): add boundary cases for REQ-003`"],
        ], widths=["20%", "42%", "38%"], first_bold=True),
        h2("End-of-week-one checklist"),
        checklist([
            "All tools verified and screenshots committed to `journal/`.",
            "Practice Shop and Practice API run locally; you can explain what each URL does.",
            "You can state the Golden Rules from memory and explain why each exists.",
            "Assignment A0 (next chapter) started: requirement table read, questions raised with the buddy.",
            "Level 1 quiz scheduled for the end of week two.",
        ]),
        end_chapter(),
    )


def first_assignment() -> str:
    return join(
        h1("First Manual-Testing Assignment (A0)",
           "Test the **VEL Practice Shop** registration and login flow against a written specification, produce a "
           "traceable test suite, execute it, log defects and report results. Time-box: **4 working days**."),
        callout("info", "Why this assignment first",
                "It exercises the whole manual-testing chain in miniature: read a requirement, design cases, "
                "run them, capture evidence, report a defect, and summarise status. Every later project repeats "
                "this chain with more scale or more tooling."),
        h2("Specification under test"),
        table(["ID", "Requirement"], REQS, widths=["12%", "88%"], first_bold=True),
        h2("Scope"),
        table(["In scope", "Out of scope"], [[
            "Registration view, login view, shop view (cart, quantity, totals), session timeout, logout, viewport 360 px and 1280 px, keyboard access.",
            "Performance, security exploitation, other devices, real e-mail delivery, payment. Report any security concern you notice to your buddy instead of probing it."]],
            widths=["50%", "50%"]),
        h2("Procedure"),
        table(["Day", "Steps", "Deliverable"], [
            ["1", ("1. Read REQ-001 to REQ-014 twice; list ambiguities and unanswered questions.\n"
                   "2. Run the app; explore for 30 minutes without a script and note what you observe (exploratory notes).\n"
                   "3. Create the requirements traceability matrix with one row per requirement."),
             "`docs/requirements-traceability.md` with all 14 rows; `docs/questions.md`; exploratory notes"],
            ["2", ("1. For each requirement apply **equivalence partitioning** and **boundary-value analysis** where it has ranges (REQ-001, 003, 004, 010).\n"
                   "2. Write at least **30 test cases** using the template: ID, title, requirement, preconditions, steps, test data, expected result, priority.\n"
                   "3. Include positive and negative cases; at least 6 for each of REQ-003 and REQ-004."),
             "`test-cases/functional/registration-login.csv` (or tool export), 30+ cases, each linked to a REQ"],
            ["3", ("1. Execute every case on Chrome and Firefox at 1280 px; REQ-013 at 360 px.\n"
                   "2. Record Pass / Fail / Blocked with evidence file names per the convention.\n"
                   "3. Raise a defect (template) the moment you confirm a failure; attempt to reproduce it three times."),
             "Filled execution log; `evidence/` files; `defects/DEF-nnn.md` for each defect"],
            ["4", ("1. Re-check traceability: every requirement has at least one executed case.\n"
                   "2. Complete the test execution report with counts, defect list, coverage and your own risk statement.\n"
                   "3. Present for 10 minutes to the QA Lead."),
             "`docs/test-execution-report.md`; review notes from the QA Lead"],
        ], widths=["7%", "63%", "30%"]),
        h2("Worked example: one test case and one defect"),
        p("Use these as the quality bar. Notice that the expected result is **observable and unambiguous**, "
          "and the defect states exactly what happened versus what was required."),
        kv([
            ["Test case ID", "`TC-REG-004`  ·  Requirement: REQ-003  ·  Technique: boundary-value analysis  ·  Priority: High"],
            ["Title", "Registration accepts age 18 (lower boundary)"],
            ["Preconditions", "Practice Shop open at the Register view; no account exists for `bva18@example.test`."],
            ["Steps", ("ol", ["Enter name `Asha Rao`.", "Enter email `bva18@example.test`.", "Enter age `18`.",
                              "Enter password `Passw0rdOK` and the same value in Confirm password.", "Tick Terms.", "Click **Register**."])],
            ["Expected result", "The message “Registration successful” is displayed and the Login view opens. No error is shown next to Age."],
            ["Actual result / status", "Fill in during execution: **PASS** / **FAIL** / **BLOCKED** + evidence file name."],
        ]),
        kv([
            ["Defect ID / title", "`DEF-001`  ·  Age 18 rejected although REQ-003 allows 18 to 60 inclusive"],
            ["Severity / priority", "Major (valid users cannot register) / High"],
            ["Environment", "Practice Shop 1.0, Chrome 130 on Windows 11, viewport 1280×800, 2026-11-06"],
            ["Steps to reproduce", ("ol", ["Open Register view.", "Fill all fields validly, using age `18`.", "Tick Terms and click **Register**."])],
            ["Expected", "Registration succeeds (18 is the inclusive lower limit)."],
            ["Actual", "Age field shows an error; registration is blocked. Reproduced 3 of 3 attempts."],
            ["Evidence", "`evidence/2026-11-06_TC-REG-004_FAIL_1.png`"],
        ]),
        h2("Acceptance criteria and marking (100 points)"),
        table(["Criterion", "Points", "Full marks when"], [
            ["Traceability", "15", "All 14 requirements appear in the matrix and each links to at least one executed case."],
            ["Test design quality", "25", "Equivalence classes and boundaries are identified and named; cases are atomic, repeatable and have measurable expected results."],
            ["Execution discipline", "20", "Every case has a status and evidence; results are on two browsers; REQ-013 checked at 360 px."],
            ["Defect reports", "25", "Each defect is reproducible by someone else from the report alone; severity and priority are justified; no duplicates."],
            ["Report and honesty", "15", "Counts reconcile (pass + fail + blocked + not run = total); risks and untested areas are stated plainly."],
        ], widths=["24%", "10%", "66%"], first_bold=True),
        callout("gold", "Pass mark: 70 / 100",
                "Below 70 the trainee repeats the weakest part with feedback. The QA Lead keeps a hidden answer key "
                "of seeded defects (never shown to trainees). Finding more defects does not earn extra marks; "
                "**report quality** does."),
        end_chapter(),
    )
