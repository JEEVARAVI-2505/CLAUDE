"""Assemble the HTML and print the PDF. Run: python3 build.py"""
import os, subprocess, datetime, calc
from calc import inr
import base64, sections_store as ST
import sections_a as A, sections_fin as F, sections_c as C, stages_a, stages_b

HERE = os.path.dirname(os.path.abspath(__file__)); OUT = os.path.dirname(HERE)
today = datetime.date.today().isoformat()

def sec(id_, title, body, cont=False):
    return f'<section id="{id_}"{" class=cont" if cont else ""}><h2>{title}</h2>{body}</section>'

def calc_widget():
    P = calc.pricing()
    return f'''<div class="calc noprint"><b>Live calculator (web version only)</b> – defaults come from the sample model.<div class="g">
<div><label>ARPA ₹/tenant/month</label><input id="c_arpa" type="number" value="{P["arpa"]:.0f}"></div>
<div><label>Contribution margin %</label><input id="c_cp" type="number" value="{P["contrib"]/P["arpa"]*100:.1f}"></div>
<div><label>Monthly fixed cost ₹</label><input id="c_fx" type="number" value="{P["fixed"]:.0f}"></div>
<div><label>Monthly churn %</label><input id="c_ch" type="number" value="{calc.CHURN*100:.0f}"></div>
<div><label>CAC ₹</label><input id="c_cac" type="number" value="{calc.CAC}"></div>
<div><label>Dev hours (expected)</label><input id="c_h" type="number" value="{calc.labour()[1]["hrs_e"]:.0f}"></div>
<div><label>Blended ₹/hour</label><input id="c_r" type="number" value="{calc.labour()[1]["exp"]/calc.labour()[1]["hrs_e"]:.0f}"></div></div>
<div class="out"><div>Break-even<b id="o_be"></b><small id="o_mrr"></small></div><div>LTV<b id="o_ltv"></b></div><div>LTV : CAC<b id="o_ratio"></b></div><div>CAC payback<b id="o_pay"></b></div><div>Dev labour<b id="o_lab"></b></div></div></div>'''

def financial():
    s_tbl, mand, opt = F.setup_tbl()
    P = calc.pricing(); st_ = P['ct']['Starter']; floor = st_['var']+st_['support']+P['fixed']/100
    f = "<p>This section implements Phases 7 and 8 for the sample product. <b>Every unit price is an ASSUMPTION</b> until replaced by a cited official price.</p>"
    f += "<h3>A. One-time development cost – labour</h3><p><code>Development Labour Cost = Estimated Hours × Fully Loaded Hourly Cost</code>; Expected hours use PERT = (O + 4M + P) ÷ 6. Founder time is included.</p>" + F.rates_tbl() + F.labour_tbl()
    f += "<h3>B. One-time infrastructure &amp; setup cost</h3>" + s_tbl
    f += "<h3>C/D. Usage assumptions and capacity scenarios</h3>" + F.usage_assump() + F.usage_tbl()
    f += "<h3>Unit-rate assumptions</h3>" + F.unit_tbl()
    f += "<h3>E. Three production models – monthly operating cost by user count</h3><p>Cost is bottleneck-driven: compute is stepped by peak requests/second (units needed = ⌈peak ÷ unit capacity⌉), storage/bandwidth/messages scale with usage, maintenance is a fixed labour block, support scales with tenants (users ÷ 7.3 per clinic). Not a straight line.</p>" + F.model_tbl()
    f += "<h3>Model comparison</h3>" + F.model_summary() + F.model_costs()
    f += "<h3>F. Complete cost formula</h3><pre>Total One-Time Cost   = Research + Design + Development + Testing + Setup + Deployment + Legal/Compliance\nMonthly Operating Cost= Hosting + Database + Storage + Bandwidth + APIs + Email + Monitoring + Backups + Maintenance + Support + Other\nAnnual Operating Cost = Monthly Operating Cost × 12\nTotal First-Year Cost = Total One-Time Cost + Annual Operating Cost</pre>" + F.first_year()
    f += "<p><b>Shared vs product-specific:</b> Shared company items (GitHub org, design tool seats, password manager, accounting software) are totalled once and allocated to products by employee-time share (example: ₹10,000 shared × 25% = ₹2,500 charged to the product). Product-specific items (its hosting, DB, domain, SMS) are charged 100%. A cost line is never both allocated and charged directly.</p>"
    f += "<h2 style='margin-top:30px'>Pricing &amp; Profitability (Phase 8)</h2><h3>Pricing approaches</h3>" + A.table(["Approach","Method","Result for sample"],[
      ("Cost-based","(Cost to serve + share of fixed cost) ÷ (1 − target margin)",f"Starter cost to serve {inr(st_['var']+st_['support'])} + fixed share at 100 tenants {inr(P['fixed']/100)} = {inr(floor)} break-even price; with 30% margin ≈ {inr(floor/0.7)} (before gateway/leakage) – so ₹999 is a thin-margin entry plan, upsell to Professional"),
      ("Competitor-based","Place inside the observed range from ≥5 competitor pricing pages","<i>Pending research (no competitor prices invented)</i>"),
      ("Value-based","10–20% of customer's quantified saving (e.g. recovered no-show revenue)","If a clinic recovers 5 visits/month × ₹500 = ₹2,500, a ₹999 plan captures ~40%: validate in discovery")])
    f += "<h3>Plans</h3>" + F.plan_tbl() + "<h3>Fixed monthly cost allocated to the product</h3>" + F.fixed_tbl()
    f += "<h3>Key metrics</h3><p><code>CAC = S&amp;M acquisition cost ÷ new paying customers</code> · <code>MRR = Σ active subscriptions</code> · <code>ARR = MRR × 12</code> · <code>Gross margin % = (Revenue − Cost of revenue) ÷ Revenue × 100</code> · <code>Contribution = Revenue − variable delivery costs</code> · <code>Break-even = Fixed ÷ Contribution per customer (round up)</code> · <code>Operating profit = Revenue − all operating expenses</code>. Cost of revenue here = platform hosting base + maintenance labour + variable delivery + support + gateway fees + refunds/failed-payment leakage; sales/marketing and G&amp;A are operating expenses.</p>" + F.kpis()
    f += "<h3>Profitability by paying-tenant count</h3>" + F.profit_tbl() + "<p class='note'>Tax is shown separately: the model applies a flat assumed rate only to positive operating profit and ignores losses carried forward, GST input credit and founder remuneration rules – have a chartered accountant confirm.</p>"
    f += "<h3>Commercial recommendation</h3>" + F.commercial() + calc_widget()
    f += "<h2 style='margin-top:30px' id='storefees'>App store fees (mobile distribution)</h2>" + ST.store_fees()
    return f

TOC = [("Start",[("intake","Company intake"),("framework","Operating framework")]),
 ("Stages",[(f"stage{i}",n) for i,n in [(1,"1 Market research"),(2,"2 PRD & scope freeze"),(3,"3 Architecture"),(4,"4 UI/UX"),(5,"5 GitHub setup"),(6,"6 Development"),(7,"7 Cost calculation"),(8,"8 Pricing"),(9,"9 Domain & deploy"),(10,"10 Showcase & sales"),(11,"11 Maintenance"),(12,"12 Employee SOP"),(13,"13 Dashboard"),(14,"14 Sources")]]),
 ("Worked example",[("sample","SlotWise sample"),("finance","Financial model"),("storefees","App store fees"),("rollout","90-day rollout")]),
 ("Toolkit",[("templates","20 templates"),("forms","Forms"),("dashboard","Dashboard spec"),("ops","Incidents, SLA, DNS"),("assump","Assumption register"),("sources","Source index")])]

CREDIT = 'Developed by <b>V.LOKESHWARAN</b> · <a href="mailto:velsoftwaresolutions.in@gmail.com">velsoftwaresolutions.in@gmail.com</a> · VEL Software Solutions'

def build():
    logo = base64.b64encode(open(os.path.join(OUT, "assets", "vel-logo.jpg"), "rb").read()).decode()
    stages = stages_a.stages() + stages_b.stages()
    st = {s["n"]: A.stage_card(s) for s in stages}
    toc = "".join(f"<b>{g}</b>" + "".join(f'<a href="#{i}">{t}</a>' for i, t in items) for g, items in TOC)
    cover = f'''<div class="cover"><img class="logo" src="data:image/jpeg;base64,{logo}" alt="VEL Software Solutions logo"><p class="dev">{CREDIT}</p><p style="letter-spacing:.12em;text-transform:uppercase;font-size:13px">VEL Software Solutions · Internal Operating System</p>
<h1>SaaS &amp; Web Development Company Operating System</h1><p>From market research to product development, production, sales and recurring revenue – employee-ready SOPs, templates, financial models, checklists and approval gates.</p>
<div class="meta"><span>Version 1.1</span><span>Fees verified 2026-11-03</span><span>Generated {today}</span><span>12 approval gates</span><span>14 stages</span><span>20 templates</span><span>Worked example included</span></div>
{A.callout("warn","Read first","Figures marked ASSUMPTION are planning placeholders, not market or provider facts. No competitor, price or domain availability has been invented. Replace assumptions with cited, dated sources before making commercial decisions. Legal, tax and compliance items need review by qualified professionals.")}
<h3>Contents</h3><div class="flow">{"".join(f'<span>{t}</span>' for g,items in TOC for i,t in items)}</div></div>'''
    body = cover
    body += sec("intake", "Company intake &amp; assumptions", A.intake())
    body += sec("framework", "Company-wide operating framework", "<h3>Lifecycle</h3>"+A.lifecycle()+"<h3>Approval gates</h3>"+A.gates()+"<h3>Responsibility matrix</h3>"+A.raci()+"<h3>Critical rules (non-negotiable)</h3>"+A.rules()+"<h3>Reading a stage</h3><p>Every stage card has the same ten parts: instructions, responsibilities, tools, time, cost, deliverables, quality checklist, approval criteria, official sources, common mistakes. Stage 1–11 follow product order; 12–14 are continuous disciplines.</p>")
    body += "".join(st[i] for i in range(1, 8))
    body += st[8] + st[9] + st[10] + st[11] + st[12] + st[13] + st[14]
    body += sec("finance", "Financial model – cost, pricing, profitability (computed)", financial())
    body += sec("rollout", "Rollout plan – first 150 days", ST.rollout(), True)
    body += sec("sample", "Worked example – SlotWise end to end", C.sample())
    body += sec("templates", "The 20 standard templates", C.templates())
    body += sec("forms", "Standard forms", C.forms(), True)
    body += sec("dashboard", "Internal management dashboard specification", C.dashboard())
    body += sec("ops", "Incidents, maintenance agreement and DNS", C.ops())
    body += sec("assump", "Assumption register", C.assumptions(), True)
    body += sec("sources", "Source index", C.sources(), True)
    html = f'''<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>SaaS Company Operating System</title><meta name="description" content="Complete SOP system for researching, building, pricing, launching, selling and maintaining SaaS products.">
<style>:root{{--wm:url(data:image/jpeg;base64,{logo})}}{open(os.path.join(HERE,"style.css")).read()}</style></head><body>
<button id="menu">☰ Contents</button><nav id="toc"><b>SaaS Operating System</b>{toc}</nav><main>{body}<footer class="credit">{CREDIT}<br>© VEL Software Solutions. Internal operating system – all rights reserved.</footer></main>
<script>{open(os.path.join(HERE,"app.js")).read()}</script></body></html>'''
    path = os.path.join(OUT, "saas-operating-system.html"); open(path, "w").write(html); return path

if __name__ == "__main__":
    p = build(); print("HTML", p, os.path.getsize(p))
    subprocess.run(["node", os.path.join(HERE, "pdf.js"), p, os.path.join(OUT, "saas-operating-system.pdf")], check=True)
