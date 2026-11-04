"""Assemble the handbook: standalone HTML, print HTML for PDF, and per-template print HTML."""
import base64
import html
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent))
from lib import Chapter, inl  # noqa: E402
from style import BASE_CSS, SCREEN_CSS, PRINT_CSS  # noqa: E402
import c01_intro, c02_roadmap, c03_lifecycle, c04_structure_standards, c05_commercial_people  # noqa: E402,E401
import c06_projects_sources, c07_appendix_a, c08_templates, c09_templates2  # noqa: E402,E401

ROOT = Path(__file__).resolve().parent.parent
DOC_ID, VERSION, DATE = "VEL-CSE-001", "1.0", "4 November 2026"
TITLE = "Custom Software & Enterprise Solutions Department Handbook"
SUB = "From zero to professional: business systems, engineering, client delivery, costing, templates and onboarding"
HEADER_L = f"VEL Software Solutions · Custom Software & Enterprise Solutions Handbook ({DOC_ID} v{VERSION})"


def logo_b64() -> str:
    return base64.b64encode((ROOT / "assets" / "logo.png").read_bytes()).decode()


def collect() -> list[Chapter]:
    chapters: list[Chapter] = []
    for mod in (c01_intro, c02_roadmap, c03_lifecycle, c04_structure_standards, c05_commercial_people,
                c06_projects_sources, c07_appendix_a, c08_templates, c09_templates2):
        chapters += mod.build()
    # attach "Official references" to chapters
    url = {l: u for _, l, u, _ in c06_projects_sources.SOURCES}
    for ch in chapters:
        refs = c06_projects_sources.REFS.get(ch.num)
        if refs:
            ch.h2("Official references for this chapter")
            ch.raw("<ul>" + "".join(f'<li><a href="{url[r]}" target="_blank" rel="noopener">{html.escape(r)}</a> — <code>{html.escape(url[r])}</code></li>' for r in refs) + "</ul>")
    return chapters


def toc_label(ch: Chapter) -> str:
    return (f"{ch.num}. " if ch.num and ch.num[0].isdigit() else (f"{ch.num} — " if ch.num else "")) + ch.title


def heading_text(ch: Chapter) -> str:
    return f"{ch.num}. {ch.title}" if ch.num and ch.num[0].isdigit() else (f"{ch.num} — {ch.title}" if ch.num else ch.title)


# ------------------------------------------------------------------ HTML (screen)
def build_html(chs: list[Chapter]) -> str:
    lg = logo_b64()
    nav = []
    groups = [("Handbook", lambda c: c.kind == "chapter"), ("Templates & checklists", lambda c: c.kind == "appendix")]
    for gname, pred in groups:
        nav.append(f'<div class="grp">{gname}</div><ul>')
        for ch in chs:
            if not pred(ch):
                continue
            subs = ch.subs if ch.kind == "chapter" else []
            subhtml = "".join(f'<a href="#{i}">{inl(t)}</a>' for i, t in subs[:14])
            sub = f'<div class="sub">{subhtml}</div>' if subhtml else ""
            nav.append(f'<li data-target="{ch.id}"><a href="#{ch.id}" class="nl">{html.escape(toc_label(ch))}</a>{sub}</li>')
        nav.append("</ul>")
    body = "".join(ch.html() for ch in chs)
    stages = [(i, t) for ch in chs for (i, t) in ch.h3s]
    hero = f"""
<section class="hero" id="top-of-doc">
  <img src="data:image/png;base64,{lg}" alt="VEL Software Solutions logo">
  <div><div class="kicker">DEPARTMENT OPERATING SYSTEM</div>
  <h1>{TITLE}</h1><p>{SUB}</p>
  <p class="meta">Document {DOC_ID} · Version {VERSION} (draft for management approval) · {DATE} · Confidential — internal use</p>
  <p class="meta"><button class="printbtn" onclick="window.print()">Print / Save as PDF</button></p></div>
</section>
<div class="callout safety"><div class="ct">Engineer must verify</div><p>This handbook defines methods, controls and templates. It does not certify any system as secure, compliant or structurally/financially safe. Independent engineer or auditor review is required before production reliance. Vendor prices and tool versions must be re-verified from official sources on the day of use.</p></div>"""
    js = r"""
const q=document.getElementById('q'),count=document.getElementById('count'),secs=[...document.querySelectorAll('section.chapter')],
navLis=[...document.querySelectorAll('#nav li')];
function clearMarks(){document.querySelectorAll('mark').forEach(m=>{const p=m.parentNode;p.replaceChild(document.createTextNode(m.textContent),m);p.normalize();});}
function esc(s){return s.replace(/[.*+?^${}()|[\]\\]/g,'\\$&');}
function run(){clearMarks();const v=q.value.trim();
 if(v.length<2){secs.forEach(s=>s.classList.remove('hidden'));navLis.forEach(l=>l.style.display='');count.textContent='';return;}
 const lv=v.toLowerCase();let hits=0,chs=0;
 secs.forEach(s=>{const ok=s.textContent.toLowerCase().includes(lv)||(s.dataset.title||'').toLowerCase().includes(lv);s.classList.toggle('hidden',!ok);
   const li=document.querySelector('#nav li[data-target="'+s.id+'"]');if(li)li.style.display=ok?'':'none';if(ok)chs++;});
 const re=new RegExp(esc(v),'gi');let n=0;
 secs.filter(s=>!s.classList.contains('hidden')).forEach(s=>{
  const w=document.createTreeWalker(s,NodeFilter.SHOW_TEXT,{acceptNode:t=>t.parentElement.closest('script,style,mark')?NodeFilter.FILTER_REJECT:(re.test(t.nodeValue)?(re.lastIndex=0,NodeFilter.FILTER_ACCEPT):(re.lastIndex=0,NodeFilter.FILTER_REJECT))});
  const nodes=[];while(w.nextNode())nodes.push(w.currentNode);
  nodes.forEach(t=>{if(n>600)return;const f=document.createDocumentFragment();let last=0,m;re.lastIndex=0;const s0=t.nodeValue;
    while((m=re.exec(s0))){f.appendChild(document.createTextNode(s0.slice(last,m.index)));const mk=document.createElement('mark');mk.textContent=m[0];f.appendChild(mk);last=m.index+m[0].length;n++;if(m[0].length===0)re.lastIndex++;}
    f.appendChild(document.createTextNode(s0.slice(last)));t.parentNode.replaceChild(f,t);});});
 count.textContent=chs?`${chs} section(s) match “${v}”`:`No match for “${v}”`;}
q.addEventListener('input',run);
q.addEventListener('keydown',e=>{if(e.key==='Enter'){const m=document.querySelector('mark');if(m)m.scrollIntoView({block:'center'});}});
document.addEventListener('keydown',e=>{if(e.key==='/'&&document.activeElement!==q){e.preventDefault();q.focus();}});
const side=document.getElementById('side');document.getElementById('menu').onclick=()=>side.classList.toggle('show');
document.querySelectorAll('#nav a').forEach(a=>a.addEventListener('click',()=>side.classList.remove('show')));
const io=new IntersectionObserver(es=>{es.forEach(e=>{if(e.isIntersecting){const id=e.target.id;
  navLis.forEach(l=>{const on=l.dataset.target===id;l.classList.toggle('open',on);l.querySelector('a.nl').classList.toggle('active',on);});}});},{rootMargin:'-20% 0px -70% 0px'});
secs.forEach(s=>io.observe(s));
const topb=document.getElementById('top');window.addEventListener('scroll',()=>{topb.style.display=scrollY>600?'block':'none';});
topb.onclick=()=>scrollTo({top:0});
document.querySelectorAll('table').forEach(t=>{if(!t.parentElement.classList.contains('tablewrap')){const w=document.createElement('div');w.className='tablewrap';t.parentNode.insertBefore(w,t);w.appendChild(t);}});
"""
    return f"""<!doctype html><html lang="en"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>{TITLE} — VEL Software Solutions</title>
<meta name="description" content="{html.escape(SUB)}">
<style>{BASE_CSS}{SCREEN_CSS}</style></head><body>
<img class="watermark-screen" src="data:image/png;base64,{lg}" alt="">
<button id="menu" aria-label="Open navigation">☰ Menu</button>
<div class="layout"><aside id="side"><div class="brand"><img src="data:image/png;base64,{lg}" alt="VEL Software Solutions"><small>CUSTOM SOFTWARE &amp; ENTERPRISE</small></div>
<input id="q" type="search" placeholder="Search modules…  (press /)" aria-label="Search handbook"><div id="count" role="status"></div>
<nav id="nav">{''.join(nav)}</nav></aside>
<main>{hero}{body}<footer class="site">VEL Software Solutions · {TITLE} · {DOC_ID} v{VERSION} · {DATE}. Confidential — internal use. Tool versions, vendor prices and limits change: verify against official sources before use.</footer></main></div>
<button id="top" aria-label="Back to top">↑</button>
<script>{js}</script></body></html>"""


# ------------------------------------------------------------------ Print (PDF)
def print_shell(inner: str, extra: str = "") -> str:
    lg = logo_b64()
    return f"""<!doctype html><html lang="en"><head><meta charset="utf-8"><title>{TITLE}</title>
<style>{BASE_CSS}{PRINT_CSS}{extra}</style></head><body>
<div class="wm"><img src="data:image/png;base64,{lg}" alt=""></div>{inner}</body></html>"""


def build_print(chs: list[Chapter], pages: dict[str, int] | None = None) -> str:
    lg = logo_b64()
    pages = pages or {}

    def pg(key):
        return str(pages.get(key, "·"))
    cover = f"""<section class="cover"><img class="logo" src="data:image/png;base64,{lg}" alt="VEL Software Solutions">
<div class="kicker">DEPARTMENT OPERATING SYSTEM</div><h1 class="title">Custom Software &amp;<br>Enterprise Solutions<br>Department Handbook</h1>
<p class="sub">{SUB}</p><p class="by"><strong>VEL Software Solutions</strong></p>
<p class="fine">Document {DOC_ID} · Version {VERSION} (draft for management approval) · {DATE}<br>Tool versions, vendor prices and platform limits change. Always verify against official provider documentation and pricing calculators before building or quoting.</p></section>"""
    ctrl = f"""<section class="front"><h1>Document Control</h1>
<table><tbody><tr><td>Document</td><td>{TITLE} (Module M0)</td></tr><tr><td>ID / version</td><td>{DOC_ID} · v{VERSION} · draft for management approval</td></tr>
<tr><td>Date</td><td>{DATE}</td></tr><tr><td>Owner</td><td>Department Head, Custom Software &amp; Enterprise Solutions (to be named at approval)</td></tr>
<tr><td>Classification</td><td>Confidential — internal use</td></tr><tr><td>Review cycle</td><td>Quarterly, and after every closed client project (lessons learned)</td></tr></tbody></table>
<h3>Change log</h3><table><thead><tr><th>Version</th><th>Date</th><th>Change</th><th>Approved by</th></tr></thead><tbody>
<tr><td>1.0</td><td>{DATE}</td><td>First issue: handbook, onboarding plan, lifecycle, standards, costing model, templates</td><td></td></tr></tbody></table>
<div class="callout safety"><div class="ct">Engineer must verify</div><p>This handbook defines methods, controls and templates. It does not certify any system as secure, compliant, or safe. Independent engineer or auditor review is required before production reliance. Prices in Chapter 8 are internal assumptions, not vendor quotes.</p></div>
<h3>Approval</h3><table><thead><tr><th>Role</th><th>Name</th><th>Signature</th><th>Date</th></tr></thead><tbody>
<tr><td>Prepared by</td><td></td><td></td><td></td></tr><tr><td>Technical Lead</td><td></td><td></td><td></td></tr><tr><td>Department Head</td><td></td><td></td><td></td></tr></tbody></table></section>"""
    toc_items = "".join(
        f'<li class="{"app" if c.kind == "appendix" else ""}"><span class="t"><a href="#{c.id}">{html.escape(toc_label(c))}</a></span><span class="d"></span><span class="n">{pg(c.id)}</span></li>'
        for c in chs)
    stages = [(i, t) for ch in chs for (i, t) in ch.h3s]
    st_items = "".join(f'<li><span class="t"><a href="#{i}">{html.escape(t.replace("Stage ", "S"))}</a></span><span class="d"></span><span class="n">{pg(i)}</span></li>' for i, t in stages)
    toc = f"""<section class="front"><h1>Contents</h1><ul class="toc">{toc_items}</ul>
<h3>Lifecycle stage index (Chapter 5)</h3><ul class="toc stages">{st_items}</ul></section>"""
    return print_shell(cover + ctrl + toc + "".join(c.html() for c in chs))


def build_single(ch: Chapter) -> str:
    lg = logo_b64()
    head = f"""<div style="display:flex;align-items:center;gap:14px;margin-bottom:10px"><img src="data:image/png;base64,{lg}" style="width:70px;height:auto" alt="">
<div><div style="letter-spacing:.2em;color:#b5174f;font-weight:700;font-size:8pt">VEL SOFTWARE SOLUTIONS · {DOC_ID}</div>
<div style="color:#5b6578;font-size:9pt">Companion template to the {TITLE} · v{VERSION} · {DATE}</div></div></div>"""
    return print_shell(head + ch.html(), ".chapter{break-before:auto!important}")


def main():
    out = ROOT / "build" / "_tmp"
    out.mkdir(exist_ok=True)
    chs = collect()
    pages = json.loads((out / "pages.json").read_text()) if (out / "pages.json").exists() and "--pages" in sys.argv else None
    (ROOT / "output").mkdir(exist_ok=True)
    (ROOT / "output" / "VEL-Custom-Software-Enterprise-Handbook.html").write_text(build_html(chs), encoding="utf-8")
    (out / "print.html").write_text(build_print(chs, pages), encoding="utf-8")
    meta = {"chapters": [{"id": c.id, "label": heading_text(c), "kind": c.kind, "h3": c.h3s} for c in chs]}
    for c in chs:
        if c.kind == "appendix":
            (out / f"{c.id}.html").write_text(build_single(c), encoding="utf-8")
    (out / "meta.json").write_text(json.dumps(meta, ensure_ascii=False), encoding="utf-8")
    print("chapters:", len(chs))


if __name__ == "__main__":
    main()
