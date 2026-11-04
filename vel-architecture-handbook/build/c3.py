import math
from lib import *

# Illustrative training rates (INR/hour). Replace with approved company rates.
RATES = {  # role: (loaded cost/h, standard billing rate/h)
 'Junior Technical Analyst': (450, 1200), 'Business Analyst': (700, 1800), 'Solution Architect': (1200, 3200),
 'Cloud Architect': (1300, 3500), 'Security Architect': (1400, 3800), 'Principal Architect': (2000, 5500), 'Project Manager': (900, 2200)}
ROLES = list(RATES)

MODELS = [  # name, scope text, hours dict, expenses [(label, amt)], risk%, target GM, unit
 ('A. Fixed-scope architecture assessment', '3 weeks; current-state review, findings, options, roadmap.',
  {'Junior Technical Analyst': 80, 'Business Analyst': 40, 'Solution Architect': 60, 'Cloud Architect': 16, 'Security Architect': 16, 'Principal Architect': 12, 'Project Manager': 16},
  [('Workshop / travel', 12000), ('Tools and licences', 3000)], 0.10, 0.50),
 ('B. Architecture design engagement', '8 weeks; full lifecycle stages 02-12 with documentation and presentation.',
  {'Junior Technical Analyst': 240, 'Business Analyst': 120, 'Solution Architect': 200, 'Cloud Architect': 60, 'Security Architect': 60, 'Principal Architect': 40, 'Project Manager': 48},
  [('Workshop / travel', 30000), ('Tools, calculators, licences', 6000)], 0.12, 0.50),
 ('C. Technical due diligence', '4 weeks; independent assessment for an investor or acquirer; fixed deadline.',
  {'Junior Technical Analyst': 60, 'Business Analyst': 20, 'Solution Architect': 80, 'Cloud Architect': 24, 'Security Architect': 40, 'Principal Architect': 40, 'Project Manager': 20},
  [('Travel / data-room access', 10000), ('Tools (code and dependency scanning)', 5000)], 0.15, 0.60),
 ('D. Implementation advisory', '12 weeks; governance reviews, design Q&A, compliance checks.',
  {'Junior Technical Analyst': 60, 'Solution Architect': 144, 'Security Architect': 12, 'Principal Architect': 24, 'Project Manager': 24},
  [('Travel / workshops', 8000)], 0.08, 0.50),
 ('E. Monthly architecture consulting (retainer)', 'Per month; fixed hours of advice, reviews, ADR support.',
  {'Junior Technical Analyst': 16, 'Solution Architect': 24, 'Principal Architect': 6, 'Project Manager': 4},
  [('Tools and admin', 1500)], 0.05, 0.45)]

def inr(n):
    n = int(round(n)); s = str(abs(n))
    if len(s) > 3:
        head, tail = s[:-3], s[-3:]
        parts = []
        while len(head) > 2: parts.insert(0, head[-2:]); head = head[:-2]
        if head: parts.insert(0, head)
        s = ','.join(parts + [tail])
    return ('-' if n < 0 else '') + 'Rs ' + s

def calc(m):
    name, scope, hrs, exp, risk, gm = m[:6]
    labour = sum(h * RATES[r][0] for r, h in hrs.items()); expenses = sum(a for _, a in exp)
    dc = labour + expenses; rr = risk * dc
    price = math.ceil((dc + rr) / (1 - gm) / 1000) * 1000
    gp = price - dc; gmp = gp / price
    var = 0.04 * price  # 3% sales commission + 1% payment costs
    cm = price - dc - var - rr  # conservative: risk reserve fully consumed
    stress = dc * 1.25 + (0)  # 25% effort overrun on labour+expenses
    gm_stress = (price - stress) / price
    hours = sum(hrs.values()); std = sum(h * RATES[r][1] for r, h in hrs.items())
    return dict(name=name, scope=scope, hrs=hrs, exp=exp, labour=labour, expenses=expenses, dc=dc, rr=rr, risk=risk, price=price, gp=gp, gm=gmp, var=var, cm=cm,
                cmp=cm / price, gm_stress=gm_stress, hours=hours, blended=price / hours, std=std, disc=1 - price / std)

def costing():
    res = [calc(m) for m in MODELS]
    b = [H1('Commercial Costing and Pricing', 'costing', 9),
     CALL('red', 'Illustrative numbers', 'All rates, hours and margins below are **training defaults in Indian rupees (Rs), excluding GST**. They demonstrate the method. Finance and the Principal Architect must approve real rates before any quote is issued.'),
     H2('Cost components to estimate on every engagement'),
     TABLE(['Component', 'How to estimate', 'Common mistake'], [
      ['Discovery workshops', 'Sessions x people x hours + preparation (about 1:1) + write-up', 'Forgetting preparation and write-up'],
      ['Business analysis', 'Requirements count x hours each; interviews x hours', 'Underestimating review cycles'],
      ['Technical assessment', 'Systems x hours per system (inventory, review, interview)', 'Assuming documents exist'],
      ['Architecture design', 'Components x hours; options x hours; ADRs x 2-4 h', 'Designing only one option'],
      ['Architecture reviews', 'Review rounds x reviewer hours', 'No time for rework after review'],
      ['Security architecture review', 'Data flows x threat-model hours + control mapping', 'Treating it as a checkbox'],
      ['Cloud cost estimation', 'Official calculator; region, size, hours, data transfer; record date', 'Ignoring data transfer, backups, support plans'],
      ['Documentation', 'Pages x hours; diagrams x hours', 'Leaving it to the end'],
      ['Stakeholder presentations', 'Preparation + rehearsal + delivery + follow-up', 'No rehearsal'],
      ['Implementation support', 'Review milestones x hours + on-call Q&A', 'Open-ended unpaid support'],
      ['Travel / workshop expenses', 'Actual receipts or fixed allowance in the SOW', 'Not agreeing who pays'],
      ['Ongoing advisory retainer', 'Fixed monthly hours; rollover rules; response times', 'No cap on out-of-hours requests']], [24, 44, 32]),
     H2('Rate card (internal, illustrative)'),
     TABLE(['Role', 'Loaded cost per hour', 'Standard billing rate per hour'], [[r, inr(RATES[r][0]), inr(RATES[r][1])] for r in ROLES], [44, 28, 28]),
     P('**Loaded cost** = salary + statutory benefits + equipment, licences, space and a share of management overhead, divided by realistic billable hours (not 2,080).'),
     H2('Definitions and formulas'),
     TABLE(['Term', 'Formula', 'Meaning'], [
      ['Delivery cost (DC)', 'Σ(hours x loaded cost) + direct expenses', 'What it costs us to deliver'],
      ['Risk reserve (RR)', 'Risk % x DC', 'Money set aside for estimating error; embedded in price'],
      ['Selling price', 'Round-up((DC + RR) / (1 − target gross margin))', 'Quoted fee (ex GST)'],
      ['Gross margin %', '(Price − DC) / Price', 'Profit after direct delivery costs'],
      ['Contribution margin', 'Price − DC − variable selling costs (4%) − RR', 'What remains to cover fixed overhead and profit; reserve assumed fully used (conservative)'],
      ['Overrun stress test', '(Price − 1.25 x DC) / Price', 'Gross margin if effort overruns by 25%'],
      ['Recurring revenue', 'MRR = monthly fee; ARR = 12 x MRR', 'Predictable income; basis for valuing the practice']], [20, 44, 36])]
    for r in res:
        rows = [[k, str(h), inr(RATES[k][0]), inr(h * RATES[k][0])] for k, h in r['hrs'].items()]
        rows.append(['**Total labour**', f'**{r["hours"]}**', '', f'**{inr(r["labour"])}**'])
        rows += [[l, '', '', inr(a)] for l, a in r['exp']]
        b += [H2(r['name']), P(r['scope']),
              TABLE(['Role / expense', 'Hours', 'Cost per hour', 'Cost'], rows, [40, 14, 22, 24]),
              TABLE(['Metric', 'Value'], [
                ['Delivery cost (DC)', inr(r['dc'])], [f'Risk reserve ({int(r["risk"]*100)}% of DC)', inr(r['rr'])],
                ['**Selling price (ex GST)**', f'**{inr(r["price"])}**'], ['Gross profit', inr(r['gp'])], ['**Gross margin**', f'**{r["gm"]*100:.1f}%**'],
                ['Variable selling costs (4%)', inr(r['var'])], ['Contribution margin', f'{inr(r["cm"])} ({r["cmp"]*100:.1f}%)'],
                ['Blended rate per hour', inr(r['blended'])], ['Discount vs standard rate card (negative = premium)', f'{r["disc"]*100:.1f}% (standard value {inr(r["std"])})'],
                ['Stress test: gross margin at +25% effort', f'{r["gm_stress"]*100:.1f}%']], [60, 40])]
    e = res[-1]; d = res[1]
    b += [H2('Recurring revenue (retainer)'),
          TABLE(['Metric', 'Value'], [['Monthly fee (MRR)', inr(e['price'])], ['Annual recurring revenue (ARR)', inr(e['price'] * 12)],
                ['Annual gross profit at current margin', inr(e['gp'] * 12)],
                ['Annual delivery cost', inr(e['dc'] * 12)]], [60, 40]),
          P(f'**Pipeline logic:** if {int(0.3*100)}% of design engagements (Model B, price {inr(d["price"])}) convert to retainers (an *assumption* to measure, not a promise), ten design engagements per year would add about {inr(0.3 * 10 * e["price"] * 12)} of ARR after conversion. Track the actual conversion rate and replace this assumption.'),
          H2('Project risk scoring for fixed-price quotes'),
          TABLE(['Risk factor', 'Low (1)', 'Medium (2)', 'High (3)'], [
            ['Requirement clarity', 'Signed requirements', 'Partly known', 'Vague'],
            ['Client access to people/data', 'Named and available', 'Limited', 'Unknown'],
            ['Technology novelty', 'Familiar', 'Some new', 'Mostly new'],
            ['Stakeholder alignment', 'Single sponsor', 'Several, aligned', 'Conflicting'],
            ['Deadline pressure', 'Flexible', 'Firm', 'Immovable']], [28, 24, 24, 24]),
          P('Total 5-7 → reserve 5-8% · 8-11 → 10-12% · 12-15 → 15-20% **or** quote time-and-materials. Never absorb unlimited risk in a fixed price.'),
          CALL('gold', 'Pricing rules', '1) Price on value and risk, not only on hours. 2) Put scope, assumptions, exclusions and change control in the SOW. 3) Cloud run-costs are estimates from official calculators, shown as ranges with dates; we do not guarantee client cloud bills. 4) Discounts above 10% need Principal Architect and Finance approval. 5) GST and taxes are added as applicable; confirm with Finance.')]
    return b
