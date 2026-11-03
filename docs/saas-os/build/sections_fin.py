"""Computed financial model sections."""
import calc
from calc import inr
from sections_a import table, callout

def labour_tbl():
    rows, t = calc.labour()
    body = [(a, r, o, f"{m}", p, inr(rt), inr(co), inr(ce), inr(cp)) for a, r, o, m, p, rt, co, ce, cp in rows]
    body.append(("<b>Total</b>", "", f"<b>{t['hrs_o']:.0f}</b>", f"<b>{t['hrs_e']:.0f}</b>", f"<b>{t['hrs_p']:.0f}</b>", "", f"<b>{inr(t['opt'])}</b>", f"<b>{inr(t['exp'])}</b>", f"<b>{inr(t['pes'])}</b>"))
    return table(["Activity","Role","O h","Expected h (PERT)","P h","₹/h","Optimistic","Expected","Pessimistic"], body, "num")

def rates_tbl():
    return table(["Role","Fully loaded ₹/hour (ASSUMPTION)"], [(k, inr(v)) for k, v in calc.RATES.items()], "num") + \
      "<p class='note'>Fully loaded = salary or founder-equivalent pay + employer taxes/benefits + equipment + software seats + office/overheads, divided by productive (not paid) hours. Replace with your actuals.</p>"

def setup_tbl():
    rows = [(i, inr(a), "Mandatory" if m else "Optional", n) for i, a, m, n in calc.SETUP]
    mand = sum(a for _, a, m, _ in calc.SETUP if m); opt = sum(a for _, a, m, _ in calc.SETUP if not m)
    rows.append(("<b>Total mandatory</b>", f"<b>{inr(mand)}</b>", "", "")); rows.append(("<b>Total optional</b>", f"<b>{inr(opt)}</b>", "", ""))
    return table(["Item","Amount (ASSUMPTION)","Type","Note"], rows, "num"), mand, opt

def usage_tbl():
    rows = []
    for u in calc.USERS:
        s = calc.scenario(u)
        rows.append((f"{u:,}", f"{s['mau']:,.0f}", f"{s['dau']:,.0f}", f"{s['reqs_day']:,.0f}", f"{s['peak_rps']:.1f}", f"{s['peak_conc']:,.0f}", f"{s['db_q_day']:,.0f}", f"{s['gb']:.1f}", f"{s['bw_gb']:.0f}", f"{s['emails']:,.0f}", f"{s['msgs']:,.0f}"))
    return table(["Users","MAU","DAU","Requests/day","Peak req/s (3× burst)","Peak concurrent","DB queries/day","Storage GB","Bandwidth GB/mo","Emails/mo","SMS+WA msgs/mo"], rows, "num")

def usage_assump():
    U = calc.USE
    return table(["Assumption","Value"], [("Monthly active users (of registered)", f"{U['mau_pct']:.0%}"),("Daily active users (of registered)", f"{U['dau_pct']:.0%}"),("Requests per DAU per day", U["req_per_dau_day"]),("Share of daily requests in busiest hour", f"{U['peak_hour_share']:.0%}"),("Burst factor on peak hour", "3×"),("Peak concurrency", "10% of DAU"),("DB queries per request", U["db_q_per_req"]),("Storage per user", f"{U['mb_storage_user']} MB"),("Bandwidth per DAU per day", f"{U['mb_bw_dau_day']} MB"),("Emails per user per month", U["emails_user_month"]),("SMS/WhatsApp per user per month", U["msgs_user_month"])], "num") + "<p class='note'>Low/High scenarios: halve or double the request, storage and messaging values; re-run the model.</p>"

def unit_tbl():
    u = calc.UNIT
    rows = [("Object storage", f"₹{u['gb_storage']} / GB-month"),("Bandwidth / CDN egress", f"₹{u['gb_bw']} / GB"),("Transactional email", f"₹{u['per_1000_email']} / 1,000 emails"),("SMS / WhatsApp", f"₹{u['per_msg']} / message"),("Maintenance labour", f"₹{u['maint_rate']} / hour"),("Support labour", f"₹{u['support_rate']} / hour; {u['support_h_tenant']} h per tenant per month")]
    for k, m in calc.MODELS.items():
        rows.append((f"{k}: base platform", f"{inr(m['fixed'])} / month incl. app host, managed DB, backups, monitoring"))
        rows.append((f"{k}: extra compute unit", f"{inr(m['unit'])} per unit; one unit sustains {m['rps']} req/s"))
    return table(["Unit rate (ALL ASSUMPTIONS)","Value"], rows, "num") + callout("warn","Replace before use","Every rate above is a planning placeholder to make the model computable. Before Gate G7, replace each with the provider's current official price (URL + access date) using the Cost Estimation template. Do not quote customers from these numbers.")

def model_tbl():
    out = ""
    for mname, m in calc.MODELS.items():
        rows = []
        for u in calc.USERS:
            sc = calc.scenario(u); r = calc.monthly(sc, mname)
            ok = "OK" if (mname != "Lean MVP" or r["units"] <= 2) else "Outgrown – upgrade"
            rows.append((f"{u:,}", r["units"], inr(r["compute"]), inr(r["var"]), inr(r["maint"]), inr(r["support"]), f"<b>{inr(r['total'])}</b>", inr(r["total"]*12), ok))
        out += f"<h4>{mname}</h4>" + table(["Users","Compute units","Platform + compute","Usage (storage, BW, email, msgs)","Maintenance labour","Support labour","Monthly operating","Annual","Status"], rows, "num")
    return out

def model_summary():
    rows = [("Lean / Low-cost MVP","Managed PaaS free/low tier + small managed Postgres, daily backup, basic uptime + error tracking","≈ up to 1,000 users (peak &lt; 10 req/s)","Free tiers can change or be withdrawn; shared resources; limited backups; single region; no redundancy","Paying customers > 25, any SLA promise, peak load > 70% of unit, DB > 70% of storage cap"),
     ("Professional","Paid PaaS/VM, managed Postgres with point-in-time recovery, staged environments, monitoring, security scanning","≈ up to 5,000 users with extra compute units","Single region; manual failover; cost grows stepwise","Enterprise customers, uptime > 99.5% contractually, p95 latency breaches, DB CPU > 60%"),
     ("Scalable","Multi-AZ app + DB, autoscaling, CDN/WAF, centralised logs, DR in second region, 24×7 alerting","10,000+ users","Highest fixed cost and operations load; needs on-call staff","Revenue/MRR > monthly cost × 3 and enterprise commitments")]
    return table(["Model","Typical build","Estimated capacity","Limitations / risks","Upgrade triggers"], rows)

def model_costs():
    rows = []
    for mname, m in calc.MODELS.items():
        sc = calc.scenario(1000); r = calc.monthly(sc, mname)
        rows.append((mname, inr(sum(a for _, a, mm, _ in calc.SETUP if mm)), inr(r["compute"]+r["var"]), inr(r["maint"]), inr(r["total"]), inr(r["total"]*12)))
    return table(["Model (at 1,000 users)","One-time setup (non-labour)","Monthly infrastructure","Monthly maintenance","Monthly operating (incl. support)","Annual recurring"], rows, "num")

def first_year():
    _, t = calc.labour(); sc = calc.scenario(100); r = calc.monthly(sc, "Professional")
    _, mand, opt = setup_tbl()
    one = t["exp"] + mand
    rows = [("Total one-time cost (labour expected + mandatory setup)", inr(one)),
            ("Monthly operating cost @100 users, Professional", inr(r["total"])),
            ("Annual operating cost = monthly × 12", inr(r["total"]*12)),
            ("<b>Total first-year cost = one-time + annual operating</b>", f"<b>{inr(one + r['total']*12)}</b>"),
            ("Range using pessimistic labour", inr(t["pes"] + mand + r["total"]*12)),
            ("Range using optimistic labour", inr(t["opt"] + mand + r["total"]*12))]
    return table(["Item","Amount"], rows, "num")

def plan_tbl():
    rows = []; P = calc.pricing()
    for n, price, users, gb, api, sup in calc.PLANS:
        c = P["ct"][n]
        rows.append((n, inr(price), inr(price*10), users, f"{gb} GB", f"{api:,}", sup, inr(c["var"]+c["support"]+c["pg"]+c["leak"]), inr(c["contrib"]), f"{c['contrib']/price:.0%}"))
    rows.insert(0, ("Free Trial", "₹0 (14 days)", "–", 2, "0.5 GB", "2,000", "Docs only", inr(2*(calc.USE["mb_storage_user"]/1024*calc.UNIT["gb_storage"]+calc.USE["dau_pct"]*calc.USE["mb_bw_dau_day"]*30/1024*calc.UNIT["gb_bw"]+calc.UNIT["per_msg"]*calc.USE["msgs_user_month"]+calc.USE["emails_user_month"]/1000*calc.UNIT["per_1000_email"])*14/30)+" variable (excl. support time)", "–", "Counted in CAC"))
    rows.append(("Enterprise", "Custom (from ≈ ₹15,000)", "Custom", "Custom", "Custom", "Custom", "Named contact, SLA by contract", "Quote per deal", "≥ 60% target", "Contract"))
    return table(["Plan","Monthly (ex-GST)","Annual (10× monthly = 2 months free)","Users","Storage","API calls/mo","Support","Cost to serve + leakage","Contribution / month","Contribution %"], rows, "num") + \
     "<p class='note'>Overage (all plans): extra user ₹150/month; extra 10 GB ₹100/month; extra 10,000 API calls ₹200; SMS/WhatsApp at provider cost + 20%. Upgrade condition: usage > 80% of any limit for 2 consecutive months triggers an upgrade prompt. All prices are ASSUMPTIONS to be validated in discovery.</p>"

def fixed_tbl():
    P = calc.pricing()
    rows = [(k, inr(v)) for k, v in calc.FIXED] + [("<b>Monthly fixed operating cost</b>", f"<b>{inr(P['fixed'])}</b>")]
    return table(["Fixed monthly cost (allocated to product)","₹ / month"], rows, "num")

def profit_tbl():
    P = calc.pricing(); rows = []
    for n in (10, 25, 28, 50, 100, 250, 500):
        p = calc.profit_at(n, P)
        rows.append((n, inr(p["mrr"]), inr(p["arr"]), inr(p["contrib"]), inr(p["op"]), inr(p["tax"]), inr(p["net"]), f"{p['gm']:.0f}%"))
    return table(["Paying tenants","MRR","ARR","Contribution","Operating profit (before tax)","Income tax @25.17% (ASSUMPTION; confirm with CA)","Profit after tax","Gross margin"], rows, "num")

def kpis():
    P = calc.pricing(); L = calc.ltv(P)
    rows = [("Blended ARPA (plan mix 50/35/15)", inr(P["arpa"]) + " / tenant / month"),
            ("Contribution per tenant (after gateway fees, refunds, failed payments, variable delivery, support)", inr(P["contrib"])),
            ("Monthly fixed operating cost", inr(P["fixed"])),
            ("<b>Break-even customers = fixed ÷ contribution = %s ÷ %s</b>" % (inr(P["fixed"]), inr(P["contrib"])), f"<b>{P['fixed']/P['contrib']:.2f} → round UP to {P['be']} paying tenants</b>"),
            ("Churn (ASSUMPTION)", f"{calc.CHURN:.0%} / month → lifetime {L['life']:.0f} months"),
            ("LTV = contribution × lifetime", inr(L["ltv"])),
            ("CAC (ASSUMPTION; = sales+marketing spend ÷ new paying customers)", inr(calc.CAC)),
            ("LTV : CAC", f"{L['ratio']:.1f} : 1 (target ≥ 3)"),
            ("CAC payback", f"{L['payback']:.1f} months (target &lt; 12)"),
            ("Maximum acceptable CAC (LTV ÷ 3)", inr(L["maxcac"])),
            ("Revenue leakage assumed", f"refunds {calc.REFUND:.0%} + failed payments {calc.FAIL:.0%}"),
            ("Payment gateway fee (ASSUMPTION; verify gateway page)", f"{calc.PG_FEE:.0%} of collected amount"),
            ("GST on subscriptions", "18% collected from customer, remitted to government – NOT revenue (confirm rate/place-of-supply with CA & CBIC)"),
            ("Monthly revenue target for 12-month goal", f"{inr(100*P['arpa'])} MRR (100 tenants)")]
    return table(["Metric","Value"], rows, "num")

def commercial():
    P = calc.pricing()
    rows = [("Initial setup / onboarding fee","₹5,000 (Starter) · ₹10,000 (Professional) · ₹25,000 (Business) – covers data import and training; recovers CAC"),
            ("Monthly subscription","₹999 / ₹2,499 / ₹5,999 (ex-GST)"),("Annual subscription","10× monthly (2 months free); billed upfront – improves cash flow and reduces churn"),
            ("Minimum viable selling price","Starter ₹999: contribution %s = %.0f%% of price; contribution is still positive at ₹799 but leaves almost no room for CAC, so do not discount below it" % (inr(P['ct']['Starter']['contrib']), P['ct']['Starter']['contrib']/999*100)),
            ("Target gross margin","≥ 60%% – model reaches %.0f%% at 100 tenants and %.0f%% at 250 tenants; below 60%% until roughly 175 tenants" % (calc.profit_at(100,P)["gm"], calc.profit_at(250,P)["gm"])),
            ("Maximum CAC", inr(calc.ltv(P)["maxcac"]) + " (LTV ÷ 3)"),
            ("Break-even customers", f"{P['be']} paying tenants"),
            ("Renewal strategy","90/60/30-day renewal reminders; annual plans auto-renew with notice; quarterly business review for Business/Enterprise; price rises ≤ 10% with 60-day notice")]
    return table(["Recommendation","Value"], rows)
