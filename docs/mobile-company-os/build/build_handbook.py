"""Build the beginner Delivery Handbook (HTML + PDF).  Run: python3 build_handbook.py"""
import os, subprocess
from hb_lib import *
from lib import section
import hb_part1 as P1, hb_part2 as P2, hb_part3 as P3
from build import LOGO, WM, DEV, OUT

TITLE = "Software Delivery Handbook"

def overview():
    rows = [["0", "Get ready", "Accounts, laptop, folders, rules", "Working setup + first pull request", "-"], ["1", "Business check", "Is it worth building?", "Market report", "Gate 1"], ["2", "Requirements", "What exactly will we build?", "Frozen PRD + screen sheets", "Gate 2"],
            ["3", "Technology", "Tools and blueprint", "Decision record + diagrams", "Gate 3"], ["4", "UI / UX design", "How it looks and works", "Prototype + test results", "Gate 4"], ["5", "Workspace setup", "Repo, folders, checks", "Ready repository", "Gate 5"],
            ["6", "Backend + database", "The server and its data", "Tested API", "-"], ["7", "Mobile app", "The screens", "Working app", "Gate 6"], ["8", "Test + security", "Find problems early", "QA report", "Gate 7"],
            ["9", "Cost + price", "What it costs, what to charge", "Price sheet", "Gate 8"], ["10", "Go live", "Server, domain, backups", "Live system", "Gate 9"], ["11", "Publish", "Play Store + App Store", "Released app", "Gate 10"],
            ["12", "Demo + handover", "Deliver to customer", "Signed handover", "Gate 11"], ["13", "Support", "Keep it running", "Monthly reports", "-"]]
    return (flow_map() + table(["#", "Stage", "Question it answers", "You deliver", "Approval"], rows) +
            callout("warn", "Golden rule", "Do not start a stage until the previous gate is signed. If a stage looks too big, break it into tickets of at most one day each."))

COVER = f"""<div class="cover"><img class="logo" src="{LOGO}" alt="VEL Software Solutions logo">
<div class="tag">VEL Software Solutions · Employee Training Guide · v1.0 · 3 Nov 2026</div>
<h1>Software Delivery Handbook</h1><p class="lead" style="font-size:20px"><b>From zero to 100% delivery</b> - the stage-by-stage process of a professional software company, explained for beginners.</p>
{table(["For", "Details"], [["Reader", "New developers, testers, designers and project coordinators (basic knowledge is enough)"], ["Covers", "How a software company works · folder system · 14 stages with exact steps · templates · glossary"],
 ["Example project", "SiteDiary (sample offline site-report app: Flutter + NestJS + PostgreSQL)"], ["Prepared by", "<b>V.LOKESHWARAN</b> · VEL Software Solutions · velsoftwaresolutions.in@gmail.com"],
 ["Companion document", "Mobile App Company Operating System (management: costing, pricing, gates)"]])}
{callout("ok", "How to read", "Start with Part A and B. Then follow Stage 0, 1, 2 … in order. Each stage ends with a hand-in list and a manager sign-off.")}</div>"""

SECS = [
 ("start", "★", "Start here", P1.welcome() + overview(), None),
 ("partA", "A", "How a software company works", P1.part_a(), None),
 ("partB", "B", "Folder system and naming", P1.folders(), None),
]
STAGES = [("s0", "0. Get ready", P1.stage0()), ("s1", "1. Business check", P1.stage1()), ("s2", "2. Requirements", P1.stage2()), ("s3", "3. Technology & architecture", P2.stage3()),
          ("s4", "4. UI / UX design", P2.stage4()), ("s5", "5. Workspace setup", P2.stage5()), ("s6", "6. Backend & database", P2.stage6()), ("s7", "7. Mobile app", P2.stage7()),
          ("s8", "8. Testing & security", P3.stage8()), ("s9", "9. Cost & price", P3.stage9()), ("s10", "10. Go live (server)", P3.stage10()), ("s11", "11. Publish to stores", P3.stage11()),
          ("s12", "12. Demo & handover", P3.stage12()), ("s13", "13. Support", P3.stage13())]

def page(print_mode=False):
    toc = "".join(f'<a href="#{i}">{("Part " + n + ". ") if n.isalpha() else ""}{t}</a>' for i, n, t, _, _ in SECS)
    toc += '<div class="sub" style="margin:10px 8px 2px">STAGES</div>' + "".join(f'<a href="#{i}">{t}</a>' for i, t, _ in STAGES) + '<a href="#appendix">Appendix: templates & glossary</a>'
    body = COVER + "".join(section(i, n, t, b, g) for i, n, t, b, g in SECS) + "".join(h for _, _, h in STAGES) + section("appendix", "C", "Appendix: templates, glossary, help", P3.appendix(), None)
    return f"""<!doctype html><html lang="en"{' class="print"' if print_mode else ''}><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>{TITLE}</title><style>{CSS}</style></head><body>
<button id="menu">Contents</button><nav id="toc"><img class="navlogo" src="{LOGO}" alt="VEL"><div class="brand">{TITLE}</div><div class="sub">VEL Software Solutions · v1.0</div>{toc}<div class="dev">{DEV}</div></nav>
<div class="wm"><img src="{WM}" alt=""><div class="diag"><span>VEL SOFTWARE SOLUTIONS<br>V.LOKESHWARAN</span></div><div class="t">VEL Software Solutions · {DEV}</div></div>
<main>{body}<footer><b>VEL Software Solutions</b> · {DEV} · © 2026 · Internal training document. Costs shown are examples; use the current rate card and verified prices.</footer></main><script>{JS}</script></body></html>"""

if __name__ == "__main__":
    name = "Software-Delivery-Handbook"
    open(os.path.join(OUT, name + ".html"), "w", encoding="utf-8").write(page())
    pr = os.path.join(OUT, "build", "_hb_print.html"); open(pr, "w", encoding="utf-8").write(page(True))
    js = """const {chromium}=require('playwright');(async()=>{const b=await chromium.launch();const p=await b.newPage();
await p.goto('file://'+process.argv[2]);await p.emulateMedia({media:'print'});
await p.pdf({path:process.argv[3],format:'A4',printBackground:true,margin:{top:'18mm',bottom:'18mm',left:'14mm',right:'14mm'},displayHeaderFooter:true,
headerTemplate:'<div style="font-size:8px;width:100%;padding:0 14mm;color:#0a2e6e;display:flex;justify-content:space-between"><b>VEL Software Solutions</b><span>V.LOKESHWARAN · velsoftwaresolutions.in@gmail.com</span></div>',
footerTemplate:'<div style="font-size:8px;width:100%;text-align:center;color:#666">Software Delivery Handbook v1.0 &nbsp;·&nbsp; Developed by V.LOKESHWARAN · velsoftwaresolutions.in@gmail.com &nbsp;·&nbsp; <span class=pageNumber></span>/<span class=totalPages></span></div>'});await b.close()})()"""
    jp = os.path.join(OUT, "build", "_hbpdf.js"); open(jp, "w").write(js)
    subprocess.check_call(["node", jp, pr, os.path.join(OUT, name + ".pdf")]); os.remove(pr); os.remove(jp); print("built handbook")
