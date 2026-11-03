"""HTML helpers, stylesheet and small script for the document."""
from html import escape as esc

def table(headers, rows, cls="", num_cols=(), total_last=False):
    h = "".join(f"<th{' class=n' if i in num_cols else ''}>{x}</th>" for i, x in enumerate(headers))
    out = []
    for ri, r in enumerate(rows):
        tr = " class=total" if total_last and ri == len(rows) - 1 else ""
        out.append(f"<tr{tr}>" + "".join(f"<td{' class=n' if i in num_cols else ''}>{c}</td>" for i, c in enumerate(r)) + "</tr>")
    return f'<div class="tw"><table class="{cls}"><thead><tr>{h}</tr></thead><tbody>{"".join(out)}</tbody></table></div>'

def ul(items, cls=""):
    return f'<ul class="{cls}">' + "".join(f"<li>{i}</li>" for i in items) + "</ul>"

def ol(items):
    return "<ol>" + "".join(f"<li>{i}</li>" for i in items) + "</ol>"

def checklist(items):
    return '<ul class="check">' + "".join(f"<li>{i}</li>" for i in items) + "</ul>"

def callout(kind, title, body):
    return f'<div class="callout {kind}"><b>{title}</b><div>{body}</div></div>'

def formula(text):
    return f'<pre class="formula">{text}</pre>'

def code(text):
    return f"<pre class=code>{esc(text)}</pre>"

def section(sid, num, title, body, gate=None):
    g = f'<span class="gatebadge">{gate}</span>' if gate else ""
    return f'<section id="{sid}"><h2><span class="sn">{num}</span>{title}{g}</h2>{body}</section>'

def sop(owner, inputs, steps, outputs, gate):
    """Standard SOP card used by every phase."""
    return (f'<div class="sop"><div class="sopmeta"><div><b>Owner</b>{owner}</div>'
            f'<div><b>Inputs</b>{inputs}</div><div><b>Output</b>{outputs}</div><div><b>Exit gate</b>{gate}</div></div>'
            f'<h4>Procedure</h4>{ol(steps)}</div>')

CSS = r"""
:root{--bg:#fff;--fg:#17202a;--mut:#5b6775;--line:#dde3ea;--acc:#0b5fff;--acc2:#0a2e6e;--soft:#f3f6fb;--ok:#0f7b45;--warn:#a85d00;--bad:#b3261e;--code:#f6f8fa}
@media (prefers-color-scheme:dark){:root:not(.print){--bg:#0f141a;--fg:#e6ebf1;--mut:#9aa7b5;--line:#2a3441;--acc:#6aa2ff;--acc2:#9cc0ff;--soft:#161d26;--code:#151c25}}
*{box-sizing:border-box}html{scroll-behavior:smooth}
body{margin:0;background:var(--bg);color:var(--fg);font:15px/1.55 -apple-system,"Segoe UI",Roboto,Helvetica,Arial,sans-serif}
nav#toc{position:fixed;top:0;left:0;bottom:0;width:272px;overflow:auto;padding:18px 14px;border-right:1px solid var(--line);background:var(--soft);font-size:13px}
nav#toc .brand{font-weight:700;font-size:15px;color:var(--acc2);margin-bottom:4px}
nav#toc .sub{color:var(--mut);font-size:12px;margin-bottom:12px}
nav#toc a{display:block;color:var(--fg);text-decoration:none;padding:4px 8px;border-radius:6px}
nav#toc a:hover,nav#toc a.on{background:var(--acc);color:#fff}
main{margin-left:272px;max-width:1040px;padding:36px 44px 80px}
#menu{display:none}
@media(max-width:900px){nav#toc{display:none;position:fixed;z-index:9;width:80%}nav#toc.open{display:block}main{margin:0;padding:20px 16px 60px}#menu{display:block;position:fixed;right:12px;top:10px;z-index:10;background:var(--acc);color:#fff;border:0;border-radius:8px;padding:8px 12px}}
h1{font-size:34px;line-height:1.15;margin:0 0 8px;color:var(--acc2)}
h2{font-size:25px;margin:54px 0 14px;padding-top:10px;border-top:3px solid var(--acc);color:var(--acc2);display:flex;align-items:center;gap:10px;flex-wrap:wrap}
h2 .sn{background:var(--acc);color:#fff;border-radius:8px;padding:2px 10px;font-size:17px}
h3{font-size:18px;margin:28px 0 8px;color:var(--fg)}h4{font-size:14px;margin:14px 0 4px;text-transform:uppercase;letter-spacing:.04em;color:var(--mut)}
p{margin:8px 0}.mut{color:var(--mut)}.small{font-size:12.5px}
.gatebadge{margin-left:auto;font-size:12px;background:#fff3d6;color:#7a4b00;border:1px solid #f0cf85;border-radius:20px;padding:2px 10px;font-weight:600}
.tw{overflow-x:auto;margin:10px 0 16px;border:1px solid var(--line);border-radius:8px}
table{border-collapse:collapse;width:100%;font-size:13px}
th{background:var(--acc2);color:#fff;text-align:left;padding:7px 9px;font-weight:600;white-space:nowrap}
td{padding:6px 9px;border-top:1px solid var(--line);vertical-align:top}
tbody tr:nth-child(even){background:var(--soft)}
.n{text-align:right;font-variant-numeric:tabular-nums;white-space:nowrap}
tr.total td{font-weight:700;background:#e6eefc;color:#0a2e6e;border-top:2px solid var(--acc2)}
@media (prefers-color-scheme:dark){:root:not(.print) tr.total td{background:#1d2a40;color:#cfe0ff}}
.callout{border-left:5px solid var(--acc);background:var(--soft);padding:10px 14px;margin:14px 0;border-radius:0 8px 8px 0}
.callout>b:first-child{display:block;margin-bottom:2px}
.callout.warn{border-color:var(--warn)}.callout.bad{border-color:var(--bad)}.callout.ok{border-color:var(--ok)}
pre{white-space:pre-wrap;background:var(--code);border:1px solid var(--line);border-radius:8px;padding:10px 12px;overflow:auto;font:12.5px/1.5 ui-monospace,Menlo,Consolas,monospace;margin:10px 0}
pre.formula{border-left:4px solid var(--acc);font-weight:600}
ul.check{list-style:none;padding-left:4px}ul.check li:before{content:"\2610  ";color:var(--acc)}
ul,ol{padding-left:22px;margin:6px 0}li{margin:2px 0}
.sop{border:1px solid var(--line);border-radius:10px;padding:12px 16px;margin:12px 0;background:var(--bg)}
.sopmeta{display:grid;grid-template-columns:repeat(4,1fr);gap:10px;font-size:12.5px;border-bottom:1px dashed var(--line);padding-bottom:8px}
.sopmeta b{display:block;color:var(--mut);font-size:11px;text-transform:uppercase;letter-spacing:.05em}
@media(max-width:700px){.sopmeta{grid-template-columns:1fr 1fr}}
.cover{min-height:92vh;display:flex;flex-direction:column;justify-content:center;border-bottom:3px solid var(--acc);margin-bottom:20px}
.cover .tag{color:var(--acc);font-weight:700;letter-spacing:.12em;font-size:12px;text-transform:uppercase}
.cover p.lead{font-size:18px;color:var(--mut);max-width:720px}
.kpis{display:grid;grid-template-columns:repeat(auto-fit,minmax(170px,1fr));gap:10px;margin:14px 0}
.kpi{border:1px solid var(--line);border-radius:10px;padding:10px 12px;background:var(--soft)}
.kpi b{display:block;font-size:22px;color:var(--acc2)}.kpi span{font-size:12px;color:var(--mut)}
.flow{display:flex;flex-wrap:wrap;gap:6px;margin:12px 0}
.flow div{background:var(--soft);border:1px solid var(--line);border-radius:8px;padding:6px 10px;font-size:12.5px}
.flow div.g{background:#fff3d6;border-color:#f0cf85;color:#7a4b00;font-weight:600}
.calc{border:2px solid var(--acc);border-radius:12px;padding:14px;margin:14px 0;background:var(--soft)}
.calc .grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(150px,1fr));gap:8px}
.calc label{font-size:12px;color:var(--mut);display:block}
.calc input{width:100%;padding:6px;border:1px solid var(--line);border-radius:6px;background:var(--bg);color:var(--fg)}
.bar{height:10px;background:var(--acc);border-radius:5px}
.wm{position:fixed;inset:0;z-index:50;pointer-events:none;display:flex;align-items:center;justify-content:center}
.wm img{width:min(55vmin,520px);opacity:.07}
.wm .t{position:absolute;bottom:14px;right:18px;font-size:11px;color:var(--mut);opacity:.75;text-align:right}
.wm .diag{position:absolute;inset:0;display:flex;align-items:center;justify-content:center}
.wm .diag span{transform:rotate(-30deg);font-size:46px;font-weight:800;letter-spacing:.08em;color:var(--acc2);opacity:.045;white-space:nowrap;text-align:center;line-height:1.3}
.cover .logo{width:230px;margin:0 0 14px 0;background:#fff;border-radius:12px;padding:6px}
nav#toc .navlogo{width:92px;display:block;margin:0 0 8px}
nav#toc .dev{margin-top:16px;padding-top:10px;border-top:1px solid var(--line);font-size:11px;color:var(--mut)}
footer{margin-top:60px;border-top:1px solid var(--line);padding-top:12px;color:var(--mut);font-size:12px}
@media print{
 .wm .t{display:none}.wm img{width:150mm;opacity:.08}.wm .diag span{font-size:34pt}
 nav#toc,#menu,.calc .grid{display:none}main{margin:0;max-width:none;padding:0}
 h2{break-before:page;margin-top:0}h2:first-of-type{break-before:auto}
 .cover{min-height:auto;height:230mm;break-after:page}
 .sop,.callout,pre{break-inside:avoid}tr{break-inside:avoid}.tw{overflow:visible}h3,h4{break-after:avoid}pre{white-space:pre-wrap;word-break:break-word}
 body{font-size:10.5pt}table{font-size:8.6pt}
}
"""

JS = r"""
const toc=document.getElementById('toc');
document.getElementById('menu').onclick=()=>toc.classList.toggle('open');
toc.querySelectorAll('a').forEach(a=>a.onclick=()=>toc.classList.remove('open'));
const links=[...toc.querySelectorAll('a')];
const io=new IntersectionObserver(es=>{es.forEach(e=>{if(e.isIntersecting){links.forEach(l=>l.classList.toggle('on',l.getAttribute('href')==='#'+e.target.id))}})},{rootMargin:'-20% 0px -70% 0px'});
document.querySelectorAll('main section').forEach(s=>io.observe(s));
// ---- live labour/price calculator (same formulas as model.py)
(function(){
 const D=JSON.parse(document.getElementById('calcdata').textContent);
 const $=id=>document.getElementById(id);
 const fmt=x=>'₹'+Math.round(x).toString().replace(/(\d+?)(?=(\d\d)+(\d)(?!\d))(\.\d+)?/g,'$1,');
 function indian(n){n=Math.round(n);let s=Math.abs(n).toString(),l=s.slice(-3),r=s.slice(0,-3);if(r)l=r.replace(/\B(?=(\d{2})+(?!\d))/g,',')+','+l;return (n<0?'-':'')+'₹'+l}
 function run(){
  const oh=+$('c_oh').value/100,av=+$('c_av').value,ut=+$('c_ut').value/100,ga=+$('c_ga').value/100,mg=+$('c_mg').value/100,rs=+$('c_rs').value/100;
  const prod=av*ut,rate={};
  for(const r in D.ctc){rate[r]=D.ext[r]?D.ext[r]:Math.round(+$('c_'+r).value*(1+oh)/prod)}
  let le=0,lp=0;
  D.tasks.forEach(t=>{const m=t.h.reduce((a,x)=>a+x[1],0),c=t.h.reduce((a,x)=>a+x[1]*rate[x[0]],0);
   const o=m*t.of,p=m*t.pf,e=(o+4*m+p)/6;le+=c*e/m;lp+=c*t.pf});
  const ov=le*ga,risk=rs*(lp-le),cost=le+D.setup+ov+risk,price=cost/(1-mg);
  $('o_rates').textContent=Object.keys(rate).map(r=>r+' '+indian(rate[r])+'/h').join(' · ');
  $('o_le').textContent=indian(le);$('o_lp').textContent=indian(lp);$('o_cost').textContent=indian(cost);$('o_price').textContent=indian(price);
 }
 document.querySelectorAll('.calc input').forEach(i=>i.oninput=run);run();
})();
"""
