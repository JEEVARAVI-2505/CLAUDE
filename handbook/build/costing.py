"""Deterministic costing model used by the handbook (all rates are ASSUMPTIONS in INR, ex-GST)."""

# Internal fully-loaded cost per person-day (salary + overhead share). Assumption — replace with finance data.
COST_PER_DAY = {
    "Discovery": 4000, "Business analysis": 4000, "Architecture": 8000, "UI/UX": 4000,
    "Development": 4500, "Database": 5000, "Integration": 5000, "Migration": 5000,
    "QA": 3000, "Deployment": 5500, "Training": 3500, "Documentation": 3500,
}
PM_COST_PER_DAY = 6000
PM_RATIO = 0.12          # PM effort as share of all other effort days
TARGET_MARGIN = 0.40     # gross margin target on price
# phase effort as a multiple of development days (calibration constants — recalibrate from actuals every quarter)
RATIO = {"Discovery": 0.08, "Business analysis": 0.12, "Architecture": 0.06, "UI/UX": 0.12,
         "Database": 0.10, "QA": 0.30, "Deployment": 0.05, "Training": 0.05, "Documentation": 0.06}

EXAMPLES = {
    "Small": dict(
        name="Single-clinic appointment & billing system", team=3, contingency=0.10, support_pct=0.15, upgrade_pct=0.08,
        modules={"Patient registration": 6, "Appointments & slots": 8, "Billing & receipts": 10, "Reports": 4, "Users, roles & audit": 5},
        integration=4, migration=3, licences=0, hosting_month=6000, tools="Free/open-source stack; SMS/WhatsApp provider charges billed to client"),
    "Medium": dict(
        name="Distributor inventory + billing + CRM with Tally integration", team=5, contingency=0.15, support_pct=0.18, upgrade_pct=0.10,
        modules={"Masters, users & roles": 12, "Inventory & warehouses": 25, "Sales & billing": 28, "Purchase & GRN": 15,
                 "CRM (leads, pipeline)": 18, "Approval workflow": 10, "Reports & dashboards": 15},
        integration=12, migration=10, licences=25000, hosting_month=20000, tools="Managed PostgreSQL + app hosting; allowance for client-specific licences"),
    "Enterprise": dict(
        name="Multi-branch hospital group management system", team=9, contingency=0.20, support_pct=0.20, upgrade_pct=0.12,
        modules={"Registration & EMR core": 30, "Appointments": 25, "OPD/IPD": 60, "Billing & insurance": 45, "Pharmacy": 40,
                 "Laboratory": 35, "Inventory & purchase": 30, "Workflow & approvals": 25, "Reports & BI": 30,
                 "Security, RBAC & audit": 30, "Multi-branch / tenant isolation": 25},
        integration=45, migration=35, licences=200000, hosting_month=85000, tools="HA managed database, monitoring, backups; allowance for third-party tools"),
}


def inr(x: float) -> str:
    """Format with Indian digit grouping, e.g. 1234567 -> ₹12,34,567."""
    n = int(round(x))
    s = str(abs(n))
    if len(s) > 3:
        head, tail = s[:-3], s[-3:]
        parts = []
        while len(head) > 2:
            parts.insert(0, head[-2:])
            head = head[:-2]
        if head:
            parts.insert(0, head)
        s = ",".join(parts + [tail])
    return ("-" if n < 0 else "") + "₹" + s


def compute(key: str, overhead_month: float = 300000.0) -> dict:
    e = EXAMPLES[key]
    dev = float(sum(e["modules"].values()))
    days = {"Development": dev, "Integration": float(e["integration"]), "Migration": float(e["migration"])}
    for k, r in RATIO.items():
        days[k] = round(dev * r, 1)
    base_days = sum(days.values())
    days["Project management"] = round(base_days * PM_RATIO, 1)
    cost_rate = dict(COST_PER_DAY, **{"Project management": PM_COST_PER_DAY})
    lines = []
    for k, d in days.items():
        lines.append((k, d, cost_rate[k], d * cost_rate[k]))
    labour = sum(l[3] for l in lines)
    contingency = labour * e["contingency"]
    build_months = max(1, round(sum(days.values()) / (e["team"] * 18) + 0.4))
    hosting_build = e["hosting_month"] * build_months
    direct = labour + contingency + e["licences"] + hosting_build
    price = direct / (1 - TARGET_MARGIN)
    margin = price - direct
    support = price * e["support_pct"]
    upgrades = price * e["upgrade_pct"]
    recurring_hosting = e["hosting_month"] * 12 * 1.15      # cost + 15 % management fee
    annual_recurring = support + recurring_hosting + upgrades
    total_dev = dev
    # module-wise price: module share of development days x (price minus integration, migration, licences, hosting)
    shared = (days["Integration"] * cost_rate["Integration"] + days["Migration"] * cost_rate["Migration"])
    shared_price = shared * (1 + e["contingency"]) / (1 - TARGET_MARGIN)
    fixed_rest = price - shared_price - (e["licences"] + hosting_build) / (1 - TARGET_MARGIN)
    module_price = {m: fixed_rest * d / total_dev for m, d in e["modules"].items()}
    contribution = margin
    breakeven_projects = 12 * overhead_month / contribution
    blended_cost = labour / sum(days.values())
    rate_card_day = blended_cost / (1 - TARGET_MARGIN)
    return dict(e=e, days=days, lines=lines, labour=labour, contingency=contingency, hosting_build=hosting_build,
                build_months=build_months, direct=direct, price=price, margin=margin, support=support,
                upgrades=upgrades, recurring_hosting=recurring_hosting, annual_recurring=annual_recurring,
                module_price=module_price, shared_price=shared_price, licence_hosting_price=(e["licences"] + hosting_build) / (1 - TARGET_MARGIN),
                breakeven_projects=breakeven_projects, blended_cost=blended_cost, rate_card_day=rate_card_day,
                total_days=sum(days.values()), overhead_month=overhead_month)


def change_request(key: str, days_by_role: dict[str, float], after_freeze: bool = True) -> dict:
    r = compute(key)
    rate = {k: v / (1 - TARGET_MARGIN) for k, v in COST_PER_DAY.items()}
    rate["Project management"] = PM_COST_PER_DAY / (1 - TARGET_MARGIN)
    assess = 0.5 * rate["Business analysis"]
    work = sum(d * rate[k] for k, d in days_by_role.items())
    premium = 0.20 * work if after_freeze else 0.0
    return dict(assess=assess, work=work, premium=premium, total=assess + work + premium, rate=rate)
