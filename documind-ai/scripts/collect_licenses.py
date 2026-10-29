"""Copy licence files of every runtime dependency into a folder (shipped with the installer)."""
import re
import shutil
import sys
from importlib import metadata
from pathlib import Path

ROOTS = ["PySide6-Essentials", "pydantic", "pypdf", "pypdfium2", "Pillow", "openpyxl"]
out = Path(sys.argv[1]); out.mkdir(parents=True, exist_ok=True)
seen = set()


def walk(name):
    k = re.sub(r"[-_.]+", "-", name).lower()
    if k in seen:
        return
    try:
        d = metadata.distribution(name)
    except metadata.PackageNotFoundError:
        return
    seen.add(k)
    dest = out / f"{d.metadata['Name']}-{d.version}"
    for f in d.files or []:
        if re.search(r"(?i)(licen[sc]e|copying|notice|authors)", f.name) and "dist-info" in str(f):
            dest.mkdir(exist_ok=True); shutil.copy(d.locate_file(f), dest / f.name)
    for r in d.requires or []:
        if "extra ==" not in r:
            m = re.match(r"[A-Za-z0-9_.\-]+", r)
            if m:
                walk(m.group(0))


for r in ROOTS:
    walk(r)
print(f"collected licence files for {len(seen)} packages into {out}")
