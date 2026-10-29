# PyInstaller spec - lean one-folder build (no UPX: it triggers antivirus false positives).
# Build:  pyinstaller --noconfirm --clean documind.spec
import sys
from pathlib import Path

ROOT = Path(SPECPATH)
SRC = ROOT / "src"

# Qt modules DocuMind never imports. Excluding them keeps the Qt DLLs (and their plugins) out of the package.
QT_UNUSED = [
    "PySide6.QtNetwork", "PySide6.QtQml", "PySide6.QtQuick", "PySide6.QtQuickWidgets", "PySide6.QtQuickControls2", "PySide6.QtOpenGL",
    "PySide6.QtOpenGLWidgets", "PySide6.QtPdf", "PySide6.QtPdfWidgets", "PySide6.QtSvg", "PySide6.QtSvgWidgets", "PySide6.QtSql",
    "PySide6.QtTest", "PySide6.QtUiTools", "PySide6.QtXml", "PySide6.QtConcurrent", "PySide6.QtDBus", "PySide6.QtHelp", "PySide6.QtDesigner",
    "PySide6.QtPrintSupport", "PySide6.QtWebChannel", "PySide6.QtWebSockets", "PySide6.QtMultimedia", "PySide6.Qt3DCore",
]
PY_UNUSED = ["tkinter", "unittest", "pydoc", "doctest", "lib2to3", "distutils", "setuptools", "pip", "test", "xmlrpc", "curses", "sqlite3.test",
             "numpy", "matplotlib", "IPython", "pytest", "PIL.ImageTk", "PIL.ImageQt"]

a = Analysis(
    [str(SRC / "launcher.py")],
    pathex=[str(SRC)],
    datas=[(str(SRC / "documind" / "resources"), "documind/resources"),
           (str(SRC / "documind" / "infrastructure" / "migrations"), "documind/infrastructure/migrations")],
    hiddenimports=["documind.ui.pages.dashboard", "documind.ui.pages.import_page", "documind.ui.pages.review_page", "documind.ui.pages.history_page",
                   "documind.ui.pages.export_page", "documind.ui.pages.settings_page", "documind.ui.pages.help_page"],
    excludes=QT_UNUSED + PY_UNUSED,
    noarchive=False,
    optimize=2,
)
pyz = PYZ(a.pure)
exe = EXE(pyz, a.scripts, [], exclude_binaries=True, name="DocuMind", console=False, icon=str(ROOT / "installer" / "documind.ico") if (ROOT / "installer" / "documind.ico").exists() else None,
          upx=False, version=str(ROOT / "installer" / "version_info.txt") if (ROOT / "installer" / "version_info.txt").exists() else None)
coll = COLLECT(exe, a.binaries, a.datas, strip=sys.platform != "win32", upx=False, name="DocuMind")
