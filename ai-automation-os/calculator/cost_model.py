"""Developed by V. LOKESHWARAN, VEL Software Solutions, velsoftwaresolutions.in@gmail.com

AI automation cost, pricing and profitability model.

Every formula maps to a section of the operating framework (Phases 8-9).
All money is INR unless a name ends in _usd. Inputs live in pricing_inputs.json;
nothing is hardcoded here. Deterministic and pure.
"""
from __future__ import annotations
import json
from pathlib import Path

VOLUMES = [100, 500, 1000, 5000, 10000, 25000, 50000, 100000]
CUSTOMER_COUNTS = [5, 10, 25, 50, 100, 250, 500]


def load(path: str | Path | None = None) -> dict:
    p = Path(path) if path else Path(__file__).with_name("pricing_inputs.json")
    return json.loads(p.read_text())


def unverified(cfg: dict) -> list[str]:
    """Names of every priced input whose verified flag is not true."""
    out: list[str] = []
    def walk(node, name):
        if isinstance(node, dict):
            if "verified" in node and node["verified"] is not True:
                out.append(name)
            for k, v in node.items():
                walk(v, f"{name}.{k}")
    walk(cfg, "")
    return sorted(set(n.lstrip(".").rsplit(".", 0)[0] for n in out))


def token_cost_usd(model: dict, in_tok: float, out_tok: float) -> float:
    """(in/1e6 x in price) + (out/1e6 x out price)."""
    return in_tok / 1e6 * model["in_per_mtok_usd"] + out_tok / 1e6 * model["out_per_mtok_usd"]


def ai_cost_per_attempt_inr(cfg: dict, scenario: str = "medium", model_override: str | None = None) -> float:
    """Expected AI cost of one execution attempt, incl. retries and escalation."""
    w, fx = cfg["workload"], cfg["fx"]["usd_inr"]
    ext = w["extraction"]
    m = cfg["models"][model_override or ext["model"]]
    base = token_cost_usd(m, ext["in_tok"][scenario], ext["out_tok"][scenario])
    esc = w["escalation"]
    esc_cost = esc["rate"] * token_cost_usd(cfg["models"][esc["model"]], esc["in_tok"], esc["out_tok"])
    return (base * (1 + w["retry_rate"]) + esc_cost) * fx


def variable_cost_per_attempt_inr(cfg: dict, scenario: str = "medium", model_override: str | None = None) -> float:
    w = cfg["workload"]
    return (ai_cost_per_attempt_inr(cfg, scenario, model_override)
            + w["pages_per_invoice"] * cfg["ocr_per_page_inr"]["value"]
            + w["whatsapp_msgs_per_exec"] * cfg["whatsapp_per_msg_inr"]["value"]
            + w["other_variable_inr_per_exec"])


def human_review_cost_per_exec_inr(cfg: dict) -> float:
    """Review minutes x review rate x hourly / 60 (Phase 9F)."""
    w = cfg["workload"]
    return w["human_review_minutes"] * w["human_review_rate"] * cfg["labour_hourly_inr"]["reviewer"] / 60


def cost_per_successful_exec_inr(cfg: dict, scenario: str = "medium", model_override: str | None = None) -> float:
    """Failed attempts still cost money: variable / success_rate."""
    return variable_cost_per_attempt_inr(cfg, scenario, model_override) / (1 - cfg["workload"]["failure_rate"])


def volume_table(cfg: dict, scenario: str = "medium", tier: str = "professional") -> list[dict]:
    infra = cfg["infra_monthly_inr"][tier]
    w = cfg["workload"]
    rows = []
    for n in VOLUMES:
        var = variable_cost_per_attempt_inr(cfg, scenario) * n
        ok = n * (1 - w["failure_rate"])
        review = human_review_cost_per_exec_inr(cfg) * n
        rows.append({"executions": n, "variable_inr": var, "review_inr": review,
                     "infra_inr": infra, "total_inr": var + review + infra,
                     "per_success_inr": (var + review + infra) / ok,
                     "review_items": n * w["human_review_rate"]})
    return rows


def dev_cost(cfg: dict) -> dict:
    """Labour = hours x hourly; + overhead; x optimistic/expected/pessimistic."""
    rate = cfg["labour_hourly_inr"]
    labour = sum(h * rate[r] for r, h in cfg["dev_hours"].items())
    loaded = labour * (1 + cfg["overhead_pct"])
    return {"hours": sum(cfg["dev_hours"].values()), "labour": labour, "loaded": loaded,
            **{k: loaded * m for k, m in cfg["estimate_multipliers"].items()}}


def monthly_cost_to_serve_inr(cfg: dict, executions: int, infra_tier: str = "professional") -> float:
    """Infra + variable + review + maintenance labour for ONE customer at `executions`/month.
    Shared infra is allocated per customer by the caller (see profitability)."""
    m = cfg["maintenance_hours_per_month"]
    r = cfg["labour_hourly_inr"]
    maint = sum(h * r[k] for k, h in m.items())
    return (variable_cost_per_attempt_inr(cfg) * executions
            + human_review_cost_per_exec_inr(cfg) * executions + maint)


def tier_revenue_inr(cfg: dict, tier: str, executions: int) -> float:
    t = cfg["tiers"][tier]
    return t["monthly_inr"] + max(0, executions - t["included_exec"]) * t["overage_inr"]


def tier_margin(cfg: dict, tier: str, utilisation: float = 0.8, shared_customers: int = 25) -> dict:
    """Contribution margin per customer at `utilisation` of included executions;
    professional infra shared across `shared_customers`."""
    t = cfg["tiers"][tier]
    n = int(t["included_exec"] * utilisation)
    rev = tier_revenue_inr(cfg, tier, n)
    cost = monthly_cost_to_serve_inr(cfg, n) + cfg["infra_monthly_inr"]["professional"] / shared_customers
    return {"tier": tier, "executions": n, "revenue": rev, "cost_to_serve": cost,
            "contribution": rev - cost, "margin_pct": (rev - cost) / rev}


def project_price_inr(cfg: dict, third_party_inr: float = 0.0) -> dict:
    """Price = (dev + third party) x (1+risk) / (1 - target margin)."""
    d = dev_cost(cfg)["expected"]
    base = d + third_party_inr
    with_risk = base * (1 + cfg["risk_allowance_pct"])
    return {"cost_base": base, "with_risk": with_risk, "price": with_risk / (1 - cfg["target_margin_pct"])}


def customer_roi(cfg: dict, invoices_per_month: int, subscription_inr: float) -> dict:
    """Net monthly benefit = realisable labour saving - subscription; payback = investment / net."""
    c = cfg["customer"]
    hours_saved = invoices_per_month * c["manual_minutes_per_invoice"] / 60
    saving = hours_saved * c["staff_hourly_inr"] * c["realisable_saving_pct"]
    net = saving - subscription_inr
    return {"hours_saved": hours_saved, "labour_saving": saving, "net_monthly": net,
            "payback_months": (c["implementation_investment_inr"] / net) if net > 0 else None}


def profitability(cfg: dict, tier: str = "professional", utilisation: float = 0.8) -> list[dict]:
    """Company P&L by customer count. Infra scales in steps of 50 customers per
    professional stack; fixed company cost is constant; tax shown separately."""
    b, t = cfg["business"], cfg["tiers"][tier]
    n_exec = int(t["included_exec"] * utilisation)
    rows = []
    for n in CUSTOMER_COUNTS:
        mrr = n * tier_revenue_inr(cfg, tier, n_exec)
        ai_api = n * variable_cost_per_attempt_inr(cfg) * n_exec
        review = n * human_review_cost_per_exec_inr(cfg) * n_exec
        stacks = -(-n // 50)
        infra = stacks * cfg["infra_monthly_inr"]["professional"]
        m = cfg["maintenance_hours_per_month"]; r = cfg["labour_hourly_inr"]
        maint = n * sum(h * r[k] for k, h in m.items()) * (0.5 if n >= 25 else 1.0)
        sales = mrr * b["sales_cost_pct_of_mrr"]
        gross = mrr - ai_api - review - infra - maint
        op = gross - sales - b["fixed_company_monthly_inr"]
        rows.append({"customers": n, "mrr": mrr, "arr": mrr * 12, "ai_api": ai_api, "review": review,
                     "infra": infra, "maintenance_support": maint, "sales": sales,
                     "gross_margin_pct": gross / mrr, "operating_profit": op,
                     "tax": max(0.0, op) * b["tax_rate"]})
    return rows


def ltv(cfg: dict, contribution_per_month: float) -> float:
    """LTV = monthly contribution / monthly churn."""
    return contribution_per_month / cfg["business"]["monthly_churn"]


def break_even_customers(cfg: dict, tier: str = "professional", utilisation: float = 0.8) -> int | None:
    for n in range(1, 2001):
        row = next(iter(_profit_at(cfg, tier, utilisation, n)))
        if row["operating_profit"] >= 0:
            return n
    return None


def _profit_at(cfg, tier, utilisation, n):
    global CUSTOMER_COUNTS
    saved = CUSTOMER_COUNTS
    CUSTOMER_COUNTS = [n]
    try:
        return profitability(cfg, tier, utilisation)
    finally:
        CUSTOMER_COUNTS = saved


def assert_quotable(cfg: dict) -> None:
    """Gate 9: refuse to quote on unverified prices."""
    bad = unverified(cfg)
    if bad:
        raise RuntimeError("Unverified price inputs, cannot quote: " + ", ".join(bad))
