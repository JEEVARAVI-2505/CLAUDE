"""Helpers + CSS for the beginner-friendly Delivery Handbook."""
from html import escape as esc
from lib import table, ul, ol, checklist, callout, code, formula, CSS as BASE_CSS, JS as BASE_JS

GROUPS = {"plan": ("#0b5fff", "PLAN"), "design": ("#0a8f8f", "DESIGN & SETUP"), "build": ("#5b3fd1", "BUILD"),
          "quality": ("#d97706", "QUALITY"), "launch": ("#0f7b45", "LAUNCH"), "run": ("#5b6775", "RUN")}

EXTRA_CSS = r"""
.stagehead{display:flex;gap:16px;align-items:center;border-radius:14px;padding:16px 18px;color:#fff;margin:6px 0 14px}
.stagehead .big{font-size:44px;font-weight:800;line-height:1;opacity:.95;min-width:64px}
.stagehead .t{font-size:22px;font-weight:700;line-height:1.2}.stagehead .g{font-size:11px;letter-spacing:.14em;opacity:.85;font-weight:700}
.glance{display:grid;grid-template-columns:repeat(4,1fr);gap:8px;margin:8px 0 14px}
.glance div{border:1px solid var(--line);border-radius:10px;padding:8px 10px;background:var(--soft);font-size:12.5px}
.glance b{display:block;font-size:10.5px;letter-spacing:.06em;text-transform:uppercase;color:var(--mut)}
@media(max-width:760px){.glance{grid-template-columns:1fr 1fr}}
.progress{display:flex;gap:3px;margin:4px 0 10px}.progress i{flex:1;height:6px;border-radius:3px;background:var(--line)}
.step{display:flex;gap:12px;margin:10px 0;border:1px solid var(--line);border-radius:12px;padding:10px 14px;background:var(--bg);break-inside:avoid}
.step .no{flex:0 0 34px;height:34px;border-radius:50%;color:#fff;display:flex;align-items:center;justify-content:center;font-weight:700;font-size:15px;margin-top:2px}
.step .bd{flex:1;min-width:0}.step h4{margin:0 0 3px;text-transform:none;letter-spacing:0;font-size:15px;color:var(--fg)}
.step p{margin:3px 0;font-size:14px}
.box{border-radius:10px;padding:9px 13px;margin:10px 0;font-size:14px;break-inside:avoid}
.box b.l{display:block;font-size:11px;letter-spacing:.08em;text-transform:uppercase;margin-bottom:2px}
.box.plain{background:#eef5ff;border:1px solid #c5dafc;color:#10315f}.box.plain b.l{color:#0b5fff}
.box.do{background:#eaf7ef;border:1px solid #b9e2c9;color:#0d4a2a}.box.do b.l{color:#0f7b45}
.box.dont{background:#fdecea;border:1px solid #f3bcb7;color:#6a1812}.box.dont b.l{color:#b3261e}
.box.tip{background:#fff7e0;border:1px solid #f0d78a;color:#5c4200}.box.tip b.l{color:#a85d00}
.box.out{background:#f1ecfb;border:1px solid #d3c6f0;color:#2f1a6b}.box.out b.l{color:#5b3fd1}
@media (prefers-color-scheme:dark){:root:not(.print) .box.plain{background:#13233d;color:#cfe0ff;border-color:#27406b}:root:not(.print) .box.do{background:#10291c;color:#c9efd9;border-color:#245a3b}
:root:not(.print) .box.dont{background:#33151a;color:#f6cfcb;border-color:#6a2b2b}:root:not(.print) .box.tip{background:#2f2810;color:#f4e2a8;border-color:#5f5018}:root:not(.print) .box.out{background:#1f1836;color:#dcd2f7;border-color:#3e3170}}
.form{border:2px dashed var(--line);border-radius:10px;padding:8px 12px;margin:10px 0;background:var(--soft)}
.form h5{margin:0 0 6px;font-size:13px;text-transform:uppercase;letter-spacing:.06em;color:var(--acc2)}
.cmd{background:#0f1720;color:#d7e6f7;border-radius:8px;padding:9px 12px;font:12.5px/1.5 ui-monospace,Menlo,Consolas,monospace;margin:8px 0;white-space:pre-wrap;word-break:break-word;border:0}
.cmd:before{content:"$ ";color:#6fd08c}
.cmd.nopfx:before{content:""}
.map{width:100%;height:auto;margin:10px 0}
.map text{font-family:inherit}
h2.stage{border-top:none;padding-top:0;margin-top:46px}
.rule{border-left:5px solid var(--bad);background:var(--soft);padding:8px 12px;margin:10px 0;border-radius:0 8px 8px 0;font-size:14px}
@media print{section>h2{break-before:page !important}.box,.form{break-inside:avoid}.step{break-inside:auto}.step h4{break-after:avoid}.stagehead{break-before:page}.stagehead,.step .no{-webkit-print-color-adjust:exact;print-color-adjust:exact}h2.stage{break-before:avoid}}
"""

def stage_head(n, title, group, subtitle=""):
    col, gname = GROUPS[group]
    prog = "".join(f'<i style="background:{col if i <= n else ""}"></i>' for i in range(14))
    return (f'<div class="stagehead" style="background:{col}"><div class="big">{n:02d}</div><div><div class="g">{gname} · STAGE {n} OF 13</div>'
            f'<div class="t">{title}</div><div style="font-size:13px;opacity:.92">{subtitle}</div></div></div><div class="progress">{prog}</div>')

def glance(time, who, tools, approver):
    items = [("Time guide", time), ("You work with", who), ("Tools", tools), ("Approved by", approver)]
    return '<div class="glance">' + "".join(f"<div><b>{a}</b>{b}</div>" for a, b in items) + "</div>"

def box(kind, label, body):
    return f'<div class="box {kind}"><b class="l">{label}</b>{body}</div>'
def plain(term, text): return box("plain", f"In plain words: {term}", text)
def do(text): return box("do", "Do this", text)
def dont(text): return box("dont", "Never do this", text)
def tip(text): return box("tip", "Tip", text)
def output(text): return box("out", "What you must hand in (output)", text)

def step(n, title, body, col="#0b5fff"):
    return f'<div class="step"><div class="no" style="background:{col}">{n}</div><div class="bd"><h4>{title}</h4>{body}</div></div>'

def steps(items, group):
    col = GROUPS[group][0]
    return "".join(step(i + 1, t, b, col) for i, (t, b) in enumerate(items))

def cmd(*lines):
    return "".join(f'<pre class="cmd">{esc(l)}</pre>' for l in lines)

def snippet(text):
    return f'<pre class="cmd nopfx">{esc(text)}</pre>'

def form(title, rows):
    body = "".join(f"<tr><td style='width:34%'><b>{a}</b></td><td>{b}</td></tr>" for a, b in rows)
    return f'<div class="form"><h5>{title}</h5><table><tbody>{body}</tbody></table></div>'

def rule(text): return f'<div class="rule"><b>Company rule:</b> {text}</div>'

def stage(sid, n, title, group, subtitle, g, body):
    return (f'<section id="{sid}"><h2 class="stage" style="display:none">{n}. {title}</h2>' + stage_head(n, title, group, subtitle) + g + body + "</section>")

def flow_map():
    """SVG process map of all 14 stages."""
    names = [("0", "Get ready", "plan", ""), ("1", "Business check", "plan", "Gate 1"), ("2", "Requirements", "plan", "Gate 2"), ("3", "Technology", "design", "Gate 3"),
             ("4", "UI / UX design", "design", "Gate 4"), ("5", "Workspace setup", "design", "Gate 5"), ("6", "Backend + DB", "build", ""), ("7", "Mobile app", "build", "Gate 6"),
             ("8", "Test + security", "quality", "Gate 7"), ("9", "Cost + price", "quality", "Gate 8"), ("10", "Go live (server)", "launch", "Gate 9"),
             ("11", "Publish to stores", "launch", "Gate 10"), ("12", "Demo + handover", "launch", "Gate 11"), ("13", "Support", "run", "")]
    W, H, gx, gy, per = 176, 74, 28, 66, 5
    out = [f'<svg class="map" viewBox="0 0 {per*(W+gx)+10} {3*(H+gy)+10}" role="img" aria-label="Process map of the 14 delivery stages">',
           '<defs><marker id="ar" markerWidth="9" markerHeight="9" refX="7" refY="4.5" orient="auto"><path d="M0,0 L9,4.5 L0,9 z" fill="#5b6775"/></marker></defs>']
    pos = []
    for i, (n, t, g, gate) in enumerate(names):
        r, c = divmod(i, per)
        x = 5 + c * (W + gx); y = 5 + r * (H + gy); pos.append((x, y))
        col = GROUPS[g][0]
        out.append(f'<a href="#s{n}"><rect x="{x}" y="{y}" width="{W}" height="{H}" rx="12" fill="{col}"/>'
                   f'<text x="{x+14}" y="{y+34}" font-size="26" font-weight="800" fill="#fff" opacity=".95">{int(n):02d}</text>'
                   f'<text x="{x+58}" y="{y+30}" font-size="12.5" font-weight="700" fill="#fff">{t}</text>'
                   f'<text x="{x+58}" y="{y+50}" font-size="11" fill="#fff" opacity=".9">{GROUPS[g][1].title()}</text></a>')
        if gate:
            out.append(f'<rect x="{x+W-62}" y="{y+H-4}" width="62" height="20" rx="10" fill="#fff3d6" stroke="#f0cf85"/><text x="{x+W-31}" y="{y+H+10}" font-size="10.5" font-weight="700" fill="#7a4b00" text-anchor="middle">{gate}</text>')
    for i in range(len(names) - 1):
        (x1, y1), (x2, y2) = pos[i], pos[i + 1]
        if y1 == y2:
            out.append(f'<line x1="{x1+W}" y1="{y1+H/2}" x2="{x2-2}" y2="{y2+H/2}" stroke="#5b6775" stroke-width="2.2" marker-end="url(#ar)"/>')
        else:
            my = y1 + H + gy / 2 + 6
            out.append(f'<path d="M{x1+W/2},{y1+H+22} V{my} H{x2+W/2} V{y2-3}" fill="none" stroke="#5b6775" stroke-width="2.2" marker-end="url(#ar)"/>')
    out.append("</svg>")
    return "".join(out)

def loop_map():
    labels = ["1 Pick a task", "2 New branch", "3 Write code + test", "4 Run checks", "5 Pull request", "6 Review", "7 Merge", "8 Update tracker"]
    chips = "".join(f'<div>{l}</div>' + ('<div style="background:none;border:0;padding:6px 0">→</div>' if i < len(labels) - 1 else "") for i, l in enumerate(labels))
    return f'<div class="flow">{chips}</div>'

CSS = BASE_CSS + EXTRA_CSS
JS = BASE_JS.split("// ---- live")[0]
