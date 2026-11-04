import os, sys
sys.path.insert(0, os.path.dirname(__file__))
from openpyxl import Workbook
from openpyxl.styles import Font, PatternFill, Alignment, Border, Side
from openpyxl.utils import get_column_letter
import c3
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..')); OUT = os.path.join(ROOT, 'output')

def xlsx():
    wb = Workbook(); NAVY = '14306B'
    hf = Font(bold=True, color='FFFFFF'); hfill = PatternFill('solid', fgColor=NAVY); inp = PatternFill('solid', fgColor='FFF3C4'); thin = Side(style='thin', color='D5DCEC'); bd = Border(left=thin, right=thin, top=thin, bottom=thin)
    def head(ws, r, vals):
        for i, v in enumerate(vals, 1):
            c = ws.cell(r, i, v); c.font = hf; c.fill = hfill; c.alignment = Alignment(wrap_text=True, vertical='center'); c.border = bd
    ws = wb.active; ws.title = 'README'
    for i, t in enumerate(['VEL Software Solutions · Cost Estimation Template (VEL-SA-011 v1.0)', 'Yellow cells are inputs; all other numbers are formulas.', 'Rates are ILLUSTRATIVE training defaults (Rs, ex GST). Replace with company-approved rates before quoting.', 'Sheets: Rates → Estimate → Pricing → CloudRunCost → TCO.', 'Cloud costs are estimates from official calculators: record the date and region; do not guarantee client bills.'], 1):
        ws.cell(i, 1, t).font = Font(bold=(i == 1), size=13 if i == 1 else 11, color=NAVY if i == 1 else '000000')
    ws.column_dimensions['A'].width = 110
    r = wb.create_sheet('Rates'); head(r, 1, ['Role', 'Loaded cost / hour', 'Billing rate / hour'])
    for i, (k, (c, b)) in enumerate(c3.RATES.items(), 2):
        r.cell(i, 1, k); r.cell(i, 2, c).fill = inp; r.cell(i, 3, b).fill = inp
    r.column_dimensions['A'].width = 30; r.column_dimensions['B'].width = 20; r.column_dimensions['C'].width = 20
    n = len(c3.RATES) + 1
    e = wb.create_sheet('Estimate'); head(e, 1, ['Activity', 'Role', 'Hours', 'Cost / hour', 'Cost'])
    sample = [('Discovery workshops', 'Business Analyst', 24), ('Current-state assessment', 'Junior Technical Analyst', 40), ('Architecture design', 'Solution Architect', 60), ('Security review', 'Security Architect', 16), ('Review and approval', 'Principal Architect', 12), ('Project management', 'Project Manager', 16)]
    for i in range(2, 22):
        if i - 2 < len(sample): a, ro, h = sample[i - 2]; e.cell(i, 1, a); e.cell(i, 2, ro); e.cell(i, 3, h)
        for c in (1, 2, 3): e.cell(i, c).fill = inp
        e.cell(i, 4, f'=IFERROR(VLOOKUP(B{i},Rates!$A$2:$C${n},2,FALSE),0)'); e.cell(i, 5, f'=C{i}*D{i}')
    e.cell(22, 1, 'Total labour').font = Font(bold=True); e.cell(22, 3, '=SUM(C2:C21)'); e.cell(22, 5, '=SUM(E2:E21)')
    e.cell(24, 1, 'Direct expenses').font = Font(bold=True); head(e, 25, ['Item', '', '', '', 'Amount'])
    for i, (a, v) in enumerate([('Workshop / travel', 12000), ('Tools and licences', 3000)], 26): e.cell(i, 1, a).fill = inp; e.cell(i, 5, v).fill = inp
    for i in range(28, 31): e.cell(i, 1).fill = inp; e.cell(i, 5).fill = inp
    e.cell(31, 1, 'Total expenses').font = Font(bold=True); e.cell(31, 5, '=SUM(E26:E30)')
    for col, w in zip('ABCDE', (34, 28, 10, 14, 16)): e.column_dimensions[col].width = w
    p = wb.create_sheet('Pricing'); head(p, 1, ['Line', 'Value', 'Formula note'])
    rows = [('Delivery cost (DC)', '=Estimate!E22+Estimate!E31', 'labour + expenses'), ('Risk reserve %', 0.10, 'input: see risk scoring in handbook Ch. 9'), ('Risk reserve', '=B2*B3', 'risk % x DC'),
            ('Target gross margin %', 0.50, 'input'), ('Selling price (ex GST, rounded up to 1,000)', '=CEILING((B2+B4)/(1-B5),1000)', '(DC + reserve) / (1 - GM)'), ('Gross profit', '=B6-B2', 'price - DC'), ('Gross margin %', '=B7/B6', ''),
            ('Variable selling cost %', 0.04, 'input: commission + payment costs'), ('Variable selling costs', '=B6*B9', ''), ('Contribution margin', '=B6-B2-B10-B4', 'reserve assumed fully used'), ('Contribution margin %', '=B11/B6', ''),
            ('Stress: gross margin at +25% effort', '=(B6-1.25*B2)/B6', 'price vs 125% of DC'), ('Hours total', '=Estimate!C22', ''), ('Blended rate / hour', '=IFERROR(B6/B14,0)', '')]
    for i, (a, v, n_) in enumerate(rows, 2):
        p.cell(i, 1, a); c = p.cell(i, 2, v); p.cell(i, 3, n_)
        if not (isinstance(v, str) and v.startswith('=')): c.fill = inp
        if 'margin %' in a.lower() or 'cost %' in a.lower() or 'reserve %' in a.lower() or 'Stress' in a: c.number_format = '0.0%'
        else: c.number_format = '#,##0'
    p.column_dimensions['A'].width = 46; p.column_dimensions['B'].width = 18; p.column_dimensions['C'].width = 46
    cl = wb.create_sheet('CloudRunCost'); head(cl, 1, ['Service / component', 'Quantity and size', 'Region', 'Monthly low', 'Monthly high', 'Calculator link and date'])
    for i in range(2, 14):
        for c in range(1, 7): cl.cell(i, c).fill = inp
    cl.cell(14, 1, 'Total per month').font = Font(bold=True); cl.cell(14, 4, '=SUM(D2:D13)'); cl.cell(14, 5, '=SUM(E2:E13)'); cl.cell(15, 1, 'Total per year').font = Font(bold=True); cl.cell(15, 4, '=D14*12'); cl.cell(15, 5, '=E14*12')
    for col, w in zip('ABCDEF', (30, 24, 14, 14, 14, 40)): cl.column_dimensions[col].width = w
    t = wb.create_sheet('TCO'); head(t, 1, ['Cost (3 years)', 'Option A', 'Option B', 'Baseline'])
    for i, a in enumerate(['Build (one-time)', 'Infrastructure', 'Licences', 'Support and operations', 'Training', 'Risk contingency'], 2):
        t.cell(i, 1, a)
        for c in (2, 3, 4): t.cell(i, c).fill = inp
    t.cell(8, 1, 'Total').font = Font(bold=True)
    for c in 'BCD': t[f'{c}8'] = f'=SUM({c}2:{c}7)'
    for col, w in zip('ABCD', (30, 16, 16, 16)): t.column_dimensions[col].width = w
    wb.save(os.path.join(OUT, 'Cost-Estimation-Template.xlsx'))

def skeleton():
    base = os.path.join(OUT, 'architecture-project-template')
    desc = {
     'README.md': 'Project name, client, version, status, owners, folder map, how to render diagrams.', 'docs/executive-summary.md': 'Problem, recommendation, cost range, key risks, decisions needed. Max 2 pages, no jargon.',
     'docs/business-context.md': 'Organisation, measurable objectives, stakeholders, processes, user journeys.', 'docs/constraints.md': 'Budget, dates, skills, legal, technology. Mark CONFIRMED or ASSUMED.',
     'docs/assumptions.md': 'ID, statement, impact if wrong, owner, confirm-by date, status.', 'docs/risks.md': 'Risk register: ID, likelihood, impact, score, mitigation, owner.', 'docs/implementation-roadmap.md': 'Phases, milestones, dependencies, rollout and migration plan.',
     'docs/requirements/': 'functional.md, non-functional.md, acceptance-criteria.md, traceability.csv.', 'docs/current-state/': 'As-is inventory, integrations, infrastructure, data, security, operations.',
     'docs/target-state/': 'Options paper, target architecture, components, migration.md, operations.md.', 'docs/architecture-decisions/': 'Narrative of decisions and comparison matrices; links to /adr.',
     'docs/security/': 'Trust boundaries, threat model, control mapping, data classification.', 'docs/cost-analysis/': 'Build estimate, run-cost model with dated calculator links, TCO.',
     'diagrams/context/': 'C4 Level 1 source + exports.', 'diagrams/containers/': 'C4 Level 2 source + exports.', 'diagrams/components/': 'C4 Level 3 source + exports.', 'diagrams/deployment/': 'Where software runs.',
     'diagrams/data-flow/': 'Data flows with trust boundaries.', 'diagrams/network/': 'Subnets, routing, firewalls.', 'specifications/api/': 'OpenAPI / AsyncAPI, versioning and error policy.',
     'specifications/database/': 'ERD, data dictionary, backup and retention policy.', 'specifications/integrations/': 'Interface catalogue, formats, retry and error handling.', 'specifications/infrastructure/': 'Resource list, IaC structure, environment matrix.',
     'reviews/': 'Peer review records, ARB minutes, checklist results, findings.', 'adr/': 'Architecture Decision Records 0001-title.md; never deleted, only superseded.', 'handover/': 'Handover checklist, specs index, acknowledgements, open risks.'}
    for path, text in desc.items():
        full = os.path.join(base, path)
        if path.endswith('/'):
            os.makedirs(full, exist_ok=True); open(os.path.join(full, 'README.md'), 'w').write(f'# {path}\n\n**Purpose / expected contents:** {text}\n\nOwner: ____  ·  Version: ____  ·  Last reviewed: ____\n')
        else:
            os.makedirs(os.path.dirname(full), exist_ok=True)
            title = os.path.basename(path).replace('.md', '').replace('-', ' ').title() if path != 'README.md' else 'Architecture Project: <name>'
            open(full, 'w').write(f'# {title}\n\n> **Expected contents:** {text}\n\nStatus: Draft · Owner: ____ · Reviewer: ____ · Version: 0.1\n\n## Assumptions and unresolved decisions\n\n_Mark each fact CONFIRMED (source), ASSUMED (owner, confirm-by) or UNKNOWN._\n')
    os.makedirs(os.path.join(base, 'adr'), exist_ok=True)
    open(os.path.join(base, 'adr', '0000-template.md'), 'w').write('# ADR-NNNN: <decision>\n\nStatus: Proposed | Accepted | Deprecated | Superseded by ADR-XXXX\nDate: YYYY-MM-DD   Deciders: ____   Reviewer: ____   Impact: Low | Medium | High\n\n## Context\n\n## Options considered\n1. \n2. \n3. Do nothing\n\n## Decision\n\n## Consequences\nPositive:\nNegative / risks:\nReview trigger:\n')

def links():
    urls = ['https://c4model.com/', 'https://martinfowler.com/architecture/', 'https://www.opengroup.org/togaf', 'https://www.iso.org/', 'https://learn.microsoft.com/azure/architecture/', 'https://docs.aws.amazon.com/wellarchitected/', 'https://cloud.google.com/architecture/framework', 'https://owasp.org/', 'https://www.nist.gov/cyberframework', 'https://www.uml.org/', 'https://plantuml.com/', 'https://mermaid.js.org/', 'https://www.drawio.com/', 'https://adr.github.io/', 'https://arc42.org/', 'https://docs.structurizr.com/', 'https://developer.hashicorp.com/terraform/docs', 'https://learning.postman.com/', 'https://owasp.org/www-project-threat-dragon/', 'https://learn.microsoft.com/azure/security/develop/threat-modeling-tool', 'https://spec.openapis.org/oas/latest.html', 'https://sre.google/books/', 'https://12factor.net/']
    s = '#!/usr/bin/env bash\n# Checks every official source link used in the handbook. Run from a machine with normal internet access.\nset -u\nok=0; bad=0\nfor u in \\\n' + ' \\\n'.join(f'  {u}' for u in urls) + '\ndo\n  code=$(curl -s -o /dev/null -L -m 25 -A "Mozilla/5.0 (VEL link check)" -w "%{http_code}" "$u")\n  case "$code" in 2*|3*) echo "OK   $code $u"; ok=$((ok+1));; *) echo "FAIL $code $u"; bad=$((bad+1));; esac\ndone\necho "OK=$ok FAIL=$bad  ($(date -u +%F))"\n[ "$bad" -eq 0 ]\n'
    p = os.path.join(OUT, 'check-links.sh'); open(p, 'w').write(s); os.chmod(p, 0o755)

if __name__ == '__main__':
    xlsx(); skeleton(); links(); print('extras ok')
