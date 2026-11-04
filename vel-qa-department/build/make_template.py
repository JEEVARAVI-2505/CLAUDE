"""Regenerate generated files of qa-project-template/: controlled templates and the docs/ starting points."""
import pathlib
import sys

HERE = pathlib.Path(__file__).parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE / "content"))
import c14_templates  # noqa: E402

ROOT = HERE.parent / "qa-project-template"
DOCS = {"strategy": "test-strategy.md", "plan": "test-plan.md", "rtm": "requirements-traceability.md",
        "environment": "test-environment.md", "execution": "release-report.md", "signoff": "qa-signoff.md"}


def make() -> int:
    written = c14_templates.export(ROOT)
    (ROOT / "docs").mkdir(exist_ok=True)
    for key, name in DOCS.items():
        text = (ROOT / "templates" / f"{key}.md").read_text(encoding="utf-8")
        if key == "execution":
            text = text.replace("# Test Execution Report", "# Release Report", 1)
        (ROOT / "docs" / name).write_text(text, encoding="utf-8")
    return len(written) + len(DOCS)


if __name__ == "__main__":
    print(f"template files written: {make()}")
