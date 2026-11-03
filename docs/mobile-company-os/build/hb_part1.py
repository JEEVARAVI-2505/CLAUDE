"""Handbook part 1: welcome, how a company works, folder system, stages 0-2."""
from hb_lib import *

def welcome():
    how = ol(["Read <b>Part A</b> once, fully. It explains how a software company works, so everything else makes sense.",
              "Keep the <b>process map</b> open. It shows where you are: Stage 0 to Stage 13.",
              "Work on <b>one stage at a time</b>. Do the steps in order. Do not skip a step because it looks small.",
              "At the end of each stage, fill the <b>hand-in list</b>, tick the <b>quality check</b>, and ask your manager to sign the <b>gate form</b>.",
              "If anything is unclear, use the <b>stuck ladder</b> in Part A (try 30 minutes → ask a teammate → ask the lead). Never guess silently."])
    return (f'<p>This handbook takes a person who knows only the basics from <b>an idea (0%) to a finished app delivered to a customer (100%)</b>, '
            f'in the same way a professional software company does it. Every stage has the same layout so you always know what to expect:</p>'
            + table(["Block", "What it tells you"], [["Stage at a glance", "Time, people, tools, who approves"], ["In plain words", "Simple meaning of difficult terms"], ["Before you start", "What must already exist"],
                     ["Steps 1, 2, 3 …", "Exactly what to do, in order, with commands and examples"], ["What you must hand in", "The files/proofs this stage produces"], ["Quality check", "Tick-list you must pass"],
                     ["Common mistakes", "What beginners usually get wrong"]]) + "<h3>How to use it</h3>" + how +
            callout("warn", "The sample project", "All examples use <b>SiteDiary</b>, a sample app that lets a site supervisor record daily work, attendance and photos, even without internet. Replace the names with your own project when you work."))

def part_a():
    org = table(["Team / role", "What they do in simple words", "You ask them when…"], [
        ["Founder / Managing Director", "Decides what the company builds and signs the big approvals", "A gate needs final approval, or money is involved"],
        ["CTO / Tech Lead", "Chooses technology, protects code quality, reviews difficult code", "You are unsure how to build something"],
        ["Product Manager (PM)", "Talks to customers, writes requirements, controls scope, schedule and budget", "You are unsure <i>what</i> to build or something changes"],
        ["UI/UX Designer", "Designs how the app looks and how people use it", "A screen is unclear or does not fit"],
        ["Mobile Developer", "Builds the app people install on phones", "App screens, device problems"],
        ["Backend Developer", "Builds the server and APIs the app talks to", "Data, login, server errors"],
        ["Database / DevOps Engineer", "Keeps databases, servers, deployments and backups running", "Deployments, environments, secrets"],
        ["QA Engineer (tester)", "Tries to break the app before customers do; decides if it can be released", "Test cases, bug severity"],
        ["Sales / Support", "Demos, contracts, customer questions after delivery", "Customer complaints and requests"]])
    flow = ('<div class="flow"><div>Customer or idea</div><div>→</div><div>Product Manager writes requirements</div><div>→</div><div>Designer designs screens</div><div>→</div><div>Developers build</div><div>→</div>'
            '<div>QA tests</div><div>→</div><div>DevOps deploys</div><div>→</div><div>Release to stores</div><div>→</div><div>Customer uses it</div><div>→</div><div>Support + updates</div></div>')
    ticket = table(["Ticket status", "Meaning", "Who moves it"], [["Backlog", "Idea or task not planned yet", "PM"], ["To do", "Planned for this sprint, ready to start", "PM"], ["In progress", "You are working on it right now (max 2 at a time)", "You"],
                  ["In review", "Code finished; another person is checking it", "You → Reviewer"], ["In QA", "Merged to the test server; tester verifies it", "QA"], ["Done", "Meets the Definition of Done and tester approved", "QA / PM"], ["Blocked", "You cannot continue; you wrote why and who can unblock", "You"]])
    rhythm = table(["Meeting / routine", "When", "Length", "What to say or bring"], [["Daily stand-up", "Every morning", "15 min", "Yesterday I did…, today I will…, I am blocked by…"], ["Sprint planning", "Start of 2-week sprint", "1 h", "Ask questions until you understand every task"],
                   ["Demo", "End of sprint", "30 min", "Show working software, not slides"], ["Retrospective", "End of sprint", "30 min", "What went well, what to improve (be honest, be kind)"], ["Weekly report", "Friday", "10 min to write", "Template in Appendix"], ["1-to-1 with lead", "Weekly", "20 min", "Progress, learning goals, problems"]])
    env = table(["Environment", "Where it runs", "Who uses it", "Data", "Rule"], [["Local", "Your laptop", "You", "Fake", "Break things freely here"], ["Dev", "Company test server", "Developers", "Fake", "Updates automatically when code is merged"],
                ["Staging", "Copy of production", "QA, PM, customer demo", "Fake / anonymised", "Last stop before customers"], ["Production", "Live server", "Real customers", "<b>Real</b>", "Only approved releases; never experiment here"]])
    rules = [["Never put passwords, keys or customer data in code, chat or screenshots", "Use the secret manager; keys leak through Git history forever"], ["Use two-factor authentication on email, GitHub and cloud accounts", "Stolen password alone must not be enough"],
             ["Work only on company-approved tools and storage", "Client code on a personal drive is a legal and security problem"], ["One task = one branch = one pull request", "Small changes are easy to review and to undo"],
             ["Nobody merges their own code without a review", "A second pair of eyes catches mistakes"], ["Never test with real customer data", "Use synthetic (fake) data"], ["Write it down", "If it is not in the ticket, document or message, it did not happen"]]
    stuck = ol(["Try for <b>30 minutes</b>: read the error message completely, search it, check the docs.", "Write the problem in 3 lines: what you wanted, what happened, what you tried.", "Ask a <b>teammate</b> with that note.",
                "Still stuck after another 30 minutes: ask the <b>lead</b>. Mark the ticket <i>Blocked</i>.", "Production problem or customer data risk: tell the lead <b>immediately</b>, do not wait."])
    return (f"<h3>A.1 How a software company turns an idea into a product</h3>{flow}" + plain("software company", "It is a factory for software. Instead of machines it has <b>process</b>: the same steps for every project, so quality does not depend on luck.") +
            "<h3>A.2 Who is who</h3>" + org + "<p class=small>In a small team one person may wear several hats. The jobs still exist - ask which hat someone is wearing when they give you an instruction.</p>" +
            "<h3>A.3 How work moves (tickets)</h3>" + plain("ticket", "A small written task with a number, an owner, a deadline and a clear 'done' definition, stored in the project tracker. All work starts from a ticket.") + ticket +
            "<h3>A.4 The daily loop of every developer</h3>" + loop_map() + "<h3>A.5 Meetings and routines</h3>" + rhythm +
            "<h3>A.6 Four environments</h3>" + plain("environment", "A separate copy of the system for a separate purpose, so experiments never touch real customers.") + env +
            "<h3>A.7 Company rules you must follow from day one</h3>" + table(["Rule", "Why"], rules) + "<h3>A.8 When you are stuck - the stuck ladder</h3>" + stuck +
            callout("ok", "Professional habits that get noticed", "Update tickets before asked · reply to messages within working hours · say 'I do not know yet, I will find out by 3 pm' instead of staying silent · estimate honestly · admit mistakes early.")
            + "<h3>A.9 What a Gate is</h3>" + plain("gate", "A checkpoint between two stages. A manager checks your evidence and signs. No signature = you do not start the next stage. This protects the customer's money and your time.") +
            form("GATE APPROVAL FORM (copy for every gate)", [["Project / Stage / Gate", "PRJ-2026-001 · Stage __ · Gate __"], ["Prepared by / date", "________"], ["Evidence links", "1) ____ 2) ____ 3) ____"],
                 ["Checklist passed?", "☐ yes  ☐ no (list failures)"], ["Decision", "☐ Approved  ☐ Revise  ☐ Rejected"], ["Approver name / signature / date", "________"], ["Comments", "________"]]))

def folders():
    comp = snippet("""VEL-Software/                         <- shared company drive (backed up)
  00-Company/
    Policies/  Templates/  Brand/  Contracts/  Finance/(restricted)
  01-Projects/
    PRJ-2026-001-sitediary/           <- one folder per project; ID + short name
      01-Research/        market report, interview notes, evidence register
      02-Requirements/    PRD, screen specs, change requests
      03-Architecture/    tech decision (ADR), diagrams, threat model
      04-Design/          Figma links, exports, design tokens, usability report
      05-Source/          <- Git repository lives here (clone) - code is NOT stored in the drive copy
      06-QA/              test plan, test cases, test reports, security checklist
      07-Costing-Pricing/ cost sheet, price sheet, price-source screenshots
      08-Release/         store listings, screenshots, release notes, checklists
      09-Delivery/        manuals, training, handover form, customer sign-off
      10-Support/         tickets, incident reports, maintenance log
      99-Archive/         old versions, closed items""")
    mk = cmd("mkdir -p ~/VEL-Software/{00-Company/{Policies,Templates,Brand,Contracts},01-Projects}",
             "cd ~/VEL-Software/01-Projects",
             "mkdir -p PRJ-2026-001-sitediary/{01-Research,02-Requirements,03-Architecture,04-Design,05-Source,06-QA,07-Costing-Pricing,08-Release,09-Delivery,10-Support,99-Archive}",
             "ls PRJ-2026-001-sitediary")
    names = table(["Item", "Convention", "Example"], [["Project folder", "PRJ-YYYY-NNN-shortname", "PRJ-2026-001-sitediary"], ["Document", "<code>PRJ-ID_DocType_vMAJOR.MINOR_YYYY-MM-DD.ext</code>", "PRJ-2026-001_PRD_v1.0_2026-11-20.pdf"],
                   ["Git branch", "type/ticket-short-description", "feature/SD-14-attendance-form"], ["Git commit", "type(scope): message", "feat(S-014): add attendance form"], ["Release tag", "vMAJOR.MINOR.PATCH", "v1.0.0"],
                   ["Ticket ID", "PROJECTCODE-number", "SD-14"], ["Bug ID", "BUG-number", "BUG-231"]])
    repo = snippet("""sitediary/                          <- the Git repository (what developers share)
  README.md             <- what this project is + how to run it (read first!)
  CONTRIBUTING.md       <- team rules: branches, commits, reviews
  .gitignore            <- list of files Git must never store (secrets, build output)
  .env.example          <- NAMES of settings (no real passwords)
  .github/workflows/    <- automatic checks that run on every pull request
  mobile/               <- the phone app (Flutter)
    lib/
      core/             <- shared helpers: errors, constants, utilities
      config/           <- environment settings (dev / staging / prod URLs)
      services/         <- talks to outside things: API, storage, notifications
      models/           <- data shapes (Site, Report, Worker)
      repositories/     <- one place to read/write each kind of data (API or local DB)
      features/         <- one folder per feature: auth/, sites/, reports/, sync/
      widgets/          <- reusable UI pieces (buttons, cards)
      navigation/       <- screens and routes
      state/            <- app state management
    assets/             <- images, icons, fonts
    test/               <- automatic tests for logic and widgets
    integration_test/   <- tests that click through whole flows
  backend/              <- the server (NestJS)
    src/                <- server code, one folder per module (auth, sites, reports, files)
    tests/              <- server tests
    migrations/         <- database change scripts, numbered, never edited after merge
  admin-dashboard/      <- web admin (only if the requirements need it)
  docs/                 <- project knowledge that travels with the code
    requirements/ architecture/ api/ database/ ui-ux/ deployment/ testing/
  infra/                <- server and deployment configuration files""")
    return ("<h3>B.1 Folder system on the company drive</h3>" + plain("why folders matter", "A new colleague must find any document in under a minute. Same structure for every project = no searching, no confusion, easy audits.") +
            comp + "<p><b>Create it yourself</b> (Linux / macOS / Git Bash on Windows):</p>" + mk +
            "<h3>B.2 Naming rules</h3>" + names + do("Use lowercase and hyphens in folders, no spaces. Put the <b>version and date</b> in every document name. Never name files <i>final_final2.docx</i>.") +
            "<h3>B.3 Folder system inside the code repository</h3>" + repo +
            plain("repository (repo)", "The project's code folder with its full history, stored on GitHub. Everyone gets a copy (clone), makes changes on a branch, and merges them back through a pull request.") +
            dont("Do not store source code only on your laptop or in a zip file by email. If it is not in Git, it does not exist."))

def stage0():
    g = glance("1 - 2 days", "Your lead / mentor", "Laptop, email, GitHub, tracker, password manager", "Lead")
    steps_ = [("Get your accounts", "<p>Company email · GitHub (join the organisation) · project tracker · design tool · cloud console (view access only at first) · password manager. Turn on <b>two-factor authentication</b> on every one.</p>"),
              ("Prepare your laptop", "<p>Install: Git, VS Code, Node.js (LTS), Flutter SDK, Android Studio. macOS only for iOS: Xcode. Check each with the commands below.</p>" + cmd("git --version", "node -v", "flutter --version", "flutter doctor -v") + tip("<code>flutter doctor</code> shows a ✗ next to anything missing. Fix them one by one using its instructions.")),
              ("Set your Git identity", cmd('git config --global user.name "Your Name"', 'git config --global user.email "you@company.com"', "git config --global init.defaultBranch main")),
              ("Create your SSH key and add it to GitHub", "<p>This is your secure ID card for GitHub.</p>" + cmd('ssh-keygen -t ed25519 -C "you@company.com"', "cat ~/.ssh/id_ed25519.pub") + "<p>Copy the printed line into <i>GitHub → Settings → SSH and GPG keys → New SSH key</i>. Never share the file without <code>.pub</code>.</p>"),
              ("Create your folder system", "<p>Use the commands in section B.1. Put the project folder in the company drive.</p>"),
              ("Read the project documents", "<p>README.md, CONTRIBUTING.md and the requirements (PRD) of the project you join. Write 5 questions you still have and ask them in the first 1-to-1.</p>"),
              ("Do the 'hello' test", "<p>Clone the repo, run the app on an emulator or phone, change one text, make a branch, commit and open a pull request. This proves your setup works.</p>" + cmd("git clone git@github.com:<org>/<repo>.git", "cd <repo>/mobile", "flutter pub get", "flutter run"))]
    return stage("s0", 0, "Get ready", "plan", "Accounts, laptop, folders and rules - before any project work", g,
                 plain("stage 0", "Nothing is built here. You are only making sure you can work safely and others can find your work.") + "<h3>Steps</h3>" + steps(steps_, "plan") +
                 output("Screenshot of <code>flutter doctor</code> with no ✗ for your targets · your first merged pull request · folder structure created · accounts list ticked") +
                 "<h3>Quality check</h3>" + checklist(["2FA on all accounts", "flutter doctor clean for chosen targets", "SSH key works (<code>ssh -T git@github.com</code>)", "Folder structure created exactly as B.1", "I know who my lead and mentor are"]) +
                 "<h3>Common mistakes</h3>" + ul(["Skipping 2FA 'for now'", "Installing tools from random websites - use official downloads only", "Saving code in Downloads or Desktop"]))

def stage1():
    g = glance("1 - 2 weeks", "PM, Founder, 10+ real people", "Google Play, App Store, Google Trends, Reddit, G2/Capterra, spreadsheet", "Founder (Gate 1)")
    st = [("Write the idea in one sentence", form("PROBLEM STATEMENT", [["Who has the problem?", "Site supervisors in small construction firms"], ["What is the problem?", "Daily site records are lost or late because of poor network and paper"], ["How do they solve it today?", "Paper notebooks, WhatsApp photos"], ["What would success look like?", "Owner receives a complete report every evening"]])),
          ("Find existing apps", "<p>Search Google Play and the App Store with 5 different keywords. Open the 5 best apps. Write: name, price, rating, number of reviews, last update date, main features.</p>"),
          ("Read the complaints", "<p>Open each app's 1-star and 2-star reviews. Tag each complaint: <i>missing feature · price · bug · support</i>. Count them. The most common tag is your opportunity.</p>"),
          ("Talk to 10 real customers", ol(["Ask for 30 minutes. Do not sell. Ask how they work today.", "Questions: What was the last time this problem happened? What did it cost? What do you use now? What would you pay?", "Write notes the same day. Ask for a 60-day pilot or a small deposit at the end."])),
          ("Write every fact in the Evidence Register", table(["ID", "Fact", "Source URL", "Date accessed", "Confidence"], [["E-001", "Competitor X charges ₹___/month", "https://…", "2026-11-05", "High (official page)"]]) + dont("Never write a number without a source. If you cannot find it, write <b>Unknown - research needed</b>.")),
          ("Estimate market size bottom-up", formula("TAM = number of possible customers × yearly revenue per customer\nSAM = the part you can reach in your language/region/channel\nSOM = the part you can realistically win in 3 years") + "<p>Use sourced counts (e.g. government or industry numbers). Show your arithmetic.</p>"),
          ("Decide how it makes money", table(["Model", "Use when"], [["Subscription", "Customers get value every month (recommended default)"], ["One-time paid app", "Single-use tool"], ["Freemium", "Free users cost almost nothing to serve"], ["Enterprise contract", "Large buyers, custom needs"]])),
          ("Score the idea and recommend", "<p>Use the scorecard in the OS document (§3): ≥70 Go · 50-69 Pivot · &lt;50 Stop. Write one page: recommendation and why.</p>"),
          ("Present and get Gate 1", "<p>Send the report to the Founder. Gate 1 is approved only with evidence and interview proof.</p>")]
    return stage("s1", 1, "Business check", "plan", "Is this worth building? Find out before spending money", g,
                 plain("why research first", "The most expensive mistake is building something nobody pays for. A week of research can save months of work.") + "<h3>Steps</h3>" + steps(st, "plan") +
                 output("Market Research Report (PDF) · Evidence Register (sheet) · interview notes · go/pivot/stop scorecard") +
                 "<h3>Quality check</h3>" + checklist(["Every number has URL + date + confidence", "≥ 10 interviews done and noted", "≥ 5 competitors compared", "Bottom-up sizing shows its formula", "Recommendation is clear"]) +
                 "<h3>Common mistakes</h3>" + ul(["Asking friends who are not customers", "Copying statistics from random blogs", "Falling in love with the idea and ignoring the score"]))

def stage2():
    g = glance("1 - 2 weeks", "PM, designer, tech lead, QA", "Document editor, tracker, spreadsheet", "Founder + customer (Gate 2)")
    st = [("List users and roles", "<p>Who uses the app? (Owner, Manager, Supervisor, Worker). What may each role do? Draw a table: capability × role (see OS §4).</p>"),
          ("List the modules (big features)", "<p>Example: Login · Sites · Daily report · Photos · Offline sync · Notifications · Admin. Mark each <b>Must / Should / Could / Won't</b> (MoSCoW). The first release is only the Musts.</p>"),
          ("Draw the screen list", "<p>Give every screen an ID: S-001 Login, S-010 Site list, S-014 Attendance … This ID is used in tickets, tests and code.</p>"),
          ("Fill one Screen Sheet per screen", form("SCREEN SHEET - S-014 Attendance", [["Purpose / role", "Record who worked today / Supervisor"], ["Inputs", "worker (pick list, required) · status (present/absent/half) · hours (0-16, step 0.5) · note (≤200 characters)"],
              ["Validation", "hours required if present; no duplicate worker per day"], ["Buttons", "Save → store on phone + send later · Submit → lock the day"], ["Calls API", "POST /v1/reports/{id}/attendance"], ["Database", "UPSERT attendance"],
              ["Errors", "E-4101 'Hours must be between 0 and 16'"], ["Loading / offline", "Skeleton rows · works fully offline · 'Pending sync' badge"]])),
          ("Write what happens when things fail", table(["Situation", "App must…"], [["No internet", "Keep working; save locally; show 'Offline'"], ["Slow internet", "Show saved data first; retry; never lose typed data"], ["Server error", "Friendly message + Retry"], ["Session expired", "Save draft, ask login, restore draft"], ["Wrong password", "Generic message; lock after 5 tries"], ["Phone restarted", "Reopen on same screen with draft"]])),
          ("Review with everyone", "<p>Designer checks screens, tech lead checks feasibility, QA checks that every rule can be tested. Fix all open questions.</p>"),
          ("Freeze the scope", "<p>Tag the document <code>PRD v1.0 - FROZEN</code>. From now on any new idea needs a <b>Change Request</b>.</p>" + form("CHANGE REQUEST", [["CR number / date", "CR-001 / ____"], ["Requested by", "____"], ["What and why", "____"], ["Hours and cost (use rate card)", "____"], ["Delay (days)", "____"], ["Decision", "☐ Approve ☐ Reject ☐ Later  - signed ____"]]))]
    return stage("s2", 2, "Requirements", "plan", "Write exactly what will be built - and freeze it", g,
                 plain("scope freeze", "Agreeing in writing what is included. It protects the customer from surprise bills and the team from endless changes.") + "<h3>Steps</h3>" + steps(st, "plan") +
                 output("PRD v1.0 (frozen) · all Screen Sheets · screen→API→database table · failure behaviour table · signed Gate 2") +
                 "<h3>Quality check</h3>" + checklist(["Every screen has an ID and a sheet", "Every input has type, validation, required/optional", "Offline rules written per feature", "Roles and permissions table complete", "Customer or Founder signed"]) +
                 "<h3>Common mistakes</h3>" + ul(["'The developer will figure it out' - write it", "Adding features during build without a change request", "Forgetting error and empty screens"]))
