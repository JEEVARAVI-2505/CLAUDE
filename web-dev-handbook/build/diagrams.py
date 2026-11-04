"""Inline-SVG diagrams (no external assets). Palette matches style.css."""
from html import escape

NAVY, NAVY2, MAG, ZEB, LINE, MUTED = "#14306b", "#1d4a94", "#c2185b", "#eef2f9", "#c6d0e4", "#5b6577"
AMBER, GREEN, BLUE = "#fff3e8", "#e9f6ee", "#eaf1fb"


def svg(w, h, body, label="diagram"):
    return (f'<svg viewBox="0 0 {w} {h}" width="{w}" height="{h}" role="img" aria-label="{escape(label)}" '
            f'xmlns="http://www.w3.org/2000/svg" font-family="VEL Inter, Inter, Segoe UI, Arial, sans-serif">'
            f'<defs><marker id="ah" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="7" markerHeight="7" orient="auto-start-reverse">'
            f'<path d="M0 0L10 5L0 10z" fill="{MAG}"/></marker></defs>{body}</svg>')


def text(x, y, s, size=11, fill=NAVY, weight=600, anchor="middle"):
    lines = s.split("\n")
    spans = [f'<tspan x="{x}" dy="{(-(len(lines) - 1) / 2) * (size + 3) if i == 0 else size + 3}">{escape(ln)}</tspan>'
             for i, ln in enumerate(lines)]
    return (f'<text x="{x}" y="{y}" font-size="{size}" font-weight="{weight}" fill="{fill}" text-anchor="{anchor}" '
            f'dominant-baseline="central">{"".join(spans)}</text>')


def box(x, y, w, h, label, fill=ZEB, stroke=LINE, color=NAVY, size=11, weight=600, r=8):
    return (f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{r}" fill="{fill}" stroke="{stroke}" stroke-width="1.2"/>'
            + text(x + w / 2, y + h / 2, label, size, color, weight))


def arrow(x1, y1, x2, y2, label=None, dash=False):
    d = ' stroke-dasharray="5 4"' if dash else ""
    out = f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="{MAG}" stroke-width="1.6"{d} marker-end="url(#ah)"/>'
    if label:
        out += text((x1 + x2) / 2, (y1 + y2) / 2 - 9, label, 9, MUTED, 500)
    return out


def web_request():
    b = [box(10, 70, 100, 56, "Browser\n(client)", BLUE), box(150, 70, 100, 56, "DNS\nresolver", ZEB),
         box(290, 70, 100, 56, "CDN / WAF\n(Cloudflare)", ZEB), box(430, 70, 100, 56, "Web server\n(Nginx)", ZEB),
         box(570, 20, 140, 50, "App / API\n(Node, FastAPI)", AMBER), box(570, 126, 140, 50, "Database\n(PostgreSQL)", GREEN)]
    b += [arrow(110, 98, 150, 98, "1 name?"), arrow(250, 98, 290, 98, "2 IP + HTTPS"), arrow(390, 98, 430, 98, "3 cache miss"),
          arrow(530, 90, 580, 60, "4"), arrow(640, 70, 640, 126, "5 SQL"),
          text(360, 168, "6  Response travels back: DB → API (JSON) → server → CDN (cache) → browser renders HTML/CSS/JS", 10, MUTED, 500)]
    return svg(720, 190, "".join(b), "Web request path")


def lifecycle():
    phases = [("1 · Win & define", [(1, "Lead\nqualification"), (2, "Client\ndiscovery"), (3, "Requirement\ncollection"), (4, "Market\nresearch"), (5, "Scope &\nSOW"), (6, "Feature\nfreeze")]),
              ("2 · Design", [(7, "Sitemap\n& IA"), (8, "UI/UX\napproval"), (9, "Technical\narchitecture"), (10, "Database\ndesign")]),
              ("3 · Build", [(11, "Dev env\nsetup"), (12, "GitHub\nrepo"), (13, "Frontend"), (14, "Backend"), (15, "API\nintegration"), (16, "Database\nimpl."), (17, "Auth &\npermissions"), (18, "Payments &\n3rd-party")]),
              ("4 · Verify & release", [(19, "Unit & integ.\ntesting"), (20, "Security &\nperformance"), (21, "Staging\ndeploy"), (22, "Client\nreview"), (23, "Production\ndeploy"), (24, "DNS &\ndomain"), (25, "SSL/TLS\nverify")]),
              ("5 · Operate", [(26, "Client\nhandover"), (27, "Maintenance\n& support")])]
    out, y = [], 6
    for name, items in phases:
        out.append(text(8, y + 20, name, 10, MAG, 700, "start"))
        x = 128
        for n, lab in items:
            out.append(box(x, y + 2, 66, 36, lab, ZEB if n % 2 else BLUE, size=8.2, weight=600, r=6))
            out.append(f'<circle cx="{x + 8}" cy="{y + 2}" r="8" fill="{NAVY}"/>' + text(x + 8, y + 2, str(n), 7.5, "#fff", 700))
            x += 72
        y += 50
    return svg(720, y + 2, "".join(out), "27-stage client project lifecycle")


def roadmap():
    lv = [("L1", "Web\nfundamentals", BLUE), ("L2", "Frontend\ndevelopment", ZEB), ("L3", "Backend\ndevelopment", BLUE),
          ("L4", "Database\nengineering", ZEB), ("L5", "Professional\nengineering", AMBER)]
    out = []
    for i, (k, t, f) in enumerate(lv):
        x, h = 10 + i * 140, 40 + i * 22
        y = 150 - h
        out.append(f'<rect x="{x}" y="{y}" width="128" height="{h + 30}" rx="8" fill="{f}" stroke="{LINE}"/>')
        out.append(text(x + 64, y + 16, k, 14, MAG, 700) + text(x + 64, y + 44, t, 10.5))
    out.append(text(360, 190, "Each level ends with a Technical Lead sign-off gate — no skipping levels.", 10, MUTED, 500))
    return svg(720, 205, "".join(out), "Learning roadmap")


def git_flow():
    lanes = {"main": 40, "develop": 92, "feature/login": 144, "hotfix/pay-bug": 196}
    out = []
    for n, y in lanes.items():
        out.append(f'<line x1="140" y1="{y}" x2="705" y2="{y}" stroke="{LINE}" stroke-width="2"/>' + text(10, y, n, 10, NAVY, 700, "start"))

    def dot(x, y, c=NAVY):
        return f'<circle cx="{x}" cy="{y}" r="6" fill="{c}"/>'
    out += [dot(160, 40), dot(160, 92), dot(225, 92), dot(265, 144), dot(335, 144), dot(405, 92), dot(485, 40, MAG),
            dot(545, 40), dot(585, 196), dot(625, 196), dot(665, 40, MAG), dot(665, 92)]
    out += [arrow(160, 46, 160, 86), arrow(228, 98, 262, 138), arrow(340, 140, 402, 98, "PR + review + CI"),
            arrow(410, 88, 480, 46, "release"), arrow(548, 46, 582, 190), arrow(628, 190, 662, 46), arrow(628, 192, 662, 98)]
    out += [text(485, 20, "v1.0", 9.5, MAG, 700), text(665, 20, "v1.0.1", 9.5, MAG, 700),
            text(395, 236, "Branch from develop → small PRs → squash-merge → tag releases on main → merge every hotfix back to develop", 9.5, MUTED, 500)]
    return svg(720, 250, "".join(out), "Git branching strategy")


def cicd():
    st = [("Commit\n& PR", BLUE), ("Lint &\ntype check", ZEB), ("Unit\ntests", ZEB), ("Build &\nDocker image", ZEB),
          ("Security\nscan", AMBER), ("Deploy\nstaging", ZEB), ("E2E tests\n(Playwright)", ZEB), ("Manual\napproval", AMBER), ("Deploy\nproduction", GREEN)]
    out = []
    for i, (t, f) in enumerate(st):
        x = 6 + i * 79
        out.append(box(x, 40, 68, 48, t, f, size=9, r=6))
        if i < len(st) - 1:
            out.append(arrow(x + 68, 64, x + 79, 64))
    out.append(text(360, 14, "Any red stage stops the pipeline — nothing reaches production on a failing check", 10, MUTED, 500))
    out.append(text(360, 118, "Rollback = redeploy previous image tag (kept for at least 5 releases)", 10, MAG, 600))
    return svg(720, 135, "".join(out), "CI/CD pipeline")


def auth_flow():
    cols = [("Browser", 70), ("Next.js / SPA", 250), ("API", 430), ("Database", 620)]
    out = []
    for n, x in cols:
        out.append(box(x - 55, 4, 110, 28, n, BLUE, size=10))
        out.append(f'<line x1="{x}" y1="32" x2="{x}" y2="262" stroke="{LINE}" stroke-dasharray="4 4"/>')
    steps = [(70, 250, 56, "1 email + password (HTTPS)"), (250, 430, 80, "2 POST /auth/login"),
             (430, 620, 104, "3 find user + verify hash"), (620, 430, 128, "4 user row"),
             (430, 250, 152, "5 Set-Cookie: httpOnly session / JWT"), (250, 70, 176, "6 redirect to dashboard"),
             (70, 430, 204, "7 later requests carry cookie"), (430, 430, 228, "8 check role → allow / 403")]
    for a, b, y, lab in steps:
        if a == b:
            out.append(f'<path d="M{a} {y} h26 v14 h-26" fill="none" stroke="{MAG}" stroke-width="1.6" marker-end="url(#ah)"/>' + text(a + 32, y + 7, lab, 9.5, MUTED, 500, "start"))
        else:
            out.append(arrow(a, y, b, y, None, a > b) + text((a + b) / 2, y - 8, lab, 9.5, MUTED, 500))
    return svg(720, 270, "".join(out), "Authentication sequence")


def erd():
    def ent(x, y, name, cols):
        h = 22 + 15 * len(cols) + 4
        s = f'<rect x="{x}" y="{y}" width="140" height="{h}" rx="6" fill="#fff" stroke="{NAVY}" stroke-width="1.3"/>'
        s += f'<rect x="{x}" y="{y}" width="140" height="22" rx="6" fill="{NAVY}"/>' + text(x + 70, y + 11, name, 10.5, "#fff", 700)
        for i, c in enumerate(cols):
            s += text(x + 8, y + 33 + 15 * i, c, 9, NAVY if ("PK" in c or "FK" in c) else MUTED, 700 if "PK" in c else 500, "start")
        return s
    out = [ent(10, 8, "users", ["PK id", "email (unique)", "password_hash", "role", "created_at"]),
           ent(290, 8, "bookings", ["PK id", "FK user_id", "FK service_id", "starts_at", "status"]),
           ent(570, 8, "services", ["PK id", "name", "duration_min", "price_minor", "active"]),
           ent(290, 140, "payments", ["PK id", "FK booking_id", "provider_ref", "amount_minor", "status"])]
    out += [arrow(150, 62, 290, 62), text(220, 50, "1 user → N bookings", 9, MUTED, 500),
            arrow(570, 62, 430, 62), text(500, 50, "1 service → N", 9, MUTED, 500),
            arrow(360, 117, 360, 140), text(425, 130, "1 booking → 0..N payments", 9, MUTED, 500)]
    return svg(720, 245, "".join(out), "Sample entity relationship diagram (booking system)")


def cost_model():
    parts = [("Labour\n(hours × rate)", 80, NAVY2), ("Third-party\n& tools", 30, "#5b7fb8"), ("Contingency\n(10–20%)", 24, "#8aa4cf"),
             ("Total\ndelivery cost", 134, NAVY), ("Margin\n(target %)", 90, MAG), ("Selling\nprice", 224, "#157347")]
    out, x = [], 10
    for lab, h, c in parts:
        out.append(f'<rect x="{x}" y="{236 - h}" width="96" height="{h}" rx="5" fill="{c}"/>')
        out.append(text(x + 48, 258, lab, 9.5, NAVY, 600))
        x += 118
    out.append(text(360, 12, "Price = Cost ÷ (1 − target gross margin)   ·   markup ≠ margin   ·   bar heights are schematic", 10, MAG, 700))
    return svg(720, 278, "".join(out), "Cost to price build-up")


def escalation():
    lv = [("L4 · Director / client sponsor", "Business-critical, legal or security breach", AMBER),
          ("L3 · Project Manager + Technical Lead", "Blocked > 1 working day, scope or budget impact", BLUE),
          ("L2 · Senior developer / mentor", "Stuck > 2 hours after own research", ZEB),
          ("L1 · Self + documentation", "Docs, search, rubber-duck, notes in ticket", "#fff")]
    out = []
    for i, (a, b, f) in enumerate(lv):
        y = 6 + i * 52
        x = 10 + (3 - i) * 28
        out.append(f'<rect x="{x}" y="{y}" width="{700 - 2 * x + 20}" height="44" rx="8" fill="{f}" stroke="{LINE}"/>')
        out.append(text(360, y + 15, a, 11, NAVY, 700) + text(360, y + 32, b, 9.5, MUTED, 500))
    return svg(720, 216, "".join(out), "Escalation ladder")


def stack_choice():
    out = [box(250, 4, 220, 34, "What does the client need?", BLUE),
           box(10, 80, 150, 44, "Brochure / marketing\nSEO-led", ZEB), box(190, 80, 150, 44, "Content-managed\nsite", ZEB),
           box(370, 80, 150, 44, "Logged-in app /\ndashboard / SaaS", ZEB), box(550, 80, 160, 44, "Shop / bookings\n/ payments", ZEB),
           box(10, 160, 150, 56, "Static / Next.js SSG\n+ Cloudflare Pages", AMBER, size=9.5), box(190, 160, 150, 56, "Headless CMS or\nWordPress + Next.js", AMBER, size=9.5),
           box(370, 160, 150, 56, "Next.js + FastAPI or\nNode + PostgreSQL", AMBER, size=9.5), box(550, 160, 160, 56, "Shopify / Woo or custom\nNext.js + PostgreSQL", AMBER, size=9.5)]
    for x in (85, 265, 445, 630):
        out.append(arrow(360, 38, x, 80) + arrow(x, 124, x, 160))
    out.append(text(360, 236, "Pick the simplest stack that meets the requirement, then justify it in an Architecture Decision Record.", 10, MUTED, 500))
    return svg(720, 250, "".join(out), "Stack selection tree")


def payment_flow():
    cols = [("Customer", 60), ("Our frontend", 210), ("Our backend", 370), ("Payment gateway", 540), ("Bank / UPI / card", 665)]
    out = []
    for n, x in cols:
        out.append(box(x - 50, 4, 100, 26, n, BLUE, size=9))
        out.append(f'<line x1="{x}" y1="30" x2="{x}" y2="236" stroke="{LINE}" stroke-dasharray="4 4"/>')
    s = [(60, 210, 54, "1 Pay now"), (210, 370, 78, "2 create order"), (370, 540, 102, "3 create payment (server-side key)"),
         (540, 210, 126, "4 checkout token"), (210, 540, 150, "5 customer pays on hosted page"),
         (540, 665, 174, "6 authorise"), (540, 370, 198, "7 signed webhook"), (370, 370, 222, "8 verify signature, mark PAID")]
    for a, b, y, lab in s:
        if a == b:
            out.append(f'<path d="M{a} {y} h24 v12 h-24" fill="none" stroke="{MAG}" stroke-width="1.6" marker-end="url(#ah)"/>' + text(a + 30, y + 6, lab, 9, MUTED, 500, "start"))
        else:
            out.append(arrow(a, y, b, y, None, a > b) + text((a + b) / 2, y - 8, lab, 9, MUTED, 500))
    return svg(720, 246, "".join(out), "Payment gateway sequence")


DIAGRAMS = {k: v for k, v in dict(web_request=web_request, lifecycle=lifecycle, roadmap=roadmap, git_flow=git_flow, cicd=cicd,
                                  auth_flow=auth_flow, erd=erd, cost_model=cost_model, escalation=escalation,
                                  stack_choice=stack_choice, payment_flow=payment_flow).items()}
