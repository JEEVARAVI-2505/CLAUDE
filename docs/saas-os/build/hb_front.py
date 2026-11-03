"""Handbook front parts: how the company works, folders, rules, glossary, learning path, schematics."""
from sections_a import table, callout

# ---------- SVG schematics ----------
PH = {"Discover": "#0b5cff", "Define": "#6a3df0", "Build": "#0f7a4a", "Verify": "#b45f06", "Launch": "#c2185b", "Operate": "#37474f"}
ST = [(0,"Get ready","Discover"),(1,"Check the idea","Discover"),(2,"Write what we build","Define"),(3,"Plan how it works","Define"),
      (4,"Design screens","Define"),(5,"Set up project","Build"),(6,"Build it","Build"),(7,"Test it","Verify"),(8,"Secure it","Verify"),
      (9,"Cost & price","Launch"),(10,"Put it online","Launch"),(11,"Show, sell, deliver","Launch"),(12,"Keep it running","Operate")]

def flow_svg():
    W, bw, bh, gx, gy = 980, 168, 74, 26, 60; per = 5
    out = [f'<svg viewBox="0 0 {W} 436" role="img" aria-label="Stage by stage flow from Stage 0 to Stage 12" xmlns="http://www.w3.org/2000/svg" font-family="system-ui,Segoe UI,Arial">']
    out.append('<defs><marker id="ar" markerWidth="9" markerHeight="9" refX="8" refY="4.5" orient="auto"><path d="M0,0 L9,4.5 L0,9 z" fill="#5b6577"/></marker></defs>')
    pos = []
    for i, (n, t, ph) in enumerate(ST):
        r, c = divmod(i, per)
        if r % 2: c = per-1-c
        x = 16 + c*(bw+gx); y = 14 + r*(bh+gy+14); pos.append((x, y))
    for i in range(len(ST)-1):
        (x1, y1), (x2, y2) = pos[i], pos[i+1]
        if y1 == y2:
            if x2 > x1: out.append(f'<line x1="{x1+bw}" y1="{y1+bh/2}" x2="{x2-2}" y2="{y2+bh/2}" stroke="#5b6577" stroke-width="2" marker-end="url(#ar)"/>')
            else: out.append(f'<line x1="{x1}" y1="{y1+bh/2}" x2="{x2+bw+2}" y2="{y2+bh/2}" stroke="#5b6577" stroke-width="2" marker-end="url(#ar)"/>')
        else:
            cx = x1+bw/2; out.append(f'<line x1="{cx}" y1="{y1+bh}" x2="{cx}" y2="{y2-2}" stroke="#5b6577" stroke-width="2" marker-end="url(#ar)"/>')
    for (n, t, ph), (x, y) in zip(ST, pos):
        col = PH[ph]
        out.append(f'<rect x="{x}" y="{y}" width="{bw}" height="{bh}" rx="10" fill="#fff" stroke="{col}" stroke-width="2.5"/>')
        out.append(f'<rect x="{x}" y="{y}" width="{bw}" height="22" rx="10" fill="{col}"/><rect x="{x}" y="{y+12}" width="{bw}" height="10" fill="{col}"/>')
        out.append(f'<text x="{x+10}" y="{y+16}" font-size="12.5" font-weight="700" fill="#fff">STAGE {n}</text>')
        if n: out.append(f'<text x="{x+bw-10}" y="{y+16}" font-size="11.5" font-weight="700" fill="#fff" text-anchor="end">Gate G{n}</text>')
        out.append(f'<text x="{x+bw/2}" y="{y+46}" font-size="14" font-weight="700" fill="#14213d" text-anchor="middle">{t}</text>')
        out.append(f'<text x="{x+bw/2}" y="{y+64}" font-size="11" fill="#5b6577" text-anchor="middle">{ph}</text>')
    lx = 16
    for k, col in PH.items():
        out.append(f'<rect x="{lx}" y="408" width="14" height="14" rx="3" fill="{col}"/><text x="{lx+20}" y="420" font-size="12" fill="#14213d">{k}</text>'); lx += 110
    out.append('</svg>'); return "".join(out)

def gate_svg():
    steps = [("1. DO","Follow the steps\nin the stage"),("2. SELF-CHECK","Tick every box\nin the checklist"),("3. PEER REVIEW","A colleague checks\nyour work"),("4. SIGN-OFF","Approver signs\nthe gate"),("5. NEXT STAGE","Start only now")]
    o = ['<svg viewBox="0 0 980 190" xmlns="http://www.w3.org/2000/svg" font-family="system-ui,Segoe UI,Arial"><defs><marker id="a2" markerWidth="9" markerHeight="9" refX="8" refY="4.5" orient="auto"><path d="M0,0 L9,4.5 L0,9 z" fill="#5b6577"/></marker></defs>']
    for i, (t, d) in enumerate(steps):
        x = 10 + i*196; col = "#0f7a4a" if i == 4 else "#0b5cff"
        o.append(f'<rect x="{x}" y="20" width="170" height="90" rx="10" fill="#fff" stroke="{col}" stroke-width="2.5"/>')
        o.append(f'<text x="{x+85}" y="48" text-anchor="middle" font-size="13.5" font-weight="700" fill="{col}">{t}</text>')
        for k, line in enumerate(d.split("\n")): o.append(f'<text x="{x+85}" y="{72+k*17}" text-anchor="middle" font-size="12.5" fill="#14213d">{line}</text>')
        if i < 4: o.append(f'<line x1="{x+170}" y1="65" x2="{x+194}" y2="65" stroke="#5b6577" stroke-width="2" marker-end="url(#a2)"/>')
    o.append('<path d="M 660 112 L 660 160 L 95 160 L 95 114" fill="none" stroke="#c0392b" stroke-width="2" stroke-dasharray="6 4" marker-end="url(#a2)"/>')
    o.append('<text x="380" y="153" text-anchor="middle" font-size="12.5" fill="#c0392b" font-weight="600">If rejected or "changes requested": fix the items listed, then repeat from step 2</text></svg>')
    return "".join(o)

def git_svg():
    o = ['<svg viewBox="0 0 980 230" xmlns="http://www.w3.org/2000/svg" font-family="system-ui,Segoe UI,Arial"><defs><marker id="a3" markerWidth="9" markerHeight="9" refX="8" refY="4.5" orient="auto"><path d="M0,0 L9,4.5 L0,9 z" fill="#5b6577"/></marker></defs>']
    o.append('<line x1="30" y1="45" x2="950" y2="45" stroke="#0f7a4a" stroke-width="5"/><text x="30" y="30" font-size="13" font-weight="700" fill="#0f7a4a">main  (always working; only changed through a reviewed Pull Request)</text>')
    for x in (90, 330, 600, 860): o.append(f'<circle cx="{x}" cy="45" r="9" fill="#0f7a4a"/>')
    o.append('<path d="M 90 54 C 130 100, 150 130, 190 130 L 440 130 C 500 130, 560 100, 600 54" fill="none" stroke="#0b5cff" stroke-width="3.5" marker-end="url(#a3)"/>')
    for x in (230, 330, 420): o.append(f'<circle cx="{x}" cy="130" r="7" fill="#0b5cff"/>')
    o.append('<text x="260" y="160" font-size="12.5" fill="#0b5cff" font-weight="700">feature/12-booking-page  (your work; many small commits)</text>')
    o.append('<path d="M 330 54 C 360 90, 380 190, 420 190 L 560 190 C 620 190, 680 150, 720 60" fill="none" stroke="#b45f06" stroke-width="3" stroke-dasharray="5 4"/>')
    o.append('<text x="440" y="208" font-size="12.5" fill="#b45f06" font-weight="700">fix/18-login-error  (bug fix, same process)</text>')
    o.append('<text x="610" y="95" font-size="12.5" fill="#14213d">① Pull Request opened  ② CI runs  ③ review  ④ merge</text></svg>')
    return "".join(o)

# ---------- Content ----------
def company_works():
    h = "<h3>1.1 What a software company really is</h3><p>A software company turns <b>a customer's problem</b> into <b>a working product</b> and then <b>keeps it working</b> while customers pay for it. Everything in this handbook exists to make that repeatable: the same steps, in the same order, with the same checks, whoever does the work. Corporate work is not about being clever alone; it is about being <b>predictable, documented and reviewed</b>.</p>"
    h += "<h3>1.2 Who does what (roles)</h3>" + table(["Role","In plain words","Typical output","You report to"],[
      ("Managing Director / Owner","Owns the company, money and final yes/no on big decisions","Approvals, budget, priorities","–"),
      ("Product Owner (PO)","Speaks for the customer; decides <i>what</i> gets built and in what order","PRD, priorities, acceptance of work","Owner"),
      ("Tech Lead","Decides <i>how</i> it is built; guards quality and security","Architecture, code reviews, technical approvals","Owner"),
      ("Developer (frontend / backend / full-stack)","Writes and tests the code","Features, tests, documentation","Tech Lead"),
      ("UI/UX Designer","Designs how the product looks and feels","Wireframes, designs, prototype","PO"),
      ("QA Engineer","Tries to break the product before customers do","Test plans, bug reports, QA report","Tech Lead"),
      ("DevOps Engineer","Runs servers, deployment, monitoring and backups","Pipelines, environments, uptime","Tech Lead"),
      ("Support Lead","First contact for customers after launch","Tickets, onboarding, training","PO"),
      ("Sales / Business Development","Finds and signs customers","Leads, demos, proposals","Owner")])
    h += "<p class='note'>In a small startup one person can hold several roles. The <b>responsibilities</b> stay separate: the person who wrote the code never approves their own code.</p>"
    h += "<h3>1.3 A normal working day</h3>" + table(["Time","What happens","Why"],[
      ("09:30 – 09:45","<b>Stand-up</b>: each person says what they did yesterday, what they do today, and what blocks them (max 1 minute each)","Everyone sees problems early"),
      ("09:45 – 13:00","<b>Focused work</b> on your ticket. Chat is for questions, not conversations; mute notifications if needed","Deep work produces quality"),
      ("13:00 – 14:00","Lunch",""),
      ("14:00 – 17:30","Focused work, code reviews you owe to others, meetings","Review is part of the job, not extra"),
      ("17:30 – 17:45","<b>Update the board and timesheet</b>: move your ticket, log hours against Project ID + stage, write a 2-line note","Management can see progress without asking you"),
      ("Friday 16:00","<b>Weekly demo + review</b>: show working software on staging; fill weekly report","Progress is proven by working software, not by words")])
    h += "<p class='note'>Hours are an example. Use the actual working hours of your company, but keep the rhythm: stand-up, focus, review, update.</p>"
    h += "<h3>1.4 Meeting rhythm</h3>" + table(["Meeting","How often","Who","Output"],[
      ("Stand-up","Daily, 15 min","Whole team","Blockers list"),("Weekly review","Weekly, 30–45 min per project","Owner, PO, Tech Lead, team","Status, risks, decisions"),
      ("Demo","Weekly","Team + PO (+ customer)","Accepted / rejected features"),("Retrospective","Every 2–4 weeks","Team","3 things to keep, 3 to change"),
      ("Cost & KPI review","Monthly","Owner, PO, Tech Lead","Cost vs plan, MRR, churn, margin")])
    h += "<h3>1.5 How work flows: from request to done</h3><ol><li>Work starts as a <b>ticket</b> (an item on the board) written by the PO: what, why, acceptance criteria.</li><li>Board columns: <b>Backlog → To Do → In Progress → In Review → In Test → Done</b>. Only move a ticket when the exit rule of its column is true.</li><li>A ticket in <b>In Progress</b> has your name, a branch, and an estimate in hours. Nobody works on something without a ticket.</li><li>A ticket reaches <b>Done</b> only when it meets the <b>Definition of Done</b> (section 3).</li><li>New ideas during work are <b>not</b> built. They become a new ticket or a Change Request for the PO to decide.</li></ol>"
    h += "<h3>1.6 Environments: where the software lives</h3>" + table(["Environment","Who uses it","Data","Rule"],[
      ("<b>Local</b> (your computer)","You","Fake / seed data","Break things freely"),("<b>Staging</b> (test copy online)","Team, PO, QA, customer demo","Fake or authorised data","Every merge lands here first"),
      ("<b>Production</b> (the real thing)","Customers","Real customer data","<b>Never</b> edit directly; only deploy through the pipeline after approval")])
    h += callout("warn","Golden rule of production","Real customer data is a legal and ethical responsibility. Never copy production data to your laptop, never paste it in chat, never use it in demos without written permission.")
    h += "<h3>1.7 Documents, tickets and code: where truth lives</h3>" + table(["Thing","Lives in","Never in"],[
      ("Decisions, requirements, designs","Documents folder / repo <code>docs/</code>, versioned","Chat messages, memory"),("Tasks, bugs, progress","Board (tickets)","Private notes"),
      ("Code","GitHub repository","Email, USB, personal cloud"),("Passwords and API keys","Company password manager and provider secret stores","Chat, email, code, screenshots"),
      ("Time spent","Timesheet","Memory at month end")])
    h += "<h3>1.8 Professional communication</h3><ul><li><b>Write so someone can act without asking you again</b>: context, what you did, what you need, by when.</li><li>Ask for help after <b>30 minutes</b> of honest effort. Bring: what you wanted, what you tried, the exact error text, a screenshot.</li><li>Bad news early is a small problem; bad news late is a big one. Say 'I am blocked' or 'this will take longer' as soon as you know.</li><li>Be polite and specific in code reviews. Review the code, never the person.</li><li>Answer messages within the same working day; acknowledge long ones with 'seen, will reply by 4 pm'.</li></ul>"
    h += "<h3>1.9 Confidentiality and conduct</h3><ul><li>Customer data, source code, prices, contracts and plans are <b>confidential</b>. Do not share them outside the company.</li><li>Use only company-approved tools and accounts for company work. Do not use personal email or personal cloud storage.</li><li>Do not install pirated software; use only licences the company owns or free, permissively licensed software approved by the Tech Lead.</li><li>Report any suspected security problem immediately to the Tech Lead, even if it was your mistake. Hiding it is the real offence.</li><li>Credit: this system is owned by VEL Software Solutions; do not copy it out of the company.</li></ul>"
    return h

def folders():
    h = "<p>A tidy structure is how a team finds things at 3 pm on a Friday. Use these structures <b>exactly</b>. Folder names are lowercase with hyphens, no spaces.</p>"
    h += "<h3>2.1 Company shared drive</h3><pre>VEL-Software-Solutions/\n├─ 00-company/              policies, templates, brand (logo), handbook\n│   ├─ templates/           the 20 standard templates (copy, never edit the original)\n│   └─ policies/            security, code of conduct, leave\n├─ 01-clients/              one folder per customer\n│   └─ acme-clinic/         contracts, SOWs, meeting notes, acceptance forms\n├─ 02-projects/             one folder per project, named P001-slotwise\n│   └─ P001-slotwise/\n│       ├─ 01-research/     market report, competitor sheet, interview notes\n│       ├─ 02-requirements/ PRD, input-output spec, scope freeze, change requests\n│       ├─ 03-architecture/ diagrams, ERD, API list, threat model\n│       ├─ 04-design/       wireframes, designs, prototype link, usability notes\n│       ├─ 05-qa-security/  test plan, bug list, QA report, security report\n│       ├─ 06-finance/      cost model, pricing model, invoices\n│       ├─ 07-deployment/   domain/DNS report, deployment report, runbook\n│       ├─ 08-sales/        demo script, proposal, demo recording\n│       ├─ 09-handover/     manuals, training slides, acceptance form\n│       └─ 10-maintenance/  monthly reports, incident reports\n├─ 03-finance/              company-level accounts, tax, subscriptions\n└─ 04-hr/                   timesheets, onboarding checklists (restricted access)</pre>"
    h += "<h3>2.2 Your computer</h3><pre>~/work/                        (Windows: C:\\work\\)\n├─ P001-slotwise/              the Git repository you cloned (see 2.3)\n├─ training-sandbox/           practice repository\n└─ scratch/                    throw-away experiments (never company data)</pre><p>Keep code <b>outside</b> synced personal folders (Desktop, personal OneDrive/Google Drive). Real work is saved by <b>committing and pushing to GitHub</b>, not by copying folders.</p>"
    h += "<h3>2.3 Inside a project repository (read every line once)</h3><pre>slotwise/\n├─ apps/\n│   ├─ web/                  the website users see (frontend)\n│   └─ api/                  the server logic (backend) – may live inside web/ for small projects\n├─ packages/\n│   ├─ ui/                   reusable buttons, forms, tables\n│   └─ shared/               types and helpers used by web and api\n├─ docs/\n│   ├─ requirements/         PRD, input-output spec\n│   ├─ architecture/         diagrams, decisions (ADR-001.md ...)\n│   ├─ database/             ERD, data dictionary\n│   ├─ api/                  endpoint list / OpenAPI file\n│   ├─ deployment/           deploy steps, runbook, rollback\n│   └─ testing/              test plan, QA reports\n├─ tests/\n│   ├─ unit/                 test one small function\n│   ├─ integration/          test API + database together\n│   └─ e2e/                  test a whole user journey in a browser\n├─ infra/                    server/pipeline configuration files\n├─ .github/\n│   ├─ workflows/ci.yml      automatic checks on every Pull Request\n│   ├─ ISSUE_TEMPLATE/       bug / feature forms\n│   └─ pull_request_template.md\n├─ .env.example              list of setting NAMES with fake values (committed)\n├─ .env                      your real local settings (NEVER committed)\n├─ .gitignore                files Git must ignore (node_modules, .env, builds)\n├─ README.md                 what this is and how to run it in 30 minutes\n└─ CONTRIBUTING.md           how to work on it (branches, commits, reviews)</pre>"
    h += "<h3>2.4 Naming rules</h3>" + table(["Thing","Rule","Example"],[
      ("Project ID","P + 3 digits, then short name","P001-slotwise"),("Branch","type/ticket-short-name","feature/12-booking-page · fix/18-login-error · hotfix/21-payment-crash"),
      ("Commit message","type: what changed (present tense, ≤72 chars)","feat: add slot conflict check"),("Document file","ProjectID_Document_vMajor.Minor_YYYY-MM-DD","P001_PRD_v1.0_2026-11-03.pdf"),
      ("Database table","lowercase, plural, snake_case","appointments, patients"),("Code files / components","As per the language convention used in the repo","BookingForm.tsx"),
      ("Change Request","CR-ProjectID-number","CR-P001-004")])
    h += "<p>Commit types: <code>feat</code> new feature · <code>fix</code> bug fix · <code>docs</code> documentation · <code>test</code> tests · <code>refactor</code> restructure without behaviour change · <code>chore</code> tooling.</p>"
    h += "<h3>2.5 Git workflow in one picture</h3>" + '<div class="fig">' + git_svg() + '</div>'
    return h

def rules():
    L = [("Never work without a ticket.","If it is not on the board, it is not approved work."),
         ("Never commit passwords, keys or customer data.","Use environment variables. If you commit one by accident, tell the Tech Lead at once; the secret must be replaced, not just deleted."),
         ("Never push straight to main.","Always branch → Pull Request → review → merge."),
         ("Never edit production by hand.","Only through the approved deployment pipeline."),
         ("Never approve your own work.","A second person reviews every change and every gate."),
         ("Never guess a price, a DNS value or a legal clause.","Open the official page, copy the exact value, save the link and date."),
         ("Never skip a gate.","No coding before G2; no launch before G7, G8, G10."),
         ("Never change scope silently.","New ideas go through a Change Request."),
         ("Never claim 'done' without proof.","Done = passes acceptance criteria + tested + reviewed + documented + demonstrated."),
         ("Always log your time and update the board daily.","It is how the company is paid and how projects are priced.")]
    return table(["#","Rule","What it means for you"], [(i+1, f"<b>{a}</b>", b) for i, (a, b) in enumerate(L)])

def dod():
    return table(["Definition of Done – every task","Tick"],[(x,"☐") for x in ["Acceptance criteria all pass (shown, not claimed)","Automatic tests written and passing","Code reviewed and approved by a second person","No new warnings from lint / security checks","Merged through a Pull Request; branch deleted","Works on staging (not only on your laptop)","Documentation / README updated if behaviour changed","Time logged and ticket moved to Done"]])

GL = [("API","A set of doors a program offers so other programs can ask it for data or actions."),("Acceptance criteria","Plain 'Given/When/Then' sentences that say exactly when a feature counts as correct."),("Backend","The hidden server part: rules, database access, security."),("Frontend","What the user sees and clicks in the browser."),("Branch","A separate copy of the code where you work without disturbing others."),("CI (Continuous Integration)","Robot that runs checks on every change automatically."),("Commit","A saved snapshot of your changes with a message."),("CDN","Network of servers that deliver files quickly from near the user."),("Database","Organised storage of data (tables of rows and columns)."),("Deploy","Putting a version of the software on a server where users can use it."),("DNS","The internet's phone book: turns example.com into a server address."),("Environment variable","A setting (like a password) kept outside the code."),("ERD","Diagram of database tables and how they link."),("Gate","A mandatory approval before the next stage starts."),("HTTPS / TLS","Encrypted connection that protects data between browser and server."),("Input validation","Checking data is allowed before using it."),("Merge","Combining a branch into main."),("Migration","A script that changes the database structure safely and repeatably."),("MVP","Smallest product that delivers real value to a first customer."),("MFA","Second proof of identity (app code) besides the password."),("MRR / ARR","Monthly / Annual recurring revenue from subscriptions."),("CAC","Cost to win one new paying customer."),("Churn","Share of customers who cancel in a period."),("Modular monolith","One application, organised inside into clear modules."),("Pull Request (PR)","A request to merge your branch; where review happens."),("RBAC","Role-based access control: what each role may do."),("Repository (repo)","A project folder tracked by Git."),("Rollback","Returning to the previous working version."),("SaaS","Software sold as a subscription over the internet."),("SLA","Written promise about service levels (uptime, response time)."),("Smoke test","Quick check that the main functions work after a deployment."),("Staging","Online test copy of production."),("Tenant","One customer organisation using a shared product."),("Ticket","A tracked task or bug."),("TAM / SAM / SOM","Total / reachable / winnable market."),("Unit test","Automatic test of one small piece of code."),("User story","'As a <role> I want <action> so that <benefit>.'"),("Version tag","A label such as v1.2.0 on a release."),("WCAG","International accessibility guidelines."),("Webhook","A message another service sends your app when something happens.")]

def glossary():
    return table(["Term","Plain-English meaning"], [(f"<b>{a}</b>", b) for a, b in GL])

def learning():
    rows = [("Week 1","Computers & terminal basics; Git","Pro Git book (free)","https://git-scm.com/book/en/v2","Clone, branch, commit, push, open a PR in the practice repo"),
            ("Week 2","HTML, CSS, how the web works","MDN Learn web development","https://developer.mozilla.org/en-US/docs/Learn_web_development","Build a static page, responsive at 360 px"),
            ("Week 3","JavaScript / TypeScript","MDN JavaScript; TypeScript Handbook","https://www.typescriptlang.org/docs/handbook/intro.html","Small functions with unit tests"),
            ("Week 4","React","React Learn","https://react.dev/learn","Form component with validation"),
            ("Week 5","Next.js","Next.js Learn / docs","https://nextjs.org/learn","Multi-page app with one API route"),
            ("Week 6","Databases & SQL","PostgreSQL tutorial and docs","https://www.postgresql.org/docs/current/tutorial.html","Design 3 tables, write joins"),
            ("Week 7","APIs & auth","MDN HTTP; OWASP Cheat Sheets","https://cheatsheetseries.owasp.org/","Login with hashed password, role check"),
            ("Week 8","Testing","Playwright / framework docs","https://playwright.dev/docs/intro","One unit, one integration, one end-to-end test"),
            ("Week 9","Security","OWASP Top 10, ASVS","https://owasp.org/www-project-top-ten/","Run the security checklist on your app"),
            ("Week 10","Deploy, DNS, HTTPS","Cloudflare Learning Center; hosting docs","https://www.cloudflare.com/learning/dns/what-is-dns/","Deploy to staging with a custom subdomain"),
            ("Week 11","Accessibility & performance","web.dev Learn; WCAG 2.2","https://web.dev/learn","Lighthouse score ≥ 90 on your page"),
            ("Week 12","Business basics: SaaS metrics","This handbook, Appendix B","","Compute break-even for a sample plan")]
    t = table(["When","Topic","Official / free source","Link","Proof you learned it"], [(a,b,c,(f'<a href="{d}">{d}</a>' if d else "–"),e) for a,b,c,d,e in rows])
    return t + callout("warn","Verify","Links are starting points to official documentation; open each, confirm it is current and record the date in your learning log. Learning time is working time: agree 5 hours per week with your Tech Lead.")

def guide_use():
    return table(["If you are...","Read first","Then"],[
      ("New to the company (Day 1)","Parts 1–5 (about 2 hours)","Stage 0, then ask your Tech Lead for your first ticket"),
      ("Starting a new product","Stage 1 → Stage 2","Do not open an editor until Gate G2 is signed"),
      ("A developer on a ticket","Stage 5, Stage 6, Appendix A (commands)","Stage 7 when the feature is done"),
      ("Preparing to launch","Stages 8, 9, 10","Never launch without G7, G8, G9, G10"),
      ("After the customer goes live","Stage 11, Stage 12","Use the monthly checklists"),
      ("Stuck","Section 3.3 (how to ask for help)","")])
