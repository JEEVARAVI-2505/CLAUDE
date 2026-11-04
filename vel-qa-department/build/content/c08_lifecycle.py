"""Mandatory 16-stage client QA lifecycle: procedures, templates, responsibilities, evidence, approval gates."""
from lib import *  # noqa: F401,F403
from c08a_stage_data import STAGES


def _stage(s: dict) -> str:
    r, a, c, i = s["raci"]
    gname, gapprover, gcheck = s["gate"]
    return join(
        h2(f"Stage {s['n']:02d} — {s['name']}"),
        kv([
            ["Objective", s["objective"]],
            ["Responsibilities", f"**Responsible:** {r}\n**Accountable:** {a}\n**Consulted:** {c}\n**Informed:** {i}"],
            ["Inputs", s["inputs"]],
            ["Procedure", ("ol", s["steps"])],
            ["Deliverables and templates", s["deliverables"]],
            ["Evidence required", s["evidence"]],
            ["Acceptance criteria", s["acceptance"]],
        ], keep=True),
        callout("gold", f"Approval gate {gname}",
                f"**Approver:** {gapprover}. **Checks:** {gcheck} "
                "Record the decision (approved / approved with conditions / rejected), date and approver name in `docs/qa-signoff.md`."),
    )


def lifecycle() -> str:
    names = ["01 Qualification", "02 Requirements", "03 Strategy", "04 Planning", "05 Test cases", "06 Environment",
             "07 Execution", "08 Defects", "09 Automation", "10 API / integration", "11 Performance", "12 Regression",
             "13 UAT", "14 Release rec.", "15 Prod smoke", "16 Maintenance"]
    gate_rows = [[f"{s['n']:02d} {s['name']}", s["gate"][0], s["gate"][1]] for s in STAGES]
    return join(
        h1("Mandatory Client QA Lifecycle: Stages 01–16",
           "Every client project follows these 16 stages in order. Each stage lists its objective, responsibilities, inputs, "
           "procedure, deliverables, evidence, acceptance criteria and **approval gate**. A stage is closed only when its gate is signed "
           "by the named approver. Stages may overlap in time (for example, automation starts while manual execution continues), "
           "but a gate must never be skipped."),
        pills(names[:8]), pills(names[8:]),
        h2("Approval gates at a glance"),
        table(["Stage", "Gate", "Approver"], gate_rows, widths=["32%", "36%", "32%"], first_bold=True),
        callout("danger", "Do not start execution without the entry gate",
                "Stage 07 must not begin until Gate G6 (environment and entry criteria) is signed. Testing on an unready environment "
                "produces false failures, wasted effort and unreliable metrics."),
        h2("Role summary across stages (RACI)"),
        p("R = responsible, A = accountable, C = consulted, I = informed. “—” means not involved. Each project's test plan may refine this table."),
        table(["Stage", "Junior QA", "Manual QA", "Automation QA", "API Tester", "Perf Tester", "QA Lead", "Test Manager", "Release Coord."], [
            ["01 Qualification", "—", "—", "—", "—", "—", "C", "A/R", "I"],
            ["02 Requirements", "I", "R", "C", "C", "C", "A", "I", "—"],
            ["03 Strategy", "—", "C", "C", "C", "C", "R", "A", "I"],
            ["04 Planning", "—", "C", "C", "C", "C", "R", "A", "C"],
            ["05 Test cases", "R", "R", "C", "C", "—", "A", "I", "—"],
            ["06 Environment", "I", "R", "C", "C", "C", "A/R", "I", "C"],
            ["07 Execution", "R", "R", "I", "I", "—", "A", "I", "I"],
            ["08 Defects", "R", "R", "R", "R", "R", "A", "I", "I"],
            ["09 Automation", "—", "C", "A/R", "R", "—", "A", "I", "—"],
            ["10 API/integration", "—", "C", "R", "R", "—", "A", "I", "—"],
            ["11 Performance", "—", "—", "C", "C", "R", "C", "A", "I"],
            ["12 Regression", "R", "R", "R", "R", "—", "A", "I", "I"],
            ["13 UAT support", "I", "R", "—", "—", "—", "A/R", "I", "C"],
            ["14 Release rec.", "—", "C", "C", "C", "C", "R", "A", "C"],
            ["15 Production smoke", "—", "R", "C", "—", "—", "C", "I", "A/R"],
            ["16 Maintenance", "C", "R", "R", "R", "C", "A", "I", "—"],
        ], widths=["20%", "9%", "9%", "11%", "9%", "9%", "11%", "11%", "11%"], cls="raci", first_bold=True),
        *[_stage(s) for s in STAGES],
        h2("Change control during a project"),
        ol(["Any change to scope, schedule, environments or exit criteria is raised in writing.",
            "The QA Lead assesses impact on effort, risk and dates using the costing method.",
            "The client approves the revised plan/quote.",
            "The Test Plan version is incremented and filed before work on the change starts."]),
        end_chapter(),
    )
