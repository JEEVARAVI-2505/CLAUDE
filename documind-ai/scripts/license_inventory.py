"""Print exact versions and licence metadata for the runtime dependency tree (input for THIRD_PARTY_NOTICES.md)."""
import re
import sys
from importlib import metadata

ROOTS = ["PySide6-Essentials", "pydantic", "pypdf", "pypdfium2", "Pillow", "openpyxl"]


def norm(n: str) -> str:
    return re.sub(r"[-_.]+", "-", n).lower()


def walk(name: str, seen: dict) -> None:
    key = norm(name)
    if key in seen:
        return
    try:
        d = metadata.distribution(name)
    except metadata.PackageNotFoundError:
        return
    seen[key] = d
    for r in d.requires or []:
        if "extra ==" in r:
            continue
        m = re.match(r"[A-Za-z0-9_.\-]+", r)
        if m:
            walk(m.group(0), seen)


def licence(d) -> str:
    md = d.metadata
    lic = md.get("License-Expression") or ""
    if not lic:
        lic = (md.get("License") or "").strip().splitlines()[0][:80] if md.get("License") else ""
    cls = [c.split("::")[-1].strip() for c in md.get_all("Classifier") or [] if c.startswith("License ::")]
    return " | ".join(x for x in (lic, ", ".join(cls)) if x) or "UNKNOWN"


seen: dict = {}
for r in ROOTS:
    walk(r, seen)
for k in sorted(seen):
    d = seen[k]
    print(f"{d.metadata['Name']}\t{d.version}\t{licence(d)}\t{(d.metadata.get('Home-page') or '')}")
