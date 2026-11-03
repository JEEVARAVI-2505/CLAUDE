"""Build the VEL Software Delivery Handbook (HTML + PDF). Run: python3 build.py"""
import os, re, base64, subprocess, datetime, json
import hb_front as H, hb1, hb2, hb3, hb_app, fin_section, sections_c as C
from sections_a import table, callout

HERE = os.path.dirname(os.path.abspath(__file__)); OUT = os.path.dirname(HERE)
NAME = "VEL-Software-Delivery-Handbook"
TITLE = "Software Delivery Handbook"
VERSION = "2.0"
today = datetime.date.today().isoformat()
CREDIT = 'Developed by <b>V.LOKESHWARAN</b> · <a href="mailto:velsoftwaresolutions.in@gmail.com">velsoftwaresolutions.in@gmail.com</a> · VEL Software Solutions'
PHCOL = H.PH

def stage_html(s):
    col = PHCOL[s["phase"]]
    gate = f'<span class="gchip">{s["gate"]}</span>' if s["gate"] else '<span class="gchip off">No gate – readiness check</span>'
    info = table(["Start when","Who is involved","Time","Tools","Skills needed"], [(s["start"], s["who"], s["time"], s["tools"], s["skills"])], "info")
    rows = "".join(f'<tr><td class="no">{i+1}</td><td><b>{w}</b></td><td>{h}</td><td class="proof">{p}</td></tr>' for i, (w, h, p) in enumerate(s["steps"]))
    steps = f'<div class="tw"><table class="steps"><thead><tr><th>#</th><th>What to do</th><th>How to do it (exact)</th><th>Proof to save</th></tr></thead><tbody>{rows}</tbody></table></div>'
    chk = '<ul class="chk">' + "".join(f"<li>☐ {x}</li>" for x in s["selfcheck"]) + "</ul>"
    errs = table(["Common mistake","What to do instead"], s["errors"])
    learn = "<ul class='src'>" + "".join(f'<li><a href="{u}">{t}</a> <small>{u}</small></li>' for t, u in s["learn"]) + "</ul>"
    sig = f'<div class="sign"><b>Sign-off:</b> {s["signoff"]}<div class="sl"><span>Prepared by (name/date)</span><span>Reviewed by (name/date)</span><span>Approved by (name/date)</span></div></div>'
    return f'''<section class="stage" id="s{s["n"]}" style="--ph:{col}">
<div class="banner"><div class="big">STAGE {s["n"]} / 12</div><div class="bt"><h2>{s["title"]}</h2><div class="phase">{s["phase"]} phase</div></div>{gate}</div>
<p class="goal"><b>Goal:</b> {s["goal"]}</p>
{info}
<h3>Step by step</h3>{steps}
{s.get("extra","")}
<h3>Self-check before you ask for approval</h3>{chk}
{sig}
<h3>Common mistakes</h3>{errs}
<div class="callout"><b>Example (fictional training product SlotWise – numbers are illustrative)</b><div>{s["example"]}</div></div>
<h3>Learn more (official sources – verify the page and note the date)</h3>{learn}
<p class="stuck"><b>Stuck?</b> Try 30 minutes alone → ask a colleague → ask the Tech Lead (technical) or Product Owner (scope). Bring the goal, what you tried, the exact error, a screenshot.</p></section>'''

def glance(stages):
    rows = [(f'<b>{s["n"]}</b>', s["title"], s["phase"], s["goal"], s["gate"] or "–", s["time"]) for s in stages]
    return table(["Stage","Name","Phase","Goal in one line","Gate","Typical time"], rows)

def asking_help():
    return table(["Step","Action","Time"],[("1","Re-read the ticket, the PRD section and this handbook; search the error text","up to 30 min"),("2","Ask a colleague; bring: goal, what you tried, exact error text, screenshot","next"),("3","Ask the Tech Lead (technical) or Product Owner (what/why)","if still blocked"),("4","Owner decides money, legal, customer-relationship questions","rarely")])

def sec(id_, marker, title, body, cls=""):
    return f'<section id="{id_}" class="part {cls}"><div class="partno">{marker}</div><h2>{title}</h2>{body}</section>'

TOCITEMS = []  # (label, marker-to-find, anchor)

def build(pages=None):
    stages = hb1.stages() + hb2.stages() + hb3.stages()
    logo = base64.b64encode(open(os.path.join(OUT, "assets", "vel-logo.jpg"), "rb").read()).decode()
    items = [("How to use this handbook","HOW TO USE","use"),("The map – all stages on one page","PART 0","map"),("How a software company works","PART 1","p1"),("Folder structures – where everything lives","PART 2","p2"),
             ("Ground rules, Definition of Done, asking for help","PART 3","p3"),("12-week learning path","PART 4","p4"),("Plain-English glossary","PART 5","p5")]
    items += [(f"Stage {s['n']} – {s['title']}" + (f"  ({s['gate'].split(' –')[0]})" if s['gate'] else ""), f"STAGE {s['n']} / 12", f"s{s['n']}") for s in stages]
    items += [("Appendix A – Forms and templates to fill","APPENDIX A","apA"),("Appendix B – Worked cost, price and profit model","APPENDIX B","apB"),("Appendix C – Quick-reference commands","APPENDIX C","apC"),("Appendix D – The 20 standard documents","APPENDIX D","apD"),("Appendix E – Source index and assumption register","APPENDIX E","apE")]
    trs = ""
    for lab, mk, anc in items:
        pg = (pages or {}).get(mk, "–")
        trs += f'<tr><td><a href="#{anc}">{lab}</a></td><td class="pg">{pg}</td></tr>'
    toc = f'<table class="toc"><thead><tr><th>Contents</th><th>Page</th></tr></thead><tbody>{trs}</tbody></table>'
    nav = "".join(f'<a href="#{a}">{l}</a>' for l, m, a in items)
    cover = f'''<div class="cover"><img class="logo" src="data:image/jpeg;base64,{logo}" alt="VEL Software Solutions logo">
<div class="kicker">VEL SOFTWARE SOLUTIONS · INTERNAL STANDARD OPERATING PROCEDURE</div>
<h1>{TITLE}</h1><p class="sub">From zero to delivery: the stage-by-stage guide every employee follows to research, design, build, test, secure, launch, sell and maintain a software product.</p>
{table(["Document control",""],[("Document","VEL-SOP-001 · Software Delivery Handbook"),("Version",f"{VERSION}"),("Date",today),("Owner / author","V.LOKESHWARAN – VEL Software Solutions"),("Contact","velsoftwaresolutions.in@gmail.com"),("Audience","All employees, including beginners"),("Classification","Internal – confidential"),("Review","Every 6 months or after any major process change")],"dc")}
<p class="dev">{CREDIT}</p></div>'''
    use = sec("use","HOW TO USE","How to use this handbook", f'''<p>This handbook has one job: let a beginner finish a real software project the way a professional company does, <b>without having to guess</b>. Read Parts 0–5 once, then work stage by stage.</p>
{toc}
<h3>Which part to read now?</h3>{H.guide_use()}
<h3>How every stage page is laid out</h3><ol><li><b>Banner</b>: stage number, phase and the gate (approval) at its end.</li><li><b>Goal and quick facts</b>: when to start, who is involved, time, tools, skills.</li><li><b>Step by step</b>: <i>what</i> to do, <i>how</i> to do it exactly, and the <i>proof</i> to save.</li><li><b>Self-check</b>: tick every box before asking for approval.</li><li><b>Sign-off</b>: who approves; signature lines.</li><li><b>Common mistakes</b>, a <b>fictional example</b> (SlotWise), and <b>official sources</b> to learn more.</li></ol>
{callout("warn","Important","Figures marked ASSUMPTION are planning placeholders. Prices, tax rates and legal duties change: always open the official source, confirm, and note the date. Nothing in this handbook is legal, tax or financial advice; confirm those with qualified professionals. Never claim a product is secure or production-ready without the evidence the gates require.")}''')
    mapp = sec("map","PART 0","The map – all stages on one page", '<div class="fig">'+H.flow_svg()+'</div><p>Each stage ends with a numbered <b>gate</b>: a sign-off by a named person. Stage 0 has no gate. You may start Stage <i>n</i>+1 only when Gate G<i>n</i> is signed.</p><h3>Every gate works the same way</h3><div class="fig">'+H.gate_svg()+'</div><h3>Stages at a glance</h3>'+glance(stages))
    p1 = sec("p1","PART 1","How a software company works", H.company_works())
    p2 = sec("p2","PART 2","Folder structures – where everything lives", H.folders())
    p3 = sec("p3","PART 3","Ground rules, Definition of Done, asking for help", "<h3>3.1 The ten rules</h3>"+H.rules()+"<h3>3.2 Definition of Done</h3>"+H.dod()+"<h3>3.3 How to ask for help</h3>"+asking_help())
    p4 = sec("p4","PART 4","12-week learning path for beginners", "<p>You do not need to know everything on Day 1. Spend about 5 working hours a week following this path while you do real tickets. Each week has a <b>proof</b> your Tech Lead can check.</p>"+H.learning())
    p5 = sec("p5","PART 5","Plain-English glossary", H.glossary())
    body = cover + use + mapp + p1 + p2 + p3 + p4 + p5 + "".join(stage_html(s) for s in stages)
    body += sec("apA","APPENDIX A","Forms and templates to fill", hb_app.forms())
    body += sec("apB","APPENDIX B","Worked cost, price and profit model (sample product)", "<p>Used in Stage 9. It shows <b>how</b> to calculate; replace every ASSUMPTION with a cited, dated official figure.</p>"+fin_section.financial())
    body += sec("apC","APPENDIX C","Quick-reference commands", hb_app.commands(), "cont")
    body += sec("apD","APPENDIX D","The 20 standard documents and the management dashboard", C.templates()+"<h3>Internal management dashboard (for managers)</h3>"+C.dashboard())
    body += sec("apE","APPENDIX E","Source index and assumption register", C.sources()+"<h3>Assumption register</h3>"+C.assumptions(), "cont")
    css = open(os.path.join(HERE, "style.css")).read() + open(os.path.join(HERE, "style2.css")).read()
    html = f'''<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>{TITLE} – VEL Software Solutions</title><meta name="description" content="Stage-by-stage guide from zero to delivery for every VEL Software Solutions employee.">
<style>:root{{--wm:url(data:image/jpeg;base64,{logo})}}{css}</style></head><body>
<button id="menu">☰ Contents</button><nav id="toc"><b>{TITLE}</b>{nav}</nav><main>{body}<footer class="credit">{CREDIT}<br>© VEL Software Solutions. Internal handbook – all rights reserved.</footer></main>
<script>{open(os.path.join(HERE,"app.js")).read()}</script></body></html>'''
    path = os.path.join(OUT, NAME + ".html"); open(path, "w").write(html)
    return path, items

def find_pages(pdf, items):
    n = int(re.search(r"Pages:\s+(\d+)", subprocess.run(["pdfinfo", pdf], capture_output=True, text=True).stdout).group(1))
    texts = [subprocess.run(["pdftotext", "-f", str(i), "-l", str(i), pdf, "-"], capture_output=True, text=True).stdout for i in range(1, n+1)]
    res = {}
    for _, mk, _ in items:
        for i, t in enumerate(texts):
            if i >= 1 and mk.replace(" ","") in re.sub(r"\s+","",t): res[mk] = i+1; break
    return res

if __name__ == "__main__":
    pdf = os.path.join(OUT, NAME + ".pdf")
    p, items = build(); subprocess.run(["node", os.path.join(HERE, "pdf.js"), p, pdf], check=True)
    pages = find_pages(pdf, items); print("pages", pages)
    p, items = build(pages); subprocess.run(["node", os.path.join(HERE, "pdf.js"), p, pdf], check=True)
    pages2 = find_pages(pdf, items); print("stable:", pages == pages2)
    if pages != pages2:
        p, items = build(pages2); subprocess.run(["node", os.path.join(HERE, "pdf.js"), p, pdf], check=True)
    print(p, os.path.getsize(p), pdf)
