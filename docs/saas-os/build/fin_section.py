"""Financial appendix assembly (from calc.py)."""
import calc
from calc import inr
import sections_a as A, sections_fin as F, sections_store as ST

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

