"""Level 6 — Advanced QA."""
from lib import *  # noqa: F401,F403


def level6() -> str:
    return join(
        h1("Level 6 — Advanced QA",
           "Performance, load, stress and reliability testing; accessibility at scale; security-test coordination; database "
           "validation; coverage; and managing flaky tests."),
        level_header("About 4 weeks", "Levels 1–5 passed.",
                     "Plans and runs a controlled performance test with a workload model and thresholds, automates accessibility checks, "
                     "validates data directly in a test database, coordinates security testing with qualified people, and keeps the "
                     "automation suite trustworthy.",
                     "Controlled performance test report + accessibility automation + database validation script + flaky-test register, all reviewed."),
        h2("Performance testing: vocabulary"),
        table(["Test type", "Question", "Load shape", "Typical duration"], [
            ["Baseline / smoke", "What is performance with one user?", "1–5 virtual users (VUs)", "Minutes"],
            ["Load", "Does it meet targets at expected peak?", "Ramp to expected peak and hold", "20–60 min steady state"],
            ["Stress", "Where does it break and how does it fail?", "Increase until errors or saturation", "Until breaking point"],
            ["Spike", "Can it absorb a sudden surge and recover?", "Sudden jump up and down", "10–30 min"],
            ["Soak (endurance)", "Does it leak memory or degrade over time?", "Moderate load, long hold", "4–24 h"],
            ["Scalability", "Does adding capacity add throughput?", "Repeat load at different sizes", "Per configuration"],
            ["Reliability", "Does it recover from faults?", "Load + fault injection in non-production", "Per scenario"],
        ], widths=["17%", "34%", "30%", "19%"], first_bold=True),
        table(["Metric", "Meaning", "Note"], [
            ["Response time (p50, p90, p95, p99)", "Time to complete a request, by percentile.", "Never report only the average; p95 and p99 show the user pain."],
            ["Throughput", "Requests (or transactions) per second.", "Compare with the workload model."],
            ["Error rate", "Failed requests ÷ total requests.", "Define “failed” (status ≥ 500, timeout, failed assertion)."],
            ["Concurrency (VUs)", "Simulated simultaneous users.", "Not the same as registered users."],
            ["Saturation", "CPU, memory, disk, network, connection pools.", "Monitor server-side during the test."],
        ], widths=["27%", "36%", "37%"], first_bold=True),
        h2("Workload model: worked calculation"),
        p("Assumptions agreed with the client and written into the test plan: **10,000 users per day**, **20 % of them in the peak hour**, "
          "each session makes **6 requests**, safety factor **2×**."),
        table(["Step", "Calculation", "Result"], [
            ["Users in peak hour", "10,000 × 0.20", "2,000 users"],
            ["User arrival rate", "2,000 ÷ 3,600 s", "0.556 users/s"],
            ["Request rate", "0.556 × 6 requests", "3.33 requests/s"],
            ["Target with safety factor", "3.33 × 2", "**6.67 requests/s**"],
            ["Concurrent users (Little's law)", "N = X × R, with X = 0.556 users/s and a 3-minute session (R = 180 s)", "≈ 100 concurrent sessions"],
        ], widths=["30%", "48%", "22%"], first_bold=True),
        callout("danger", "Controlled conditions are mandatory",
                "Run performance tests **only** on an environment the client has authorised in writing, outside business hours "
                "or on an isolated environment, with monitoring in place and a named contact who can stop the test. "
                "Never load-test production, shared third-party services or systems you do not own. A stress test against "
                "an unauthorised system is a denial-of-service attack."),
        h2("Example performance script (k6)"),
        file_code("performance/k6-products.js"),
        p("The k6 script was syntax-checked in the build environment but **not executed** (k6 is not installed there). The equivalent Locust script (`performance/locustfile.py`) *was* executed against the local practice API."),
        table(["Tool", "Licence", "Strengths", "Limitations"], [
            ["k6 (Grafana)", "AGPL-3.0 — verify on the official repository and note the copyleft implications before bundling or modifying", "Scripts in JavaScript, thresholds, CI-friendly, low resource use", "No browser rendering by default; protocol-level tests"],
            ["Apache JMeter", "Apache-2.0 — verify on the official site", "GUI plan builder, many protocols, mature plugins, CLI mode", "XML plans are hard to review in Git; heavier on resources"],
            ["Locust", "MIT (registry-verified, v2.47.0)", "Python scripts; web UI; distributed", "Python GIL limits one process per core"],
        ], widths=["17%", "31%", "28%", "24%"], first_bold=True),
        code("""# JMeter: build the plan in the GUI, but RUN it headless (non-GUI) for real tests
jmeter -n -t performance/products.jmx -l performance/results.jtl -e -o performance/report/

# Performance report must state: environment, build, data volume, workload model, thresholds,
# results per percentile, errors, server metrics, bottleneck analysis, and limitations.""", "bash"),
        h2("Accessibility automation"),
        code("""# automation/ui/test_accessibility.py  (axe-core via the Playwright page; axe-core is MPL-2.0, registry-verified)
from pathlib import Path
import json, pytest

AXE = Path("node_modules/axe-core/axe.min.js")      # npm i -D axe-core

def test_register_view_has_no_serious_a11y_violations(register_page):
    register_page.open()
    register_page.page.add_script_tag(content=AXE.read_text(encoding="utf-8"))
    result = register_page.page.evaluate("axe.run()")
    serious = [v for v in result["violations"] if v["impact"] in ("serious", "critical")]
    assert not serious, json.dumps([(v["id"], v["help"]) for v in serious], indent=2)""", "automation/ui/test_accessibility.py"),
        p("**Lighthouse** (Apache-2.0, registry-verified v13.6.0): `npx lighthouse http://localhost:8000 --only-categories=accessibility --output=html "
          "--output-path=accessibility/lighthouse.html`. Treat scores as a screening aid; **manual keyboard and screen-reader checks "
          "remain mandatory** because automated tools detect only part of the possible issues."),
        h2("Security test coordination"),
        table(["Step", "QA responsibility", "Security specialist responsibility"], [
            ["1. Scope", "Record in the test plan which security-relevant behaviours QA will check and which are handed over.", "Define penetration-test scope, rules of engagement, authorisation."],
            ["2. Safe checks by QA", "Authentication required? Role checks? Error messages leak internals? Cookie flags? Password rules? Session timeout?", "—"],
            ["3. Escalate", "Report suspicion privately to the named security coordinator; do **not** exploit, do not post in public channels, do not attach real secrets.", "Triage, reproduce in a safe way, rate (e.g. CVSS), advise fix."],
            ["4. Retest", "Retest the fix functionally.", "Verify the vulnerability is closed."],
            ["5. Report", "List security observations in the release risk summary.", "Provide formal assessment report."],
        ], widths=["17%", "45%", "38%"], first_bold=True),
        p("Reference for awareness (not for self-directed attacks): OWASP Top 10 and OWASP ASVS."),
        h2("Database validation"),
        callout("danger", "Rules",
                "Use a **test database** and a **read-only** account unless a test case explicitly needs writes. "
                "Never connect to production. Never copy production data into test environments without an approved masking process."),
        code("""-- Row-count reconciliation after migration (source vs target)
SELECT (SELECT COUNT(*) FROM legacy.customers) AS src, (SELECT COUNT(*) FROM app.customers) AS tgt;

-- Orphan check: orders without a customer
SELECT o.id FROM app.orders o LEFT JOIN app.customers c ON c.id = o.customer_id WHERE c.id IS NULL;

-- Duplicate check on a business key that must be unique
SELECT email, COUNT(*) FROM app.customers GROUP BY email HAVING COUNT(*) > 1;

-- Derived value check: order total equals sum of lines (to the paisa)
SELECT o.id FROM app.orders o
JOIN (SELECT order_id, ROUND(SUM(qty * unit_price), 2) AS s FROM app.order_lines GROUP BY order_id) l ON l.order_id = o.id
WHERE ROUND(o.total, 2) <> l.s;""", "validation.sql"),
        p("Each query returns **zero rows when the data is correct** (except the first, where the two numbers must match). "
          "Save the query, the result and the date as evidence."),
        h2("Test coverage"),
        table(["Coverage type", "Definition", "Caution"], [
            ["Requirement coverage", "Requirements with ≥ 1 executed test ÷ total requirements.", "Having a test is not the same as having enough tests."],
            ["Risk coverage", "High-risk items tested ÷ high-risk items identified.", "Needs an agreed risk register."],
            ["Automation coverage", "Automated test cases ÷ total test cases (or automated requirement coverage).", "Say what the denominator is."],
            ["Code coverage (statement/branch)", "Percentage of code executed by tests (e.g. `coverage.py`, Apache-2.0; SonarQube shows it).", "100 % does not prove correctness; do not set it as a vanity target."],
        ], widths=["24%", "42%", "34%"], first_bold=True),
        h2("Flaky test management"),
        p("A **flaky test** passes and fails on the same code without changes. Flaky tests destroy trust: people start ignoring red builds."),
        table(["Common cause", "Fix"], [
            ["Fixed sleeps and race conditions", "Use auto-waits and web-first assertions."],
            ["Shared or order-dependent data", "Unique data per test; independent tests."],
            ["Unstable environment or third-party service", "Stub external calls; stabilise the environment; separate contract tests."],
            ["Time, date, locale, random", "Freeze time; seed randomness; explicit locale."],
            ["Resource limits in CI", "Right-size runners; reduce parallelism."],
        ], widths=["38%", "62%"], first_bold=True),
        ol(["**Detect:** rerun the failed test 10 times on the same commit; flag as flaky if results differ.",
            "**Record** in the flaky-test register: ID, first seen, owner, suspected cause, ticket.",
            "**Quarantine** (tag `flaky`, run separately, not blocking) for at most **10 working days**.",
            "**Fix or delete.** A quarantined test that is not fixed in time is removed and its coverage gap is listed as risk.",
            "**Report** the flaky rate every release. Automatic retries are allowed only if every retry is counted and reported."]),
        h2("Topic packs (ten-part teaching method)"),
        card10("6.1 Performance testing",
               "Measuring speed, stability and resource use under defined conditions.",
               "Slow or unstable systems lose users and money.",
               "Test environment, monitoring, k6/JMeter/Locust, written authorisation.",
               "1. Agree workload and thresholds. 2. Prepare data. 3. Run baseline. 4. Run measured test. 5. Analyse percentiles and server metrics.",
               "Performance report with p50/p95/p99, throughput, errors, bottleneck notes.",
               "Repeat the run; results within an agreed variance.",
               "Testing a different environment than production-like; average-only reporting.",
               "Performance test report template.",
               "Project 7.",
               "Report review.")
        + card10("6.2 Load testing",
               "Verifying behaviour at the expected peak load.",
               "Confirms launch readiness.",
               "Workload model.",
               "1. Ramp to peak. 2. Hold steady state. 3. Ramp down. 4. Compare with thresholds.",
               "Pass/fail against thresholds.",
               "Server metrics show no saturation at peak.",
               "Measuring during ramp-up.",
               "Report.",
               "Run the k6 script on the Practice API.",
               "Review.")
        + card10("6.3 Stress testing",
               "Pushing beyond peak to find the breaking point and failure mode.",
               "Shows how the system fails and recovers.",
               "Isolated environment; written approval.",
               "1. Increase load stepwise. 2. Note the point errors rise. 3. Reduce load; check recovery.",
               "Breaking-point and recovery description.",
               "System recovers without manual repair.",
               "Running on shared/production systems.",
               "Report section.",
               "Stress the local Practice API; document the limit.",
               "Review.")
        + card10("6.4 Reliability testing",
               "Checking that the system keeps working or recovers when parts fail.",
               "Real systems fail; recovery must be designed and proven.",
               "Non-production environment; fault-injection plan.",
               "1. List failure scenarios (service down, slow dependency, restart). 2. Inject one at a time. 3. Observe behaviour and recovery time.",
               "Scenario table with outcomes.",
               "No data loss; user-visible messages are sensible.",
               "Injecting faults without a rollback plan.",
               "Reliability findings.",
               "Stop the API during a UI session; document UI behaviour.",
               "Review.")
        + card10("6.5 Accessibility testing (advanced)",
               "Automated and manual audit against WCAG.",
               "Legal exposure and inclusive design.",
               "axe-core, Lighthouse, screen reader.",
               "1. Automate scans for key pages. 2. Add manual checks. 3. Triage by impact and WCAG criterion.",
               "Accessibility report with WCAG mapping.",
               "No unreviewed serious/critical issues.",
               "Treating a Lighthouse score as conformance.",
               "Report + defects labelled `a11y`.",
               "Automate the scan on three pages.",
               "Review.")
        + card10("6.6 Security test coordination",
               "Organising safe security checks with qualified specialists.",
               "Protects the client and the company legally.",
               "Scope document; named security coordinator.",
               "1. Record scope. 2. Run safe checks. 3. Escalate privately. 4. Retest fixes. 5. Report risk.",
               "Security observation log.",
               "Escalations acknowledged in writing.",
               "Exploiting a finding to “prove it”.",
               "Private report to coordinator.",
               "Complete the safe-check list for the Practice API.",
               "Oral exam on boundaries.")
        + card10("6.7 Database validation",
               "Directly verifying data correctness in the database.",
               "Many defects are invisible in the UI.",
               "Test DB, read-only account, SQL client.",
               "1. Identify invariants. 2. Write queries returning violations. 3. Run after test and migration steps.",
               "Query set with zero-row expectations.",
               "Deliberately introduce an error in a copy; query must detect it.",
               "Testing against production; writes without approval.",
               "`evidence/db/`.",
               "Write 8 validation queries for a sample schema.",
               "Review.")
        + card10("6.8 Test coverage",
               "Measuring how much of the requirements, risks or code the tests exercise.",
               "Shows gaps and supports release decisions.",
               "Traceability matrix; coverage tools.",
               "1. Define denominators. 2. Compute. 3. Investigate gaps. 4. Report with context.",
               "Coverage summary.",
               "Numbers recomputed by a reviewer match.",
               "Chasing a percentage.",
               "Release report.",
               "Compute requirement and automation coverage for A0.",
               "Review.")
        + card10("6.9 Flaky test management",
               "Detecting, quarantining and fixing nondeterministic tests.",
               "Protects the value of automation.",
               "CI history; flaky register.",
               "1. Detect via reruns. 2. Log. 3. Quarantine. 4. Fix or delete. 5. Report rate.",
               "Flaky register and rate trend.",
               "Rate decreases release over release.",
               "Retrying until green.",
               "Register in `docs/`.",
               "Introduce and fix a deliberate race condition.",
               "Review."),
        callout("gold", "Level 6 exit gate",
                "Controlled performance report, accessibility automation, database validation set and flaky-test register approved by QA Lead "
                "and, for performance, by the Performance Tester role-holder. Sign-off: QA Lead."),
        end_chapter(),
    )
