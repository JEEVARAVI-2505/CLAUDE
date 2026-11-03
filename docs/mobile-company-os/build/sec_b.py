"""Part B: Phase 8 costing and Phase 9 pricing, fully worked (SiteDiary)."""
import json
from lib import *
import model as M
I, N = M.inr, M.num

def assumptions():
    rows = [["A-01", "Employee overhead loading", "25%", "Planning", "Equipment, seats, workspace, management, statutory"],
            ["A-02/03", "Paid hours / utilisation", f"{M.AVAIL_HRS} h × {int(M.UTIL*100)}% = {M.PROD_HRS:.0f} productive h/mo", "Planning", "Leave, meetings, idle time excluded"],
            ["A-04", "Risk classes (O / P multipliers)", "Low .85/1.25 · Med .80/1.40 · High .75/1.80", "Planning", "Calibrate from actual-vs-estimate history quarterly"],
            ["A-08", "Reuse factor on reusable tasks (client #2)", "40% of original effort", "Planning", "Measure after first reuse"],
            ["A-10", "USD→INR", f"₹{M.USD_INR}", "Planning", "Replace with RBI reference rate on the day"],
            ["A-11", "Backend instance cost / capacity", f"{I(M.INSTANCE_COST)}/mo · {M.RPS_PER_INSTANCE} req/s", "Planning", "Capacity must be proven by load test"],
            ["A-12", "Storage / egress / SMS / email", f"₹{M.UNIT['storage']}/GB-mo · ₹{M.UNIT['egress']}/GB · ₹{M.UNIT['sms']}/SMS · ₹{M.UNIT['email']}/email", "Planning", "Replace from provider calculators at Gate 8"],
            ["A-13", "Photo size / retention", f"{M.PHOTO_MB} MB after compression · {M.RETAIN_MONTHS} months hot", "Planning", "Measure in pilot"],
            ["A-14", "Payment gateway fee", "2.0% of billed amount", "Planning", "Verify gateway pricing + GST on fee"],
            ["A-15", "Corporate income tax", "25.17% (new regime company rate incl. surcharge+cess)", "Verify with CA", "Shown below operating profit only"],
            ["A-16", "GST on subscriptions", "18% collected, not revenue", "Verify with CA", "Pass-through; shown separately"],
            ["A-17", "Churn / CAC", f"{M.CHURN*100:.1f}% per month · {I(M.CAC)} per customer", "Hypothesis", "Validate in pilot; see sensitivity"],
            ["A-18", "Plan mix / annual share", "50/35/15% Starter/Pro/Business · 30% annual (10 months paid)", "Hypothesis", "Validate with pilot customers"]]
    return table(["ID", "Assumption", "Value", "Status", "Note"], rows)

def rates_table():
    rows = []
    for c, (name, ctc, ext) in M.ROLES.items():
        if ext: rows.append([name, "Outsourced", "-", f"{I(ext)}/h fixed", I(ext)])
        else: rows.append([name, I(ctc), I(ctc * (1 + M.OVERHEAD)), f"÷ {M.PROD_HRS:.0f} h", I(M.RATES[c])])
    rows.append(["Customer support executive (part-time)", I(M.SUPPORT_CTC), I(M.SUPPORT_CTC * 1.25), f"÷ {M.PROD_HRS:.0f} h", I(M.SUPPORT_RATE)])
    return (formula("Fully loaded hourly cost = Monthly CTC × (1 + overhead%) ÷ (paid hours × utilisation)\nExample (Flutter): 85,000 × 1.25 ÷ 120 = 885.42 → rounded ₹885/h") +
            table(["Role", "Monthly CTC", "Loaded / month", "Divisor", "Rate / hour"], rows, num_cols=(1, 2, 4)) +
            callout("warn", "Salaries are illustrative", "CTC values are placeholders chosen to demonstrate the method. Use real payroll data; never publish individual salaries outside Finance."))

def task_table():
    rows = []
    for t in M.TC:
        rows.append([t["id"], t["name"], t["scope"], t["risk"], N(t["m"]), N(t["o"]), N(t["e"]), N(t["p"]), I(t["cost_o"]), I(t["cost_e"]), I(t["cost_p"])])
    rows.append(["", "TOTAL", "", "", N(M.H_M), N(M.H_O), N(M.H_E), N(M.H_P), I(M.L_O), I(M.L_E), I(M.L_P)])
    t = table(["ID", "Task", "C/R", "Risk", "Likely h", "Opt h", "Exp h", "Pess h", "Optimistic ₹", "Expected ₹", "Pessimistic ₹"], rows, num_cols=(4, 5, 6, 7, 8, 9, 10), total_last=True)
    rr = [[M.ROLES[r][0], N(M.ROLE_HRS_M[r]), I(M.RATES[r]), I(M.ROLE_COST_M[r])] for r in M.ROLES]
    rr.append(["Total (most-likely)", N(M.H_M), "", I(M.L_M)])
    return t, table(["Role", "Most-likely hours", "Rate/h", "Cost at most-likely"], rr, num_cols=(1, 2, 3), total_last=True)

def calc_widget():
    data = dict(ctc={r: v[1] for r, v in M.ROLES.items()}, ext={r: v[2] for r, v in M.ROLES.items() if v[2]},
                tasks=[dict(h=[[r, h] for r, h in t[4].items()], of=M.RISK[t[3]][0], pf=M.RISK[t[3]][1]) for t in M.TASKS], setup=M.SETUP_COSTS)
    inputs = "".join(f'<div><label>{M.ROLES[r][0].split(" (")[0]} CTC/mo</label><input id="c_{r}" type="number" value="{v[1]}"></div>' for r, v in M.ROLES.items() if not v[2])
    return f"""<div class="calc"><b>Live labour &amp; custom-price calculator</b> <span class=mut>(web version only; same formulas as the tables)</span>
<div class="grid">{inputs}<div><label>Overhead %</label><input id="c_oh" type="number" value="25"></div><div><label>Paid hours/mo</label><input id="c_av" type="number" value="160"></div>
<div><label>Utilisation %</label><input id="c_ut" type="number" value="75"></div><div><label>Company G&amp;A % of labour</label><input id="c_ga" type="number" value="12"></div>
<div><label>Target margin % of price</label><input id="c_mg" type="number" value="30"></div><div><label>Risk share of (P−E) %</label><input id="c_rs" type="number" value="50"></div></div>
<p class=small>Rates: <span id="o_rates"></span></p>
<div class="kpis"><div class=kpi><b id="o_le"></b><span>Expected labour</span></div><div class=kpi><b id="o_lp"></b><span>Pessimistic labour</span></div>
<div class=kpi><b id="o_cost"></b><span>Total cost</span></div><div class=kpi><b id="o_price"></b><span>Target price (client #1)</span></div></div></div>
<script type="application/json" id="calcdata">{json.dumps(data)}</script>"""

def phase8():
    t, rt = task_table()
    sw = [[a, b, I(c) if c else "Free", d] for a, b, c, d in M.SOFTWARE_MONTHLY]
    one = [[a, "Paid", I(c), d] for a, _, c, d in M.SOFTWARE_ONETIME]
    st = [[a, f"US${u}" if u else "-", I(i) if i else "-", s] for a, u, i, s in M.STORE]
    inf = [[a, I(b), c] for a, b, c in M.INFRA_ONETIME] + [["Total one-time infrastructure", I(M.INFRA_ONE), ""]]
    setup = [["Tooling for build period (paid, non-optional) ×5 months", I(M.SW_BUILD)], ["Mac + test devices", I(M.SW_ONE)],
             ["One-time infrastructure", I(M.INFRA_ONE)], ["Store accounts, year 1", I(M.STORE_YEAR1)], ["Total non-labour setup (cash)", I(M.SETUP_COSTS)]]
    return f"""
<p>Sample product: <b>SiteDiary</b> - an offline-first Flutter app (Android + iOS) where site supervisors record attendance, material receipts, issues and photos, and the owner receives a PDF daily report; NestJS + PostgreSQL backend, web admin dashboard, subscription billing. Scope = tasks T01-T23 below.</p>
<h3>8.0 Assumptions register (change here first)</h3>{assumptions()}
<h3>8.1 Fully loaded hourly rates</h3>{rates_table()}
<h3>8.2 Hours and labour cost - optimistic / expected / pessimistic</h3>
{formula("PERT expected hours E = (O + 4·M + P) ÷ 6 ; O = M × optimistic factor ; P = M × pessimistic factor (by risk class)\nTask cost = Σ over roles (hours × role rate) ; scenario cost = most-likely cost × (scenario hours ÷ M)")}
{t}
<p class=small>C = customer-specific work, R = reusable company IP (auth, sync engine, uploads, billing, admin, CI). Hours are assigned once to the role that performs them - one person covering two roles must not be double counted (a developer who also tests appears once, under the role with the larger share).</p>
<h3>8.3 Hours by role (most-likely)</h3>{rt}
{calc_widget()}
<h3>8.4 Software and licence cost</h3>
{table(["Item", "Class", "₹ / month", "Basis"], sw, num_cols=(2,))}
{table(["One-time equipment", "Class", "₹", "Basis"], one, num_cols=(2,))}
<h3>8.5 App-store and developer accounts</h3>
{table(["Item", "USD", "₹ at planning FX", "Status"], st, num_cols=(1, 2))}
{callout("ok", "Fee verification (access date 2026-11-03)", "<b>Apple - verified</b> on the official page <i>developer.apple.com/help/account/membership/program-enrollment</i>: Developer Program <b>US$99/year</b>, Enterprise Program US$299/year, 'in local currency where available; prices may vary by region'. Organisations must be a legal entity with a <b>D-U-N-S number</b>; DBAs, fictitious businesses and trade names are not accepted. The Apple Developer app enrolment renews automatically until cancelled. "
 "<br><b>Google - US$25 one-time</b>, from Google's Play Console help (answer 6112435) as summarised by search on the same date; the Google support host is blocked in this environment so the page itself could not be opened - <b>confidence Medium-High; confirm on the signup screen and record a screenshot at Gate 8</b>. Google may ask for a government ID and a card in your legal name; the fee is not refunded if the details prove invalid. "
 "<br>INR amounts here use the planning rate of ₹90/US$; Apple shows the real local price during enrolment. Account approval never guarantees app approval; confirm any closed-testing prerequisite for new personal Google accounts on the day.")}
<h3>8.6 One-time infrastructure</h3>{table(["Item", "₹", "Basis"], inf, num_cols=(1,), total_last=True)}
<h3>8.7 Non-labour setup summary</h3>{table(["Component", "₹"], setup, num_cols=(1,), total_last=True)}
{callout("ok","Budget check", f"Cash need {I(M.SETUP_COSTS)} vs intake budget ₹3,00,000 → " + ("within budget." if M.SETUP_COSTS <= 300000 else "OVER budget; resequence purchases."))}
"""

def infra_sim():
    lv = ["Low", "Medium", "High"]
    u = M.USAGE
    ass = table(["Driver", "Low", "Medium", "High"], [
        ["MAU ÷ registered users"] + [f"{x*100:.0f}%" for x in u["mau_ratio"]], ["DAU ÷ MAU"] + [f"{x*100:.0f}%" for x in u["dau_mau"]],
        ["Sessions per DAU / day"] + list(map(str, u["sessions"])), ["API requests per session"] + list(map(str, u["req_per_session"])),
        ["Photos per DAU / day"] + list(map(str, u["photos_per_dau"])), ["OTP per MAU / month"] + list(map(str, u["otp_per_mau"])),
        ["Emails per MAU / month"] + list(map(str, u["emails_per_mau"]))])
    rows = []
    for n in M.SCALES:
        s = M.sim(n, 1)
        rows.append([N(n), N(s["mau"]), N(s["dau"]), N(s["conc"], 1), N(s["rps"], 1), N(s["req_month"] / 1e6, 2) + " M", N(s["stored_gb"], 1), N(s["egress_gb"], 1), M.recommended_model(n)])
    wk = table(["Registered", "MAU", "DAU", "Peak concurrent", "Peak req/s", "API req/mo", "Stored GB (12 mo)", "Egress GB/mo", "Model"], rows, num_cols=tuple(range(8)))
    cost_rows = []
    for n in M.SCALES:
        a, b, c = (M.sim(n, k)["total"] for k in range(3))
        cost_rows.append([N(n), I(a), I(b), I(c), I(b / n, 2)])
    cr = table(["Registered users", "Low usage ₹/mo", "Medium ₹/mo", "High ₹/mo", "Medium ₹ per user"], cost_rows, num_cols=(0, 1, 2, 3, 4))
    s = M.sim(10000, 1)
    br = table(["Component (10,000 users, medium)", "₹ / month"], [[k, I(v)] for k, v in s["comps"].items()] + [["Total infrastructure", I(s["total"])]], num_cols=(1,), total_last=True)
    steps = formula("Peak req/s = (DAU × sessions × req/session) ÷ (10 h × 3600) × peak factor 5\nBackend instances = max(2, ceil(peak req/s ÷ 150))        (2 minimum for availability)\n"
                    "Database tier by DAU: ≤2k Small · ≤10k Medium · ≤40k Large · else XL + replica\nStorage GB = photos/mo × 0.6 MB ÷ 1024 × 12 months retained")
    return ("<h3>8.8 User-scale simulation</h3>" + ass + steps + wk + "<h3>8.9 Monthly production cost by scale (infrastructure only)</h3>" + cr +
            callout("warn", "Why cost is not linear", "Compute stays at two instances up to the largest simulated scale because the workload is light (peak req/s is small); cost steps at database, monitoring and storage tiers. "
                    "These are model outputs from planning unit prices - run a load test before relying on instance counts, and refresh unit prices from official calculators.") + br)

def models():
    s = M.sim(1000, 1)
    m1 = M.MODELS[0]; m1m = sum(v for _, v in m1["items"])
    s5 = M.sim(5000, 1); s25 = M.sim(25000, 1)
    m2m = s5["total"]
    m3m = M.sim(50000, 1)["total"] + M.M3_MULT_FIXED
    rows = [[m["n"], I(m["setup"]), I(mm), I(mm * 12), m["cap"], m["lim"]] for m, mm in zip(M.MODELS, (m1m, m2m, m3m))]
    d1 = table(["Model", "One-time setup (infra + store yr-1 + extras)", "Monthly", "Annual", "Capacity", "Limitations"], rows, num_cols=(1, 2, 3))
    m1t = table(["Model 1 item", "₹ / month"], [[a, I(b)] for a, b in m1["items"]] + [["Total", I(m1m)]], num_cols=(1,), total_last=True)
    d2 = table(["Model", "Scaling triggers", "Risks", "Upgrade requirements"], [[m["n"], m["trig"], m["risk"], m["upg"]] for m in M.MODELS])
    return ("<h3>8.10 Three infrastructure models</h3>" + d1 +
            f"<p class=small>Model 2 monthly shown at 5,000 users (medium usage, {I(m2m)}); Model 3 at 50,000 users plus {I(M.M3_MULT_FIXED)} for multi-AZ, DR copy, WAF, SSO and security tooling (planning). Setup extras: Model 2 +₹40,000 (staging, load test, hardening), Model 3 +₹2,50,000 (DR design, pen test).</p>" +
            m1t + d2 + callout("warn", "Free tiers", "Never assume a free tier, quota or term will remain unchanged. Record the date and page of every free-tier dependency and review quarterly."))

def phase9():
    cp = M.CP1; c2 = M.CP2
    rows = [["Development labour (expected)", I(cp["labour"]), I(c2["labour"])], ["Setup & third-party (non-labour)", I(cp["setup"]), I(c2["setup"])],
            [f"Allocated company overhead ({int(M.GA_PCT*100)}% of labour)", I(cp["ov"]), I(c2["ov"])], [f"Risk allowance ({int(M.RISK_SHARE*100)}% of Pess−Exp)", I(cp["risk"]), I(c2["risk"])],
            ["Total cost", I(cp["cost"]), I(c2["cost"])], [f"Minimum price (margin {int(M.MIN_M*100)}%)", I(cp["minimum"]), I(c2["minimum"])],
            [f"<b>Target price (margin {int(M.TARGET_M*100)}%)</b>", f"<b>{I(cp['target'])}</b>", f"<b>{I(c2['target'])}</b>"]]
    custom = (formula("Project price = Labour + Setup + Third-party + Allocated overhead + Risk allowance + Target profit\n= Total cost ÷ (1 − target margin)       [margin on price, not mark-up on cost]") +
              table(["Line", "Client #1 (full build)", "Client #2 (reusable tasks at 40% effort)"], rows, num_cols=(1, 2)) +
              f"<p class=small>Reusable share of expected labour: {M.REUSE_SHARE*100:.0f}%. Customer-specific cost is recovered from the client; reusable IP stays company property and is licensed, not transferred. "
              f"Quote client #1 at or above the minimum price only with founder approval; a quote below minimum margin is a Gate 8 rejection.</p>")
    wl = table(["White-label", "₹"], [["Setup labour (branding, config, store submission)", I(M.WL_LABOUR)], [f"Setup price = labour × (1+{int(M.GA_PCT*100)}%) ÷ (1−{int(M.TARGET_M*100)}%)", I(M.WL_SETUP)],
              ["Annual licence + hosting (planning)", I(M.WL_LICENCE_YR)]], num_cols=(1,))
    ent = f"<p><b>Enterprise</b> = Business plan economics + custom integration labour (priced by the formula above) + {int(M.ENT_PREMIUM*100)}% premium for SSO, audit export, SLA and security questionnaires; subscription from {I(M.ENTERPRISE_FROM)}/month.</p>"
    pr = []
    for p, c in zip(M.PLANS, M.PC):
        pr.append([p[0], I(p[1]), I(c["annual"]), p[2], p[3], f"{p[4]} GB", p[6], f"{p[5]} min/mo", I(c["cost"], 0), I(c["contrib"]), f"{c['cm']*100:.0f}%"])
    pr.append(["Enterprise", f"from {I(M.ENTERPRISE_FROM)}", "custom", "custom", "custom", "custom", "custom", "SLA", "quote", "quote", "≥ 60% target"])
    plans = table(["Plan", "Monthly", "Annual (10 mo)", "Users", "Projects", "Storage", "API/day", "Support", "Cost to serve", "Contribution", "Margin"], pr, num_cols=(1, 2, 8, 9, 10))
    ov = table(["Overage", "Rate (planning)"], [["Extra user", "₹150 / user / month"], ["Extra storage", "₹25 / GB / month"], ["Extra API", "₹40 per 10,000 requests/day band"], ["Priority support", "₹2,500 / incident-hour"]])
    unit = formula(f"Cost to serve (plan) = users × variable infra/user (₹{M.VAR_USER:.2f}) + {M.PAY_FEE*100:.0f}% payment fee + support minutes × ₹{M.SUPPORT_RATE}/h ÷ 60\n"
                   f"ARPA = Σ(mix × fee) × billing factor = {I(M.ARPA,0)} /customer/month   (billing factor {M.BILL_FACTOR:.3f}: 30% pay annually for 10 months)\n"
                   f"Gross margin/customer = ARPA − payment fee − variable infra − support = {I(M.GM_PER_CUST)}  ({M.GM_PCT*100:.1f}%)\n"
                   f"LTV = gross margin ÷ churn = {I(M.LTV)}   ;   LTV:CAC = {M.LTV_CAC:.1f}   (CAC {I(M.CAC)}, churn {M.CHURN*100:.1f}%/mo)")
    sc1, sc2 = [], []
    for s in M.SC:
        sc1.append([N(s["n"]), N(s["users"]), I(s["rev"]), I(s["host"]), I(s["db"]), I(s["other"]), I(s["pay"]), I(s["eng"]), I(s["sup"]), I(s["sm"] + s["ga"])])
        sc2.append([N(s["n"]), I(s["rev"]), I(s["cost"]), I(s["op"]), f"{s['margin']*100:.0f}%", I(s["gst"]), I(s["tax"]), I(s["pat"])])
    prof = (table(["Paying cust.", "Users", "Revenue/mo", "Hosting", "Database", "Storage, CDN, SMS, email, monitoring", "Payment fees", "Maintenance + roadmap eng.", "Support", "S&M + G&A"], sc1, num_cols=tuple(range(10))) +
            table(["Paying cust.", "Revenue/mo", "Total cost", "Operating profit", "Op. margin", "GST collected (not revenue)", "Income tax", "Profit after tax"], sc2, num_cols=tuple(range(8))))
    base, srows = M.sens_rows()
    s1 = table(["Sensitivity at 100 customers", "Operating profit ₹/mo", "Change vs base"], [["Base case", I(base), "-"]] + [[a, I(b), I(c)] for a, b, c in srows[:4]], num_cols=(1, 2))
    s2 = table(["Churn", "LTV", "LTV:CAC"], [[a.replace("Churn ", ""), I(b), f"{c:.1f}"] for a, b, c in srows[4:]], num_cols=(1, 2))
    be = M.BE_S
    be_t = formula(f"Operating break-even at {M.BE_N} paying customers (revenue {I(be['rev'])}/mo = costs {I(be['cost'])}/mo)\n"
                   f"Payback of build labour {I(M.L_E)} + setup {I(M.SETUP_COSTS)} = {I(M.L_E + M.SETUP_COSTS)} from 100-customer profit {I(M.SC[3]['op'])}/mo ≈ {(M.L_E + M.SETUP_COSTS)/M.SC[3]['op']:.0f} months; "
                   f"from 250-customer profit {I(M.SC[4]['op'])}/mo ≈ {(M.L_E + M.SETUP_COSTS)/M.SC[4]['op']:.0f} months")
    mt = []
    for m in M.MC:
        mt.append([m["name"], f"{m['hrs']} h", I(m["labour"]), I(m["tp"]), I(m["cost"]), f"{m['margin']*100:.0f}%", f"<b>{I(m['price'])}</b>", m["resp"], m["chan"], m["incl"]])
    maint = table(["Package", "Hours/mo", "Labour", "Third-party", "Cost/mo", "Margin", "Price/mo", "Response target", "Channel", "Includes"], mt, num_cols=(1, 2, 3, 4, 6))
    rec = table(["Recommendation (sample app)", "Value"], [
        ["Custom build - minimum / target price (client #1)", f"{I(cp['minimum'])} / {I(cp['target'])}"], ["Custom build - target price (client #2, reuse)", I(c2["target"])],
        ["White-label setup fee", I(M.WL_SETUP)], ["White-label annual licence", I(M.WL_LICENCE_YR)],
        ["SaaS list prices", " / ".join(f"{p[0]} {I(p[1])}" for p in M.PLANS) + f" / Enterprise from {I(M.ENTERPRISE_FROM)}"],
        ["Annual subscription", "10 × monthly fee (2 months free)"], ["Monthly maintenance (post-handover)", " / ".join(f"{m['name']} {I(m['price'])}" for m in M.MC)],
        ["Target gross margin", f"≥ {M.GM_PCT*100:.0f}% achieved (cost to serve); target operating margin ≥ 25% beyond break-even"],
        ["Break-even customers", f"{M.BE_N}"], ["Expected operating profit at 100 / 250 / 500", " / ".join(I(M.SC[i]['op']) for i in (3, 4, 5)) + " per month"]])
    why = ul(["Variable cost per customer is a small fraction of price, so margin grows with scale; the business is limited by engineering and acquisition cost, not hosting.",
              f"Break-even at {M.BE_N} customers is reachable with an annual acquisition plan (about {M.BE_N/12:.0f} per month for 12 months); validate CAC in the pilot because LTV:CAC of {M.LTV_CAC:.0f} is only as good as the assumed {I(M.CAC)} CAC.",
              "Custom-build price covers risk and company overhead, so a pessimistic outcome still leaves positive margin (check below).",
              "Maintenance packages are priced from real labour + third-party cost with a margin, and have hour caps - no unlimited support."])
    chk = f"<p class=small>Check: at pessimistic labour {I(M.L_P)} the client #1 target price {I(cp['target'])} still earns {(cp['target'] - (M.L_P + cp['setup'] + M.L_P*M.GA_PCT))/cp['target']*100:.0f}% margin before the risk allowance is consumed.</p>"
    return f"""
<h3>9.1 Custom development pricing</h3>{custom}{chk}<h3>9.2 White-label and enterprise</h3>{wl}{ent}
<h3>9.3 SaaS unit economics</h3>{unit}<h3>9.4 Pricing tiers (ex-GST; 18% GST added on invoice)</h3>{plans}{ov}
<p class=small>*Unlimited projects under fair use. Prices are hypotheses until validated in Phase 1 interviews and the pilot.</p>
<h3>9.5 Profitability by paying-customer count (monthly)</h3>{prof}
<p class=small>Maintenance + roadmap engineering hours/mo by scale: {", ".join(f"{k}: {v} h" for k, v in M.ENG_HRS.items() if k in M.CUSTS)} at blended {I(M.BLENDED_ENG)}/h. S&amp;M = max(₹30,000, 20% of revenue). Income tax {M.TAX*100:.2f}% applies only to positive operating profit; verify with the company's CA. Revenue is never profit.</p>
<h3>9.6 Break-even and sensitivity</h3>{be_t}{s1}{s2}
<h3>9.7 Maintenance packages (Phase 13 pricing)</h3>{maint}
{formula("Package cost = eng h × avg(Flutter, Backend rate) + QA h × QA rate + support h × support rate + third-party tools\nPrice = cost ÷ (1 − margin)")}
{callout("bad", "Commercial boundary", "Hours above the package cap are billed at the blended rate plus 20%; unused hours do not roll over. Out of scope: new features, third-party outage, customer-caused data loss, OS versions older than the support matrix. P1 escalation: on-call engineer → tech lead → CTO within 30 / 60 / 120 minutes.")}
<h3>9.8 Commercial recommendation</h3>{rec}{why}
"""
