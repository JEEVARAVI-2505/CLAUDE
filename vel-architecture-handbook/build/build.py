import base64, json, os, re, subprocess, sys, html as H
sys.path.insert(0, os.path.dirname(__file__))
from lib import *
import c1, c2, c3, c4, c5

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
OUT = os.path.join(ROOT, 'output'); TMP = os.path.join(OUT, '.tmp')
os.makedirs(TMP, exist_ok=True); os.makedirs(os.path.join(OUT, 'templates'), exist_ok=True)
LOGO = base64.b64encode(open(os.path.join(ROOT, 'assets/vel-logo.png'), 'rb').read()).decode()
LOGO_URI = f'data:image/png;base64,{LOGO}'
BASE = open(os.path.join(ROOT, 'build/base.css')).read().replace('__LOGO__', LOGO_URI)
PRINT = open(os.path.join(ROOT, 'build/print.css')).read()
CO, EMAIL, VER, DATE = 'VEL Software Solutions', 'velsoftwaresolutions.in@gmail.com', '1.0', '4 November 2026'

# ------------------------------------------------------------------ documents
HB = dict(file='VEL-Software-Architecture-Handbook', id='VEL-SA-001', short='Software Architecture & Consulting Handbook',
          title='Software Architecture & Consulting Handbook', sub='From zero to professional: training, consulting lifecycle, governance, documentation, costing and client delivery', toc=True)
handbook_blocks = (c1.howto() + c1.portfolio() + c1.roles() + c1.levels() + c1.patterns() + c1.tools() + c2.lifecycle() + c2.repo() + c2.standards() + c3.costing()
    + c4.onboarding() + c4.exercise() + c4.lessons() + c4.projects() + c4.roadmap90() + c4.sources() + c4.architecture_diagrams() + c4.glossary()
    + [b for f in c5.all_templates() for b in f()])
# NOTE: roadmap90 H1 already numbered None -> unnumbered chapter.

def strip_num(blocks, new_title):
    out = list(blocks); out[0] = dict(out[0], num=None, title=new_title); return out

STANDALONE = [  # file, id, title, subtitle, blocks
 ('Employee-Onboarding-Guide', 'VEL-SA-002', 'Employee Onboarding Guide', 'Beginner onboarding and the first architecture diagram exercise', c4.onboarding() + c4.exercise()),
 ('90-Day-Training-Roadmap', 'VEL-SA-003', '90-Day Training Roadmap', 'Thirteen weeks from fundamentals to a full architecture proposal', c4.roadmap90() + c4.projects()),
 ('Client-Discovery-Questionnaire', 'VEL-SA-004', 'Client Discovery Questionnaire', 'Stage 02 interview and information-gathering template', c5.tC()),
 ('Architecture-Assessment-Template', 'VEL-SA-005', 'Architecture Assessment Template', 'Stages 03-04 current-state assessment and problem definition', c5.tB()),
 ('Requirements-Specification-Template', 'VEL-SA-006', 'Requirements Specification Template', 'Stage 05 functional and non-functional requirements baseline', c5.tE()),
 ('ADR-Template', 'VEL-SA-007', 'Architecture Decision Record Template', 'One decision, its options, reasons and consequences', c5.tD()),
 ('Technical-Feasibility-Report-Template', 'VEL-SA-008', 'Technical Feasibility Report Template', 'Stage 06 feasibility assessment', c5.tF()),
 ('Technology-Comparison-Matrix', 'VEL-SA-009', 'Technology Comparison Matrix', 'Stage 07 weighted comparison of options', c5.tG()),
 ('Architecture-Review-Checklist', 'VEL-SA-010', 'Architecture Review Checklist', 'Review standards for Stages 08-12 and 14', c5.tJ()),
 ('Cost-Estimation-Template', 'VEL-SA-011', 'Cost Estimation Template', 'Stage 10 delivery cost, pricing and client run-cost estimate', c5.tK()),
 ('Client-Presentation-Template', 'VEL-SA-012', 'Client Presentation Template', 'Stage 11 presentation structure and checklist', c5.tL()),
 ('Engineering-Handover-Checklist', 'VEL-SA-013', 'Engineering Handover Checklist', 'Stage 13 handover to development, cloud, data, API and security teams', c5.tM()),
 ('Lesson-Template-15-Part', 'VEL-SA-014', '15-Part Lesson Template', 'Employee teaching format', c5.tA()),
 ('Risk-and-Assumption-Register-Template', 'VEL-SA-015', 'Risk and Assumption Register Template', 'Risk and assumption tracking', c5.tH()),
 ('Stakeholder-Map-Template', 'VEL-SA-016', 'Stakeholder Map Template', 'Stakeholder identification and engagement', c5.tI())]

def chapters(blocks):
    return [(b['id'], (f'{b["num"]}. ' if isinstance(b.get('num'), int) else '') + b['title'], b['title']) for b in blocks if b['t'] == 'h1']

def body_html(blocks):
    h = render_html(blocks)
    return h[len('</section>'):] + '</section>'

def cover(doc, hb=False):
    return (f'<section class="cover"><img src="{LOGO_URI}" alt="VEL Software Solutions logo"><div class="tag">Software Architecture &amp; Consulting Department</div>'
            f'<h1>{H.escape(doc["title"])}</h1><div class="sub">{H.escape(doc["sub"])}</div>'
            f'<div class="by"><strong>Developed by {CO}</strong><br>Software Architecture &amp; Technical Consulting Department<br>{EMAIL}</div>'
            f'<div class="meta">Document {doc["id"]} · Version {VER} (draft for management approval) · {DATE}<br>'
            'Standards, tool versions, limits and prices change. Always verify against official sources before building, quoting or citing. '
            'Structural, security and compliance statements are advice for qualified review, not certification.</div></section>')

def print_html(doc, blocks, pages=None):
    toc = ''
    if doc.get('toc'):
        items = ''
        for i, (cid, label, _) in enumerate(chapters(blocks)):
            pg = (pages or {}).get(cid, '')
            items += f'<li><a class="t" href="#{cid}">{H.escape(label)}</a><span class="dots"></span><span class="pg">{pg}</span></li>'
        toc = f'<section class="toc chapter" id="toc"><h1>Contents</h1><ol>{items}</ol></section>'
    return (f'<!doctype html><html lang="en"><head><meta charset="utf-8"><title>{H.escape(doc["title"])} ({doc["id"]})</title>'
            f'<style>{BASE}\n{PRINT}</style></head><body><div class="wm"></div>{cover(doc)}{toc}{body_html(blocks)}</body></html>')

def pdf_jobs(items):
    jobs = []
    for doc, blocks, pages in items:
        p = os.path.join(TMP, doc['file'] + '.html'); open(p, 'w').write(print_html(doc, blocks, pages))
        dest = os.path.join(OUT, doc['file'] + '.pdf') if doc['file'] == HB['file'] else os.path.join(OUT, 'templates', doc['file'] + '.pdf')
        jobs.append(dict(html=p, pdf=dest, left=f'{CO} · {doc["short"] if "short" in doc else doc["title"]} ({doc["id"]} v{VER})', right='Confidential · internal use',
                         footL=f'Developed by {CO} · {EMAIL}'))
    json.dump(jobs, open(os.path.join(TMP, 'jobs.json'), 'w'))
    subprocess.run(['node', os.path.join(ROOT, 'build/render.js'), os.path.join(TMP, 'jobs.json')], check=True, cwd=ROOT)
    return jobs

def page_map(pdf, blocks):
    from pypdf import PdfReader
    r = PdfReader(pdf); texts = [re.sub(r'\s+', ' ', p.extract_text() or '') for p in r.pages]
    res = {}
    start = 2
    for cid, label, title in chapters(blocks):
        for i in range(start, len(texts)):
            t = texts[i]
            if t.strip().startswith(label) or t.strip().startswith(title):
                res[cid] = i + 1; start = i; break
    return res, len(r.pages)

# ------------------------------------------------------------------ web handbook
def web_html(pages_total=None):
    ch = chapters(handbook_blocks)
    nav = ''.join(f'<a href="#{cid}" data-id="{cid}">{H.escape(lbl)}</a>' for cid, lbl, _ in ch)
    cards = ''.join(f'<a class="tilec" href="#{cid}"><span>{H.escape(lbl)}</span></a>' for cid, lbl, _ in ch[:15])
    js = open(os.path.join(ROOT, 'build/app.js')).read()
    css = open(os.path.join(ROOT, 'build/web.css')).read()
    return f'''<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>VEL Architecture Handbook</title>
<meta name="description" content="VEL Software Solutions Software Architecture &amp; Consulting Handbook: training, 15-stage lifecycle, templates, costing and official sources.">
<link rel="icon" href="{LOGO_URI}">
<link rel="preconnect" href="https://fonts.googleapis.com"><link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&display=swap" rel="stylesheet">
<style>{BASE}\n{css}</style></head><body>
<a class="skip" href="#main">Skip to content</a>
<header class="top"><button id="menu" aria-label="Toggle navigation" aria-expanded="false">☰</button>
<img src="{LOGO_URI}" alt="VEL Software Solutions" class="lg"><div class="ttl"><strong>VEL Software Solutions</strong><span>Architecture &amp; Consulting Handbook · VEL-SA-001 v{VER}</span></div>
<div class="sw"><input id="q" type="search" placeholder="Search handbook  ( / )" autocomplete="off" aria-label="Search the handbook"><div id="res" role="listbox" hidden></div></div>
<a class="btn" href="VEL-Software-Architecture-Handbook.pdf" download>PDF</a><button class="btn" id="print" type="button">Print</button><button class="btn" id="theme" type="button" aria-label="Toggle dark mode">◐</button></header>
<div class="layout"><nav id="side" aria-label="Chapters"><div class="navh">Contents</div>{nav}<div class="navh">Downloads</div>
<a href="VEL-Software-Architecture-Handbook.pdf" download>Handbook (PDF)</a><a href="templates/">Templates (PDF + Markdown)</a><a href="Cost-Estimation-Template.xlsx" download>Cost estimation (XLSX)</a></nav>
<main id="main"><div class="wm"></div>
<section class="hero"><img src="{LOGO_URI}" alt="VEL Software Solutions logo"><div class="tag">Software Architecture &amp; Consulting Department</div><h1 class="ht">{HB["title"]}</h1><p class="sub">{HB["sub"]}</p>
<p class="by"><strong>Developed by {CO}</strong> · <a href="mailto:{EMAIL}">{EMAIL}</a><br><small>Document {HB["id"]} · Version {VER} (draft for management approval) · {DATE}</small></p>
<div class="tiles">{cards}</div></section>
{body_html(handbook_blocks)}
<footer class="foot">Developed by {CO} · {EMAIL} · Confidential · internal use · Verify standards and prices against official sources before relying on them.</footer></main></div>
<button id="top" aria-label="Back to top">↑</button><script>{js}</script></body></html>'''

# ------------------------------------------------------------------ extras
def write_md(doc, blocks):
    p = os.path.join(OUT, 'templates', doc['file'] + '.md')
    open(p, 'w').write(f'# {doc["title"]}\n\n_{doc["sub"]}_\n\nDocument {doc["id"]} · Version {VER} · {DATE} · {CO} · Confidential, internal use\n\n' + render_md(blocks[1:] if blocks[0]['t'] == 'h1' and False else blocks))

def main():
    pdf_items = [(dict(HB), handbook_blocks, None)]
    docs = []
    for f, i, t, s, b in STANDALONE:
        d = dict(file=f, id=i, title=t, sub=s); docs.append((d, b))
        # standalone: remove numbering in titles
        bb = [dict(x, num=None) if x['t'] == 'h1' else x for x in b]
        pdf_items.append((d, bb, None)); write_md(d, bb)
    pdf_items[0][0]['short'] = HB['short']
    # pass 1
    pdf_jobs(pdf_items)
    pages, total = page_map(os.path.join(OUT, HB['file'] + '.pdf'), handbook_blocks)
    print('chapter pages', len(pages), '/', len(chapters(handbook_blocks)), 'total pages', total)
    pdf_items[0] = (pdf_items[0][0], handbook_blocks, pages)
    pdf_jobs(pdf_items[:1])
    open(os.path.join(OUT, HB['file'] + '.html'), 'w').write(web_html())
    json.dump(dict(pages=pages, total=total), open(os.path.join(TMP, 'pages.json'), 'w'))
    open(os.path.join(OUT, HB['file'] + '.md'), 'w').write(f'# {HB["title"]}\n\n{HB["sub"]}\n\n' + render_md(handbook_blocks))

if __name__ == '__main__':
    main()
