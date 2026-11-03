"""Build the responsive WEB version of the handbook: dist/handbook.html
Developed by V. LOKESHWARAN, VEL Software Solutions, velsoftwaresolutions.in@gmail.com"""
import base64, re
from pathlib import Path
import markdown

ROOT = Path(__file__).resolve().parent.parent
import sys; sys.path.insert(0, str(Path(__file__).parent)); from handbook_sources import load
b64 = lambda p: base64.b64encode((ROOT / "assets" / p).read_bytes()).decode()
small, big = b64("vel-logo-small.webp"), b64("vel-logo.webp")
md = markdown.Markdown(extensions=["tables", "fenced_code", "sane_lists"])

def render(text, sid):
    md.reset(); h = md.convert(text)
    n = [0]
    def chk(_):
        n[0] += 1
        return f'<li class="chk"><label><input type="checkbox" data-k="{sid}-{n[0]}"> '
    h = re.sub(r"<li>\[[ x]\] ", chk, h)
    h = re.sub(r'(<li class="chk"><label>.*?)</li>', r"\1</label></li>", h, flags=re.S)
    h = re.sub(r'<p><em>Developed by.*?</em></p>', '', h)
    h = re.sub(r"<table>", '<div class="tw"><table>', h).replace("</table>", "</table></div>")
    return h.replace("<h1>", f'<h1 id="{sid}">', 1)

secs, nav = [], []
for i, (title, text) in enumerate(load()):
    sid = "p" + str(i)
    nav.append(f'<a href="#{sid}" data-s="{sid}">{title}</a>')
    secs.append(f'<article class="chap" data-s="{sid}">{render(text, sid)}</article>')

CSS = """
:root{--bg:#f7f8fa;--surface:#fff;--fg:#1b2430;--muted:#5a6573;--line:#d5dbe3;--navy:#14306b;--accent:#c2185b;--code:#eef1f5;--hl:#e8eefb}
@media (prefers-color-scheme:dark){:root:not([data-theme="light"]){--bg:#10151c;--surface:#171e28;--fg:#e4e9f0;--muted:#9aa6b5;--line:#2a3441;--navy:#9db8f5;--accent:#f06fa4;--code:#1f2835;--hl:#1d2a44;color-scheme:dark}}
:root[data-theme="dark"]{--bg:#10151c;--surface:#171e28;--fg:#e4e9f0;--muted:#9aa6b5;--line:#2a3441;--navy:#9db8f5;--accent:#f06fa4;--code:#1f2835;--hl:#1d2a44;color-scheme:dark}
*{box-sizing:border-box}html{scroll-padding-top:70px}
body{margin:0;background:var(--bg);color:var(--fg);font:16px/1.6 system-ui,-apple-system,'Segoe UI',Roboto,sans-serif}
body::after{content:"";position:fixed;inset:0;background:url(data:image/webp;base64,@@BIG@@) center/min(560px,80vw) no-repeat;opacity:.045;pointer-events:none;z-index:0}
:root[data-theme="dark"] body::after,:root:not([data-theme="light"]) body::after{}
@media (prefers-color-scheme:dark){:root:not([data-theme="light"]) body::after{filter:invert(1) hue-rotate(180deg);opacity:.04}}
:root[data-theme="dark"] body::after{filter:invert(1) hue-rotate(180deg);opacity:.04}
header.top{position:sticky;top:env(safe-area-inset-top,0px);z-index:20;background:var(--surface);border-bottom:1px solid var(--line);display:flex;align-items:center;gap:12px;padding:8px max(16px,env(safe-area-inset-right,0px)) 8px max(16px,env(safe-area-inset-left,0px))}
body::before{content:"";position:fixed;inset:0 0 auto;height:env(safe-area-inset-top,0px);background:var(--surface);z-index:30}
header.top img{height:36px;border-radius:3px;background:#fff}header.top b{font-size:15px;color:var(--navy)}header.top .sp{flex:1}
header.top a.btn,header.top button{font:600 13px system-ui;color:var(--navy);border:1px solid var(--line);background:var(--surface);padding:6px 10px;border-radius:4px;text-decoration:none;cursor:pointer}
#menu{display:none}
.layout{position:relative;z-index:1;display:grid;grid-template-columns:290px minmax(0,1fr);max-width:1280px;margin:0 auto}
nav.side{position:sticky;top:calc(53px + env(safe-area-inset-top,0px));align-self:start;height:calc(100vh - 53px);overflow:auto;padding:16px 10px 40px max(16px,env(safe-area-inset-left,0px));border-right:1px solid var(--line);font-size:14px}
nav.side a{display:block;padding:6px 10px;color:var(--muted);text-decoration:none;border-radius:4px;line-height:1.35}
nav.side a:hover{background:var(--hl)}nav.side a.on{background:var(--hl);color:var(--navy);font-weight:600}
nav.side .prog{font-size:12px;color:var(--muted);padding:4px 10px 10px}
main{padding:24px max(16px,env(safe-area-inset-right,0px)) 60px 32px;min-width:0}
.chap{max-width:820px;display:none}.chap.on{display:block}
h1{font:700 clamp(24px,4vw,32px)/1.2 Georgia,'Times New Roman',serif;color:var(--navy);border-bottom:3px solid var(--accent);padding-bottom:10px;margin:0 0 18px;text-wrap:balance}
h2{font-size:21px;color:var(--navy);margin:34px 0 10px;padding-bottom:4px;border-bottom:1px solid var(--line)}
h3{font-size:17px;margin:22px 0 6px}
p,li{max-width:70ch}
.tw{overflow-x:auto;margin:12px 0;border:1px solid var(--line);border-radius:6px;background:var(--surface)}
table{border-collapse:collapse;width:100%;font-size:14px}th,td{padding:8px 10px;border-bottom:1px solid var(--line);text-align:left;vertical-align:top}
th{background:var(--hl);color:var(--navy);white-space:nowrap}tr:last-child td{border-bottom:0}
td{min-width:90px}
code{font:13px ui-monospace,Consolas,monospace;background:var(--code);padding:1px 5px;border-radius:3px}
pre{background:var(--code);border:1px solid var(--line);border-left:3px solid var(--navy);padding:12px;overflow-x:auto;border-radius:4px}pre code{background:none;padding:0}
li.chk{list-style:none;margin-left:-22px}li.chk label{cursor:pointer;display:flex;gap:8px;align-items:flex-start}
li.chk input{margin-top:6px;width:17px;height:17px;accent-color:var(--navy);flex:none}
li.chk input:checked+*,li.chk label:has(input:checked){color:var(--muted);text-decoration:line-through}
.pager{display:flex;justify-content:space-between;gap:12px;margin-top:40px;flex-wrap:wrap}
.pager a{border:1px solid var(--line);background:var(--surface);color:var(--navy);padding:10px 14px;border-radius:6px;text-decoration:none;font-weight:600;font-size:14px}
footer.credit{margin-top:48px;border-top:1px solid var(--line);padding-top:16px;display:flex;gap:12px;align-items:center;color:var(--muted);font-size:13px}
footer.credit img{height:44px;border-radius:3px;background:#fff}footer.credit b{color:var(--fg)}
:focus-visible{outline:2px solid var(--accent);outline-offset:2px}
@media (max-width:860px){header.top b{display:none}header.top{gap:8px}header.top img{height:32px}.layout{display:block}#menu{display:inline-block}
nav.side{display:none;position:fixed;inset:calc(53px + env(safe-area-inset-top,0px)) 0 0 0;height:auto;z-index:25;background:var(--surface);border:0}
nav.side.open{display:block}main{padding:20px max(16px,env(safe-area-inset-right,0px)) 50px max(16px,env(safe-area-inset-left,0px))}}

""".replace("@@BIG@@", big)

JS = """
const arts=[...document.querySelectorAll('.chap')],links=[...document.querySelectorAll('nav.side a')];
function show(id){const i=Math.max(0,arts.findIndex(a=>a.dataset.s===id));arts.forEach((a,j)=>a.classList.toggle('on',j===i));
links.forEach((l,j)=>l.classList.toggle('on',j===i));window.scrollTo(0,0);document.getElementById('side').classList.remove('open');
const p=document.getElementById('pager');p.innerHTML=(i>0?'<a href="#'+arts[i-1].dataset.s+'">← '+links[i-1].textContent+'</a>':'<span></span>')+(i<arts.length-1?'<a href="#'+arts[i+1].dataset.s+'">'+links[i+1].textContent+' →</a>':'');}
function route(){show(location.hash.slice(1)||'p0')}addEventListener('hashchange',route);
const boxes=[...document.querySelectorAll('input[data-k]')];
function load(){try{const s=JSON.parse(localStorage.getItem('vel-handbook-checks')||'{}');boxes.forEach(b=>b.checked=!!s[b.dataset.k])}catch(e){}prog()}
function prog(){const d=boxes.filter(b=>b.checked).length;document.getElementById('prog').textContent='Checklist progress on this device: '+d+' of '+boxes.length}
boxes.forEach(b=>b.addEventListener('change',()=>{try{const s={};boxes.forEach(x=>{if(x.checked)s[x.dataset.k]=1});localStorage.setItem('vel-handbook-checks',JSON.stringify(s))}catch(e){}prog()}));
document.getElementById('menu').onclick=()=>document.getElementById('side').classList.toggle('open');
load();route();
"""
html = f"""<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1,viewport-fit=cover">
<meta name="claude:viewport-fit-sides" content="cover"><title>VEL Delivery Handbook</title><style>{CSS}</style></head><body>
<header class="top"><button id="menu" aria-label="Contents">Contents</button><img src="data:image/webp;base64,{small}" alt="VEL Software Solutions"><b>VEL Delivery Handbook</b><span class="sp"></span>
<a class="btn" href="VEL-Delivery-Handbook.pdf" target="_blank" rel="noopener">PDF</a><a class="btn" href="index.html">Summary</a></header>
<div class="layout"><nav class="side" id="side"><div class="prog" id="prog"></div>{''.join(nav)}</nav>
<main>{''.join(secs)}<div class="pager" id="pager"></div>
<footer class="credit"><img src="data:image/webp;base64,{small}" alt="VEL Software Solutions"><span>Developed by <b>V. LOKESHWARAN</b><br>VEL Software Solutions · velsoftwaresolutions.in@gmail.com</span></footer></main></div>
<script>{JS}</script></body></html>"""
(ROOT / "dist" / "handbook.html").write_text(html)
print("wrote dist/handbook.html", len(html) // 1024, "KB")
