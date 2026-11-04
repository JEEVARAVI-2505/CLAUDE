BASE_CSS = r"""
:root{--navy:#13306b;--navy2:#1b3f86;--crimson:#b5174f;--ink:#1c2230;--muted:#5b6578;--line:#d9e0ee;--stripe:#edf2fb;
--peach:#fff3e8;--rose:#fde8e8;--mint:#e9f6ef;--sky:#e8f1fd;--code:#101a2e;--bg:#ffffff}
*{box-sizing:border-box}
html{scroll-behavior:smooth}
body{margin:0;font-family:"Inter","Segoe UI",system-ui,-apple-system,Roboto,Arial,sans-serif;color:var(--ink);background:var(--bg);line-height:1.6;font-size:15px}
h1,h2,h3{color:var(--navy);line-height:1.25}
h1{font-family:"Liberation Serif","Times New Roman",Georgia,serif;font-size:2rem;margin:0 0 .6rem;padding-bottom:.35rem;border-bottom:3px solid var(--crimson)}
h1 .num{color:var(--crimson)}
h2{font-size:1.25rem;margin:1.8rem 0 .6rem}
h3{font-size:1.05rem;margin:1.3rem 0 .5rem}
p{margin:.55rem 0}
.lead{font-size:1.04rem}
a{color:var(--navy2);text-decoration:underline;text-decoration-color:#9db4e3;text-underline-offset:2px;word-break:break-word}
code{font-family:"DejaVu Sans Mono","Liberation Mono",Consolas,monospace;font-size:.86em;background:#eef1f8;border-radius:4px;padding:.05em .35em;color:#1f2a52}
code{overflow-wrap:anywhere}pre code{overflow-wrap:normal}
ul,ol{margin:.5rem 0 .8rem 1.25rem;padding:0}li{margin:.2rem 0}
table{border-collapse:collapse;width:100%;margin:.8rem 0;font-size:.86rem}
thead th{background:var(--navy);color:#fff;text-align:left;padding:.5rem .6rem;font-weight:600}
td,th{padding:.45rem .6rem;vertical-align:top;border:1px solid var(--line)}
tbody tr:nth-child(odd) td{background:var(--stripe)}
td:first-child{font-weight:500}
table.raci td,table.raci th{text-align:center;padding:.3rem .2rem}table.raci td:first-child,table.raci th:first-child{text-align:left;padding-left:.5rem}
table.kv{font-size:.84rem;margin:.4rem 0 1.1rem;border:1px solid var(--line)}
table.kv th{width:17%;background:var(--navy);color:#fff;font-weight:600;border-color:#2a4a8a}
table.kv td{background:#fff!important;font-weight:400}
table.kv tr:last-child th{background:var(--crimson);border-color:#c8426f}
.callout{border-left:5px solid var(--crimson);background:var(--peach);border-radius:6px;padding:.7rem 1rem;margin:1rem 0;break-inside:avoid}
.callout .ct{font-size:.72rem;letter-spacing:.12em;text-transform:uppercase;font-weight:700;color:#3a2a2a;margin-bottom:.3rem}
.callout p{margin:.2rem 0}.callout ol{margin:.3rem 0 0 1.2rem}
.callout.safety{background:var(--rose);border-color:#b32020}
.callout.tip{background:var(--sky);border-color:var(--navy2)}
.callout.warn{background:#fff8dc;border-color:#c78a00}
.codewrap{position:relative;margin:.8rem 0;break-inside:avoid}
.codewrap pre{margin:0;background:var(--code);color:#e8edf8;border-radius:8px;padding:.85rem 1rem;overflow:auto;font-size:.8rem;line-height:1.5}
.codewrap pre code{background:none;color:inherit;padding:0;font-size:inherit}
.codewrap .lang{position:absolute;right:.6rem;top:.3rem;font-size:.65rem;letter-spacing:.1em;text-transform:uppercase;color:#8fa3cf}
ul.check{list-style:none;margin-left:0}ul.check li{display:flex;gap:.6rem;align-items:flex-start}
.box{flex:0 0 14px;width:14px;height:14px;border:1.6px solid var(--navy);border-radius:3px;margin-top:.28rem;display:inline-block}
.pills{display:flex;flex-wrap:wrap;gap:.4rem;align-items:center;margin:.6rem 0}
.pill{background:var(--navy);color:#fff;border-radius:999px;padding:.25rem .8rem;font-size:.78rem}
.arrow{color:var(--crimson);font-weight:700}
.chapter{margin-bottom:2.5rem}
"""

SCREEN_CSS = r"""
.layout{display:flex;min-height:100vh}
#side{position:fixed;inset:0 auto 0 0;width:310px;background:#0f2250;color:#e6ecfa;display:flex;flex-direction:column;z-index:30;transition:transform .2s}
#side .brand{padding:14px 16px 10px;border-bottom:1px solid #263c73;text-align:center;background:#fff}
#side .brand img{width:96px;height:auto;display:block;margin:0 auto}
#side .brand small{display:block;color:var(--navy);font-size:.68rem;letter-spacing:.14em;font-weight:700;margin-top:2px}
#q{margin:10px 12px 6px;padding:.55rem .7rem;border-radius:8px;border:1px solid #31488a;background:#162b63;color:#fff;font:inherit;font-size:.85rem}
#q::placeholder{color:#9fb2e0}
#count{padding:0 14px 6px;font-size:.72rem;color:#a9bbe6;min-height:1.1rem}
#nav{overflow:auto;flex:1;padding:0 6px 20px}
#nav a{display:block;color:#dbe5fb;text-decoration:none;padding:.38rem .6rem;border-radius:6px;font-size:.82rem}
#nav a:hover{background:#1b3478}
#nav a.active{background:var(--crimson);color:#fff}
#nav .grp{font-size:.66rem;letter-spacing:.14em;text-transform:uppercase;color:#8ea4dc;padding:.8rem .6rem .2rem}
#nav .sub{margin-left:.7rem;border-left:1px solid #2c4585;display:none}
#nav .sub a{font-size:.76rem;color:#b8c7ee;padding:.22rem .6rem}
#nav li.open>.sub{display:block}
#nav ul{list-style:none;margin:0;padding:0}
main{margin-left:310px;padding:2rem min(5vw,56px) 4rem;max-width:1100px;width:100%}
.hero{display:flex;gap:28px;align-items:center;background:linear-gradient(135deg,#f7f9ff,#eef3fd);border:1px solid var(--line);border-radius:14px;padding:26px;margin-bottom:2rem}
.hero img{width:170px;height:auto}
.hero .kicker{letter-spacing:.3em;font-size:.72rem;font-weight:700;color:var(--crimson)}
.hero h1{border:0;font-size:2.1rem;margin:.3rem 0}
.hero p{color:var(--muted);margin:.2rem 0}
.meta{font-size:.78rem;color:var(--muted)}
#menu{display:none;position:fixed;top:10px;left:10px;z-index:40;background:var(--navy);color:#fff;border:0;border-radius:8px;padding:.5rem .7rem;font-size:1rem}
#top{position:fixed;right:18px;bottom:18px;background:var(--crimson);color:#fff;border:0;border-radius:999px;width:42px;height:42px;font-size:1.1rem;cursor:pointer;display:none;z-index:20}
mark{background:#ffe27a;color:inherit;border-radius:2px}
.chapter.hidden{display:none}
.tablewrap{overflow-x:auto}
.printbtn{margin-left:auto;background:var(--navy);color:#fff;border:0;border-radius:8px;padding:.5rem .9rem;font:inherit;font-size:.8rem;cursor:pointer}
footer.site{border-top:1px solid var(--line);margin-top:3rem;padding-top:1rem;color:var(--muted);font-size:.78rem}
@media (max-width:900px){#side{transform:translateX(-100%)}#side.show{transform:none}main{margin-left:0;padding-top:3.4rem}#menu{display:block}.hero{flex-direction:column;text-align:center}}
@media (max-width:640px){main{overflow-x:hidden}.pills{gap:.3rem}table{font-size:.78rem}td,th{padding:.35rem .4rem}h1{font-size:1.55rem}}
.watermark-screen{position:fixed;left:50%;top:50%;width:min(60vw,560px);transform:translate(-50%,-50%);opacity:.05;pointer-events:none;z-index:1}
@media print{
  #side,#menu,#top,.printbtn,.watermark-screen{display:none!important}
  main{margin:0;padding:0;max-width:none}
  .hero{break-after:page}
  .chapter{break-before:page}
}
"""

PRINT_CSS = r"""
@page{size:A4;margin:22mm 16mm 20mm 16mm}
body{font-size:10.4pt;line-height:1.55;background:#fff;-webkit-print-color-adjust:exact;print-color-adjust:exact}
.wm{position:fixed;left:50%;top:50%;width:122mm;transform:translate(-50%,-50%);opacity:.075;z-index:9999;pointer-events:none}
.wm img{width:100%;height:auto;display:block}
h1{font-size:23pt}
h2{font-size:14.5pt;break-after:avoid}h3{font-size:11.5pt;break-after:avoid}
.chapter{break-before:page;margin:0}
.chapter.appendix.cont{break-before:page}
table{font-size:8.6pt}
tr{break-inside:avoid}thead{display:table-header-group}
table.kv{break-inside:avoid}table.kv.brk{break-inside:auto}
.callout,.codewrap{break-inside:avoid}
.codewrap pre{font-size:7.9pt;white-space:pre-wrap;word-break:break-word}
.cover{height:240mm;display:flex;flex-direction:column;align-items:center;justify-content:center;text-align:center;break-after:page}
.cover img.logo{width:80mm;height:auto}
.cover .kicker{letter-spacing:.32em;color:var(--crimson);font-weight:700;font-size:10pt;margin:8mm 0 12mm}
.cover h1.title{font-size:34pt;border:0;margin:0;padding:0;line-height:1.15}
.cover .sub{color:var(--muted);font-size:13pt;margin:6mm 10mm 12mm}
.cover .by{font-size:11pt;margin:2mm 0}
.cover .fine{color:var(--muted);font-size:8.6pt;margin-top:12mm;max-width:130mm}
.front{break-after:page}
.toc{list-style:none;margin:0;padding:0}
.toc li{display:flex;gap:.5rem;align-items:baseline;padding:2.1pt 0}
.toc li .t{flex:0 1 auto}
.toc li .d{flex:1;border-bottom:1.2px dotted #9aa6c4;transform:translateY(-3px);min-width:12mm}
.toc li .n{flex:0 0 auto;color:var(--navy);font-weight:600}
.toc li.app .t{color:var(--ink)}
.toc.stages{columns:2;column-gap:8mm}.toc.stages li{break-inside:avoid;font-size:9pt}
.toc a{color:inherit;text-decoration:none}
a{text-decoration:none;color:var(--navy2)}
"""
