"""Help, troubleshooting, diagnostics, changelog and About."""

from __future__ import annotations

from pathlib import Path

from PySide6.QtWidgets import (QFileDialog, QHBoxLayout, QPlainTextEdit, QPushButton, QTabWidget, QTextBrowser, QVBoxLayout, QWidget)

from ... import APP_NAME, __version__
from ...application.context import AppContext
from ...resource import resource_path
from ..widgets.common import label
from ..workers import run_async

PAGES = [("Quick start", "quickstart.md"), ("Install OCR", "install_ocr.md"), ("Local AI", "install_ai.md"),
         ("Troubleshooting", "troubleshooting.md"), ("Privacy", "privacy.md"), ("Changelog", "changelog.md"), ("About", "about.md")]


class HelpPage(QWidget):
    def __init__(self, ctx: AppContext, win) -> None:
        super().__init__()
        self.setObjectName("Page")
        self.ctx, self.win = ctx, win
        lay = QVBoxLayout(self); lay.setContentsMargins(28, 22, 28, 22); lay.setSpacing(10)
        head = QHBoxLayout()
        head.addWidget(label("Help & About", "title")); head.addStretch()
        head.addWidget(label(f"{APP_NAME}  ·  version {__version__}", "muted"))
        lay.addLayout(head)
        self.tabs = QTabWidget(); lay.addWidget(self.tabs, 1)
        for title, fn in PAGES:
            tb = QTextBrowser(); tb.setOpenExternalLinks(False); tb.setAccessibleName(title)
            p = resource_path("help", fn)
            tb.setMarkdown(p.read_text(encoding="utf-8") if p.exists() else f"# {title}\n\n(content not found)")
            self.tabs.addTab(tb, title)
        d = QWidget(); dl = QVBoxLayout(d)
        dl.addWidget(label("Diagnostics contain versions, settings and a redacted log tail - never document text or extracted values. Read before sharing.", "muted", wrap=True))
        self.diag = QPlainTextEdit(); self.diag.setReadOnly(True); self.diag.setAccessibleName("Diagnostics report"); dl.addWidget(self.diag, 1)
        row = QHBoxLayout()
        b1 = QPushButton("Generate report"); b1.clicked.connect(self.generate)
        b2 = QPushButton("Save report to file…"); b2.setProperty("variant", "primary"); b2.clicked.connect(self.save)
        b3 = QPushButton("Run setup wizard again"); b3.clicked.connect(self.wizard)
        for b in (b1, b2, b3):
            row.addWidget(b)
        row.addStretch(); dl.addLayout(row)
        self.tabs.addTab(d, "Diagnostics")

    def generate(self) -> None:
        self.diag.setPlainText("Collecting…")
        run_async(self.ctx.diagnostics.report, self.diag.setPlainText, lambda m: self.diag.setPlainText(m), parent=self)

    def save(self) -> None:
        if not self.diag.toPlainText().strip() or self.diag.toPlainText() == "Collecting…":
            self.generate()
            self.win.notify("Report generated - review it, then save."); return
        f, _ = QFileDialog.getSaveFileName(self, "Save diagnostics", str(Path.home() / "documind-diagnostics.txt"), "Text (*.txt)")
        if f:
            try:
                Path(f).write_text(self.diag.toPlainText(), encoding="utf-8")
                self.win.notify("Diagnostics saved.")
            except OSError as e:
                self.win.error("Save failed", str(e))

    def wizard(self) -> None:
        from ..dialogs.wizard import SetupWizard
        SetupWizard(self.ctx, self.win).exec()

    def refresh(self) -> None:
        if not self.diag.toPlainText():
            self.generate()
