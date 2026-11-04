"""QA metrics (with worked numbers) and costing / pricing (all figures computed, not typed)."""
from lib import *  # noqa: F401,F403


def inr(x: float, dec: int = 0) -> str:
    """Format with Indian digit grouping: 1234567 -> ₹12,34,567."""
    neg = x < 0
    s = f"{abs(x):.{dec}f}"
    ip, _, fp = s.partition(".")
    if len(ip) > 3:
        head, tail = ip[:-3], ip[-3:]
        parts = []
        while len(head) > 2:
            parts.insert(0, head[-2:])
            head = head[:-2]
        if head:
            parts.insert(0, head)
        ip = ",".join(parts) + "," + tail
    return ("−" if neg else "") + "₹" + ip + (("." + fp) if fp else "")


# --------------------------------------------------------------------------- metrics
def metrics() -> str:
    planned, passed, failed, blocked = 400, 342, 30, 8
    not_run = planned - passed - failed - blocked            # 20
    executed = passed + failed                               # 372
    defects_qa, defects_prod, kloc = 46, 4, 23
    fixed, reopened = 40, 6
    reqs, reqs_cov = 120, 114
    auto, flaky = 160, 8
    mttd = (1 + 2 + 2 + 3 + 4.5) / 5
    mttr = (2 + 3 + 4 + 6 + 7.5) / 5
    rows = [
        ["Test execution %", "executed ÷ planned × 100, where executed = passed + failed", f"({passed}+{failed}) ÷ {planned} = **{executed / planned * 100:.1f} %**", "Progress. Blocked and not-run cases are listed separately."],
        ["Pass rate", "passed ÷ executed × 100", f"{passed} ÷ {executed} = **{passed / executed * 100:.1f} %**", "Quality signal for the executed scope only."],
        ["Fail rate", "failed ÷ executed × 100", f"{failed} ÷ {executed} = **{failed / executed * 100:.1f} %**", "Pass rate + fail rate = 100 %."],
        ["Defect density", "valid defects ÷ size (KLOC, function points or requirements)", f"{defects_qa} ÷ {kloc} KLOC = **{defects_qa / kloc:.1f} per KLOC**", "Compare like with like (same size unit, same period)."],
        ["Defect leakage", "defects found after release ÷ (found in QA + found after release) × 100", f"{defects_prod} ÷ ({defects_qa}+{defects_prod}) = **{defects_prod / (defects_qa + defects_prod) * 100:.1f} %**", "Defect-removal efficiency = 100 − leakage = " + f"{100 - defects_prod / (defects_qa + defects_prod) * 100:.1f} %."],
        ["Defect reopening rate", "reopened ÷ fixed × 100", f"{reopened} ÷ {fixed} = **{reopened / fixed * 100:.1f} %**", "High values point at fix quality or poor retest steps."],
        ["Requirement coverage", "requirements with ≥ 1 executed test ÷ total requirements × 100", f"{reqs_cov} ÷ {reqs} = **{reqs_cov / reqs * 100:.1f} %**", "List the uncovered requirements by name."],
        ["Automation coverage", "automated cases ÷ total cases × 100 (state the denominator)", f"{auto} ÷ {planned} = **{auto / planned * 100:.1f} %**", "Also report automation pass rate and runtime."],
        ["Flaky test rate", "tests with inconsistent results ÷ automated tests × 100", f"{flaky} ÷ {auto} = **{flaky / auto * 100:.1f} %**", "Target example: ≤ 2 %; trend matters."],
        ["Mean time to detect (MTTD)", "average of (detection time − time the defect was introduced / build delivered)", "(1 + 2 + 2 + 3 + 4.5) ÷ 5 = **" + f"{mttd:.1f} days**", "Shorter means earlier testing."],
        ["Mean time to resolve (MTTR)", "average of (closure time − report time)", "(2 + 3 + 4 + 6 + 7.5) ÷ 5 = **" + f"{mttr:.1f} days**", "Report by severity; do not mix S1 with S4."],
    ]
    return join(
        h1("QA Metrics",
           "Metrics support decisions; they are not a scorecard for people. Every metric has a written formula, a data source and an owner "
           "before the project starts."),
        table(["Metric", "Formula", "Worked example", "Reading it correctly"], rows, widths=["16%", "28%", "30%", "26%"], first_bold=True),
        p(f"Example data used above: {planned} planned cases = {passed} passed + {failed} failed + {blocked} blocked + {not_run} not run; "
          f"{defects_qa} valid defects found in QA and {defects_prod} in production; {fixed} defects fixed, {reopened} reopened."),
        callout("danger", "Why metrics must never be manipulated",
                "A metric that is gamed stops measuring quality and starts hiding risk. The cost surfaces later: a release goes live with "
                "unknown defects, the client loses trust, and the company's evidence becomes worthless in a dispute. "
                "We report the truth, including bad news, as early as possible."),
        h2("Common manipulations and the controls that prevent them"),
        table(["Manipulation", "Why it is wrong", "Control"], [
            ["Counting “not run” or blocked cases as passed", "Inflates pass rate and execution %.", "Formula fixed in advance; counts must reconcile to the planned total."],
            ["Splitting one test into many (or merging many) to change counts", "Case counts become meaningless.", "Test-case design standard; review for atomicity; report requirement coverage next to case counts."],
            ["Lowering severity or relabelling defects as “enhancements”", "Makes the open-defect picture look better.", "Severity defined by the table, challenged in triage; changes need a recorded reason."],
            ["Closing defects without verified retest", "Defect counts fall while risk remains.", "Only the tester closes; closure needs build number and evidence."],
            ["Excluding failed or flaky tests from the denominator", "Pass rate appears higher.", "Report flaky tests separately and keep them in the totals."],
            ["Reporting average response time only", "Hides the slow tail users experience.", "Always give p90, p95, p99 and error rate."],
            ["Rewarding people for defect counts or pass percentages", "Encourages gaming and conflict with developers.", "Appraise report quality, reproducibility, coverage and teamwork instead."],
            ["Selecting the “good” builds or runs", "Cherry-picking.", "Publish all runs of the cycle; keep raw data in the repository."],
        ], widths=["30%", "30%", "40%"], first_bold=True),
        callout("gold", "Rules for honest reporting",
                ["Publish the formula, the date, the build and the data source next to every number.",
                 "A second person reconciles the report to raw data before it leaves the company.",
                 "Show trends, not only snapshots; explain context (for example, test scope changed).",
                 "If a target is missed, say so, explain why, and state the risk and the recommended action.",
                 "Never present a metric as proof that the product is “safe” or “bug-free”."]),
        end_chapter(),
    )


# --------------------------------------------------------------------------- costing
ROLES = [  # role, monthly cost-to-company (CTC) in INR — ILLUSTRATIVE, replace with real figures
    ("Junior QA Tester", 30000), ("Manual QA Engineer", 45000), ("Automation QA Engineer", 70000),
    ("API Tester", 60000), ("Performance Tester", 75000), ("QA Lead", 100000), ("Test Manager", 130000),
]
PRODUCTIVE_HOURS = 128          # 160 h/month × 80 % utilisation
RATE = {r: round(c / PRODUCTIVE_HOURS) for r, c in ROLES}
GM_TARGET = 0.40

# activity, {role: hours}
ACTIVITIES = [
    ("Test strategy and planning", {"QA Lead": 16, "Manual QA Engineer": 8}),
    ("Requirement analysis and test design", {"Manual QA Engineer": 70, "Junior QA Tester": 40, "QA Lead": 8}),
    ("Environment and data set-up", {"QA Lead": 4, "Manual QA Engineer": 8}),
    ("Manual execution (two cycles)", {"Manual QA Engineer": 60, "Junior QA Tester": 60}),
    ("Defect management and retesting", {"Manual QA Engineer": 24, "Junior QA Tester": 16}),
    ("Automation development (25 UI tests + CI)", {"Automation QA Engineer": 80, "QA Lead": 6}),
    ("API and integration testing", {"API Tester": 40}),
    ("Performance testing (one controlled campaign)", {"Performance Tester": 32}),
    ("Regression cycle", {"Manual QA Engineer": 16, "Junior QA Tester": 8, "Automation QA Engineer": 8}),
    ("Reporting and QA sign-off", {"QA Lead": 14}),
]
NON_LABOUR = [  # item, cost INR — ASSUMPTIONS, replace with vendor quotes
    ("Test environment (cloud, 4 weeks)", 12000), ("Device/browser cloud (1 month, 1 parallel session)", 9000),
    ("Tool licences (test management, 3 seats × 1 month)", 6000), ("Performance load-generator infrastructure", 4000),
]
PM_PCT, CONT_PCT = 0.10, 0.10


def _calc():
    act_rows, hours_by_role = [], {r: 0 for r, _ in ROLES}
    for name, alloc in ACTIVITIES:
        hrs = sum(alloc.values())
        cost = sum(h * RATE[r] for r, h in alloc.items())
        act_rows.append((name, hrs, cost))
        for r, h in alloc.items():
            hours_by_role[r] += h
    delivery_hours = sum(h for _, h, _ in act_rows)
    delivery_cost = sum(c for _, _, c in act_rows)
    pm_hours = round(delivery_hours * PM_PCT)
    pm_cost = pm_hours * RATE["Test Manager"]
    labour_hours = delivery_hours + pm_hours
    labour_cost = delivery_cost + pm_cost
    nonlab = sum(c for _, c in NON_LABOUR)
    cont = round((labour_cost + nonlab) * CONT_PCT)
    cogs = labour_cost + nonlab + cont
    price = round(cogs / (1 - GM_TARGET) / 1000) * 1000      # rounded to the nearest ₹1,000 for quoting
    return dict(act=act_rows, hours=hours_by_role, dh=delivery_hours, dc=delivery_cost, pmh=pm_hours, pmc=pm_cost,
                lh=labour_hours, lc=labour_cost, nl=nonlab, cont=cont, cogs=cogs, price=price)


def costing() -> str:
    c = _calc()
    price, cogs, nl, cont = c["price"], c["cogs"], c["nl"], c["cont"]
    gp = price - cogs
    gm = gp / price * 100
    var_cost = nl                                            # variable = non-labour direct (salaried labour treated as fixed)
    contrib = price - var_cost
    cm = contrib / price * 100
    team_ctc = sum(v for _, v in ROLES)                      # one person per role (illustrative team of seven)
    overheads = 150000                                       # premises, admin, tools subscriptions, marketing (assumed)
    fixed_monthly = team_ctc + overheads
    be_projects = fixed_monthly / contrib
    be_hours = be_projects * c["lh"]
    capacity = len(ROLES) * PRODUCTIVE_HOURS
    capacity_projects = capacity / c["lh"]
    avg_rate = c["lc"] / c["lh"]
    # time-based
    hours_mix = {"QA Lead": 8, "Manual QA Engineer": 40, "Automation QA Engineer": 24, "API Tester": 8}
    ret_hours = sum(hours_mix.values())
    ret_cost = sum(h * RATE[r] for r, h in hours_mix.items())
    ret_tools = 5000
    ret_cogs = ret_cost + ret_tools
    ret_price = round(ret_cogs / (1 - 0.35) / 1000) * 1000   # retainers priced at a 35 % target margin (lower risk, steady work)
    ret_over = round(ret_cost / ret_hours / (1 - GM_TARGET) / 50) * 50
    day_rate = {r: round(RATE[r] / (1 - GM_TARGET) / 50) * 50 for r, _ in ROLES}
    sens = [(g, round(cogs / (1 - g) / 1000) * 1000) for g in (0.30, 0.35, 0.40, 0.50)]

    rate_rows = [[r, inr(ctc), inr(RATE[r]), inr(day_rate[r])] for r, ctc in ROLES]
    act_rows = [[n, str(h), inr(cst)] for n, h, cst in c["act"]]
    act_rows += [["Project management (10 % of delivery hours, Test Manager)", str(c["pmh"]), inr(c["pmc"])],
                 ["**Total labour**", f"**{c['lh']}**", f"**{inr(c['lc'])}**"]]
    return join(
        h1("Costing and Pricing",
           "How to cost a QA engagement from first principles, convert cost into a selling price, and check profitability. "
           "All rupee figures here are **illustrative assumptions** that show the method; replace them with the company's real "
           "salaries, utilisation, vendor quotes and tax rules before quoting any client."),
        h2("Cost components"),
        table(["Component", "How to calculate"], [
            ["Test planning hours", "Strategy + plan + estimate + risk register effort (QA Lead and Manual QA)."],
            ["Manual testing hours", "Requirement analysis + test design + environment support + execution cycles + defect handling."],
            ["Automation development", "Candidates × average build time per test + framework set-up + CI + review + stabilisation."],
            ["Test environment costs", "Cloud environment (instances, storage, data transfer) for the period, or client-provided (then 0, with an assumption)."],
            ["Device and browser costs", "Device-cloud plan, purchased devices (depreciation over use period), OS licences."],
            ["Tool licences", "Seats × price × months for commercial tools; open-source tools cost 0 licence but still cost set-up time."],
            ["Performance infrastructure", "Load-generator machines, monitoring, data volume, test-run hours."],
            ["Reporting", "Report preparation, review, client presentations."],
            ["Retesting", "Defect retests and regression after fixes (typically 15–25 % of execution effort)."],
            ["Maintenance", "Suite updates after releases; flaky test fixes; tool upgrades (charged as retainer or per release)."],
            ["Project management", "Planning, status reporting, meetings, client communication (5–15 % of delivery hours)."],
            ["Contingency", "Allowance for uncertainty; 10–20 % depending on requirement stability and access risk."],
        ], widths=["26%", "74%"], first_bold=True),
        h2("Step 1 — Hourly cost rate per role"),
        code(f"""cost per productive hour = monthly cost-to-company (CTC) ÷ productive hours per month
productive hours             = 160 working hours × 80 % utilisation = {PRODUCTIVE_HOURS}
Example (Manual QA):  {inr(45000)} ÷ {PRODUCTIVE_HOURS} = {inr(45000 / PRODUCTIVE_HOURS, 2)}  →  rounded {inr(RATE['Manual QA Engineer'])} per hour""", "formula"),
        table(["Role", "Monthly CTC (assumed)", "Cost per hour", f"Day rate at {int(GM_TARGET * 100)} % margin (8 h)"],
              [[r, a, b, inr(day_rate_val * 8)] for (r, a, b, _), day_rate_val in zip(rate_rows, day_rate.values())],
              widths=["32%", "22%", "20%", "26%"], first_bold=True),
        h2("Step 2 — Estimate hours (worked example)"),
        p("**Scenario:** release QA for a mid-size e-commerce web application, four weeks: manual and exploratory testing, "
          "25 automated UI tests with CI, API/integration testing, one controlled performance campaign, two execution cycles, retest and sign-off."),
        table(["Activity", "Hours", "Labour cost"], act_rows, widths=["64%", "12%", "24%"], first_bold=False),
        table(["Non-labour direct costs (assumptions)", "Cost"],
              [[n, inr(v)] for n, v in NON_LABOUR] + [["**Total non-labour**", f"**{inr(nl)}**"]], widths=["72%", "28%"]),
        h2("Step 3 — Cost, price, margins"),
        table(["Line", "Calculation", "Amount"], [
            ["Labour", f"{c['lh']} h at an average {inr(avg_rate)}/h", inr(c["lc"])],
            ["Non-labour direct costs", "Table above", inr(nl)],
            ["Contingency", f"{int(CONT_PCT * 100)} % × ({inr(c['lc'])} + {inr(nl)})", inr(cont)],
            ["**Total delivery cost (COGS)**", "Labour + non-labour + contingency", f"**{inr(cogs)}**"],
            ["**Selling price (ex-tax)**", f"COGS ÷ (1 − {GM_TARGET:.2f}), rounded to ₹1,000", f"**{inr(price)}**"],
            ["Gross profit", "Price − COGS", inr(gp)],
            ["**Gross margin**", "Gross profit ÷ Price", f"**{gm:.1f} %**"],
            ["Variable costs", "Non-labour direct costs (salaried labour is fixed in the short term)", inr(var_cost)],
            ["Contribution", "Price − variable costs", inr(contrib)],
            ["**Contribution margin**", "Contribution ÷ Price", f"**{cm:.1f} %**"],
        ], widths=["30%", "46%", "24%"], first_bold=False),
        callout("info", "Gross margin versus contribution margin",
                "**Gross margin** shows profitability after *all* direct costs including salaried labour. **Contribution margin** shows what the project "
                "adds towards *fixed* costs after only the costs that vary with taking the project (environments, licences, infrastructure, subcontractors). "
                "A project can have a modest gross margin yet a high contribution margin; use both."),
        h2("Step 4 — Break-even"),
        code(f"""Break-even (projects/month) = monthly fixed costs ÷ contribution per project
Fixed costs (illustrative) = team CTC {inr(team_ctc)} + overheads {inr(overheads)} = {inr(fixed_monthly)} per month
Contribution per project   = {inr(contrib)}
Break-even                 = {inr(fixed_monthly)} ÷ {inr(contrib)} = {be_projects:.2f} projects of this size per month

Break-even in billable hours = {be_projects:.2f} × {c['lh']} h = {be_hours:.0f} hours per month
Team capacity                = {len(ROLES)} people × {PRODUCTIVE_HOURS} productive hours = {capacity} hours = {capacity_projects:.2f} projects
Break-even uses {be_hours / capacity * 100:.0f} % of capacity  →  margin of safety = {100 - be_hours / capacity * 100:.0f} %""", "formula"),
        p("Interpretation: below the break-even volume the department makes a loss however good each project's gross margin looks. "
          "Here break-even already needs about four-fifths of the team's capacity, which leaves little room for bench time, sickness, training or "
          "a delayed client. Track break-even and utilisation every month; they set the pipeline target."),
        h2("Sensitivity of price to target margin"),
        table(["Target gross margin", "Price = COGS ÷ (1 − margin)", "Price vs 40 % case"],
              [[f"{int(g * 100)} %", inr(p_), f"{(p_ / price - 1) * 100:+.1f} %"] for g, p_ in sens], widths=["30%", "40%", "30%"], first_bold=True),
        h2("Four pricing models"),
        table(["Model", "How the price is set", "Best for", "Main risk and control"], [
            ["**Project-based**", "Estimate by activity (above) → price with milestones (e.g. 30 % at plan approval, 40 % at execution complete, 30 % at sign-off).", "Defined releases with clear scope", "Scope creep → change-request procedure; milestones tied to approval gates."],
            ["**Fixed-scope**", "Fixed price for an explicit scope list (features, platforms, cycles). Includes contingency and a risk premium. Extra cycles or scope are priced at the rate card.", "Clients who need budget certainty and have stable requirements", "Under-estimation → strict assumptions, exclusions, maximum cycles (e.g. 2 execution + 1 retest cycle)."],
            ["**Time-based (T&M)**", "Role day/hour rates × actual timesheets, with an optional cap. Rates from the table above.", "Uncertain scope, discovery work, staff augmentation", "Client budget anxiety → weekly timesheets, burn-down report, agreed cap and approval above cap."],
            ["**Monthly QA retainer**", "Committed hours per month at a blended rate + tool pass-through; unused hours expire (or roll over by agreement); overage at an agreed rate.", "Continuous releases, maintenance, regression upkeep", "Under-use or over-use → monthly utilisation report; quarterly re-sizing."],
        ], widths=["17%", "34%", "22%", "27%"]),
        h2("Retainer worked example"),
        table(["Role", "Hours/month", "Cost/hour", "Cost"],
              [[r, str(h), inr(RATE[r]), inr(h * RATE[r])] for r, h in hours_mix.items()] +
              [["Tools and reporting pass-through", "—", "—", inr(ret_tools)],
               ["**Total cost to deliver**", f"**{ret_hours}**", "", f"**{inr(ret_cogs)}**"]],
              widths=["44%", "18%", "18%", "20%"]),
        code(f"""Retainer price (35 % target margin, steady work) = {inr(ret_cogs)} ÷ (1 − 0.35) = {inr(ret_cogs / 0.65)}  →  quote {inr(ret_price)} per month for {ret_hours} committed hours
Margin check = ({inr(ret_price)} − {inr(ret_cogs)}) ÷ {inr(ret_price)} = {(ret_price - ret_cogs) / ret_price * 100:.1f} %
Overage rate (blended cost per hour {inr(ret_cost / ret_hours)} ÷ (1 − 0.40)) ≈ {inr(ret_over)} per extra hour""", "formula"),
        callout("danger", "Quoting rules",
                ["Quote only from a written estimate; list assumptions and exclusions in the proposal.",
                 "Verify every tool licence and cloud cost on the vendor's current price page on the day of quoting.",
                 "Add taxes (e.g. GST) as separate lines according to current rules; never bury them.",
                 "Never promise outcomes (“zero defects”, “100 % automation”); sell evidence, coverage and risk reduction.",
                 "A second person (Test Manager) reviews every quote before it is sent."]),
        end_chapter(),
    )
