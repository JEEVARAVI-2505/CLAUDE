"""Automated accessibility scan with axe-core (MPL-2.0). Install once: npm install --save-dev axe-core   (or set AXE_PATH)."""
import json
import os
import pathlib

import pytest
from markers import known

AXE = pathlib.Path(os.getenv("AXE_PATH", "node_modules/axe-core/axe.min.js"))
pytestmark = pytest.mark.a11y


def serious_violations(page) -> list:
    if not AXE.exists():
        pytest.skip(f"axe-core not found at {AXE}; run `npm install --save-dev axe-core` or set AXE_PATH")
    page.add_script_tag(content=AXE.read_text(encoding="utf-8"))
    result = page.evaluate("axe.run()")
    return [(v["id"], v["impact"], v["help"]) for v in result["violations"] if v["impact"] in ("serious", "critical")]


def test_register_view_has_no_serious_violations(register_page, page):
    register_page.open()
    assert serious_violations(page) == [], json.dumps(serious_violations(page), indent=2)


def test_login_view_automated_scan_passes_but_manual_review_does_not(login_page, page):
    """TEACHING POINT: axe-core accepts a placeholder as an accessible name, so this scan PASSES even though the field has no visible
    label (defect D16a, found by manual testing - see test_login_email_has_accessible_label). Automated tools find only part of the issues."""
    login_page.open()
    assert serious_violations(page) == []


@known("D16b: images without alt flagged by axe", builds=("1.0", "1.1"))
def test_shop_view_has_no_serious_violations(shop, page):
    assert serious_violations(page) == []
