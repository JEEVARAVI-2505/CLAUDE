"""Handbook part 3: stages 8-13 and appendices."""
from hb_lib import *
import model as M
I = M.inr

def stage8():
    g = glance("2 - 4 weeks", "QA engineer, developers, tech lead", "Real phones, Postman/Bruno, test sheet, bug tracker", "CTO (Gate 7)")
    bug = form("BUG REPORT", [["ID / title", "BUG-231 · Attendance not saved after reconnect"], ["Severity", "Sev-1 crash/data loss · Sev-2 major · Sev-3 minor · Sev-4 cosmetic"], ["Steps to reproduce", "1) Turn on airplane mode 2) Save attendance 3) Turn it off"], ["Expected / actual", "Row appears on server / nothing appears"],
                                  ["Device / OS / app version", "Redmi Note 11 · Android 13 · 1.0.0 (12)"], ["Attachments", "Screen recording + log"]])
    sec = checklist(["Login: wrong password, expired session, lockout", "Tokens stored in secure storage, not plain files", "User A cannot read user B's data (try changing IDs in requests)", "App uses HTTPS only", "No passwords/tokens in logs or screenshots",
                     "Permissions asked only when needed", "Deep links and web views checked", "Outside SDKs reviewed: what do they collect?", "Account and data deletion works"])
    st = [("Write test cases from the Screen Sheets", "<p>For each rule write: steps → expected result. Example: enter 17 hours → message 'Hours must be between 0 and 16'.</p>"),
          ("Run functional tests", "<p>Go through every screen and every rule. Mark Pass/Fail in the traceability sheet (requirement → test → result).</p>"),
          ("Test on many devices", table(["Test on", "Minimum"], [["Android", "3 devices: old, mid, new; one with low memory"], ["iOS", "Current and previous major version"], ["Screens", "Smallest and largest phone"], ["Rotation / tablet", "Only if the requirements say so"]])),
          ("Test the network", "<p>Wi-Fi, mobile data, very slow, airplane mode, switching networks, killing the app during upload.</p>"),
          ("Measure performance", table(["Measure", "Target"], [["App start", "≤ 3 seconds on a mid-range phone"], ["Screen load (cached)", "≤ 1 second"], ["API response (95%)", "≤ 500 ms"], ["Crash-free sessions", "≥ 99.5%"]])),
          ("Do the security checks", sec + "<p>Reference standards: OWASP MASVS (what to achieve) and MASTG (how to test).</p>"),
          ("Report bugs properly", bug),
          ("Fix and re-test", "<p>Developer fixes in a new branch; tester repeats the exact steps; run the full regression suite before release.</p>"),
          ("Write the QA & Security report", "<p>Summary: tests run, pass/fail, open bugs by severity, device list, security results, QA recommendation. <b>Gate 7 requires zero open Sev-1 and Sev-2.</b></p>")]
    return stage("s8", 8, "Testing & security", "quality", "Try to break it before customers do", g,
                 plain("QA", "Quality Assurance: a person (or process) whose job is to find problems early, when they are cheap to fix.") + "<h3>Steps</h3>" + steps(st, "quality") +
                 output("Test cases + results · bug list · device list · performance numbers · security checklist · QA & Security report · Gate 7") + "<h3>Quality check</h3>" +
                 checklist(["Every requirement has at least one test", "0 open Sev-1/Sev-2 bugs", "Tested on real devices and bad networks", "Security checklist complete", "Automated tests run in CI"]) +
                 "<h3>Common mistakes</h3>" + ul(["Only testing the happy path", "Reporting bugs without steps", "Releasing with 'known' serious bugs"]))

def stage9():
    g = glance("2 - 3 days", "PM, Finance, Founder", "Spreadsheet, official pricing pages and calculators", "Founders (Gate 8)")
    r = M.RATES
    rate = table(["Role", "Hourly rate card (loaded)", "Where it comes from"], [["Product Manager", I(r["PM"]), "PM + Finance"], ["UI/UX Designer", I(r["UX"]), ""], ["Flutter Developer", I(r["FL"]), ""], ["Backend Developer", I(r["BE"]), ""], ["QA Engineer", I(r["QA"]), ""], ["DevOps / Database", I(r["DO"]), ""], ["Security specialist (external)", I(r["SE"]), "Quote"]],
                 num_cols=(1,))
    cp = M.CP1
    ex = table(["Line", "SiteDiary example", "How to calculate"], [["Labour (expected)", I(cp["labour"]), "Σ hours × rate for each task and role (PERT: (O + 4M + P) ÷ 6)"], ["Setup & third-party", I(cp["setup"]), "tools, devices, domain, store accounts, legal"],
              ["Company overhead (12%)", I(cp["ov"]), "labour × 12%"], ["Risk allowance", I(cp["risk"]), "50% × (pessimistic − expected labour)"], ["Total cost", I(cp["cost"]), "sum of the four lines above"],
              ["<b>Selling price (30% margin)</b>", f"<b>{I(cp['target'])}</b>", "Total cost ÷ (1 − 0.30)"], ["Minimum allowed price (15% margin)", I(cp["minimum"]), "Total cost ÷ (1 − 0.15)"]], num_cols=(1,))
    st = [("Get the rate card from your PM", rate + "<p>These are hourly <b>cost</b> rates the company uses (they already include overheads). Never invent your own.</p>"),
          ("List every task with hours", "<p>Break the project into tasks (design, login, offline sync, testing …). For each task estimate three numbers per role: <b>best (O)</b>, <b>most likely (M)</b>, <b>worst (P)</b>.</p>" + formula("Expected hours E = (O + 4 × M + P) ÷ 6")),
          ("Multiply hours by rates", formula("Task cost = Σ (hours of each role × that role's hourly rate)") + "<p>Count each hour once. If one person does two jobs, give the hours to the job actually done.</p>"),
          ("Add non-labour costs", "<p>Tools, test devices, domain, legal review, store accounts. Every price needs the <b>official page link and the date you checked</b>.</p>"),
          ("Add monthly running cost", "<p>Use the official cloud calculators with your expected users (low / medium / high). Do not guess; do not assume every user is active daily.</p>"),
          ("Calculate cost and price", ex),
          ("Check the margin", "<p>If the price is below the minimum, you may not quote it. Ask the Founder.</p>"),
          ("Hand in the worksheet for Gate 8", "<p>Attach screenshots of every price source with date.</p>")]
    return stage("s9", 9, "Cost & price", "quality", "Know exactly what it costs, then price it so the company earns", g,
                 plain("margin", "The share of the selling price that is profit. 30% margin on ₹100 price = ₹30 profit, ₹70 cost. Price = cost ÷ (1 − margin).") + "<h3>Steps</h3>" + steps(st, "quality") +
                 output("Cost worksheet · price sheet · source screenshots (URL + date) · Gate 8 approval") +
                 callout("ok", "Full models", "The management document <i>Mobile App Company Operating System</i> (Phase 8-9) contains the full numbers: user-scale simulation, three infrastructure models, SaaS tiers, break-even and maintenance packages.") +
                 "<h3>Quality check</h3>" + checklist(["Every price has official source + date", "No hour counted twice", "Three scenarios (best/likely/worst) shown", "Price ≥ minimum margin price", "Founder signed"]) +
                 "<h3>Common mistakes</h3>" + ul(["Counting only developer salaries", "Forgetting testing, deployment, documentation and training", "Assuming free tiers last forever", "Quoting before getting the Gate approved"]))

def stage10():
    g = glance("2 - 4 days", "DevOps, backend dev, QA", "Cloud console, domain registrar, terminal, monitoring tool", "CTO (Gate 9)")
    st = [("Buy and secure the domain", "<p>Use an accredited registrar. Turn on registrar lock and 2FA. Domain belongs to the company account, never a personal one.</p>"),
          ("Create the production environment", "<p>Production server, production database, file storage - separate from dev/staging, with separate credentials.</p>"),
          ("Store secrets in the secret manager", "<p>Put DATABASE_URL, JWT_SIGNING_KEY, etc. in the cloud secret store. The app reads them at start-up. Rotate keys on schedule.</p>" + dont("Do not put production secrets inside the mobile app. Anything shipped inside an app can be extracted.")),
          ("Run database migrations", "<p>Run them through the deployment pipeline, not by hand. Take a backup first.</p>"),
          ("Deploy the backend", "<p>Use the approved pipeline: tag a release → pipeline builds → deploys to staging → QA checks → manual approval → production.</p>" + cmd("git tag v1.0.0", "git push origin v1.0.0")),
          ("Connect DNS", "<p>Add the records <b>exactly as the hosting provider shows them</b> (A/CNAME/TXT). Never guess values. Wait for propagation; confirm HTTPS works (padlock).</p>"),
          ("Turn on monitoring and alerts", "<p>Uptime check, error rate, slow responses, disk and database health. Alerts go to the on-call person's phone.</p>"),
          ("Verify backups by restoring one", "<p>A backup you never restored is only a hope. Restore into a test database and check the data.</p>"),
          ("Practise rollback", "<p>Redeploy the previous version on staging and confirm it works. Write the steps in <code>docs/deployment</code>.</p>"),
          ("Run the production smoke test", "<p>Register, login, create a report, upload a photo, receive a push notification - using a test account.</p>")]
    return stage("s10", 10, "Go live: server & domain", "launch", "Put the backend on the internet safely", g,
                 plain("deploy", "Copying the tested software to the live server and switching it on, using a repeatable script rather than manual steps.") + "<h3>Steps</h3>" + steps(st, "launch") +
                 output("Live API URL with HTTPS · monitoring screenshots · backup restore proof · rollback test proof · smoke-test result · Gate 9") + "<h3>Quality check</h3>" +
                 checklist(["HTTPS works; HTTP redirects", "Secrets are in the secret manager only", "Backup restored successfully", "Alerts reach a real person", "Rollback tested", "Dev and production URLs are separate"]) +
                 "<h3>Common mistakes</h3>" + ul(["Deploying by hand from a laptop", "No backup test", "Same database for staging and production"]))

def stage11():
    g = glance("1 - 3 weeks incl. review time (not guaranteed)", "Release manager, QA, designer", "Android Studio, Xcode (Mac), Play Console, App Store Connect", "CTO + PM (Gate 10)")
    key = cmd("keytool -genkey -v -keystore upload-keystore.jks -keyalg RSA -keysize 2048 -validity 10000 -alias upload")
    fees = table(["Account", "Fee", "Status (checked 3 Nov 2026)", "Needs"], [["Apple Developer Program", "US$99 per year (local currency; price varies by region)", "<b>Verified</b> on Apple's official page", "Organisation: legal entity + D-U-N-S number; no trade names/DBAs"],
                  ["Apple Enterprise Program", "US$299 per year", "Verified - internal company apps only, not for public stores", "Not for our public apps"],
                  ["Google Play Console", "US$25 one-time", "Medium-High: Google help page via search; confirm on signup screen", "Government ID and card in legal name may be requested"]])
    android = [("Fix the app ID and version", "<p>Package name never changes after publishing. In <code>pubspec.yaml</code>: <code>version: 1.0.0+1</code> (name+build number). Every upload needs a higher build number.</p>"),
               ("Create the upload key (once)", key + dont("Store the .jks file and its passwords in the company vault and one offline backup. If lost, updates become very hard. Never commit it.")),
               ("Build the Android App Bundle", cmd("flutter build appbundle --release") + "<p>Output: <code>build/app/outputs/bundle/release/app-release.aab</code></p>"),
               ("Create the store listing", "<p>Title, short and full description, icon, feature graphic, phone screenshots, privacy policy URL, category, contact email.</p>"),
               ("Complete Data safety and content forms", "<p>Declare what data the app and its SDKs collect. It must match reality - Google can reject or remove apps that do not.</p>"),
               ("Upload to Internal testing, then wider testing", "<p>Invite testers by email. Fix problems. Check current rules for your account type on the day (new personal accounts may have extra testing steps).</p>"),
               ("Submit for review and release gradually", "<p>Start with 10% → 50% → 100% while watching crashes.</p>")]
    ios = [("Register the app ID and create the app record", "<p>Apple Developer → Identifiers; App Store Connect → New App (name, bundle ID, language, SKU).</p>"),
           ("Set signing", "<p>In Xcode enable <i>Automatically manage signing</i> with the company team, or use the team's certificate process.</p>"),
           ("Build the archive and upload", cmd("flutter build ipa --release") + "<p>Upload with Xcode Organizer or Transporter.</p>"),
           ("Fill App Store Connect", "<p>Screenshots for required device sizes, description, keywords, support URL, <b>App Privacy</b> answers, age rating, <b>demo account and notes for the reviewer</b>.</p>"),
           ("TestFlight beta", "<p>Internal testers first, then external. Collect crash reports and feedback.</p>"),
           ("Submit for App Review", "<p>Respond to reviewer messages within 24 hours. Release in phases.</p>")]
    post = checklist(["Install from the store on a clean phone", "Register + login + reset password", "Main workflow end-to-end on production", "Push notification arrives", "Crash reporting receives events", "Privacy policy and support links open"])
    return stage("s11", 11, "Publish to stores", "launch", "Get the app onto Google Play and the App Store", g,
                 plain("store review", "Google and Apple check each app. They may ask for changes or reject it. Nobody can promise an approval date - plan extra time.") + "<h3>Accounts and fees</h3>" + fees +
                 callout("warn", "Re-check before paying", "Official pages change. Open the official page on the day, screenshot the fee, and file it in <code>07-Costing-Pricing</code>.") +
                 "<h3>Android steps</h3>" + steps(android, "launch") + "<h3>iOS steps</h3>" + steps(ios, "launch") + "<h3>After release - verification</h3>" + post +
                 output("Signed AAB and iOS build · store listings · privacy declarations · testing records · release notes · Gate 10 · post-release verification") +
                 "<h3>Common mistakes</h3>" + ul(["Losing the signing key", "Privacy form that contradicts what SDKs really collect", "No demo login for the Apple reviewer", "Using a personal account instead of the company's"]))

def stage12():
    g = glance("3 - 7 days", "PM, Sales, support, customer", "Staging/demo build, slides, manuals", "Founder + customer (Gate 11)")
    st = [("Prepare the demo", "<p>Use a demo account with fake data. Write a 10-minute script: problem → three main workflows → admin view → limits → price.</p>"),
          ("Present honestly", "<p>Show what works. State what is not included. Never promise features that are not in the signed scope.</p>"),
          ("Prepare the documents", checklist(["User manual with screenshots", "Admin manual", "Quick-start one-pager", "Training slides", "FAQ", "Support contacts and hours"])),
          ("Train the customer", "<p>One live session per role. Let them do the tasks while you watch. Record the session.</p>"),
          ("Hand over access", "<p>Transfer accounts, domains, store listings and credentials through the secure vault - as agreed in the contract. Remove your personal access afterwards.</p>"),
          ("Sign the handover", form("HANDOVER FORM", [["Project / version delivered", "PRJ-2026-001 · v1.0.0"], ["Deliverables handed over", "☐ app ☐ source code (per contract) ☐ manuals ☐ credentials ☐ training"], ["Known issues", "____"], ["Support package and start date", "____"],
                  ["Customer name / signature / date", "____"], ["Company name / signature / date", "____"]])),
          ("Start the support period", "<p>Open the support channel and schedule a check-in after one week and one month.</p>")]
    return stage("s12", 12, "Demo, handover & delivery", "launch", "Show it, teach it, hand it over - delivery 100%", g,
                 plain("handover", "The formal moment when the customer receives the finished product and the responsibilities change. It must be written and signed.") + "<h3>Steps</h3>" + steps(st, "launch") +
                 output("Demo recording · manuals · training record · signed handover form · Gate 11") + "<h3>Common mistakes</h3>" + ul(["Handing over without documents", "Leaving your personal accounts as owners", "No signed acceptance"]))

def stage13():
    g = glance("Continuous", "Support, developers, DevOps", "Crash reporter, monitoring, ticket tracker", "Support lead")
    st = [("Watch the dashboards daily", "<p>Crashes, errors, slow APIs, failed syncs, server health.</p>"), ("Handle tickets by severity", table(["Priority", "Example", "Response goal (Professional plan)"], [["P1", "App down, data at risk", "4 business hours"], ["P2", "Major feature broken", "1 business day"], ["P3", "Minor problem", "Next monthly release"]])),
          ("Update regularly", "<p>Monthly: dependency and security updates. Every OS season: test on new Android/iOS. Yearly: renew accounts, domain, certificates.</p>"),
          ("Review backups", "<p>Monthly check; quarterly restore drill.</p>"), ("Write incident reports", form("INCIDENT REPORT", [["What happened / when", "____"], ["Impact", "____"], ["Cause", "____"], ["Fix", "____"], ["How we prevent it", "____"]])),
          ("Change requests", "<p>New features always go through the change request form with cost and schedule.</p>"), ("Renew and report", "<p>Send the customer a monthly report: uptime, crashes, tickets, hours used.</p>")]
    return stage("s13", 13, "Support & maintenance", "run", "Keep it working and improving", g,
                 plain("maintenance", "Software needs care after launch: phones update, servers age, new bugs appear, customers ask for improvements.") + "<h3>Steps</h3>" + steps(st, "run") +
                 callout("bad", "Boundary", "Support has a fixed number of hours per month in the signed package. Extra work is billed. Never promise unlimited support."))

def appendix():
    pr = snippet("""## What changed
## Ticket (SD-14)
## How I tested it (commands / screenshots)
## Risks or things the reviewer should check
## Checklist
- [ ] Tests added   - [ ] flutter analyze clean   - [ ] No secrets   - [ ] Docs updated""")
    standup = form("DAILY STAND-UP NOTE", [["Yesterday", "____"], ["Today", "____"], ["Blocked by", "____ (who can help?)"]])
    week = form("WEEKLY REPORT (Friday)", [["Tasks planned / done / carried over", "__ / __ / __"], ["Hours estimated vs actual", "__ vs __"], ["Bugs opened / fixed", "__ / __"], ["Delays and reason", "____"], ["Risks / help needed", "____"], ["Learned this week", "____"]])
    rn = form("RELEASE NOTES", [["Version / date", "v1.0.0 · ____"], ["New", "____"], ["Improved", "____"], ["Fixed", "____"], ["Known issues", "____"]])
    gl = table(["Term", "Meaning in plain words"], [["API", "Menu of requests a server understands"], ["Backend / frontend", "Server side / what users see"], ["Branch", "Your private working copy of the code"], ["Commit", "A saved snapshot with a message"], ["Pull request (PR)", "Asking the team to review and merge your branch"],
          ["CI/CD", "Robots that test and deploy automatically"], ["Build", "Turning code into an installable app"], ["Release", "A tested version given to users"], ["Staging / production", "Test copy / live system"], ["Migration", "A recorded database change"], ["Token", "A temporary digital pass proving who you are"],
          ["Endpoint", "One URL of the API, e.g. /v1/sites"], ["JSON", "Text format for data, e.g. {\"hours\": 8}"], ["SDK", "Ready-made code library from another company"], ["Idempotent", "Safe to repeat - same result, no duplicates"], ["Sync", "Making phone and server data match"],
          ["MVP", "Smallest version that is useful"], ["Sprint", "A 2-week work period"], ["Severity", "How bad a bug is (Sev-1 worst)"], ["Keystore", "File that proves you own the Android app - protect it"], ["Bundle ID", "Permanent unique ID of an app, e.g. in.company.app"],
          ["TestFlight", "Apple's tool for beta testers"], ["Gate", "Approval checkpoint between stages"], ["Scope", "What is included in the project"], ["Margin", "Profit as a share of price"]])
    faq = table(["Problem", "What to try"], [["<code>flutter doctor</code> shows ✗ Android licenses", "Run <code>flutter doctor --android-licenses</code> and accept all"], ["<code>Permission denied (publickey)</code> on git", "Your SSH key is not added to GitHub or not loaded: repeat Stage 0 step 4"],
          ["Merge conflict", "Open the file, keep the correct lines, remove the <code>&lt;&lt;&lt;&lt;</code> markers, <code>git add</code>, <code>git commit</code>. Ask for help if unsure."], ["Port 5432 already in use", "Another PostgreSQL is running: stop it or use a different port"], ["App works in emulator not on phone", "Check USB debugging, internet, permissions, and the API URL (emulator uses 10.0.2.2 for the laptop's localhost)"],
          ["Push rejected (protected branch)", "You pushed to main. Create a branch and open a pull request"], ["CI is red", "Open the CI log, find the first error, run the same command locally, fix, push"], ["I committed a secret", "Tell the lead <b>now</b>. The secret must be rotated; deleting the commit is not enough"]])
    final = checklist(["Market research approved (Gate 1)", "Requirements frozen and signed (Gate 2)", "Architecture approved (Gate 3)", "Design and prototype approved (Gate 4)", "Repo and environments ready (Gate 5)", "All frozen screens built and demoed (Gate 6)",
                       "QA & security passed, zero Sev-1/2 (Gate 7)", "Cost and price approved (Gate 8)", "Production live, backup and rollback proven (Gate 9)", "Store release approved and verified (Gate 10)", "Handover signed by the customer (Gate 11)", "Support package active"])
    docs = table(["Topic", "Official documentation"], [["Flutter", "https://docs.flutter.dev/"], ["Android", "https://developer.android.com/docs"], ["Apple Developer", "https://developer.apple.com/documentation/"], ["App Store Connect", "https://developer.apple.com/help/app-store-connect/"],
            ["Google Play Console", "https://support.google.com/googleplay/android-developer/"], ["NestJS", "https://docs.nestjs.com/"], ["PostgreSQL", "https://www.postgresql.org/docs/"], ["Git", "https://git-scm.com/doc"], ["GitHub", "https://docs.github.com/"],
            ["OWASP MASVS / MASTG", "https://mas.owasp.org/"], ["Firebase", "https://firebase.google.com/docs"]])
    return ("<h3>C.1 Pull request template</h3>" + pr + "<h3>C.2 Daily stand-up note</h3>" + standup + "<h3>C.3 Weekly report</h3>" + week + "<h3>C.4 Release notes</h3>" + rn +
            "<h3>C.5 Glossary</h3>" + gl + "<h3>C.6 Troubleshooting guide</h3>" + faq + "<h3>C.7 The '100% delivered' checklist</h3>" + final + "<h3>C.8 Official documents to bookmark</h3>" + docs)
