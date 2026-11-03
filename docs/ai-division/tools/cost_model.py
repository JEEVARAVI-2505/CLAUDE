#!/usr/bin/env python3
"""Reproducible cost & pricing model for the reference product (KnowledgeDesk).

Every number is an ASSUMPTION in ASSUMPTIONS below. Prices are ILLUSTRATIVE
PLACEHOLDERS: before any real quote, open each provider's official pricing
page, copy the price, and write the date into `price_checked_on`.

Run:  python3 cost_model.py            (prints all tables)
Change a number in ASSUMPTIONS, re-run, and every table updates.
"""
from __future__ import annotations

ASSUMPTIONS = {
    "price_checked_on": "NOT YET VERIFIED - placeholder values",
    "fx_inr_per_usd": 85.0,
    # USD per 1,000,000 tokens (input, output)
    "models": {
        "economical": (0.15, 0.60),
        "balanced": (1.00, 5.00),
        "high": (3.00, 15.00),
    },
    "routing_mix": {"economical": 0.70, "balanced": 0.25, "high": 0.05},
    "tokens_in_per_call": 3000,     # system prompt + history + retrieved chunks
    "tokens_out_per_call": 400,
    "avg_calls_per_answer": 1.3,    # includes retries + fallback + validator
    "cached_input_share": 0.30,     # share of input tokens served from cache
    "cached_price_factor": 0.10,    # cached tokens cost 10% of normal
    "rerank_usd_per_1000_searches": 2.00,
    "embed_usd_per_1m_tokens": 0.02,
    "tokens_per_document": 6000,    # ~12 pages
    "docs_per_user": 20,
    "storage_mb_per_doc": 2.0,
    "storage_usd_per_gb_month": 0.10,
    "answers_per_user_month": 60,
    "human_qa_sample_rate": 0.01,   # 1% of answers reviewed by our staff
    "human_qa_minutes": 3,
    "human_qa_usd_per_hour": 12.0,
    "payment_fee_pct": 0.03,
}

# (max_users, setup_usd, monthly_infra_usd, label) - tiers chosen by users
INFRA_TIERS = [
    (500, 3000, 180, "Lean MVP"),
    (5000, 12000, 1100, "Professional production"),
    (100000, 60000, 7500, "Enterprise production"),
]

PLANS = {  # name: (price_usd_month, users, answers_included, overage_usd_per_answer)
    "Starter": (99, 5, 1500, 0.05),
    "Professional": (399, 25, 10000, 0.04),
    "Business": (1199, 100, 50000, 0.03),
}

OPEX_MONTHLY_USD = 14000   # 5-person team, fully loaded (see 08_stage11)
CAC_USD = 900              # sales+marketing spend per new paying customer
MONTHLY_CHURN = 0.03
ONBOARDING_COST_USD = 400  # staff time per new customer (one-time)


# role: (fully_loaded_usd_per_hour, hours) - one-time development, Stages 1-13
DEV_HOURS = {
    "Lead / AI Architect": (30, 600),
    "AI / LLM Engineers (x2)": (22, 1800),
    "Backend Engineer": (20, 700),
    "Frontend Engineer": (18, 500),
    "Product Manager": (22, 450),
    "QA / Evaluation Engineer": (14, 450),
    "DevOps (part-time)": (22, 200),
    "Security (part-time)": (28, 120),
    "Support / Docs": (10, 120),
}


def dev_cost():
    """Development Cost = Estimated Hours x Fully Loaded Hourly Cost, per role."""
    return [(r, rate, h, rate * h) for r, (rate, h) in DEV_HOURS.items()]


def cost_per_answer(a=ASSUMPTIONS) -> dict:
    """Return cost components (USD) of one answered question."""
    blended_in = sum(a["routing_mix"][m] * a["models"][m][0] for m in a["models"])
    blended_out = sum(a["routing_mix"][m] * a["models"][m][1] for m in a["models"])
    cached = a["cached_input_share"]
    eff_in = blended_in * (1 - cached + cached * a["cached_price_factor"])
    tok_in, tok_out = a["tokens_in_per_call"], a["tokens_out_per_call"]
    llm = a["avg_calls_per_answer"] * (tok_in * eff_in + tok_out * blended_out) / 1e6
    rerank = a["rerank_usd_per_1000_searches"] / 1000
    qa = a["human_qa_sample_rate"] * a["human_qa_minutes"] / 60 * a["human_qa_usd_per_hour"]
    return {"llm": llm, "rerank": rerank, "human_qa": qa, "total": llm + rerank + qa}


def per_user_fixed(a=ASSUMPTIONS) -> dict:
    """Monthly per-user amortised ingestion + storage cost (USD)."""
    embed = a["docs_per_user"] * a["tokens_per_document"] * a["embed_usd_per_1m_tokens"] / 1e6 / 12
    storage = a["docs_per_user"] * a["storage_mb_per_doc"] / 1024 * a["storage_usd_per_gb_month"]
    return {"embed": embed, "storage": storage, "total": embed + storage}


def tier_for(users: int):
    for t in INFRA_TIERS:
        if users <= t[0]:
            return t
    return INFRA_TIERS[-1]


def scenarios(user_counts=(100, 500, 1000, 5000, 10000, 25000, 50000, 100000)):
    a = ASSUMPTIONS
    cpa, puf = cost_per_answer(), per_user_fixed()
    rows = []
    for u in user_counts:
        answers = u * a["answers_per_user_month"]
        ai = answers * cpa["llm"] + answers * cpa["rerank"]
        qa = answers * cpa["human_qa"]
        fixed_users = u * puf["total"]
        _, setup, infra, label = tier_for(u)
        extra = max(0, u - INFRA_TIERS[-1][0]) / 100000 * INFRA_TIERS[-1][2]
        infra_total = infra + extra
        total = ai + qa + fixed_users + infra_total
        peak = max(1, round(u * 0.05))  # 5% of users concurrently active at peak
        rows.append((u, answers, peak, ai, qa, fixed_users, infra_total, total, total / u, label, setup))
    return rows


def plan_economics():
    a, cpa, puf = ASSUMPTIONS, cost_per_answer(), per_user_fixed()
    out = []
    for name, (price, users, included, _over) in PLANS.items():
        cogs = included * (cpa["llm"] + cpa["rerank"] + cpa["human_qa"]) + users * puf["total"]
        cogs += price * a["payment_fee_pct"]
        gm = price - cogs
        out.append((name, price, users, included, cogs, gm, gm / price))
    return out


def unit_economics(plan="Professional", customers=(10, 25, 50, 100, 200)):
    price = PLANS[plan][0]
    gm_pct = next(r[6] for r in plan_economics() if r[0] == plan)
    gm = price * gm_pct
    ltv = gm / MONTHLY_CHURN
    rows = [(n, n * price, n * gm, n * gm - OPEX_MONTHLY_USD) for n in customers]
    be = OPEX_MONTHLY_USD / gm
    return gm, ltv, ltv / CAC_USD, be, rows


def main():
    a = ASSUMPTIONS
    print(f"Price check status: {a['price_checked_on']}\n")
    cpa, puf = cost_per_answer(), per_user_fixed()
    print("== One-time development cost (hours x loaded rate) ==")
    rows = dev_cost()
    for r, rate, h, c in rows:
        print(f"  {r:26s} ${rate:>3}/h x {h:>5} h = ${c:>9,.0f}")
    print(f"  {'TOTAL':26s} {sum(x[2] for x in rows):>16} h   ${sum(x[3] for x in rows):>9,.0f}\n")

    print("== Cost per answered question (USD) ==")
    for k, v in cpa.items():
        print(f"  {k:9s} {v:.5f}")
    print(f"  per-user monthly ingestion+storage: {puf['total']:.5f}\n")

    print("== Usage scenarios (monthly, USD) ==")
    print(f"{'users':>7} {'answers':>10} {'peakConc':>8} {'AI':>9} {'humanQA':>8} {'ingest':>7} {'infra':>8} {'TOTAL':>9} {'$/user':>7}  production model (setup)")
    for u, ans, pk, ai, qa, fx, inf, tot, pu, label, setup in scenarios():
        print(f"{u:7d} {ans:10d} {pk:8d} {ai:9.0f} {qa:8.0f} {fx:7.0f} {inf:8.0f} {tot:9.0f} {pu:7.2f}  {label} (${setup:,})")

    print("\n== Plan economics at full included usage ==")
    print(f"{'plan':13s} {'price':>6} {'users':>5} {'answers':>8} {'COGS':>8} {'gross$':>8} {'GM%':>6}")
    for n, p, u, inc, c, g, pct in plan_economics():
        print(f"{n:13s} {p:6d} {u:5d} {inc:8d} {c:8.2f} {g:8.2f} {pct*100:5.1f}%")

    gm, ltv, ratio, be, rows = unit_economics()
    print(f"\n== Unit economics (Professional) ==\n  gross margin/customer/month ${gm:.2f}  LTV ${ltv:,.0f}  CAC ${CAC_USD}  LTV:CAC {ratio:.1f}  "
          f"onboarding ${ONBOARDING_COST_USD}  break-even customers {be:.1f}")
    print(f"  {'customers':>9} {'revenue':>9} {'gross':>9} {'operating profit':>17}   (opex ${OPEX_MONTHLY_USD:,}/month)")
    for n, rev, g, op in rows:
        print(f"  {n:9d} {rev:9,.0f} {g:9,.0f} {op:17,.0f}")
    print(f"\nINR view: 1 USD = {a['fx_inr_per_usd']} INR. Multiply any USD figure by that rate.")


if __name__ == "__main__":
    main()
