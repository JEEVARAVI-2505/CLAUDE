"""First-run setup wizard: privacy, data folder, OCR check, optional AI, demo."""

from __future__ import annotations

from PySide6.QtCore import Qt
from PySide6.QtWidgets import (QCheckBox, QDialog, QHBoxLayout, QLineEdit, QPushButton, QRadioButton, QStackedWidget, QVBoxLayout, QWidget)

from ...application.context import AppContext
from ...infrastructure.ollama_client import OllamaClient
from ..icons import app_icon, logo_pixmap
from ..widgets.common import Chip, label
from ..workers import run_async


class SetupWizard(QDialog):
    def __init__(self, ctx: AppContext, win=None) -> None:
        super().__init__(win)
        self.ctx, self.win = ctx, win
        self.setWindowTitle("Welcome to DocuMind AI Professional"); self.setWindowIcon(app_icon()); self.setModal(True); self.setMinimumSize(640, 460)
        lay = QVBoxLayout(self); lay.setContentsMargins(28, 24, 28, 20)
        self.stack = QStackedWidget(); lay.addWidget(self.stack, 1)
        self.dots = label("", "muted"); self.dots.setAlignment(Qt.AlignCenter); lay.addWidget(self.dots)
        nav = QHBoxLayout(); self.back = QPushButton("Back"); self.next = QPushButton("Next"); self.next.setProperty("variant", "primary")
        self.skip = QPushButton("Skip setup"); self.skip.setProperty("variant", "link")
        self.back.clicked.connect(lambda: self.go(-1)); self.next.clicked.connect(lambda: self.go(1)); self.skip.clicked.connect(self.finish)
        nav.addWidget(self.skip); nav.addStretch(); nav.addWidget(self.back); nav.addWidget(self.next); lay.addLayout(nav)
        self.want_demo = QCheckBox("Load a few fictional demo invoices so I can practise"); self.want_demo.setChecked(True)
        self.ai_enable = QCheckBox("Use a local AI model (I have installed Ollama and a model)"); self.ai_model = QLineEdit(ctx.cfg.ai.model)

        def page(*widgets: QWidget) -> QWidget:
            w = QWidget(); l = QVBoxLayout(w); l.setSpacing(10)
            for x in widgets:
                l.addWidget(x)
            l.addStretch(); self.stack.addWidget(w); return w

        logo = label(""); logo.setPixmap(logo_pixmap(56)); logo.setAlignment(Qt.AlignLeft)
        t = label("Welcome to DocuMind AI", "title")
        d = label("Read invoices and purchase orders into checked, export-ready data, privately on this computer.\n\n"
                  "• Your documents never leave this PC. There are no accounts, no telemetry and no paid AI service.\n"
                  "• Software reads the documents; people approve them. Every value can be corrected and every change is logged.\n"
                  "• Works without AI. A local AI model is an optional extra.", "muted", wrap=True)
        page(logo, t, d)
        self.dir_lbl = label(f"Data folder:\n{ctx.paths.root}", "muted", wrap=True)
        page(label("Where your data is kept", "h2"), self.dir_lbl,
             label("Your documents, the database, logs and backups are stored here. Anyone who can use this Windows account can read them, so protect the account "
                   "(and consider BitLocker). You can change the folder later in Settings › Data & backup, and make backups there.", "muted", wrap=True))
        self.dmy = QRadioButton("Day first: 03/04/2025 means 3 April 2025   (India, UK, Europe, most countries)")
        self.mdy = QRadioButton("Month first: 03/04/2025 means March 4, 2025   (USA)")
        (self.dmy if ctx.cfg.date_order == "DMY" else self.mdy).setChecked(True)
        page(label("How are dates written on your invoices?", "h2"),
             label("Many invoices write dates like 03/04/2025, which could mean two different days. Tell DocuMind once and it will stop asking you to "
                   "confirm every date. If a date is clearly in the other style (for example 25/04/2025), DocuMind still reads it correctly.", "muted", wrap=True),
             self.dmy, self.mdy, label("You can change this later in Settings > General.", "small"))
        self.ocr_chip = Chip("Checking…", "neutral"); self.ocr_msg = label("", "muted", wrap=True)
        b = QPushButton("Check again"); b.clicked.connect(self.check_ocr)
        page(label("Scanned documents (OCR)", "h2"), self.ocr_chip, self.ocr_msg, b,
             label("Digital PDFs work without OCR. For scans and photos install Tesseract (Help › Install OCR). You can do this any time.", "small", wrap=True))
        self.ai_chip = Chip("Not enabled", "neutral"); self.ai_msg = label("", "muted", wrap=True)
        b2 = QPushButton("Check Ollama and model"); b2.clicked.connect(self.check_ai)
        page(label("Local AI (optional)", "h2"), self.ai_enable, self.ai_model, b2, self.ai_chip, self.ai_msg,
             label("Skipping this is fine - rules-only mode extracts typical invoices and purchase orders without AI.", "small", wrap=True))
        page(label("Setup complete", "title"), label("The whole job is three steps: 1 Add documents, 2 Check and approve, 3 Export to Excel. Press F1 any time for help.", "muted", wrap=True), self.want_demo)
        self.i = 0
        self.update_nav()
        self.check_ocr()

    def update_nav(self) -> None:
        n = self.stack.count()
        self.stack.setCurrentIndex(self.i)
        self.back.setEnabled(self.i > 0)
        self.next.setText("Finish" if self.i == n - 1 else "Next")
        self.dots.setText(f"Step {self.i + 1} of {n}")

    def go(self, d: int) -> None:
        if d > 0 and self.i == self.stack.count() - 1:
            self.finish(); return
        self.i = max(0, min(self.stack.count() - 1, self.i + d))
        self.update_nav()

    def check_ocr(self) -> None:
        self.ocr_chip.set("Checking…", "neutral")

        def done(st):
            self.ocr_chip.set("OCR ready" if st.available else "OCR not installed", "success" if st.available else "warning")
            self.ocr_msg.setText(st.message)
        run_async(lambda: self.ctx.ocr.status(refresh=True), done, lambda m: self.ocr_msg.setText(m), parent=self)

    def check_ai(self) -> None:
        cfg = self.ctx.cfg.model_copy(deep=True); cfg.ai.model = self.ai_model.text().strip() or cfg.ai.model
        try:
            client = OllamaClient(cfg.ai)
        except Exception as e:
            self.ai_msg.setText(getattr(e, "user_message", str(e))); return
        self.ai_chip.set("Checking…", "neutral")
        run_async(client.check, lambda st: (self.ai_chip.set("AI ready" if st.ready else "Not ready", "success" if st.ready else "warning"), self.ai_msg.setText(st.message)),
                  lambda m: self.ai_msg.setText(m), parent=self)

    def finish(self) -> None:
        cfg = self.ctx.cfg.model_copy(deep=True)
        cfg.first_run_done = True
        if self.stack.currentIndex() >= 2 or self.i >= 2:  # they reached the date question
            cfg.date_order = "DMY" if self.dmy.isChecked() else "MDY"
            cfg.suppress_ambiguous_date_warnings = True
        if self.ai_enable.isChecked():
            cfg.ai.enabled = True
            cfg.ai.model = self.ai_model.text().strip() or cfg.ai.model
        self.ctx.update_config(cfg)
        self.accept()
        if self.win:
            self.win.refresh_readiness()
            if self.want_demo.isChecked() and self.stack.currentIndex() == self.stack.count() - 1:
                self.win.load_demo()
