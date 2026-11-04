"""Level 5 — Automation testing."""
from lib import *  # noqa: F401,F403


def level5() -> str:
    return join(
        h1("Level 5 — Automation Testing",
           "Turn stable, repeatable, valuable manual tests into reliable code: Python or JavaScript/TypeScript fundamentals, "
           "Playwright, Selenium, Cypress, pytest, Postman, Page Object Model, fixtures, test data and CI."),
        level_header("About 4 weeks", "Levels 1–4 passed. Comfortable using a terminal and Git.",
                     "Writes maintainable UI and API automation with a Page Object Model and fixtures, runs it locally and in CI, "
                     "and reports automation coverage honestly.",
                     "Automated suite (≥ 15 UI + ≥ 15 API tests) running green in GitHub Actions with a published report; code review passed."),
        h2("What to automate (and what not to)"),
        table(["Automate", "Do not automate (yet)"], [[
            "Stable features with clear expected results · smoke and regression paths · data-driven checks (many inputs, same steps) · "
            "API contract and schema checks · tests run on every build · tests too tedious or error-prone by hand.",
            "Features still changing daily · one-off checks · exploratory testing and usability judgement · "
            "visual design opinions · tests that need physical hardware you cannot control · anything whose expected result you cannot state precisely."]],
            widths=["50%", "50%"]),
        callout("gold", "Automation return-on-investment rule",
                "Automate when **(manual minutes per run × number of runs)** is greater than **(build time + maintenance time)**. "
                "Example: a 30-minute manual regression run 40 times a year = 20 hours. If automating costs 16 hours and "
                "maintenance 4 hours/year, break-even is in the first year; with 6 runs a year it never pays back."),
        h2("The test pyramid"),
        table(["Layer", "Share", "Speed", "Examples"], [
            ["Unit (developers)", "Many (≈ 70 %)", "Milliseconds", "Function tests, mocked dependencies"],
            ["API / integration (QA + dev)", "Some (≈ 20 %)", "Seconds", "Contract, schema, auth, error handling"],
            ["UI end-to-end (QA)", "Few (≈ 10 %)", "Tens of seconds", "Critical journeys: register → login → buy"],
        ], widths=["30%", "20%", "18%", "32%"], first_bold=True),
        p("QA owns the top two layers. The more logic you can verify at API level, the faster and less flaky the suite."),
        h2("Choosing a UI automation tool"),
        table(["Tool", "Languages", "Browsers", "Strengths", "Limitations", "Licence (registry-verified 2026-11-04)"], [
            ["Playwright", "TypeScript/JS, Python, Java, .NET", "Chromium, Firefox, WebKit", "Auto-waiting, tracing, parallel, API testing, codegen", "WebKit ≠ real Safari; no real mobile devices (emulation)", "Apache-2.0 (v1.64.1)"],
            ["Selenium WebDriver", "Java, Python, C#, JS, Ruby", "All major via drivers", "Standard, huge ecosystem, Grid", "More boilerplate, manual waits, slower", "Apache-2.0 (v4.52.0)"],
            ["Cypress", "JavaScript/TypeScript", "Chromium, Firefox, WebKit (experimental)", "Great developer experience, time-travel debugging", "Single-origin history, no multi-tab; paid cloud features", "MIT (v16.2.1); Cypress Cloud is a paid service"],
        ], widths=["11%", "14%", "15%", "21%", "22%", "17%"], first_bold=True),
        p("**Selection rule:** default to Playwright for new projects; use Selenium when the client already owns a Selenium Grid or "
          "needs a language or browser Playwright lacks; use Cypress when the client team is JavaScript-only and wants developers "
          "to write tests too. Alternatives to know: WebdriverIO (MIT), Appium for native mobile (Apache-2.0), Robot Framework."),
        h2("Python fundamentals required (one-page primer)"),
        code("""# Variables, collections, f-strings
user = {"email": "qa@example.test", "age": 18}
ages = [17, 18, 60, 61]
print(f"{user['email']} has age {user['age']}")

# Functions and loops
def is_valid_age(age: int) -> bool:
    return 18 <= age <= 60          # the spec: inclusive range

for a in ages:
    print(a, is_valid_age(a))       # False True True False

# Exceptions and files
import json, pathlib
try:
    data = json.loads(pathlib.Path("test-data/users.json").read_text(encoding="utf-8"))
except FileNotFoundError:
    data = []""", "python-primer.py"),
        p("JavaScript/TypeScript equivalent topics: `const/let`, arrow functions, arrays/objects, `async/await`, modules (`import`), "
          "`try/catch`, JSON. Teach one language first; the second takes days, not weeks."),
        h2("pytest essentials"),
        table(["Concept", "Use", "Example"], [
            ["Test discovery", "Files `test_*.py`, functions `test_*`.", "`pytest -q`"],
            ["Fixtures", "Reusable setup/teardown with scopes (`function`, `module`, `session`).", "`@pytest.fixture(scope=\"session\")`"],
            ["Parametrisation", "One test, many data rows.", "`@pytest.mark.parametrize(\"age,ok\", [(17,False),(18,True)])`"],
            ["Markers", "Select groups; mark known defects.", "`-m smoke`; `@pytest.mark.xfail(reason=\"DEF-001\", strict=True)`"],
            ["conftest.py", "Shared fixtures per directory.", "base URL, auth token, browser page"],
            ["Reports", "JUnit XML for CI; HTML via `pytest-html` (MPL-2.0).", "`--junitxml=reports/junit.xml`"],
        ], widths=["20%", "42%", "38%"], first_bold=True),
        h2("Playwright (Python) with Page Object Model and fixtures"),
        p("The code below is the **real starter code** from `qa-project-template/` (it is embedded from the repository when this handbook is built, so it cannot drift). "
          "Install and run (Playwright downloads its browsers once):"),
        code("""cd qa-project-template
python3 -m venv .venv && source .venv/bin/activate
pip install -r automation/requirements.txt
playwright install chromium firefox
export QA_EMAIL=qa@example.test QA_PASSWORD='<see practice-api/README.md>'
pytest -m smoke                 # critical path only
pytest automation/ui --browser chromium --browser firefox""", "bash"),
        file_code("automation/page-objects/register_page.py"),
        file_code("automation/ui/test_registration.py"),
        file_code("automation/ui/conftest.py"),
        file_code("pytest.ini"),
        callout("info", "Why `xfail(strict=True)`",
                "A known defect appears as an **expected failure** linked to its defect ID. When developers fix it, "
                "the test suddenly passes and `strict=True` turns that into a failure, forcing you to remove the marker and close the defect. "
                "The suite stays green yet honest. Never hide an unexplained failure behind `xfail`."),
        h2("Selenium and Cypress at a glance"),
        code("""# Selenium (Python) - explicit wait, never time.sleep
from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC

driver = webdriver.Chrome()
try:
    driver.get("http://localhost:8000/#/login")
    WebDriverWait(driver, 10).until(EC.visibility_of_element_located((By.ID, "login-email")))
    driver.find_element(By.ID, "login-email").send_keys("nobody@example.test")
finally:
    driver.quit()""", "selenium-example.py"),
        code("""// Cypress (JavaScript) - retries assertions automatically
describe('Login view', () => {
  it('shows a generic message for a wrong password', () => {
    cy.visit('http://localhost:8000/#/login');
    cy.get('#login-email').type('qa@example.test');
    cy.get('#login-password').type('wrong-password');
    cy.contains('button', 'Log in').click();
    cy.get('[role=alert]').should('contain.text', 'Invalid email or password');
  });
});""", "cypress-example.cy.js"),
        h2("Page Object Model, fixtures and test data: the rules"),
        table(["Topic", "Rules"], [
            ["Page Object Model", "One class per page/component. Locators and actions live in the page object; **assertions live in the test**. Prefer role/label locators (`get_by_role`, `get_by_label`) over CSS/XPath; never use positional XPath such as `//div[3]/span[2]`."],
            ["Waiting", "Never `sleep`. Use auto-waiting actions and web-first assertions (`expect(locator).to_be_visible()`)."],
            ["Independence", "Each test creates its own data and can run alone, in any order, in parallel."],
            ["Fixtures", "Use fixtures for browser context, base URL, authenticated session, API client; put clean-up after `yield`."],
            ["Test data", "Synthetic only; unique values (`uuid`) for anything that must be unique; data files in `test-data/` with a README; no secrets in code — read from environment variables or the CI secret store."],
            ["Reporting", "JUnit XML + HTML report + traces/screenshots on failure, uploaded as CI artifacts."],
        ], widths=["20%", "80%"], first_bold=True),
        h2("Continuous integration with GitHub Actions"),
        file_code(".github/workflows/qa.yml"),
        p("Pin actions to a major version tag at minimum (a full commit SHA is stricter). Keep secrets in "
          "**GitHub Actions secrets**, never in the YAML. Free-tier minutes and runner limits change: check the "
          "current GitHub Actions documentation before promising a client a CI budget."),
        h2("Topic packs (ten-part teaching method)"),
        card10("5.1 Python or JavaScript/TypeScript fundamentals",
               "The programming basics needed to read and write test code.",
               "Automation is software development; weak code makes flaky tests.",
               "Python 3.11+ or Node LTS; VS Code.",
               "1. Work through variables, functions, loops, collections, files, exceptions. 2. Solve 10 exercises. 3. Write a script that reads CSV test data.",
               "Script that prints a pass/fail table from a CSV.",
               "Runs on a clean machine with one command.",
               "Copy-pasting code you cannot explain.",
               "`journal/l5-programming.md`.",
               "Implement `is_valid_age` and test it with all BVA values.",
               "Coding quiz + code walk-through.")
        + card10("5.2 Playwright",
               "A framework that drives Chromium, Firefox and WebKit with auto-waiting and tracing.",
               "Fast, reliable cross-browser UI automation.",
               "`pip install pytest-playwright` (or `npm init playwright`), `playwright install`.",
               "1. Record with codegen to learn locators. 2. Rewrite as page objects. 3. Add assertions. 4. Run headed, then headless.",
               "Green tests plus trace viewer on a failure.",
               "Run 5 times; zero flakiness.",
               "Fixed sleeps; brittle CSS selectors.",
               "HTML/JUnit report; trace zip.",
               "Automate the A0 smoke list.",
               "Code review.")
        + card10("5.3 Selenium",
               "The long-standing WebDriver standard for browser automation.",
               "Many clients already run Selenium Grid.",
               "`pip install selenium`; Selenium Manager fetches drivers.",
               "1. Open a page. 2. Use explicit waits. 3. Interact. 4. Always `quit()`.",
               "A passing login-failure test.",
               "No `time.sleep` in the code.",
               "Stale-element errors; implicit + explicit wait mixing.",
               "Same report format.",
               "Port one Playwright test to Selenium and compare effort.",
               "Short comparison write-up.")
        + card10("5.4 Cypress",
               "A JavaScript test runner that executes inside the browser.",
               "Developer-friendly tests for JS teams.",
               "Node LTS; `npm i -D cypress`.",
               "1. `npx cypress open`. 2. Write a spec. 3. Use `cy.intercept` to stub a network call.",
               "Passing spec with video/screenshots.",
               "Break the app; test fails with a clear message.",
               "Trying multi-tab/multi-origin flows; async/await misuse.",
               "Cypress run output + artifacts.",
               "Write 3 specs for the login view.",
               "Review.")
        + card10("5.5 pytest",
               "The standard Python test framework with fixtures and parametrisation.",
               "Clean, scalable test suites with rich plugins.",
               "`pip install pytest`; folder `automation/`.",
               "1. Write tests. 2. Add fixtures. 3. Parametrise BVA data. 4. Add markers (`smoke`, `regression`).",
               "Suite selectable by marker.",
               "`pytest -m smoke` runs only smoke tests.",
               "Over-complicated fixtures; shared mutable state.",
               "JUnit XML.",
               "Convert 10 boundary cases to a parametrised test.",
               "Review.")
        + card10("5.6 Postman",
               "A GUI and runner for building, testing and documenting API requests.",
               "Fastest way to explore and regress-test an API.",
               "Postman app (proprietary, free tier); Newman CLI (Apache-2.0).",
               "1. Build a collection. 2. Add tests. 3. Use environments. 4. Run with Newman.",
               "Collection + environment files; Newman run output.",
               "Newman exits non-zero when a test fails.",
               "Secrets stored in exported files.",
               "Newman JUnit report.",
               "Automate Level-4 collection through Newman.",
               "Demonstration.")
        + card10("5.7 API automation",
               "Code-driven API tests with pytest and `requests`.",
               "Stable, fast regression and contract checks.",
               "`pip install requests jsonschema pytest`.",
               "1. Fixtures for base URL, token. 2. Test positive and negative cases. 3. Validate schema. 4. Mark known defects `xfail`.",
               "15+ API tests including schema checks.",
               "Seeded API defects appear as strict `xfail`.",
               "Hard-coded ids; test order dependence.",
               "JUnit report.",
               "Project 5.",
               "Code review.")
        + card10("5.8 Page Object Model",
               "A pattern that wraps each page's locators and actions in a class.",
               "One change in the UI means one change in the code.",
               "Existing UI tests.",
               "1. Identify repeated locators. 2. Move them into a class. 3. Keep assertions in tests.",
               "Refactored suite with page objects.",
               "Tests still pass; duplicated locators removed.",
               "Page objects that contain assertions and business rules.",
               "Commit message `refactor(ui): introduce RegisterPage`.",
               "Refactor your Playwright tests.",
               "Review.")
        + card10("5.9 Fixtures",
               "Reusable setup and teardown code injected into tests.",
               "Removes duplication and guarantees clean state.",
               "pytest fixtures.",
               "1. Create session, module and function fixtures. 2. Use `yield` for cleanup. 3. Compose fixtures.",
               "`conftest.py` with documented fixtures.",
               "Tests pass in any order and in parallel (`pytest -p xdist -n 2`).",
               "Global state; hidden dependencies.",
               "Docstrings on fixtures.",
               "Write an authenticated-API fixture.",
               "Review.")
        + card10("5.10 Test data management",
               "Creating, storing and cleaning the data that tests use.",
               "Most flaky tests are data problems.",
               "`test-data/`, factories, API seeding.",
               "1. Generate unique data. 2. Seed via API where possible. 3. Clean up. 4. Document datasets.",
               "Data strategy page and factory helpers.",
               "Two parallel runs do not collide.",
               "Shared accounts; real data.",
               "`test-data/README.md`.",
               "Write a user factory with unique e-mails.",
               "Review.")
        + card10("5.11 CI integration",
               "Running tests automatically on every change in a pipeline.",
               "Fast feedback; protects the main branch.",
               "GitHub repository, Actions workflow, secrets.",
               "1. Add workflow. 2. Cache dependencies. 3. Upload reports. 4. Make the check required on pull requests.",
               "Green workflow run with artifacts.",
               "Break a test on a branch; the check fails.",
               "Secrets in YAML; tests depending on a developer's machine.",
               "Link to workflow run in the QA report.",
               "Commit the workflow and show a failing and a passing run.",
               "Review."),
        callout("gold", "Level 5 exit gate",
                "≥ 15 UI + ≥ 15 API tests, green in CI with published reports, reviewed code, and an automation coverage "
                "statement that separates *automated*, *manual-only* and *not yet tested*. Sign-off: QA Lead + Automation QA."),
        end_chapter(),
    )
