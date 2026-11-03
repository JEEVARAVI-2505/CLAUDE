"""Financial model for the sample product. ALL unit prices are labelled ASSUMPTIONS
(INR) and must be replaced with quotes from official pricing pages before use."""
import math

def inr(x):
    x = round(x)
    s = f"{abs(x):,}"
    # Indian grouping
    d = str(abs(x))
    if len(d) > 3:
        head, tail = d[:-3], d[-3:]
        parts = []
        while len(head) > 2:
            parts.insert(0, head[-2:]); head = head[:-2]
        if head: parts.insert(0, head)
        s = ",".join(parts + [tail])
    else:
        s = d
    return ("-₹" if x < 0 else "₹") + s

RATES = {"Product Owner / Founder": 1200, "Tech Lead": 1500, "Full-stack Developer": 800,
         "UI/UX Designer": 700, "QA Engineer": 500, "DevOps Engineer": 900}

# (activity, role, optimistic, most-likely, pessimistic hours)
ACT = [
 ("Market research", "Product Owner / Founder", 30, 40, 60),
 ("Requirement analysis & PRD", "Product Owner / Founder", 40, 60, 90),
 ("Requirement analysis & PRD", "Tech Lead", 10, 16, 24),
 ("UI/UX design", "UI/UX Designer", 80, 110, 160),
 ("Frontend development", "Full-stack Developer", 200, 280, 400),
 ("Backend development", "Full-stack Developer", 220, 300, 440),
 ("Database & API design", "Tech Lead", 40, 60, 90),
 ("Authentication & RBAC", "Full-stack Developer", 30, 40, 60),
 ("Integrations (payments, email, SMS)", "Full-stack Developer", 40, 60, 100),
 ("Testing", "QA Engineer", 100, 140, 220),
 ("Security review", "Tech Lead", 16, 24, 40),
 ("Deployment & DevOps", "DevOps Engineer", 24, 36, 60),
 ("Documentation", "Full-stack Developer", 24, 36, 60),
 ("Customer onboarding", "Product Owner / Founder", 16, 24, 40),
]

def pert(o, m, p): return (o + 4*m + p) / 6

def labour():
    rows, tot = [], {"opt": 0, "exp": 0, "pes": 0, "hrs_o": 0, "hrs_e": 0, "hrs_p": 0}
    for a, r, o, m, p in ACT:
        e = pert(o, m, p); rate = RATES[r]
        rows.append((a, r, o, round(e, 1), p, rate, o*rate, e*rate, p*rate))
        tot["opt"] += o*rate; tot["exp"] += e*rate; tot["pes"] += p*rate
        tot["hrs_o"] += o; tot["hrs_e"] += e; tot["hrs_p"] += p
    return rows, tot

# One-time non-labour (ASSUMPTIONS) : (item, amount, mandatory?, note)
SETUP = [
 ("Domain registration (year 1)", 1000, True, "Assumption; check registrar price page for the exact TLD"),
 ("Legal: T&Cs, Privacy Policy, DPA review", 25000, True, "Assumption; obtain lawyer quote"),
 ("Hosting / DB / storage / SSL / DNS setup", 0, True, "Labour already in DevOps hours; provider setup fees assumed nil"),
 ("Payment-gateway onboarding", 0, True, "Assumed nil setup fee; verify on gateway pricing page"),
 ("Trademark search & filing", 8000, False, "Optional; assumption"),
 ("External penetration test", 40000, False, "Optional but recommended before enterprise sales; assumption"),
]

# Usage assumptions per user (staff user of a clinic)
USE = dict(mau_pct=0.80, dau_pct=0.50, req_per_dau_day=150, peak_hour_share=0.15,
           mb_storage_user=50, mb_bw_dau_day=3, emails_user_month=40, msgs_user_month=60,
           db_q_per_req=4)
USERS = [10, 50, 100, 500, 1000, 5000, 10000]

MODELS = {  # fixed monthly ₹, per-extra-compute-unit ₹, rps per unit, maintenance h/mo
 "Lean MVP": dict(fixed=2500, unit=1500, rps=10, maint_h=12),
 "Professional": dict(fixed=9000, unit=4000, rps=40, maint_h=20),
 "Scalable": dict(fixed=35000, unit=8000, rps=100, maint_h=40),
}
UNIT = dict(gb_storage=4, gb_bw=8, per_1000_email=80, per_msg=0.5, maint_rate=850,
            support_h_tenant=0.5, support_rate=600, users_per_tenant=7.3)

def scenario(users):
    mau = users*USE["mau_pct"]; dau = users*USE["dau_pct"]
    reqs_day = dau*USE["req_per_dau_day"]
    peak_rps = reqs_day*USE["peak_hour_share"]/3600
    peak_rps_safe = peak_rps*3  # burst factor 3x
    peak_conc = dau*0.10
    return dict(users=users, mau=mau, dau=dau, reqs_day=reqs_day, peak_rps=peak_rps_safe,
                peak_conc=peak_conc, db_q_day=reqs_day*USE["db_q_per_req"],
                gb=users*USE["mb_storage_user"]/1024, bw_gb=dau*USE["mb_bw_dau_day"]*30/1024,
                emails=users*USE["emails_user_month"], msgs=users*USE["msgs_user_month"])

def monthly(sc, mname):
    m = MODELS[mname]
    units = max(1, math.ceil(sc["peak_rps"]/m["rps"]))
    compute = m["fixed"] + (units-1)*m["unit"]
    var = (sc["gb"]*UNIT["gb_storage"] + sc["bw_gb"]*UNIT["gb_bw"]
           + sc["emails"]/1000*UNIT["per_1000_email"] + sc["msgs"]*UNIT["per_msg"])
    tenants = sc["users"]/UNIT["users_per_tenant"]
    maint = m["maint_h"]*UNIT["maint_rate"]
    support = tenants*UNIT["support_h_tenant"]*UNIT["support_rate"]
    return dict(units=units, compute=compute, var=var, maint=maint, support=support,
                infra=compute+var, total=compute+var+maint+support)

# Pricing (ex-GST, INR/month) ASSUMPTIONS
PLANS = [  # name, price, users, storage GB, API calls/mo, support
 ("Starter", 999, 3, 2, 10000, "Email, 2 business days"),
 ("Professional", 2499, 8, 10, 50000, "Email + chat, 1 business day"),
 ("Business", 5999, 20, 50, 250000, "Priority email/chat, 4 business hours"),
]
MIX = {"Starter": .50, "Professional": .35, "Business": .15}
PG_FEE = .02; REFUND = .02; FAIL = .03; GST = .18; CHURN = .04; CAC = 6000
CORP_TAX = .2517

def cost_to_serve(plan):
    n, price, users, gb, api, sup = plan
    var = (users*USE["mb_storage_user"]/1024*UNIT["gb_storage"]
           + users*USE["dau_pct"]*USE["mb_bw_dau_day"]*30/1024*UNIT["gb_bw"]
           + users*UNIT["per_msg"]*USE["msgs_user_month"]
           + users*USE["emails_user_month"]/1000*UNIT["per_1000_email"])
    support = UNIT["support_h_tenant"]*UNIT["support_rate"]*(1 if n!="Business" else 2)
    leak = price*(REFUND+FAIL)
    pg = price*(1-REFUND-FAIL)*PG_FEE
    return dict(var=var, support=support, leak=leak, pg=pg,
                contrib=price-leak-pg-var-support)

FIXED = [("Professional-model hosting & DB base", MODELS["Professional"]["fixed"]),
         ("Maintenance labour (20 h x ₹850)", 20*850),
         ("Domain (annualised)", 1000/12),
         ("Accounting / legal retainer (allocated)", 3000),
         ("Shared dev tools (allocated by time share 25% of ₹10,000)", 2500),
         ("Fixed marketing", 10000)]

def pricing():
    ct = {p[0]: cost_to_serve(p) for p in PLANS}
    arpa = sum(MIX[p[0]]*p[1] for p in PLANS)
    contrib = sum(MIX[p[0]]*ct[p[0]]["contrib"] for p in PLANS)
    fixed = sum(v for _, v in FIXED)
    be = math.ceil(fixed/contrib)
    gm_rev = lambda n: n*arpa
    return dict(ct=ct, arpa=arpa, contrib=contrib, fixed=fixed, be=be)

def profit_at(n, P):
    rev = n*P["arpa"]; contrib = n*P["contrib"]
    op = contrib - P["fixed"]
    tax = max(0, op)*CORP_TAX
    cor = n*(P["arpa"]-P["contrib"]) + MODELS["Professional"]["fixed"] + 20*850
    gm = (rev-cor)/rev*100 if rev else 0
    return dict(n=n, mrr=rev, arr=rev*12, contrib=contrib, op=op, tax=tax, net=op-tax, gm=gm)

def ltv(P):
    gm = P["contrib"]/P["arpa"]
    life = 1/CHURN
    v = P["contrib"]*life
    return dict(life=life, ltv=v, ratio=v/CAC, payback=CAC/P["contrib"], maxcac=v/3)
