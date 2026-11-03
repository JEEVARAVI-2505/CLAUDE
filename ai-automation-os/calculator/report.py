"""Developed by V. LOKESHWARAN, VEL Software Solutions, velsoftwaresolutions.in@gmail.com. Print every figure used in docs/02 so the document is reproducible: python report.py"""
import cost_model as cm
c = cm.load()
f = lambda x: f"{x:,.2f}"
print("UNVERIFIED INPUTS:", len(cm.unverified(c)))
for s in ("low", "medium", "high"):
    print(f"\n[{s}] AI/attempt {f(cm.ai_cost_per_attempt_inr(c,s))}  var/attempt {f(cm.variable_cost_per_attempt_inr(c,s))}  per-success {f(cm.cost_per_successful_exec_inr(c,s))}")
print("\nModel comparison (medium, extraction model swapped; escalation unchanged):")
for m in c["models"]:
    print(f"  {m:10s} AI/attempt INR {f(cm.ai_cost_per_attempt_inr(c,'medium',m))}")
print("\nHuman review/exec", f(cm.human_review_cost_per_exec_inr(c)))
print("\nVolume (professional infra, medium):")
for r in cm.volume_table(c): print(f"  {r['executions']:>7} var {f(r['variable_inr']):>12} review {f(r['review_inr']):>12} total {f(r['total_inr']):>12} per-success {f(r['per_success_inr']):>8} reviews {r['review_items']:.0f}")
print("\nInfra tiers:", c["infra_monthly_inr"])
d = cm.dev_cost(c); print("\nDEV", {k: f(v) for k, v in d.items()})
print("PROJECT PRICE", {k: f(v) for k, v in cm.project_price_inr(c).items()})
print("\nTier margin @80% utilisation, infra shared by 25:")
for t in c["tiers"]: print(" ", {k: (f(v) if isinstance(v,float) else v) for k, v in cm.tier_margin(c, t).items()})
print("\nROI professional tier:")
for inv in (500, 1500, 3000, 6000): print(f"  {inv} inv/mo", {k: (f(v) if v else v) for k, v in cm.customer_roi(c, inv, 14999).items()})
print("\nProfitability (professional, 80% util):")
for r in cm.profitability(c): print(" ", {k: (f(v) if isinstance(v,float) else v) for k, v in r.items()})
tm = cm.tier_margin(c, "professional"); print("\nLTV", f(cm.ltv(c, tm["contribution"])), "CAC", c["business"]["cac_inr"], "LTV/CAC", f(cm.ltv(c, tm["contribution"])/c["business"]["cac_inr"]))
print("Break-even customers:", cm.break_even_customers(c))
