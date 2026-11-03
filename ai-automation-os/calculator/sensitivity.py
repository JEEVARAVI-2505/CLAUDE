"""Sensitivity of Professional-tier economics to the assumptions that matter (docs/04 section 7).
Developed by V. LOKESHWARAN, VEL Software Solutions, velsoftwaresolutions.in@gmail.com"""
import copy
import cost_model as cm

def run(label, mutate):
    c = cm.load(); mutate(c)
    m = cm.tier_margin(c, "professional")
    be = cm.break_even_customers(c)
    print(f"{label:34s} contribution {m['contribution']:>9,.0f}  margin {m['margin_pct']*100:6.1f}%  break-even {be}")

def set_(path, v):
    def f(c):
        d = c
        for k in path[:-1]: d = d[k]
        d[path[-1]] = v
    return f

def scale_support(x):
    def f(c):
        for k in c["maintenance_hours_per_month"]: c["maintenance_hours_per_month"][k] *= x
    return f

print("Baseline"); run("baseline", lambda c: None)
print("\nAuto-approval (human review rate)")
for r in (0.40, 0.20, 0.10): run(f"review rate {r:.0%}", set_(["workload","human_review_rate"], r))
print("\nSupport/maintenance hours per customer")
for x in (0.5, 1, 2): run(f"support hours x{x}", scale_support(x))
print("\nModel price x2 and tokens x1.5")
def pricey(c):
    for m in c["models"].values():
        if isinstance(m, dict): m["in_per_mtok_usd"] *= 2; m["out_per_mtok_usd"] *= 2
    for s in ("low","medium","high"):
        c["workload"]["extraction"]["in_tok"][s] *= 1.5; c["workload"]["extraction"]["out_tok"][s] *= 1.5
run("model price x2, tokens x1.5", pricey)
print("\nUtilisation of included volume")
for u in (0.5, 0.8, 1.0):
    c = cm.load(); m = cm.tier_margin(c, "professional", utilisation=u)
    print(f"{'utilisation '+format(u,'.0%'):34s} contribution {m['contribution']:>9,.0f}  margin {m['margin_pct']*100:6.1f}%")
print("\nChurn -> LTV (baseline contribution)")
c = cm.load(); contrib = cm.tier_margin(c, "professional")["contribution"]
for ch in (0.03, 0.06, 0.10):
    c["business"]["monthly_churn"] = ch
    print(f"churn {ch:.0%}: LTV {cm.ltv(c, contrib):>9,.0f}  LTV/CAC {cm.ltv(c, contrib)/c['business']['cac_inr']:.2f}")
