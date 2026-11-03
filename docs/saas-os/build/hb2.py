"""Stage playbooks 5-8."""
CI = """name: CI
on:
  pull_request:
  push:
    branches: [main]
jobs:
  checks:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: 22
          cache: npm
      - run: npm ci
      - run: npm run lint
      - run: npm test
      - run: npm run build
      - run: npm audit --audit-level=high"""

BUILD_ORDER = [("1","Database & migrations","Create tables from the ERD as migration files","Fresh database builds from migrations; rollback works"),
 ("2","Backend skeleton","Project layout, config from environment variables, error handler","App starts; health endpoint returns OK"),
 ("3","Authentication","Sign-up/login, hashed passwords, sessions","Wrong password rejected; session expires"),
 ("4","Authorisation (roles)","Server-side checks from the permission matrix","Each role blocked from forbidden actions (tested)"),
 ("5","API endpoints","One endpoint at a time from the API list, with input validation","Valid input works; invalid input returns clear error"),
 ("6","Frontend skeleton","Layout, navigation, design tokens, shared components","All pages reachable; responsive at 360 px"),
 ("7","UI components","Forms, tables, dialogs from the design system","Match approved designs incl. empty/loading/error"),
 ("8","Business logic","Rules and calculations from the Output spec","Sample scenarios give exactly the expected numbers"),
 ("9","Connect frontend and API","Replace fake data with real calls; handle failures","Works end-to-end on staging"),
 ("10","File upload & storage","Safe upload: type and size limits, private storage","Bad file types rejected; files not publicly listable"),
 ("11","Email & notifications","Templates, queue, retry, unsubscribe where required","Test emails delivered; failures logged"),
 ("12","Reports & exports","PDF/CSV per Output spec","Totals match hand-calculated examples"),
 ("13","Payments (if in scope)","Gateway checkout, webhooks verified by signature","Test-mode payments update subscription state"),
 ("14","Admin dashboard","Tenant/user management, plan limits, audit log","Only Platform Admin can open it"),
 ("15","User dashboard","Key numbers and shortcuts","Loads in under 3 s on mobile data"),
 ("16","Error handling","Friendly messages to users, details to logs","No stack traces shown to users"),
 ("17","Logging & monitoring hooks","Structured logs without personal data or secrets","Search a request id and see its full path"),
 ("18","Documentation","README, API docs, runbook","New developer runs the app in 30 minutes")]

def stages():
    S = []
    S.append(dict(n=5, title="Set Up the Project", phase="Build", gate="G5 – Repository Ready", time="2–3 days",
      goal="Create one secure, automated project home so every developer starts from the same baseline.",
      start="G2, G3 and G4 are signed.", who="Tech Lead (owns) · DevOps (CI and environments) · All developers (follow)",
      tools="GitHub, VS Code, terminal, Node.js, CI (GitHub Actions)", skills="Stage 0 completed.",
      steps=[
       ("Create the private repository","GitHub → organisation → New repository. Name = project name in lowercase (<code>slotwise</code>). Visibility <b>Private</b>. Add a short description. Tick 'Add README'.","Repository link"),
       ("Clone it to your work folder","<code>cd ~/work</code> then <code>git clone git@github.com:ORG/slotwise.git</code> then <code>cd slotwise</code>.","Folder exists on your computer"),
       ("Create the standard folders","<code>mkdir -p apps/web apps/api packages/ui packages/shared docs/requirements docs/architecture docs/database docs/api docs/deployment docs/testing tests/unit tests/integration tests/e2e infra .github/workflows .github/ISSUE_TEMPLATE</code>. Git ignores empty folders, so add an empty <code>.gitkeep</code> file inside each one you want to keep.","Tree matches Part 2.3"),
       ("Add .gitignore and .env.example","<code>.gitignore</code> must list at least: <code>node_modules/</code>, <code>.env</code>, <code>.env.*</code> (except .env.example), build output folders, OS files. <code>.env.example</code> lists every setting <b>name</b> with a fake value, e.g. <code>DATABASE_URL=postgres://user:password@localhost:5432/slotwise</code>.","Both files committed; real .env ignored"),
       ("Write README.md and CONTRIBUTING.md","README: what this is, requirements, <b>exact steps to run it locally</b> (install, copy .env.example to .env, create database, run migrations, start). CONTRIBUTING: branch names, commit rules, Pull Request rules, how to run tests. Ask a new colleague to follow your README; fix every place they get stuck.","A colleague runs the app in under 30 minutes"),
       ("Create the app skeleton","If the Tech Lead has a company starter template, use it. Otherwise inside <code>apps/web</code> run the official generator <code>npx create-next-app@latest</code> and accept the TypeScript and ESLint options. Make sure <code>npm run dev</code>, <code>npm run lint</code>, <code>npm test</code> and <code>npm run build</code> exist and pass.","All four commands succeed"),
       ("Protect the main branch","GitHub → repository Settings → Branches (or Rules) → add rule for <code>main</code>: require a Pull Request, require at least 1 approval, dismiss stale approvals, require status checks to pass, require branch up to date, block force pushes, block deletion.","Screenshot of the rule"),
       ("Add the automatic checks (CI)","Create <code>.github/workflows/ci.yml</code> with the file shown below. It runs lint, tests, build and a dependency audit on every Pull Request. Mark it as a required check in the branch rule.","Green check on a test PR"),
       ("Turn on security features","Settings → Code security: enable Dependabot alerts and updates, secret scanning with push protection, and code scanning. These stop leaked keys and flag vulnerable libraries.","Features show 'Enabled'"),
       ("Prepare three environments","<b>Local</b>: your <code>.env</code>. <b>Staging</b> and <b>Production</b>: separate databases, separate secrets, stored in the hosting platform's secret settings (never in the repo). Record only the <i>names</i> of the settings in docs/deployment/environments.md.","Environment matrix document"),
       ("Run a dry-run Pull Request","Create <code>feature/0-ci-check</code>, change the README, push, open a PR, watch CI turn green, get a review, merge. Tag the first version: <code>git tag v0.1.0</code> then <code>git push --tags</code>.","Merged PR + tag v0.1.0")],
      selfcheck=["Repository is private","main is protected and requires review + passing checks","CI is green on a test PR","No secrets in the repo; .env is ignored","Dependabot and secret scanning are on","Staging and production are fully separate","README works for a new person"],
      signoff="Tech Lead signs G5 after the dry-run Pull Request passes everything.",
      extra="<h4>ci.yml (copy exactly, then adapt commands to your project)</h4><pre>"+CI+"</pre>",
      errors=[("Committed .env by mistake","Tell the Tech Lead immediately. Change every password/key in it. Remove it from the repo history with Tech Lead help"),("Branch rule not enforced for admins","Tick 'include administrators'"),("CI passes locally but fails on GitHub","Different Node version or missing environment variable; copy the CI error text and compare")],
      example="SlotWise: repository slotwise created, structure committed, CI green, v0.1.0 tagged, G5 signed in 2 days.",
      learn=[("GitHub: protected branches","https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches"),("GitHub Actions","https://docs.github.com/en/actions"),("Conventional Commits","https://www.conventionalcommits.org/")]))
    rows = "".join(f"<tr><td>{a}</td><td>{b}</td><td>{c}</td><td>{d}</td></tr>" for a, b, c, d in BUILD_ORDER)
    S.append(dict(n=6, title="Build It", phase="Build", gate="G6 – Feature Complete", time="8–14 weeks (sample product)",
      goal="Turn the frozen requirements into working software in small, tested, reviewed pieces with a demo every week.",
      start="G5 is signed and tickets exist for the first module.", who="Developers (build) · Tech Lead (reviews) · QA (prepares tests) · Product Owner (accepts)",
      tools="VS Code, Git, terminal, framework, database client, API client, board", skills="Basic coding in the chosen language; Parts 2–4 read.",
      steps=[
       ("Take one small ticket","Pick the top ticket in <b>To Do</b>. It should take one day or less; if bigger, ask the PO/Tech Lead to split it. Move it to <b>In Progress</b> and write your time estimate.","Ticket assigned to you"),
       ("Make a branch","<code>git switch main</code>, <code>git pull</code>, then <code>git switch -c feature/12-booking-form</code>. One ticket = one branch.","Branch visible on GitHub"),
       ("Write the test first or alongside","Turn the ticket's acceptance criteria into automatic tests. Run them: they fail. That is correct; they will pass when you finish.","Failing tests that describe the goal"),
       ("Write the smallest code that works","Keep functions short; use clear names; validate every input <b>on the server</b>; read secrets only from environment variables; never trust data from the browser; handle errors and show friendly messages.","Code compiles; tests pass"),
       ("Run the checks locally","<code>npm run lint</code>, <code>npm test</code>, <code>npm run build</code>. Fix everything before pushing. Run the app and try it like a user would.","All three pass"),
       ("Commit often with clear messages","<code>git add .</code> (check <code>git status</code> first so you do not add .env) then <code>git commit -m \"feat: add slot conflict check\"</code>. Small commits are easy to review and undo.","Meaningful commit history"),
       ("Push and open a Pull Request","<code>git push -u origin feature/12-booking-form</code>. On GitHub open a PR; fill the template: what changed, why, ticket link, how to test, screenshots, checklist. Assign a reviewer.","PR link on the ticket; ticket in <b>In Review</b>"),
       ("Respond to review","Reviewer comments are about the code, not you. Fix each comment with a new commit or explain politely why not. Do not merge until approved and CI is green.","Approval + green checks"),
       ("Merge and clean up","Use 'Squash and merge'. Delete the branch. The change deploys to <b>staging</b> automatically. Open staging and verify your feature works there.","Feature works on staging"),
       ("Update documentation","If behaviour, settings or API changed, update README / docs in the same PR. Move the ticket to <b>In Test</b> (QA) then <b>Done</b> when it meets the Definition of Done.","Docs updated"),
       ("Log time and show the weekly demo","Every Friday show working features on staging to the PO. Accepted features are ticked; rejected ones go back with reasons. Compare actual hours with estimate; tell the Tech Lead if you are over 20%.","Demo notes; timesheet complete"),
       ("Finish the module list and request G6","When all in-scope modules (list below) are accepted by the PO and no Critical/High bugs are open, request G6 'Feature complete'. No new features after this; only fixes.","G6 signed")],
      selfcheck=["Each ticket has a branch, a PR, a review and green checks","Every input is validated on the server","No secrets or customer data in code or logs","Tests exist for business rules","Docs updated with each change","Weekly demos held; PO accepted every in-scope module"],
      signoff="Product Owner accepts all in-scope modules; Tech Lead confirms zero open Critical or High defects.",
      extra="<h4>Standard build order (follow top to bottom)</h4><div class='tw'><table><thead><tr><th>#</th><th>Module</th><th>What to build</th><th>Done when</th></tr></thead><tbody>"+rows+"</tbody></table></div>",
      errors=[("Working on a huge branch for weeks","Merge small changes daily; split tickets"),("'Works on my machine'","Test on staging; keep README and .env.example accurate"),("Fixing something not in the ticket","Write a new ticket; keep PRs focused"),("Trusting form validation only","Validate again on the server")],
      example="SlotWise: 18 modules built over 11 weeks; 142 Pull Requests, all reviewed; weekly demos recorded; G6 signed.",
      learn=[("React docs","https://react.dev/learn"),("Next.js docs","https://nextjs.org/docs"),("OWASP Cheat Sheets","https://cheatsheetseries.owasp.org/")]))
    S.append(dict(n=7, title="Test It", phase="Verify", gate="G7 – Quality Approved", time="2–3 weeks (overlaps Stage 6)",
      goal="Find the problems before customers do, with proof that every requirement works.",
      start="Features are on staging (testing runs alongside Stage 6 and finishes after G6).", who="QA Engineer (leads) · Developers (fix) · Product Owner (user acceptance)",
      tools="Test framework, Playwright (browser tests), Lighthouse, browsers and phones, bug tracker", skills="Reading the PRD; patience.",
      steps=[
       ("Write the test plan from the acceptance criteria","A table: test ID · requirement · steps · expected result · result (pass/fail) · tester · date. Every acceptance criterion needs at least one test case.","Test plan sheet"),
       ("Check unit tests","Developers' tests for single functions must pass in CI. QA reviews that business rules (calculations, validations) have tests with the hand-calculated examples.","CI green; coverage report"),
       ("Run integration tests","Tests that call the API and use a test database: create → read → update → delete, permission checks, error responses.","Passing integration suite"),
       ("Run end-to-end (browser) tests for key journeys","Automate the 5–10 most important journeys (sign-up, login, create booking, pay, export) with Playwright; run in CI against staging.","E2E results"),
       ("Do manual exploratory testing","Try to break it: wrong inputs, empty fields, very long text, special characters, double-click submit, back button, slow network, two browser tabs. Test on Chrome, Edge, Firefox, Safari, and on at least one real Android and one iPhone.","Exploratory notes + bug list"),
       ("Check accessibility","Use only the keyboard to complete a journey; run Lighthouse accessibility; check contrast and labels.","Accessibility result"),
       ("Check performance","Run Lighthouse; test the main pages on a throttled mobile connection; do a simple load test with the expected peak requests per second from the cost model.","Performance numbers vs targets in the PRD"),
       ("Report bugs properly","Use the bug template (Appendix A): title, environment, steps to reproduce, expected, actual, screenshot, severity. <b>Critical</b> = data loss/security/unusable; <b>High</b> = main feature broken; <b>Medium</b> = workaround exists; <b>Low</b> = cosmetic.","Tickets for every bug"),
       ("Retest and run regression","When a bug is fixed retest it, then rerun the whole plan (regression) because fixes can break other things.","Updated results"),
       ("User acceptance test (UAT)","The PO (and customer) run the sample scenarios from the PRD on staging and sign each as accepted.","UAT sign-off sheet"),
       ("Write the QA report and request G7","Summary: tests run/passed/failed, open bugs by severity, performance and accessibility results, known limitations, recommendation.","G7 signed")],
      selfcheck=["Every acceptance criterion has a test result","No open Critical or High bugs","Key journeys pass on 4 browsers and 2 phones","Performance and accessibility targets met or exceptions approved","UAT signed by PO","QA report written"],
      signoff="QA Lead and Product Owner sign G7. QA never tests their own code.",
      errors=[("Testing only the happy path","Add invalid, empty, huge and duplicate inputs"),("Vague bug reports","Always include steps to reproduce"),("Skipping regression","Re-run the full plan after fixes")],
      example="SlotWise: 212 test cases, 3 Critical bugs found in week 1 and fixed; final run 212/212 pass; G7 signed.",
      learn=[("Playwright","https://playwright.dev/docs/intro"),("Lighthouse","https://developer.chrome.com/docs/lighthouse/overview")]))
    S.append(dict(n=8, title="Secure It", phase="Verify", gate="G8 – Security Approved", time="3–5 days",
      goal="Make sure we do not leak data, allow break-ins, or ship known vulnerabilities.",
      start="G6 signed; QA in progress.", who="Tech Lead (owns) · Developers (fix) · DevOps · optional external tester", tools="npm audit, GitHub security tabs, browser dev tools, OWASP checklists", skills="Understanding of the app's roles and data.",
      steps=[
       ("Scan for vulnerable libraries","Run <code>npm audit</code> and read GitHub's Dependabot alerts. Update or replace packages with High/Critical findings; record any exception with a reason and date.","Audit output saved"),
       ("Scan for leaked secrets","Check GitHub secret-scanning alerts and search the code for keys or passwords. If any real secret was ever committed, <b>replace it</b> (changing the code alone is not enough).","Zero open alerts"),
       ("Test access control (OWASP #1)","Log in as customer A and try to open customer B's record by changing the ID in the address or API call. Log in as each role and try forbidden actions. All must be refused <b>by the server</b>.","Test table: role × action × result"),
       ("Test injection and input handling","Type quotes, scripts and very long text into every field; confirm the app shows them safely and never errors. Check the code uses parameterised queries from the ORM, never string-built SQL.","Results noted per form"),
       ("Test login security","Passwords hashed; limit repeated login attempts; sessions expire; logout really logs out; password-reset links expire and work once.","Auth checklist ticked"),
       ("Check HTTPS and headers","Only HTTPS works (HTTP redirects); cookies are Secure and HttpOnly; basic security headers set; no secrets in browser source or API responses.","Header check output"),
       ("Check files and uploads","Only allowed types and sizes; stored privately; no executable files; links expire.","Upload tests"),
       ("Check logs and errors","Logs contain no passwords, keys or personal data; users never see stack traces.","Log sample reviewed"),
       ("Check backups and restore","Restore the latest backup into staging and compare row counts. A backup never restored is only a hope.","Restore evidence"),
       ("Privacy basics","Collect only needed data; record consent where needed; define retention and deletion; write the privacy policy with a lawyer. Confirm legal duties (for example India's Digital Personal Data Protection Act, 2023) with the company's legal adviser.","Privacy checklist + legal note"),
       ("Fix, retest and write the Security Report","Fix all Critical/High items, retest, then write: what was checked, findings, fixes, residual risk, recommendation. Request G8.","G8 signed")],
      selfcheck=["No High/Critical vulnerable libraries","No secrets in code or history","Cross-customer access impossible (tested)","Every role tested for forbidden actions","HTTPS only; cookies secure","Backup restore proven","Privacy policy and consent reviewed by legal adviser"],
      signoff="Tech Lead signs G8; Owner accepts any residual risk in writing.",
      errors=[("Hiding a mistake you made","Report it at once; the fix is quick, the cover-up is not"),("Relying on the front end to hide buttons","Hidden is not secure; check on the server"),("Never testing restore","Do it before every launch")],
      example="SlotWise: cross-tenant test found one endpoint missing a tenant filter; fixed and covered by an automatic test; G8 signed.",
      learn=[("OWASP Top 10","https://owasp.org/www-project-top-ten/"),("OWASP ASVS","https://asvs.owasp.org/"),("NIST SSDF","https://csrc.nist.gov/pubs/sp/800/218/final")]))
    return S
