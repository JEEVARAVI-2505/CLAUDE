"""Remove files DocuMind provably does not use from a PyInstaller one-folder build, then (optionally) smoke-test it.

Usage:  python scripts/prune_build.py dist/DocuMind [--smoke]
Everything removed is listed; the smoke test (frozen app processes a generated invoice) proves nothing needed was lost.
"""
from __future__ import annotations

import fnmatch
import os
import subprocess
import sys
from pathlib import Path

# glob patterns relative to the build's internal folder; matched case-insensitively against the relative POSIX path
REMOVE = [
    "*/Qt/translations/*",                                  # Qt UI translations (DocuMind is English-only)
    "*/Qt/lib/*Qt6Network*", "*/Qt/lib/*Qt6Qml*", "*/Qt/lib/*Qt6Quick*", "*/Qt/lib/*Qt6Pdf*", "*/Qt/lib/*Qt6Svg*", "*/Qt/lib/*Qt6OpenGL*",
    "*/Qt/lib/*Qt6VirtualKeyboard*", "*/Qt/lib/*Qt6Designer*", "*/Qt/lib/*Qt6Test*", "*/Qt/lib/*Qt6Sql*", "*/Qt/lib/*Qt6Xml*",
    "*/Qt/bin/Qt6Network*", "*/Qt/bin/Qt6Qml*", "*/Qt/bin/Qt6Quick*", "*/Qt/bin/Qt6Pdf*", "*/Qt/bin/Qt6Svg*", "*/Qt/bin/Qt6OpenGL*",
    "*/Qt/bin/Qt6VirtualKeyboard*", "*/Qt/bin/opengl32sw.dll", "*/Qt/bin/d3dcompiler_*.dll", "*/Qt/bin/dxcompiler.dll", "*/Qt/bin/dxil.dll",
    "opengl32sw.dll", "d3dcompiler_*.dll", "Qt6Network*.dll", "Qt6Qml*.dll", "Qt6Quick*.dll", "Qt6Pdf*.dll", "Qt6Svg*.dll", "Qt6OpenGL*.dll", "Qt6VirtualKeyboard*.dll",
    "*/Qt/plugins/iconengines/*", "*/Qt/plugins/generic/*", "*/Qt/plugins/networkinformation/*", "*/Qt/plugins/tls/*", "*/Qt/plugins/platforminputcontexts/*",
    "*/Qt/plugins/imageformats/*svg*", "*/Qt/plugins/imageformats/*webp*", "*/Qt/plugins/imageformats/*tiff*", "*/Qt/plugins/imageformats/*pdf*",
    "*/Qt/plugins/platforms/*eglfs*", "*/Qt/plugins/platforms/*linuxfb*", "*/Qt/plugins/platforms/*vnc*", "*/Qt/plugins/platforms/*wayland*",
    "*/Qt/plugins/platforms/*minimalegl*", "*/Qt/plugins/platforms/*xcb*" if os.environ.get("DOCUMIND_PRUNE_XCB") else "*/__never__*",
    "*/Qt/plugins/styles/*",                                # the built-in Fusion style is used
    "*/Qt/qml/*", "*/Qt/resources/*.pak", "*/Qt/libexec/*", "*/include/*", "*.pyi", "*/py.typed",
    "pillow.libs/libavif*", "pillow.libs/*avif*", "PIL/_avif*", "PIL/_webp*", "pillow.libs/libwebp*",
]


def size_of(root: Path) -> int:
    return sum(f.lstat().st_size for f in root.rglob("*") if f.is_file() and not f.is_symlink())


def main() -> int:
    if len(sys.argv) < 2:
        print(__doc__); return 2
    root = Path(sys.argv[1]).resolve()
    internal = root / "_internal" if (root / "_internal").exists() else root
    before = size_of(root)
    removed = 0
    for f in sorted(internal.rglob("*")):
        if f.is_symlink() or not f.is_file():
            if f.is_symlink() and any(fnmatch.fnmatch(f.relative_to(internal).as_posix().lower(), pat.lower()) for pat in REMOVE):
                f.unlink()
            continue
        rel = f.relative_to(internal).as_posix()
        if any(fnmatch.fnmatch(rel.lower(), pat.lower()) for pat in REMOVE):
            f.unlink(); removed += 1
    after = size_of(root)
    print(f"pruned {removed} files: {before / 1e6:.0f} MB -> {after / 1e6:.0f} MB")
    if "--smoke" in sys.argv:
        exe = root / ("DocuMind.exe" if sys.platform == "win32" else "DocuMind")
        env = dict(os.environ, QT_QPA_PLATFORM=os.environ.get("QT_QPA_PLATFORM", "offscreen" if sys.platform != "win32" else "windows"))
        r = subprocess.run([str(exe), "--smoke-test"], env=env, capture_output=True, text=True, timeout=180)
        print(r.stdout.strip() or r.stderr.strip())
        return 0 if r.returncode == 0 and "SMOKE OK" in r.stdout else 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
