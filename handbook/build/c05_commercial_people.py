from lib import Chapter
from costing import (EXAMPLES, COST_PER_DAY, PM_COST_PER_DAY, PM_RATIO, TARGET_MARGIN, RATIO, compute, inr, change_request)


def build():
    out = []
    # ------------------------------------------------------------ Chapter 8
    c = Chapter("8", "Costing and Commercial Model",
                "Price = **cost to deliver ÷ (1 − target gross margin)**, built bottom-up from effort, never from what the client “can afford”. "
                "All figures below are **worked illustrations with internal assumptions** (INR, ex-GST); recalibrate rates from timesheets each quarter.",
                search="costing price margin break-even change request support hosting small medium enterprise module-wise")
    c.callout("warn", "Vendor pricing — verify before quoting",
              "Hosting, database, licence and SaaS prices change. The hosting and licence lines below are **allowances**, not vendor quotes. "
              "Before any quotation, price the real services in the official calculators — [AWS Pricing Calculator](https://calculator.aws/), "
              "[Azure Pricing Calculator](https://azure.microsoft.com/pricing/calculator/), [Google Cloud Pricing Calculator](https://cloud.google.com/products/calculator), "
              "[GitHub Plans & pricing](https://github.com/pricing), [Microsoft SQL Server pricing](https://www.microsoft.com/sql-server/sql-server-2022-pricing) — "
              "and record the date, region, SKU and screenshot in the quote file. GST/tax treatment must be confirmed with the finance owner and official tax portal.")
    c.h2("Cost components (all 17 must appear in every estimate)")
    c.table(["Component", "How it is estimated", "Cost basis"], [
        ["Discovery", f"{RATIO['Discovery']:.0%} of development days (min 3 days)", "BA day rate"],
        ["Business analysis", f"{RATIO['Business analysis']:.0%} of dev days", "BA day rate"],
        ["Architecture", f"{RATIO['Architecture']:.0%} of dev days", "Technical Lead day rate"],
        ["UI/UX", f"{RATIO['UI/UX']:.0%} of dev days", "UX/Frontend day rate"],
        ["Development", "Σ module days (3-point estimate: (O + 4M + P) / 6)", "Developer day rate"],
        ["Database", f"{RATIO['Database']:.0%} of dev days", "DB Engineer day rate"],
        ["Integration", "Per interface: simple 3 d, medium 6 d, complex 12 d", "Integration Engineer day rate"],
        ["Migration", "Per source: 2 d + 1 d per 10 tables + cleansing allowance", "DB Engineer day rate"],
        ["QA", f"{RATIO['QA']:.0%} of dev days", "QA day rate"],
        ["Deployment", f"{RATIO['Deployment']:.0%} of dev days", "Tech Lead/DevOps day rate"],
        ["Training", f"{RATIO['Training']:.0%} of dev days + client sessions", "BA/Support day rate"],
        ["Documentation", f"{RATIO['Documentation']:.0%} of dev days", "BA/dev day rate"],
        ["Licences", "Itemised from vendor quote; client-owned where possible", "Pass-through + handling"],
        ["Hosting", "Staging + production from official calculators × months", "Pass-through + management fee"],
        ["Support & maintenance", "% of project price per year (see below)", "Support Engineer + developer time"],
        ["Future upgrades", "Reserved annual block for framework/DB/security upgrades", "Developer time"],
        ["Project management", f"{PM_RATIO:.0%} of all other effort days", f"PM day rate ({inr(PM_COST_PER_DAY)})"],
    ], widths=[22, 52, 26])
    c.h2("Assumed internal cost rates (INR / person-day)")
    c.table(["Activity", "Cost / day", "Billing rate card at 40 % margin"],
            [[k, inr(v), inr(v / (1 - TARGET_MARGIN))] for k, v in COST_PER_DAY.items()] +
            [["Project management", inr(PM_COST_PER_DAY), inr(PM_COST_PER_DAY / (1 - TARGET_MARGIN))]],
            widths=[40, 30, 30])
    c.callout("tip", "Formulas",
              ["Labour cost = Σ (days × cost/day). Contingency = labour × risk % (small 10 %, medium 15 %, enterprise 20 %).",
               f"Direct cost = labour + contingency + licences + build-period hosting. **Price = direct cost ÷ (1 − {TARGET_MARGIN:.0%}).**",
               "Gross margin % = (price − direct cost) ÷ price. Discounts reduce margin 1:1 — never discount without Department Head approval.",
               "Break-even projects per year = 12 × monthly non-billable overhead ÷ gross margin per project.",
               "Annual support = support % × project price; annual hosting = monthly hosting × 12 × 1.15 (management fee)."])

    for key in ("Small", "Medium", "Enterprise"):
        r = compute(key)
        e = r["e"]
        c.h2(f"Example — {key}: {e['name']}")
        c.p(f"Team {e['team']} · indicative build **{r['build_months']} month(s)** · {r['total_days']:.1f} person-days · {e['tools']}.")
        c.table(["Cost line", "Days", "Rate / day", "Cost"],
                [[n, f"{d:.1f}", inr(rt), inr(cost)] for (n, d, rt, cost) in r["lines"]] +
                [["**Labour subtotal**", f"**{r['total_days']:.1f}**", "", f"**{inr(r['labour'])}**"],
                 [f"Contingency ({e['contingency']:.0%})", "", "", inr(r["contingency"])],
                 ["Licences (allowance)", "", "", inr(e["licences"])],
                 [f"Build-period hosting ({r['build_months']} × {inr(e['hosting_month'])})", "", "", inr(r["hosting_build"])],
                 ["**Direct cost**", "", "", f"**{inr(r['direct'])}**"],
                 [f"**Project price (÷ 0.60)**", "", "", f"**{inr(r['price'])}**"],
                 ["Gross margin", "", "", f"{inr(r['margin'])} ({r['margin'] / r['price']:.0%})"]],
                widths=[46, 14, 18, 22])
        mp = r["module_price"]
        rows = [[m, str(d), inr(mp[m])] for m, d in e["modules"].items()]
        rows.append(["Integration + data migration (incl. contingency & margin)", f"{e['integration'] + e['migration']}", inr(r["shared_price"])])
        rows.append(["Licences + build hosting (with margin)", "—", inr(r["licence_hosting_price"])])
        rows.append(["**Total = project price**", f"**{sum(e['modules'].values())}**", f"**{inr(sum(mp.values()) + r['shared_price'] + r['licence_hosting_price'])}**"])
        c.h3("Module-wise price")
        c.table(["Module", "Dev days", "Price"], rows, widths=[58, 14, 28])
        c.h3("Recurring costs and year-1 view")
        c.table(["Item", "Basis", "Per year"], [
            ["Annual support", f"{e['support_pct']:.0%} of project price (SLA tiers P1–P4, monthly report)", inr(r["support"])],
            ["Hosting & monitoring", f"{inr(e['hosting_month'])}/month × 12 + 15 % management fee (verify with calculators)", inr(r["recurring_hosting"])],
            ["Planned upgrades", f"{e['upgrade_pct']:.0%} of project price reserved block", inr(r["upgrades"])],
            ["**Total recurring**", "", f"**{inr(r['annual_recurring'])}**"],
        ], widths=[26, 52, 22])
        c.p(f"**Break-even:** with assumed non-billable overhead of {inr(r['overhead_month'])}/month, the department must deliver "
            f"**{r['breakeven_projects']:.1f} projects of this size per year** to cover overhead from project margin alone "
            f"(recurring-support margin is extra). Blended cost/day {inr(r['blended_cost'])}; blended rate-card day {inr(r['rate_card_day'])}.")

    c.h2("Commercial rules")
    c.table(["Topic", "Policy"], [
        ["Payment schedule (default)", "30 % on SOW signature · 30 % on design approval (G4) · 30 % on UAT sign-off (G7) · 10 % on go-live + 30 days (G8/G9). Adjust only with Department Head approval."],
        ["Estimate validity", "30 days; re-verify vendor prices at signature."],
        ["Warranty", "Defects against signed requirements fixed free for 60–90 days after go-live; enhancements are change requests."],
        ["Discount authority", "≤ 5 %: Project Manager · ≤ 10 %: Department Head · > 10 %: Director. Margin floor 30 %."],
        ["Fixed price vs T&M", "Fixed price only after requirements freeze; T&M for discovery, legacy investigation and unclear scope."],
        ["Hosting/licences", "Client-owned accounts preferred; if resold, show pass-through and management fee separately."],
        ["Taxes", "Quotes are ex-tax; apply GST/other taxes per current law as confirmed by Finance."],
    ], widths=[26, 74])

    c.h2("Change-request pricing")
    c.ol(["Log the CR (ID, requester, description, reason, priority). The client SPOC must request in writing.",
          "BA performs impact assessment (scope, schedule, cost, risk, tests, docs) — charged at **0.5 day BA rate** as a minimum fee, credited if the CR is approved.",
          "Price = Σ(role days × rate card) + **20 % premium if raised after requirements freeze** (G2), 0 % if it replaces an equal-size in-scope item (scope swap).",
          "Client approves in writing; Project Manager updates SOW schedule and baseline; only then is work started."])
    cr = change_request("Medium", {"Business analysis": 1, "Development": 4, "QA": 2, "Database": 0.5})
    c.p("*Worked example (Medium project):* “Add a stock-transfer approval step between warehouses” — BA 1 d, Dev 4 d, QA 2 d, DB 0.5 d.")
    c.table(["Line", "Amount"], [
        ["Impact assessment (0.5 d BA at rate card)", inr(cr["assess"])],
        ["Work: 1×BA + 4×Dev + 2×QA + 0.5×DB at rate card", inr(cr["work"])],
        ["Post-freeze premium (20 % of work)", inr(cr["premium"])],
        ["**Change-request price (ex-tax)**", f"**{inr(cr['total'])}**"],
    ], widths=[70, 30])
    c.callout("safety", "Fixing the numbers",
              "These examples exist to teach the method. Do not quote a client from this chapter: build the real estimate with the **Costing Model Specification (Appendix L)**, "
              "get Technical Lead review of the days, and Finance review of margin.")
    out.append(c)

    # ------------------------------------------------------------ Chapter 9
    c = Chapter("9", "Employee Roles, Responsibility Matrix and Approvals",
                "Ten roles, each with defined responsibilities, entry level and approval authority. In a startup one person may hold several roles, "
                "but **the author of work is never its approver**.", search="roles responsibility matrix raci approval business analyst developer qa technical lead project manager support")
    c.table(["Role", "Primary responsibility", "Minimum level to work unsupervised", "Key outputs"], [
        ["**Business Analyst**", "Discovery, requirements, process maps, UAT preparation, change impact.", "Levels 1–2 + Level 3 basics", "BRD, FRS, stories, UAT scripts"],
        ["**Junior Software Developer**", "Implements assigned stories under review; writes tests.", "Level 3 · pairs with a senior", "PRs, unit tests"],
        ["**Backend Developer**", "APIs, domain logic, security, transactions, performance.", "Levels 3, 4(basic), 6", "Services, API docs, tests"],
        ["**Frontend Developer**", "UI, accessibility, UX prototypes, API client.", "Level 3 (frontend track), 6", "UI components, prototypes"],
        ["**Database Engineer**", "ERD, migrations, performance, backup/restore, data migration.", "Levels 4–5", "ERD, migrations, restore drills"],
        ["**Integration Engineer**", "Third-party and legacy interfaces, queues, idempotency.", "Levels 3, 5", "Adapters, contract tests"],
        ["**QA Engineer**", "Test strategy, automation, defect management, UAT support.", "Levels 1–2, 3 basics, 6", "Test plan, reports"],
        ["**Technical Lead**", "Architecture, standards, estimates, code/PR gatekeeping, mentoring.", "All levels + 3 delivered projects", "ADRs, reviews, approvals"],
        ["**Project Manager**", "Scope, plan, risks, budget, communication, gates, change control.", "Levels 1–2 + PM training", "Plans, status reports, change log"],
        ["**Support Engineer**", "Monitoring, tickets, patching, backups verification, monthly reports.", "Levels 4 basics, 6", "Tickets, reports, runbooks"],
    ], widths=[18, 38, 26, 18])

    c.h2("Responsibility matrix (RACI)")
    c.p("**R** = does the work · **A** = accountable (one per row; signs) · **C** = consulted · **I** = informed. Roles: "
        "BA, JD (Junior Dev), BD (Backend), FD (Frontend), DB, IE (Integration), QA, TL (Technical Lead), PM, SE (Support). "
        "The Department Head additionally signs gates G1, G3, G8 and G9 and is accountable above the matrix.")
    cols = ["Activity", "BA", "JD", "BD", "FD", "DB", "IE", "QA", "TL", "PM", "SE"]
    M = [
        ["Lead qualification", "R", "", "", "", "", "", "", "C", "A", ""],
        ["Discovery & process study", "R", "", "", "", "", "", "I", "C", "A", ""],
        ["AS-IS / TO-BE design", "R", "", "I", "I", "", "", "I", "C", "A", ""],
        ["FR / NFR / I-O / permissions", "R", "", "C", "C", "C", "C", "C", "A", "I", ""],
        ["Requirements freeze", "R", "", "I", "I", "I", "I", "I", "C", "A", ""],
        ["SOW & costing", "R", "", "C", "", "", "", "", "C", "A", ""],
        ["Software architecture", "C", "", "R", "C", "C", "C", "I", "A", "I", ""],
        ["Database architecture", "C", "", "C", "", "R", "C", "I", "A", "I", ""],
        ["UI/UX design", "C", "", "", "R", "", "", "C", "A", "I", ""],
        ["Development planning", "C", "I", "C", "C", "C", "C", "C", "R", "A", ""],
        ["Repository & CI setup", "", "I", "R", "C", "", "", "C", "A", "I", ""],
        ["Module development", "C", "R", "R", "R", "C", "C", "I", "A", "I", ""],
        ["API & integrations", "C", "", "C", "", "", "R", "C", "A", "I", ""],
        ["Migrations & data migration", "C", "", "C", "", "R", "", "C", "A", "I", ""],
        ["Security implementation", "", "R", "R", "R", "C", "C", "C", "A", "I", "I"],
        ["Testing", "C", "C", "C", "C", "C", "C", "R", "A", "I", ""],
        ["UAT", "R", "", "", "", "", "", "R", "C", "A", ""],
        ["Staging & production deploy", "", "", "R", "", "C", "", "C", "A", "I", "R"],
        ["End-user training", "R", "", "", "", "", "", "", "C", "A", "R"],
        ["Handover", "R", "", "R", "", "C", "", "C", "C", "A", "R"],
        ["Maintenance & upgrades", "", "C", "R", "C", "C", "C", "C", "A", "I", "R"],
    ]
    c.table(cols, M, cls="raci", widths=[28] + [7.2] * 10)

    c.h2("Approval structure and authority limits")
    c.table(["Decision", "Author / requester", "Approver", "Second reviewer / evidence"], [
        ["Pull request merge to `main`", "Developer", "Any peer with CODEOWNERS rights", "CI green; tests added; docs updated"],
        ["Architecture decision (ADR)", "Technical Lead / Backend", "Technical Lead of another project or Department Head", "Alternatives + cost of ownership"],
        ["Database migration to production", "Database Engineer", "Technical Lead", "Rehearsal log; verified backup"],
        ["Production deployment", "Developer/Support", "Technical Lead (client sponsor for go-live)", "Change record; rollback plan"],
        ["Security exception / risk acceptance", "Technical Lead", "Department Head (client for client-owned risk)", "Written risk statement with expiry date"],
        ["Quotation / SOW", "BA / PM", "Department Head (Director above ₹50 lakh — adjust to policy)", "Finance margin check; Legal review"],
        ["Change request", "Client SPOC", "PM (≤ 5 % of project price) · Department Head (> 5 %)", "Impact assessment"],
        ["Employee level pass", "Employee", "Technical Lead / BA lead for the track", "Quiz + demo + capstone review"],
        ["Release of client data / access", "Support Engineer", "Client data owner + Technical Lead", "Ticket with authorisation"],
    ], widths=[24, 20, 28, 28])
    c.callout("rule", "Segregation of duties",
              "The developer of a change cannot approve its PR; the person who deploys cannot be the sole approver of the change; "
              "the person who prepares a quote cannot approve it; production credentials are never shared.")
    out.append(c)
    return out
