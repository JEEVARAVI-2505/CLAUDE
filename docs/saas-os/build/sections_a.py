"""Front matter: intake, operating model, gates, roles, rules."""
from html import escape as e


def table(head, rows, cls=""):
    h = "".join(f"<th>{x}</th>" for x in head)
    b = "".join("<tr>" + "".join(f"<td>{c}</td>" for c in r) + "</tr>" for r in rows)
    return f'<div class="tw"><table class="{cls}"><thead><tr>{h}</tr></thead><tbody>{b}</tbody></table></div>'

def callout(kind, title, body):
    return f'<div class="callout {kind}"><b>{title}</b><div>{body}</div></div>'

def intake():
    rows = [
     ("Company name","VEL Software Solutions (owner and developer: V.LOKESHWARAN)","Legal entity name for contracts, registrar and billing"),
     ("Business model","B2B SaaS subscriptions + custom web development (project work)","Recurring revenue is the long-term goal; project work funds the runway"),
     ("Employee count","1 founder + 5 roles (Tech Lead, Full-stack dev, UI/UX, QA, DevOps) – can be part-time","Roles can be combined; gates cannot be skipped"),
     ("Target customers","Small and mid-sized businesses (10–200 staff) in India; service-led businesses first","Narrow ICP gives cheaper acquisition"),
     ("Available budget","[Fill in]  – model uses ₹10–15 lakh first-product budget as a planning figure","ASSUMPTION"),
     ("Technology preferences","TypeScript, React/Next.js, Node.js or FastAPI, PostgreSQL, managed PaaS","Final choice only at Gate G3"),
     ("Initial product focus","Sample: SlotWise – appointment scheduling and reminders for small clinics","Illustrative; replace with your first validated opportunity")]
    return table(["Intake question","Working default (edit me)","Why it matters"], rows) + callout("warn","How to use this page","The defaults above let the framework and the worked example run end-to-end. Replace them with your real answers; the financial model recalculates from the same inputs (see the web calculator). Nothing here is a fact about your company until you confirm it.")

def gates():
    rows = [("G1","Opportunity Approval","Market Research & Opportunity Report","Business Owner"),
     ("G2","Scope Freeze","PRD + I/O Spec + Scope Freeze","Product Owner + Customer"),
     ("G3","Architecture Approval","Architecture Document","Tech Lead + Business Owner"),
     ("G4","Design Approval","UI/UX system + prototype","Product Owner + Customer"),
     ("G5","Repo Readiness","Configured repository + CI","Tech Lead"),
     ("G6","Release Candidate","Tested software, QA + security reports","Product Owner + Tech Lead"),
     ("G7","Cost Model Approval","Production cost model","Business Owner + Finance"),
     ("G8","Pricing Approval","Pricing & profitability model","Business Owner"),
     ("G9","Release Approval","Domain/DNS + Deployment reports","Tech Lead + Business Owner"),
     ("G10","Commercial Approval","Proposal/SOW/contract","Business Owner"),
     ("G11","Operational Readiness","Maintenance agreement + runbooks","Tech Lead"),
     ("G12","Handover & Renewal","Acceptance form + handover pack","Customer + Support Lead")]
    return table(["Gate","Name","Evidence required","Approver"], rows)

def raci():
    heads = ["Activity","Owner (Founder)","Product Owner","Tech Lead","Developer","Designer","QA","DevOps"]
    R = [("Market research","A","R","C","–","–","–","–"),("PRD / Scope","A","R","C","C","C","C","–"),
     ("Architecture","C","C","A/R","C","–","–","C"),("UI/UX","C","A","C","C","R","C","–"),
     ("Repo / CI","–","–","A","C","–","–","R"),("Development","–","C","A","R","C","C","C"),
     ("Testing","–","C","C","C","–","A/R","–"),("Security review","C","–","A/R","C","–","C","C"),
     ("Cost & pricing","A","R","C","–","–","–","C"),("Deployment","C","–","A","C","–","C","R"),
     ("Sales & delivery","A","R","C","–","–","–","–"),("Maintenance","C","C","A","R","–","C","R")]
    return table(heads, R, "center") + '<p class="note">R = Responsible, A = Accountable, C = Consulted, – = not involved.</p>'

def rules():
    L = ["Never start coding before product scope approval (G2).","Never launch without testing and security review.",
     "Never estimate production cost without documenting usage assumptions.","Never confuse one-time development cost with monthly operating cost.",
     "Never treat free-tier services as guaranteed permanent infrastructure.","Never invent domain availability or DNS values.",
     "Never calculate profit using revenue alone.","Never hide recurring third-party costs.",
     "Never ignore maintenance and customer-support labour.","Never recommend an expensive architecture without justification.",
     "Never claim a product is production-ready without evidence.","Never proceed past an approval gate without authorisation."]
    return "<ol class='rules'>" + "".join(f"<li>{x}</li>" for x in L) + "</ol>"

def lifecycle():
    steps = ["Market research","Customer validation","Product definition","Input/Output freeze","Architecture","UI/UX","VS Code & GitHub","Development","Testing","Production cost","Pricing approval","Domain & DNS","Deployment","Customer showcase","Sales","Handover","Maintenance","Recurring revenue"]
    return '<div class="flow">' + "".join(f"<span>{i+1}. {s}</span>" for i, s in enumerate(steps)) + "</div>"

def stage_card(s):
    li = lambda xs: "<ul>" + "".join(f"<li>{x}</li>" for x in xs) + "</ul>"
    ol = lambda xs: "<ol>" + "".join(f"<li>{x}</li>" for x in xs) + "</ol>"
    roles = table(["Role","Responsibility"], s["roles"])
    srcs = "<ul class='src'>" + "".join(f'<li><a href="{u}">{t}</a> <small>{u}</small></li>' for t, u in s["sources"]) + "</ul>"
    mk = table(["Common mistake","Prevention"], s["mistakes"])
    chk = "<ul class='chk'>" + "".join(f"<li>☐ {x}</li>" for x in s["checklist"]) + "</ul>"
    return f'''<section class="phase" id="stage{s["n"]}"><h2><span class="num">{s["n"]}</span>{s["name"]} <small class="gate">{s["gate"]}</small></h2>
<p class="why">{s["why"]}</p>
<h3>1. Detailed instructions</h3>{ol(s["steps"])}
<div class="callout"><b>In practice – precise targets and artefacts</b>{li([])}</div>
<h3>2. Employee responsibilities</h3>{roles}
<div class="grid2"><div><h3>3. Required tools</h3><p>{s["tools"]}</p><h3>4. Estimated time</h3><p>{s["time"]}</p><h3>5. Estimated cost</h3><p>{s["cost"]}</p></div>
<div><h3>6. Expected deliverables</h3>{li(s["deliver"])}<h3>7. Quality checklist</h3>{chk}</div></div>
<h3>8. Approval criteria</h3><p>{s["approval"]}</p>
<h3>9. Official source references <small>(verify and record access date)</small></h3>{srcs}
<h3>10. Common mistakes &amp; prevention</h3>{mk}</section>'''
