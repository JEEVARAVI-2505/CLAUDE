"""Professional QA repository structure and test-documentation standards."""
from lib import *  # noqa: F401,F403

TREE = """qa-project/
├── README.md                     # purpose, how to run, owners, links
├── THIRD_PARTY.md                # every tool + version + licence
├── docs/
│   ├── test-strategy.md
│   ├── test-plan.md
│   ├── requirements-traceability.md
│   ├── test-environment.md
│   ├── release-report.md
│   └── qa-signoff.md
├── test-cases/
│   ├── functional/
│   ├── regression/
│   ├── integration/
│   └── acceptance/
├── automation/
│   ├── ui/                       # Playwright / Selenium / Cypress tests
│   ├── api/                      # pytest + requests, Postman/Newman
│   ├── fixtures/
│   ├── page-objects/
│   └── reports/                  # generated; git-ignored
├── performance/                  # k6 / JMeter scripts, results summaries
├── accessibility/                # axe / Lighthouse configs and findings
├── test-data/                    # synthetic data only
├── evidence/                     # screenshots, logs (index in Git, bulk in artefact store)
├── defects/                      # defect reports (if not in a tracker)
└── .github/
    └── workflows/                # CI pipelines"""


def repository() -> str:
    return join(
        h1("Professional QA Repository Structure",
           "Every client project has one repository (or one folder in the client's repository) with this structure. "
           "A ready-made skeleton is supplied in `qa-project-template/`."),
        code(TREE, "qa-project/"),
        table(["Folder", "Contents", "Rules"], [
            ["`docs/`", "Strategy, plan, traceability matrix, environment description, release report, QA sign-off.", "Markdown, one file per document, version in the header, changes through pull requests."],
            ["`test-cases/`", "Cases grouped by type: functional, regression, integration, acceptance.", "One CSV or Markdown file per feature; ID pattern `TC-<area>-<nnn>`; each case links a `REQ-` ID."],
            ["`automation/ui`, `api`", "Executable tests.", "Independent tests; no secrets; code review before merge."],
            ["`automation/page-objects`, `fixtures`", "Reusable page classes and fixtures.", "Locators and actions only; fixtures documented."],
            ["`automation/reports/`", "JUnit XML, HTML, traces.", "Generated; listed in `.gitignore`; published as CI artefacts."],
            ["`performance/`", "Scripts, workload model, result summaries.", "Raw result files > 5 MB go to the artefact store."],
            ["`accessibility/`", "Scan configuration, WCAG findings, manual-check records.", "Findings reference WCAG success criteria."],
            ["`test-data/`", "Synthetic data files and a README.", "Never real personal data or production extracts."],
            ["`evidence/`", "Index of evidence and small files.", "Name pattern `<date>_<TC-id>_<result>_<n>.<ext>`; large videos live in the artefact store, linked from the index."],
            ["`defects/`", "Defect write-ups when no tracker is available.", "One file per defect: `DEF-nnn.md`."],
            ["`.github/workflows/`", "CI pipelines.", "Secrets only via CI secret store."],
        ], widths=["22%", "38%", "40%"], first_bold=True),
        callout("danger", "Never commit these",
                ["Passwords, API keys, access tokens, private keys, certificates, `.env` files with real values.",
                 "Sensitive customer data or personal data of any real person.",
                 "Production database extracts or dumps, even “just a small sample”.",
                 "Unsanitised HAR files, screenshots showing tokens, e-mails or personal data.",
                 "Client confidential documents in public repositories."]),
        h2("If a secret is committed by accident"),
        ol(["**Stop** and tell the QA Lead immediately; do not try to hide it.",
            "**Revoke or rotate** the secret at its source first (the commit history cannot be trusted to be private).",
            "Remove it from the repository history using the approved procedure from the QA Lead (rewriting history is a controlled operation).",
            "Record an incident note: what, when, who was told, what was rotated.",
            "Add a prevention step (pre-commit secret scan, push protection, `.gitignore` rule)."]),
        h2("Starter `.gitignore` for QA repositories"),
        code("""# secrets and local config
.env
*.pem
*.key
secrets/
# generated output
automation/reports/
playwright-report/
test-results/
performance/results/
*.har
# environments and dependencies
.venv/
node_modules/
__pycache__/
.pytest_cache/
.DS_Store""", ".gitignore"),
        h2("Git workflow and conventions"),
        table(["Topic", "Convention"], [
            ["Branches", "`main` is always releasable. Work in `qa/<initials>/<topic>`; open a pull request; at least one reviewer (the QA Lead for strategy, plan, sign-off)."],
            ["Commits", "`<type>(<scope>): <summary>` with types `docs`, `test`, `fix`, `refactor`, `ci`, `chore`. One logical change per commit."],
            ["Pull requests", "Describe what changed, why, how it was verified; link requirement or defect IDs."],
            ["Protected main", "Require reviews and passing CI checks before merge; forbid force-push."],
            ["Releases", "Tag the QA baseline of each release: `qa-<release>-<yyyymmdd>`."],
            ["Retention", "Keep evidence for the period in the contract (default: until 12 months after final acceptance); delete securely afterwards."],
        ], widths=["20%", "80%"], first_bold=True),
        h2("`THIRD_PARTY.md` entry format"),
        code("""| Tool / package | Version | Licence | Used for | Source / checked on |
|---|---|---|---|---|
| Playwright (pytest-playwright) | 1.64.1 | Apache-2.0 | UI automation | npm/PyPI metadata, 2026-11-04 |
| pytest | 9.2.0 | MIT | Test runner | PyPI metadata, 2026-11-04 |""", "THIRD_PARTY.md"),
        end_chapter(),
    )


def doc_standards() -> str:
    return join(
        h1("Test Documentation Standards",
           "Eleven controlled documents are produced on every full project. Each has a template in the Templates chapter and "
           "a fixed place in the repository."),
        table(["Document", "Purpose", "Produced at", "Owner", "Approver", "Repo path"], [
            ["Test Strategy", "How we will test and why.", "Stage 03", "QA Lead", "Test Manager + client", "`docs/test-strategy.md`"],
            ["Test Plan", "What, when, who, criteria, risks.", "Stage 04", "QA Lead", "Test Manager + client", "`docs/test-plan.md`"],
            ["Test Scenario", "High-level conditions to test.", "Stage 05", "Manual QA", "QA Lead", "`test-cases/`"],
            ["Test Case", "Precise repeatable test.", "Stage 05", "Manual QA", "QA Lead (review)", "`test-cases/<type>/`"],
            ["Requirements Traceability Matrix", "Maps requirements ↔ cases ↔ defects ↔ automation.", "Stages 02–14", "QA Lead", "QA Lead", "`docs/requirements-traceability.md`"],
            ["Defect Report", "Reproducible failure description.", "Stage 08", "Reporter", "Triage", "Tracker or `defects/`"],
            ["Test Execution Report", "Results of an execution cycle.", "Stage 07, 12", "QA Lead", "Test Manager", "`docs/`"],
            ["Automation Coverage Report", "What is automated, stable and running.", "Stage 09, 16", "Automation QA", "QA Lead", "`docs/`"],
            ["Performance Test Report", "Workload, thresholds, results, bottlenecks.", "Stage 11", "Performance Tester", "Test Manager + client", "`performance/`"],
            ["UAT Report", "Client acceptance results.", "Stage 13", "QA Lead", "Client business owner", "`docs/`"],
            ["Release QA Sign-off", "Recommendation, evidence, residual risk.", "Stage 14", "QA Lead", "Test Manager + client", "`docs/qa-signoff.md`, `docs/release-report.md`"],
        ], widths=["17%", "21%", "10%", "13%", "17%", "22%"], first_bold=True),
        h2("Writing standards"),
        ul(["Every document starts with a header block: **ID, title, version, status (draft / in review / approved), author, reviewer, date**.",
            "Use plain, unambiguous language. Avoid “works correctly”, “should be fine”, “etc.”, “as expected”.",
            "State numbers with units and the date/build they refer to.",
            "Cite evidence by file name or link; do not paste unreferenced claims.",
            "Separate **facts** (observed), **interpretation** (what it means) and **recommendation** (what we suggest).",
            "Version every change; approved documents change only through a new version with a change note.",
            "Mark client-confidential documents and store them only in approved locations."]),
        h2("Status and approval vocabulary"),
        table(["Status", "Meaning"], [
            ["Draft", "Author is still writing; not for client use."],
            ["In review", "Internal or client review in progress; comments tracked."],
            ["Approved", "Signed by the named approver; baseline for further work."],
            ["Superseded", "Replaced by a newer approved version; retained for history."],
        ], widths=["20%", "80%"], first_bold=True),
        end_chapter(),
    )
