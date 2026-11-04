"""Templates and sign-off checklist chapter + Markdown/CSV export for the repository skeleton."""
import pathlib
import re

from lib import *  # noqa: F401,F403
from c14a_templates_data import TEMPLATES, CHECKLIST


def _lead(text: str):
    m = re.match(r"\s*(\d+)\.", text)
    return int(m.group(1)) if m else None


def ordered_items(t: dict):
    """Merge fields and tables in numeric section order: yields ('f', label, hint) or ('t', caption, cols)."""
    items = [("f", a, b) for a, b in t["fields"]]
    for cap, cols in t["tables"]:
        n = _lead(cap)
        pos = len(items)
        if n is not None:
            for i, it in enumerate(items):
                ln = _lead(it[1])
                if ln is not None and ln > n:
                    pos = i
                    break
        items.insert(pos, ("t", cap, cols))
    return items


def _blank_table(cols, rows=3):
    return table(cols, [[""] * len(cols) for _ in range(rows)])


def _render(t: dict) -> str:
    out, buf = [h2(t["title"]), p(f"**Purpose:** {t['purpose']}  **Repository file:** `{t['path']}`")], []
    for kind, a, b in ordered_items(t):
        if kind == "f":
            buf.append([a, f"*{b}*" if b else ""])
        else:
            if buf:
                out.append(kv(buf))
                buf = []
            out.append(h3(a))
            out.append(_blank_table(b))
    if buf:
        out.append(kv(buf))
    return "".join(out)


def templates() -> str:
    body = "".join(_render(t) for t in TEMPLATES)
    check = "".join(h3(sec) + checklist(items) for sec, items in CHECKLIST)
    return join(
        h1("Templates and QA Sign-off Checklist",
           "Controlled templates for every document in the lifecycle. Copy the Markdown or CSV version from `qa-project-template/` into the project "
           "repository; the forms below show the required fields. Grey hint text describes what to enter — delete it when filling in."),
        callout("gold", "How to use a template",
                ["Copy it, fill every field or write “N/A — reason”; never leave a field silently blank.",
                 "Keep the header block (ID, version, status, author, reviewer, date) and update it on every change.",
                 "Store the file in the path shown; commit with a message such as `docs(test-plan): v0.2 add risks`.",
                 "Evidence and attachments are referenced by file name or link, with secrets removed."]),
        body,
        h2("QA sign-off checklist"),
        p("Complete before every release recommendation (Stage 14). Every box ticked needs a reference to evidence; the second reviewer re-checks a sample."),
        check,
        table(["Role", "Name", "Signature", "Date", "Decision"], [["QA Lead", "", "", "", ""], ["Second reviewer (Test Manager)", "", "", "", ""],
                                                               ["Client release owner", "", "", "", ""]], widths=["30%", "20%", "20%", "12%", "18%"]),
        end_chapter(),
    )


# ------------------------------------------------------------------ export to the repository skeleton
def to_markdown(t: dict) -> str:
    from c14a_templates_data import HDR
    hdr_labels = {a for a, _ in HDR}
    lines = [f"# {t['title']}", "", f"> {t['purpose']}  ", f"> Controlled template · copy to `{t['path']}` · remove hints when filled.", ""]
    hdr = [(a, b) for k, a, b in ordered_items(t) if k == "f" and a in hdr_labels]
    if hdr:
        lines += ["| Field | Value |", "|---|---|"] + [f"| {a} | _{b}_ |" for a, b in hdr] + [""]
    for kind, a, b in ordered_items(t):
        if kind == "f" and a in hdr_labels:
            continue
        if kind == "f":
            lines += [f"## {a}", f"_{b}_" if b else "", "", "TODO", ""]
        else:
            lines += [f"## {a}", "", "| " + " | ".join(b) + " |", "|" + "---|" * len(b), "| " + " | ".join([" "] * len(b)) + " |", ""]
    return "\n".join(lines).rstrip() + "\n"


def export(outdir: pathlib.Path) -> list[str]:
    """Write Markdown templates + CSV headers + checklist into qa-project-template/. Returns files written."""
    tdir = outdir / "templates"
    tdir.mkdir(parents=True, exist_ok=True)
    written = []
    for t in TEMPLATES:
        f = tdir / f"{t['key']}.md"
        f.write_text(to_markdown(t), encoding="utf-8")
        written.append(str(f))
    cols = {
        "testcase.csv": "Test case ID,Title,Requirement ID(s),Module,Type,Technique,Priority,Preconditions,Test data,Steps,Expected result,Postconditions,Automation candidate,Author,Reviewer,Date",
        "scenario.csv": "Scenario ID,Title (Verify that ...),REQ ID(s),Priority,Type,Derived TC IDs",
        "rtm.csv": "REQ ID,Requirement (short),Risk,Test case IDs,Automated?,Last result,Defect IDs,Coverage status",
        "execution-log.csv": "Test case ID,Build,Date,Tester,Result,Evidence file,Defect ID,Comment",
        "defect-log.csv": "Defect ID,Title,Severity,Priority,Status,Reporter,Assignee,Found in build,Fixed in build,Requirement ID,Test case ID",
    }
    for name, header in cols.items():
        f = tdir / name
        f.write_text(header + "\n", encoding="utf-8")
        written.append(str(f))
    md = ["# QA Sign-off Checklist", "", "Complete before every release recommendation (Stage 14). Reference evidence for each tick.", ""]
    for sec, items in CHECKLIST:
        md += [f"## {sec}", ""] + [f"- [ ] {i}" for i in items] + [""]
    md += ["## Approvals", "", "| Role | Name | Signature | Date | Decision |", "|---|---|---|---|---|",
           "| QA Lead | | | | |", "| Second reviewer (Test Manager) | | | | |", "| Client release owner | | | | |", ""]
    f = tdir / "qa-signoff-checklist.md"
    f.write_text("\n".join(md), encoding="utf-8")
    written.append(str(f))
    return written
