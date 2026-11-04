"""Costing model - the single source of numbers for the handbook tables AND the Excel workbook.

IMPORTANT: every rate and every vendor figure below is a PLACEHOLDER (illustrative, in generic currency units "CU").
No vendor prices are asserted. Replace them using each vendor's official pricing page / calculator and record the
source and date in the 'Vendor Pricing Log' sheet.
"""
from __future__ import annotations
from math import ceil

SIZES = ["Small", "Medium", "Enterprise"]
DESCR = {
    "Small": "Corporate website - about 8 pages, CMS-lite, contact form, SEO basics",
    "Medium": "Booking / customer portal - login, roles, database, payment gateway, admin screens",
    "Enterprise": "Multi-tenant SaaS - roles, billing, dashboards, integrations, audit, high availability",
}

# internal cost per hour (CU) - PLACEHOLDER rate card, set by Finance
RATES = {"Project Manager": 25, "Technical Lead": 32, "UI/UX Designer": 20, "Frontend Developer": 18,
         "Backend Developer": 20, "Database Engineer": 20, "Full-stack Developer": 20, "QA Engineer": 15, "DevOps Engineer": 22}

# (line, role, small, medium, enterprise hours)
LINES = [
    ("Discovery and planning", "Project Manager", 12, 40, 140),
    ("Architecture and technical-lead review", "Technical Lead", 4, 24, 100),
    ("UI/UX design", "UI/UX Designer", 20, 70, 240),
    ("Frontend development", "Frontend Developer", 40, 160, 620),
    ("Backend development", "Backend Developer", 10, 180, 760),
    ("Database design and implementation", "Database Engineer", 0, 30, 150),
    ("Integrations (APIs, payment, email)", "Full-stack Developer", 6, 40, 180),
    ("Testing and QA", "QA Engineer", 12, 60, 260),
    ("Security review", "Technical Lead", 4, 16, 80),
    ("Deployment and DevOps", "DevOps Engineer", 8, 24, 120),
    ("Project management", "Project Manager", 16, 56, 220),
]
ONE_TIME_THIRD_PARTY = {"Small": 40, "Medium": 150, "Enterprise": 600}   # licences / plugins / stock assets (PLACEHOLDER)
CONTINGENCY = {"Small": 0.10, "Medium": 0.15, "Enterprise": 0.20}
TARGET_MARGIN = {"Small": 0.40, "Medium": 0.40, "Enterprise": 0.35}
COMMISSION = 0.05                      # referral / sales commission on price (variable cost) - PLACEHOLDER
FIXED_ANNUAL = 360_000                 # salaries + overhead (non-project) per year - PLACEHOLDER
ROUND_TO = 100

# monthly vendor pass-through (CU) - PLACEHOLDERS to replace from official calculators
OPS = [("Hosting / compute", 12, 85, 900), ("Domain and DNS (annual cost / 12)", 1.25, 1.25, 2.5), ("SSL/TLS certificate (if chargeable)", 0, 0, 0),
       ("Email service", 4, 15, 80), ("Storage and backups", 2, 10, 120), ("Bandwidth / CDN", 3, 20, 250),
       ("Monitoring and logging", 0, 15, 150), ("Software licences / SaaS", 5, 25, 300)]
OPS_LABOUR = [("Maintenance and patching", "DevOps Engineer", 3, 10, 40), ("Support (tickets)", "Full-stack Developer", 2, 8, 30),
              ("Account management", "Project Manager", 0.5, 2, 8)]
OPS_CONTINGENCY = 0.05
OPS_MARGIN = 0.45
VENDOR_INFLATION, WAGE_INFLATION = 0.05, 0.06          # renewal assumptions - PLACEHOLDERS


def r2(x): return round(x + 1e-9, 2)
def money(x, d=0): return f"{x:,.{d}f}"
def pct(x): return f"{x * 100:.1f}%"


def model() -> dict:
    out = {}
    for i, size in enumerate(SIZES):
        hours = {l[0]: l[2 + i] for l in LINES}
        lab = {l[0]: l[2 + i] * RATES[l[1]] for l in LINES}
        labour = sum(lab.values())
        direct = ONE_TIME_THIRD_PARTY[size]
        subtotal = labour + direct
        cont = subtotal * CONTINGENCY[size]
        cost = subtotal + cont
        price = ceil(cost / (1 - TARGET_MARGIN[size]) / ROUND_TO) * ROUND_TO
        gross = price - cost
        var = direct + COMMISSION * price
        contrib = price - var
        be_projects = FIXED_ANNUAL / contrib
        tot_h = sum(hours.values())
        # break-even hours: hours the team can spend (at blended rate) before gross profit reaches zero
        blended = labour / tot_h
        be_hours = (price - direct - cont - COMMISSION * price) / blended
        ops_vendor = sum(o[1 + i] for o in OPS)
        ops_lab_hours = sum(o[2 + i] for o in OPS_LABOUR)
        ops_lab = sum(o[2 + i] * RATES[o[1]] for o in OPS_LABOUR)
        ops_cost = (ops_vendor + ops_lab) * (1 + OPS_CONTINGENCY)
        monthly_fee = ceil(ops_cost / (1 - OPS_MARGIN) / 5) * 5
        annual_cost = ops_cost * 12
        annual_fee = monthly_fee * 12
        next_cost = (ops_vendor * (1 + VENDOR_INFLATION) + ops_lab * (1 + WAGE_INFLATION)) * (1 + OPS_CONTINGENCY)
        renewal_month = max(monthly_fee, ceil(next_cost / (1 - OPS_MARGIN) / 5) * 5)
        out[size] = dict(hours=hours, lab=lab, labour=labour, direct=direct, subtotal=subtotal, cont=cont, cost=cost, price=price,
                         gross=gross, gm=gross / price, var=var, contrib=contrib, cm=contrib / price, be_projects=be_projects,
                         tot_h=tot_h, blended=blended, be_hours=be_hours, ops_vendor=ops_vendor, ops_lab=ops_lab, ops_lab_hours=ops_lab_hours,
                         ops_cost=ops_cost, monthly_fee=monthly_fee, annual_cost=annual_cost, annual_fee=annual_fee,
                         ops_gm=(monthly_fee - ops_cost) / monthly_fee, renewal_month=renewal_month, renewal_year=renewal_month * 12,
                         markup=(price - cost) / cost)
    return out


def md_table(head, rows):
    s = "| " + " | ".join(head) + " |\n|" + "|".join("---" for _ in head) + "|\n"
    return s + "\n".join("| " + " | ".join(str(c) for c in r) + " |" for r in rows) + "\n"


def markdown(key: str) -> str:
    m = model()
    if key == "ratecard":
        return md_table(["Role", "Internal cost / hour (CU)"], [(k, v) for k, v in RATES.items()])
    if key == "assumptions":
        rows = [("Contingency (small / medium / enterprise)", " / ".join(pct(CONTINGENCY[s]) for s in SIZES)),
                ("Target gross margin (price = cost / (1 - margin))", " / ".join(pct(TARGET_MARGIN[s]) for s in SIZES)),
                ("Price rounding", f"up to the next {ROUND_TO} CU"), ("Referral/sales commission (variable)", pct(COMMISSION)),
                ("Company fixed costs per year (salaries + overhead)", f"{money(FIXED_ANNUAL)} CU"),
                ("Monthly service contingency / target margin", f"{pct(OPS_CONTINGENCY)} / {pct(OPS_MARGIN)}"),
                ("Renewal inflation - vendor / wages", f"{pct(VENDOR_INFLATION)} / {pct(WAGE_INFLATION)}")]
        return md_table(["Assumption (all placeholders)", "Value"], rows)
    if key == "hours":
        rows = [(l[0], l[1], l[2], l[3], l[4]) for l in LINES]
        rows.append(("**Total hours**", "", *[f"**{m[s]['tot_h']}**" for s in SIZES]))
        return md_table(["Work item", "Role", "Small (h)", "Medium (h)", "Enterprise (h)"], rows)
    if key == "project":
        rows = [(l[0], *[money(m[s]["lab"][l[0]]) for s in SIZES]) for l in LINES]
        rows += [("**Labour subtotal**", *[f"**{money(m[s]['labour'])}**" for s in SIZES]),
                 ("Third-party one-time (placeholder)", *[money(m[s]["direct"]) for s in SIZES]),
                 ("Subtotal", *[money(m[s]["subtotal"]) for s in SIZES]),
                 ("Contingency", *[f"{money(m[s]['cont'])} ({pct(CONTINGENCY[s])})" for s in SIZES]),
                 ("**Total delivery cost**", *[f"**{money(m[s]['cost'])}**" for s in SIZES]),
                 ("Target margin", *[pct(TARGET_MARGIN[s]) for s in SIZES]),
                 ("**Selling price** (cost / (1 - margin), rounded up)", *[f"**{money(m[s]['price'])}**" for s in SIZES]),
                 ("Gross profit (price - cost incl. contingency)", *[money(m[s]["gross"]) for s in SIZES]),
                 ("**Gross margin %**", *[f"**{pct(m[s]['gm'])}**" for s in SIZES]),
                 ("Equivalent markup on cost", *[pct(m[s]["markup"]) for s in SIZES])]
        return md_table(["Cost line (CU)", "Small", "Medium", "Enterprise"], rows)
    if key == "margins":
        rows = [("Variable costs (third-party + commission)", *[money(m[s]["var"]) for s in SIZES]),
                ("Contribution margin (price - variable costs)", *[money(m[s]["contrib"]) for s in SIZES]),
                ("Contribution margin %", *[pct(m[s]["cm"]) for s in SIZES]),
                ("Break-even: projects per year to cover fixed costs", *[f"{m[s]['be_projects']:.1f}" for s in SIZES]),
                ("Blended labour cost per hour", *[f"{m[s]['blended']:.2f}" for s in SIZES]),
                ("Break-even hours (gross profit reaches zero)", *[f"{m[s]['be_hours']:.0f} vs {m[s]['tot_h']} planned" for s in SIZES])]
        return md_table(["Measure", "Small", "Medium", "Enterprise"], rows)
    if key == "ops":
        rows = [(o[0], *[money(o[1 + i], 2) for i in range(3)]) for o in OPS]
        rows.append(("**Vendor pass-through / month**", *[f"**{money(m[s]['ops_vendor'], 2)}**" for s in SIZES]))
        for o in OPS_LABOUR:
            rows.append((f"{o[0]} ({o[1]}, {o[2]}/{o[3]}/{o[4]} h)", *[money(o[2 + i] * RATES[o[1]], 2) for i in range(3)]))
        rows += [("Service labour / month", *[money(m[s]["ops_lab"], 2) for s in SIZES]),
                 ("Contingency 5%", *[money((m[s]["ops_vendor"] + m[s]["ops_lab"]) * OPS_CONTINGENCY, 2) for s in SIZES]),
                 ("**Monthly operating cost**", *[f"**{money(m[s]['ops_cost'], 2)}**" for s in SIZES]),
                 ("**Annual operating cost** (x 12)", *[f"**{money(m[s]['annual_cost'])}**" for s in SIZES]),
                 ("Monthly service fee (cost / (1 - 45%), rounded up to 5)", *[money(m[s]["monthly_fee"]) for s in SIZES]),
                 ("Annual service fee (MRR x 12 = ARR)", *[money(m[s]["annual_fee"]) for s in SIZES]),
                 ("Service gross margin", *[pct(m[s]["ops_gm"]) for s in SIZES]),
                 ("**Renewal fee / month** (next-year cost, never below current)", *[f"**{money(m[s]['renewal_month'])}**" for s in SIZES]),
                 ("Renewal fee / year", *[money(m[s]["renewal_year"]) for s in SIZES])]
        return md_table(["Operating cost line (CU / month)", "Small", "Medium", "Enterprise"], rows)
    if key == "worked":
        x, lab = m["Small"], m["Small"]["lab"]
        L = [f"Labour      = sum(hours x rate)           = {money(x['labour'])} CU   ({x['tot_h']} h, blended {x['blended']:.2f} CU/h)",
             f"Third-party = one-time licences/assets    = {money(x['direct'])} CU",
             f"Subtotal    = {money(x['labour'])} + {money(x['direct'])}                  = {money(x['subtotal'])} CU",
             f"Contingency = {money(x['subtotal'])} x {pct(CONTINGENCY['Small'])}              = {money(x['cont'], 1)} CU",
             f"Total cost  = {money(x['subtotal'])} + {money(x['cont'], 1)}              = {money(x['cost'], 1)} CU",
             f"Price       = cost / (1 - {TARGET_MARGIN['Small']:.2f})  = {money(x['cost'], 1)} / {1 - TARGET_MARGIN['Small']:.2f} = {money(x['cost'] / (1 - TARGET_MARGIN['Small']), 1)} -> rounded up to {money(x['price'])} CU",
             f"Gross profit= {money(x['price'])} - {money(x['cost'], 1)}             = {money(x['gross'], 1)} CU",
             f"Gross margin= {money(x['gross'], 1)} / {money(x['price'])}               = {pct(x['gm'])}",
             f"Markup      = {money(x['gross'], 1)} / {money(x['cost'], 1)}               = {pct(x['markup'])}   (markup is NOT margin)",
             f"Contribution= price - (third-party + {pct(COMMISSION)} commission) = {money(x['price'])} - {money(x['var'])} = {money(x['contrib'])} CU ({pct(x['cm'])})",
             f"Break-even  = fixed costs / contribution  = {money(FIXED_ANNUAL)} / {money(x['contrib'])} = {x['be_projects']:.1f} small projects per year"]
        return "```text\n" + "\n".join(L) + "\n```\n"
    if key == "summary":
        rows = [(s, DESCR[s], money(m[s]["cost"]), money(m[s]["price"]), pct(m[s]["gm"]), money(m[s]["monthly_fee"]), money(m[s]["annual_fee"])) for s in SIZES]
        return md_table(["Size", "Typical scope", "Delivery cost", "Selling price", "Gross margin", "Service / month", "Service / year"], rows)
    raise KeyError(key)


if __name__ == "__main__":
    import json
    for s, v in model().items():
        print(s, {k: (round(x, 2) if isinstance(x, float) else x) for k, x in v.items() if not isinstance(x, dict)})
