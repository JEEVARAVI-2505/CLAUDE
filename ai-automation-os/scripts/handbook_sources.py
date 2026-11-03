"""Single list of what goes into the handbook, shared by the PDF and web builders.
Developed by V. LOKESHWARAN, VEL Software Solutions, velsoftwaresolutions.in@gmail.com"""
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
# (path relative to ROOT, title prefix added to the H1 or "")
SOURCES = [("handbook/00-start-here.md", ""), ("handbook/01-how-a-software-company-works.md", ""),
           ("handbook/02-folder-structure-explained.md", ""), ("handbook/03-market-research-and-industry-playbook.md", ""),
           ("handbook/04-solution-catalogue.md", ""), ("handbook/05-platform-selection-matrix.md", ""),
           ("handbook/06-reliability-dr-and-maintenance-packages.md", "")] + \
          [(f"handbook/{n}.md", "") for n in (
           "S00-workstation-and-habits", "S01-opportunity-and-customer-discovery", "S02-understand-the-process-as-is",
           "S03-feasibility-and-to-be-design", "S04-requirements-and-scope-freeze", "S05-technology-and-architecture",
           "S06-project-setup", "S07-build-the-core", "S08-ai-and-integrations", "S09-testing-and-ai-evaluation",
           "S10-security-review", "S11-costing-and-pricing", "S12-deployment", "S13-demo-acceptance-onboarding",
           "S14-maintenance-and-improvement", "A-appendices", "H-coverage-map")] + \
          [("02-sample-lifecycle-gst-bill-automation.md", "Annex A — "), ("00-intake-and-assumptions.md", "Annex B — "),
           ("04-technology-competitors-and-costing-reference.md", "Annex C — "), ("05-people-portal-and-support.md", "Annex D — "),
           ("01-operating-framework.md", "Annex E — "), ("03-sop-and-template-pack.md", "Annex F — ")]


def load():
    """Return [(title, markdown_text)] with logo header/footer blocks stripped and annex prefixes applied."""
    out = []
    for rel, prefix in SOURCES:
        text = (ROOT / rel).read_text()
        text = re.sub(r'<p align="center">.*?</p>\s*', "", text, flags=re.S)
        text = re.sub(r"^# (?:\d\d — )?", "# " + prefix, text, count=1, flags=re.M) if prefix else text
        title = re.search(r"^# (.+)$", text, re.M).group(1)
        out.append((title, text))
    return out
