"""Calculation model for the Mobile App Company Operating System.

Every number printed in the document is computed here from the ASSUMPTIONS
block, so employees can reproduce it. Currency: INR. No external data is
fetched; values marked 'planning' must be replaced from official pricing
pages/quotes at Gate 8.
"""
from __future__ import annotations
import math

# ---------------------------------------------------------------- formatting
def inr(x: float, dec: int = 0) -> str:
    """Indian digit grouping, e.g. 1234567 -> ₹12,34,567."""
    neg = x < 0
    x = abs(round(x, dec))
    s = f"{x:.{dec}f}"
    ip, _, fp = s.partition(".")
    if len(ip) > 3:
        head, tail = ip[:-3], ip[-3:]
        parts = []
        while len(head) > 2:
            parts.insert(0, head[-2:]); head = head[:-2]
        if head: parts.insert(0, head)
        ip = ",".join(parts + [tail])
    return ("-" if neg else "") + "₹" + ip + ("." + fp if dec else "")

def num(x: float, dec: int = 0) -> str:
    s = inr(x, dec)
    return s.replace("₹", "")

# ---------------------------------------------------------------- roles/rates
OVERHEAD = 0.25          # A-01 employee overhead loading (equipment, seats, mgmt, statutory)
AVAIL_HRS = 160          # A-02 paid hours / month
UTIL = 0.75              # A-03 billable utilisation
PROD_HRS = AVAIL_HRS * UTIL   # 120 productive hours / month

# role code -> (name, monthly CTC INR, outsourced hourly or None)
ROLES = {
    "PM": ("Product Manager / Business Researcher", 90000, None),
    "UX": ("UI/UX Designer", 70000, None),
    "FL": ("Flutter Developer (Android+iOS)", 85000, None),
    "BE": ("Backend Developer", 90000, None),
    "QA": ("QA Engineer", 50000, None),
    "DO": ("DevOps / Database Engineer", 90000, None),
    "SE": ("Security Specialist (external)", 0, 2500),
}
def rate(code: str) -> int:
    name, ctc, ext = ROLES[code]
    if ext: return ext
    return round(ctc * (1 + OVERHEAD) / PROD_HRS)
RATES = {c: rate(c) for c in ROLES}
BLENDED_ENG = round(sum(RATES[c] for c in ("FL", "BE", "DO", "QA")) / 4)
SUPPORT_CTC = 35000
SUPPORT_RATE = round(SUPPORT_CTC * (1 + OVERHEAD) / PROD_HRS)

# ---------------------------------------------------------------- task plan
# (id, task, scope C=customer-specific/R=reusable core, risk, {role: most-likely hrs})
RISK = {"Low": (0.85, 1.25), "Med": (0.80, 1.40), "High": (0.75, 1.80)}
TASKS = [
    ("T01", "Market research & validation", "R", "Low", {"PM": 40}),
    ("T02", "Requirements, PRD & I/O freeze", "C", "Med", {"PM": 50, "UX": 10, "BE": 10, "FL": 10}),
    ("T03", "UI/UX design, design system & prototype", "C", "Med", {"UX": 110, "PM": 12, "FL": 8}),
    ("T04", "Architecture & technology selection", "R", "Low", {"BE": 16, "FL": 12, "DO": 12, "PM": 4}),
    ("T05", "Repository, CI/CD & environments", "R", "Low", {"DO": 24, "BE": 6, "FL": 6}),
    ("T06", "Database design & migrations", "C", "Low", {"BE": 20, "DO": 16}),
    ("T07", "Authentication, roles & user management", "R", "Med", {"BE": 36, "FL": 28, "QA": 8}),
    ("T08", "Project/site & worker master modules", "C", "Low", {"BE": 28, "FL": 40, "QA": 10}),
    ("T09", "Daily report: attendance, materials, issues", "C", "High", {"BE": 40, "FL": 80, "QA": 20}),
    ("T10", "Offline storage & sync engine", "R", "High", {"BE": 44, "FL": 72, "QA": 28}),
    ("T11", "Resumable photo upload & file storage", "R", "Med", {"BE": 20, "FL": 28, "DO": 6, "QA": 8}),
    ("T12", "Report PDF generation & sharing", "C", "Med", {"BE": 24, "FL": 16, "QA": 8}),
    ("T13", "Push notifications (FCM/APNs)", "R", "Low", {"BE": 12, "FL": 14, "QA": 6}),
    ("T14", "Admin dashboard (web)", "R", "Med", {"FL": 60, "BE": 36, "UX": 12, "QA": 12}),
    ("T15", "Subscription billing & payments", "R", "High", {"BE": 36, "FL": 12, "QA": 10}),
    ("T16", "Third-party integrations (OTP, email, maps)", "R", "Low", {"BE": 14, "FL": 8}),
    ("T17", "Functional, device & network testing", "C", "Med", {"QA": 120, "FL": 24, "BE": 24}),
    ("T18", "Security review & remediation", "R", "Med", {"SE": 24, "BE": 16, "FL": 16, "DO": 8}),
    ("T19", "Performance tuning & bug-fix reserve", "C", "Med", {"FL": 40, "BE": 32, "QA": 24}),
    ("T20", "Production deployment & store submission", "R", "Med", {"DO": 40, "FL": 16, "BE": 8, "PM": 8}),
    ("T21", "Documentation & user/admin manuals", "C", "Low", {"PM": 24, "QA": 16, "BE": 8}),
    ("T22", "Customer training & pilot onboarding", "C", "Low", {"PM": 20, "QA": 8}),
    ("T23", "Project management & coordination", "C", "Low", {"PM": 60}),
]

def task_calc(t):
    tid, name, scope, risk, hrs = t
    m = sum(hrs.values())
    o_f, p_f = RISK[risk]
    o, p = m * o_f, m * p_f
    e = (o + 4 * m + p) / 6
    cost_m = sum(h * RATES[r] for r, h in hrs.items())
    return dict(id=tid, name=name, scope=scope, risk=risk, m=m, o=o, e=e, p=p,
                cost_m=cost_m, cost_o=cost_m * o_f, cost_e=cost_m * e / m, cost_p=cost_m * p_f)
TC = [task_calc(t) for t in TASKS]
H_M = sum(t["m"] for t in TC); H_O = sum(t["o"] for t in TC)
H_E = sum(t["e"] for t in TC); H_P = sum(t["p"] for t in TC)
L_O = sum(t["cost_o"] for t in TC); L_E = sum(t["cost_e"] for t in TC)
L_P = sum(t["cost_p"] for t in TC)
L_M = sum(t["cost_m"] for t in TC)
ROLE_HRS_M = {r: sum(t[4].get(r, 0) for t in TASKS) for r in ROLES}
ROLE_COST_M = {r: ROLE_HRS_M[r] * RATES[r] for r in ROLES}
REUSE_SHARE = sum(t["cost_e"] for t in TC if t["scope"] == "R") / L_E
REUSE_FACTOR = 0.40   # A-08 effort needed on reusable tasks for the 2nd client

# ---------------------------------------------------------------- one-time non-labour
USD_INR = 90          # A-10 planning FX; replace with RBI reference rate on the day
SOFTWARE_ONETIME = [  # (item, class, amount, basis)
    ("Mac for iOS builds/signing (planning allowance)", "Paid", 75000, "Quote needed"),
    ("Test devices: 3 Android + 1 iPhone (planning allowance)", "Paid", 60000, "Quote needed"),
]
SOFTWARE_MONTHLY = [  # (item, class, INR/month, basis)
    ("VS Code, Android Studio, Flutter SDK, Xcode, Git", "Free", 0, "Official downloads"),
    ("GitHub (private repos, Actions) - 6 seats", "Paid", 6 * 400, "Planning - verify github.com/pricing"),
    ("Figma (2 design seats)", "Paid", 2 * 1300, "Planning - verify figma.com/pricing"),
    ("Firebase Crashlytics / Analytics / FCM", "Free", 0, "Verify Firebase pricing page"),
    ("Sentry or similar (optional)", "Optional", 2400, "Planning"),
    ("Postman / Bruno API testing", "Free", 0, "Free tier"),
    ("Device cloud (BrowserStack-type), optional", "Optional", 3000, "Planning"),
    ("Security scanning: Dependabot + CodeQL (public/free tier)", "Free", 0, "Verify GitHub plan limits"),
]
SW_MONTHS = 5   # build period for tooling cost allocation
SW_BUILD = sum(a for _, c, a, _ in SOFTWARE_MONTHLY if c != "Optional") * SW_MONTHS
SW_ONE = sum(a for _, _, a, _ in SOFTWARE_ONETIME)

STORE = [  # item, USD, INR, status
    ("Google Play Console registration (one-time)", 25, round(25 * USD_INR), "Last known US$25 - NOT verified this session"),
    ("Apple Developer Program (annual)", 99, round(99 * USD_INR), "Last known US$99/yr (INR price differs) - NOT verified"),
    ("D-U-N-S number (organisation accounts)", 0, 0, "Free from D&B via store flow - verify"),
    ("Store assets: screenshots, icon, listing (in-house hours)", 0, 0, "Covered in T20"),
]
STORE_YEAR1 = STORE[0][2] + STORE[1][2]

INFRA_ONETIME = [
    ("Domain registration (1 yr)", 1000, "Planning"),
    ("DNS / SSL (managed, Let's Encrypt or provider)", 0, "Free tier"),
    ("Email/SMS sender setup, DLT registration (India SMS)", 6000, "Planning - verify telecom/provider fees"),
    ("Initial load test & restore drill (tool/credits)", 5000, "Planning"),
    ("Legal: privacy policy, ToS, DPA templates review", 25000, "Quote needed"),
]
INFRA_ONE = sum(a for _, a, _ in INFRA_ONETIME)

# ---------------------------------------------------------------- usage scenarios
USAGE = {  # low / medium / high
    "mau_ratio": (0.40, 0.60, 0.80), "dau_mau": (0.25, 0.35, 0.45),
    "sessions": (2, 3, 4), "req_per_session": (15, 25, 35),
    "photos_per_dau": (3, 6, 10), "otp_per_mau": (2, 2, 2), "emails_per_mau": (6, 10, 14),
}
PHOTO_MB = 0.6; API_KB = 8; DL_FACTOR = 1.5; RETAIN_MONTHS = 12
SESSION_MIN = 5; ACTIVE_MIN = 600; PEAK_CONC = 3; PEAK_RPS = 5
RPS_PER_INSTANCE = 150; INSTANCE_COST = 3500
UNIT = dict(storage=2.0, egress=8.0, sms=0.25, email=0.08, backup_pct=0.25, domain=83)
DB_TIERS = [(2000, 6000, "Small"), (10000, 14000, "Medium"), (40000, 32000, "Large"), (10**9, 70000, "XL + replica")]
MON_TIERS = [(2000, 1500), (10000, 4000), (40000, 10000), (10**9, 25000)]
SCALES = [100, 500, 1000, 5000, 10000, 25000, 50000, 100000]

def tier(table, v):
    for row in table:
        if v <= row[0]: return row
    return table[-1]

def sim(users: int, lvl: int = 1) -> dict:
    u = USAGE
    mau = users * u["mau_ratio"][lvl]; dau = mau * u["dau_mau"][lvl]
    sess = dau * u["sessions"][lvl]; req_day = sess * u["req_per_session"][lvl]
    req_month = req_day * 30
    conc = sess * SESSION_MIN / ACTIVE_MIN * PEAK_CONC
    rps = req_day / (10 * 3600) * PEAK_RPS
    photos_month = dau * u["photos_per_dau"][lvl] * 30
    up_gb = photos_month * PHOTO_MB / 1024
    stored_gb = up_gb * RETAIN_MONTHS
    egress_gb = up_gb * DL_FACTOR + req_month * API_KB / 1024 / 1024
    inst = max(2, math.ceil(rps / RPS_PER_INSTANCE))
    c = {}
    c["Backend compute"] = inst * INSTANCE_COST
    dbt = tier(DB_TIERS, dau); c["Database (" + dbt[2] + ")"] = dbt[1]
    c["File storage"] = stored_gb * UNIT["storage"]
    c["CDN / egress"] = egress_gb * UNIT["egress"]
    c["Backups"] = c["File storage"] * UNIT["backup_pct"] + 0.1 * dbt[1]
    c["SMS OTP"] = mau * u["otp_per_mau"][lvl] * UNIT["sms"]
    c["Email"] = mau * u["emails_per_mau"][lvl] * UNIT["email"]
    c["Push (FCM)"] = 0
    c["Monitoring & logs"] = tier(MON_TIERS, dau)[1]
    c["Domain & DNS"] = UNIT["domain"]
    total = sum(c.values())
    return dict(users=users, mau=mau, dau=dau, sess=sess, req_month=req_month, conc=conc,
                rps=rps, photos=photos_month, up_gb=up_gb, stored_gb=stored_gb, egress_gb=egress_gb,
                inst=inst, comps=c, total=total, per_user=total / users,
                notif=dau * 4 * 30, writes=req_month * 0.3, reads=req_month * 0.7)

def recommended_model(users: int) -> str:
    return "1 Lean" if users <= 500 else ("2 Professional" if users <= 25000 else "3 Enterprise")

# fixed models (monthly INR) -------------------------------------------------
MODELS = [
 dict(n="1 - Lean MVP", setup=INFRA_ONE + STORE_YEAR1,
      items=[("Compute: 1 small container/VM", 1500), ("Database: free/entry managed PG", 0 + 800), ("Storage + CDN", 300),
             ("Monitoring (free tiers)", 0), ("Email/SMS (pay-per-use)", 500), ("Backups: daily dump to object store", 200), ("Domain/DNS", 83)],
      cap="up to ~500 registered users, 1 region, no HA",
      lim="Single point of failure; free-tier quotas can change; manual restore; no staging parity",
      trig="DB CPU >60% for 1 week, p95 API >800 ms, >400 users, first paying B2B contract with SLA",
      risk="Outage = full downtime; free-tier terms may change without notice",
      upg="Move DB to managed HA, add second instance, add staging env"),
 dict(n="2 - Professional", setup=INFRA_ONE + STORE_YEAR1 + 40000,
      items=None, cap="~1,000-25,000 registered users (see simulation)",
      lim="Single region; RTO ~4 h, RPO ~15 min (PITR)", trig="Sustained peak RPS >70% of capacity; contractual 99.9% SLA; data-residency ask",
      risk="Cost step-ups at tier boundaries; vendor lock-in on managed services", upg="Multi-AZ DB, autoscaling, WAF, read replica"),
 dict(n="3 - Enterprise / Scalable", setup=INFRA_ONE + STORE_YEAR1 + 250000,
      items=None, cap="50,000+ registered users, multi-AZ, DR region", lim="Needs dedicated DevOps; higher fixed cost",
      trig="Enterprise contract needing 99.95% SLA, SSO, audit export, DR drills", risk="Over-provisioning if demand does not arrive",
      upg="Multi-region, SIEM, formal ISO 27001 / SOC 2 programme"),
]
M3_MULT_FIXED = 38000   # A-19 extra for multi-AZ, DR copy, WAF, SIEM-lite, SSO (planning)

# ---------------------------------------------------------------- pricing
PLANS = [  # name, monthly, users, projects, storage GB, support min/month, API/day
    ("Starter", 1499, 3, "2", 5, 10, "5,000"),
    ("Professional", 3999, 10, "10", 25, 25, "25,000"),
    ("Business", 9999, 30, "Unlimited*", 100, 60, "100,000"),
]
ENTERPRISE_FROM = 24999
MIX = (0.50, 0.35, 0.15)
ANNUAL_SHARE = 0.30; ANNUAL_MONTHS = 10
BILL_FACTOR = (1 - ANNUAL_SHARE) + ANNUAL_SHARE * ANNUAL_MONTHS / 12
ARPA = sum(m * p[1] for m, p in zip(MIX, PLANS)) * BILL_FACTOR
USERS_PER_CUST = sum(m * p[2] for m, p in zip(MIX, PLANS))
PAY_FEE = 0.02; CHURN = 0.025; CAC = 9000; TAX = 0.2517; GST = 0.18
PLAN_STORE_MBMO = 0.6 * 0.35 * 6 * 30 * PHOTO_MB  # medium: MB new storage / registered user / month

def var_cost_per_user() -> float:
    s = sim(10000, 1)
    c = s["comps"]
    keys = ["File storage", "CDN / egress", "SMS OTP", "Email", "Backups"]
    return sum(c[k] for k in keys) / s["users"]
VAR_USER = var_cost_per_user()

def plan_cost(plan):
    name, fee, users, _, _, sup, _ = plan
    infra = VAR_USER * users
    pay = fee * PAY_FEE
    support = sup * SUPPORT_RATE / 60
    cost = infra + pay + support
    return dict(name=name, fee=fee, annual=fee * ANNUAL_MONTHS, infra=infra, pay=pay,
                support=support, cost=cost, contrib=fee - cost, cm=(fee - cost) / fee)

PC = [plan_cost(p) for p in PLANS]
MIX_SUPPORT_MIN = sum(m * p[5] for m, p in zip(MIX, PLANS))

ENG_HRS = {10: 80, 25: 100, 50: 120, 100: 160, 250: 240, 500: 320, 1000: 480}
GA = {10: 30000, 25: 30000, 50: 30000, 100: 30000, 250: 50000, 500: 50000, 1000: 80000}
CUSTS = [10, 25, 50, 100, 250, 500, 1000]

def scenario(n: int, price_f: float = 1.0, churn_f: float = 1.0, usage_lvl: int = 1) -> dict:
    users = round(n * USERS_PER_CUST)
    infra = sim(users, usage_lvl)
    rev = n * ARPA * price_f
    host = infra["comps"]["Backend compute"]
    db = [v for k, v in infra["comps"].items() if k.startswith("Database")][0]
    other = infra["total"] - host - db
    pay = rev * PAY_FEE
    eng = ENG_HRS[n] * BLENDED_ENG
    sup = n * MIX_SUPPORT_MIN / 60 * SUPPORT_RATE
    sm = max(30000, 0.20 * rev)
    ga = GA[n]
    cost = host + db + other + pay + eng + sup + sm + ga
    op = rev - cost
    tax = max(0, op) * TAX
    return dict(n=n, users=users, rev=rev, host=host, db=db, other=other, pay=pay, eng=eng,
                sup=sup, sm=sm, ga=ga, cost=cost, op=op, tax=tax, pat=op - tax,
                gst=rev * GST, margin=op / rev if rev else 0)
SC = [scenario(n) for n in CUSTS]

def breakeven():
    """Smallest customer count whose operating profit >= 0, interpolating engineering/G&A steps."""
    for n in range(1, 2001):
        # step functions from table
        key = min([k for k in CUSTS if k >= n], default=1000)
        ENG_HRS.setdefault(n, ENG_HRS[key]); GA.setdefault(n, GA[key])
        s = scenario(n)
        if s["op"] >= 0: return n, s
    return None, None
BE_N, BE_S = breakeven()

GM_PER_CUST = ARPA - ARPA * PAY_FEE - VAR_USER * USERS_PER_CUST - MIX_SUPPORT_MIN * SUPPORT_RATE / 60
GM_PCT = GM_PER_CUST / ARPA
LTV = GM_PER_CUST / CHURN
LTV_CAC = LTV / CAC

# ---------------------------------------------------------------- custom dev pricing
GA_PCT = 0.12; RISK_SHARE = 0.5; TARGET_M = 0.30; MIN_M = 0.15
SETUP_COSTS = SW_BUILD + SW_ONE + INFRA_ONE + STORE_YEAR1
def custom_price(labour: float):
    ov = labour * GA_PCT; risk = RISK_SHARE * (L_P - L_E)
    cost = labour + SETUP_COSTS + ov + risk
    return dict(labour=labour, setup=SETUP_COSTS, ov=ov, risk=risk, cost=cost,
                target=cost / (1 - TARGET_M), minimum=cost / (1 - MIN_M))
CP1 = custom_price(L_E)
reuse_cut = sum(t["cost_e"] for t in TC if t["scope"] == "R") * (1 - REUSE_FACTOR)
CP2 = custom_price(L_E - reuse_cut)
WL_HRS = {"PM": 16, "UX": 20, "FL": 32, "BE": 16, "QA": 16, "DO": 20}
WL_LABOUR = sum(h * RATES[r] for r, h in WL_HRS.items())
WL_SETUP = (WL_LABOUR * (1 + GA_PCT)) / (1 - TARGET_M)
WL_LICENCE_YR = 120000
ENT_PREMIUM = 0.25

# ---------------------------------------------------------------- maintenance
MAINT = [  # name, eng hrs, QA hrs, support hrs, third-party INR, margin
    ("Basic", 8, 2, 2, 2500, 0.40, "Next business day", "Email, 5x9", "Critical bugs, OS compat patch 1x/qtr"),
    ("Professional", 24, 6, 6, 6000, 0.40, "4 business hours (P1) / 1 day (P2)", "Email + chat, 6x10", "Monthly release, dependency & security updates"),
    ("Enterprise", 60, 15, 15, 18000, 0.35, "1 hour (P1, 24x7 on-call) / 4 h (P2)", "Phone + named engineer", "Weekly releases, DR drill, SLA credits"),
]
def maint_calc(m):
    name, e, q, s, tp, mg = m[:6]
    labour = e * (RATES["FL"] + RATES["BE"]) / 2 + q * RATES["QA"] + s * SUPPORT_RATE
    cost = labour + tp
    return dict(name=name, labour=labour, tp=tp, cost=cost, price=cost / (1 - mg), margin=mg,
                hrs=e + q + s, resp=m[6], chan=m[7], incl=m[8])
MC = [maint_calc(m) for m in MAINT]

# ---------------------------------------------------------------- sensitivity
def sens_rows():
    rows = []
    base = scenario(100)["op"]
    for label, kw in [("Price -20%", dict(price_f=0.8)), ("Price +20%", dict(price_f=1.2)),
                      ("Usage: low", dict(usage_lvl=0)), ("Usage: high", dict(usage_lvl=2))]:
        rows.append((label, scenario(100, **kw)["op"], scenario(100, **kw)["op"] - base))
    for ch in (0.015, 0.025, 0.04, 0.06):
        life = 1 / ch; ltv = GM_PER_CUST / ch
        rows.append((f"Churn {ch*100:.1f}%/mo", ltv, ltv / CAC))
    return base, rows

if __name__ == "__main__":
    print("RATES", RATES, "blended", BLENDED_ENG, "support", SUPPORT_RATE)
    print("Hours M/O/E/P", H_M, round(H_O), round(H_E), round(H_P))
    print("Labour O/E/P", inr(L_O), inr(L_E), inr(L_P), "M", inr(L_M))
    print("ARPA", ARPA, "users/cust", USERS_PER_CUST, "VAR_USER", VAR_USER)
    print("BE", BE_N, "GM", GM_PCT, "LTV", LTV, LTV_CAC)
    for s in SC: print(s["n"], inr(s["rev"]), inr(s["cost"]), inr(s["op"]))
    for n in SCALES: print(n, inr(sim(n)["total"]), sim(n)["inst"])
    print(CP1["target"], CP2["target"], WL_SETUP, [ (m["name"], round(m["cost"]), round(m["price"])) for m in MC])
