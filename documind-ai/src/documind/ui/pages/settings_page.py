"""Settings: appearance, OCR, AI, limits, exports, data/backup, templates & custom fields."""

from __future__ import annotations

from pathlib import Path

from PySide6.QtCore import QUrl
from PySide6.QtGui import QDesktopServices
from PySide6.QtWidgets import (QCheckBox, QComboBox, QFileDialog, QFormLayout, QHBoxLayout, QHeaderView, QLineEdit, QPushButton,
                               QScrollArea, QSpinBox, QTableWidget, QTableWidgetItem, QTabWidget, QVBoxLayout, QWidget)
from pydantic import ValidationError

from ...application.context import AppContext
from ...config import AppConfig
from ...domain.schemas import CustomFieldDef
from ...infrastructure.ollama_client import OllamaClient
from ..widgets.common import label
from ..widgets.data_tab import DataTab
from ..workers import run_async

AI_HELP = ("Optional. DocuMind can ask a model running on THIS computer through Ollama to help with unusual layouts.\n"
           "1. Install Ollama from ollama.com (free).   2. In a terminal:  ollama pull {model}   3. Tick 'Use local AI' and press Check.\n"
           "Models are large downloads (several GB) and have their own licences - review them before commercial use. Nothing is downloaded by DocuMind.")


def _form(parent: QWidget) -> QFormLayout:
    f = QFormLayout(parent)
    f.setLabelAlignment(f.labelAlignment())
    f.setVerticalSpacing(10)
    return f


class SettingsPage(QWidget):
    def __init__(self, ctx: AppContext, win) -> None:
        super().__init__()
        self.setObjectName("Page")
        self.ctx, self.win = ctx, win
        lay = QVBoxLayout(self); lay.setContentsMargins(28, 22, 28, 22); lay.setSpacing(10)
        lay.addWidget(label("Settings", "title"))
        self.tabs = QTabWidget(); lay.addWidget(self.tabs, 1)

        self._forms: list[QFormLayout] = []

        def tab(title: str) -> QFormLayout:
            w = QWidget(); sc = QScrollArea(); sc.setWidgetResizable(True); sc.setWidget(w)
            self.tabs.addTab(sc, title)
            f = _form(w)
            self._forms.append(f)
            return f

        # General
        f = tab("General")
        self.theme = QComboBox(); self.theme.addItem("Light", "light"); self.theme.addItem("Dark", "dark")
        self.date_order = QComboBox(); self.date_order.addItem("Day first (31/12/2025) - India, UK, EU", "DMY"); self.date_order.addItem("Month first (12/31/2025) - USA", "MDY")
        self.quiet = QCheckBox("I have confirmed this is right - stop warning about ambiguous dates such as 03/04/2025")
        self.tips = QCheckBox("Show short how-to tips at the top of each step")
        self.cur = QLineEdit(); self.cur.setPlaceholderText("e.g. INR - used only if a document shows no currency"); self.cur.setMaxLength(3)
        for k, w in (("Theme", self.theme), ("Date order", self.date_order), ("", self.quiet), ("Default currency", self.cur), ("", self.tips)):
            f.addRow(k, w)
        # OCR
        f = tab("OCR (scans)")
        self.ocr_mode = QComboBox(); self.ocr_mode.addItem("Automatic - only when a page has no text", "auto"); self.ocr_mode.addItem("Always OCR every page", "always"); self.ocr_mode.addItem("Off (digital PDFs only)", "off")
        self.ocr_langs = QLineEdit(); self.ocr_langs.setPlaceholderText("eng  or  eng+hin")
        self.ocr_path = QLineEdit(); self.ocr_path.setPlaceholderText("Leave empty to auto-detect")
        b_pick = QPushButton("Browse…"); b_pick.clicked.connect(self._pick_tess)
        rp = QHBoxLayout(); rp.addWidget(self.ocr_path, 1); rp.addWidget(b_pick)
        self.ocr_dpi = QSpinBox(); self.ocr_dpi.setRange(100, 400); self.ocr_dpi.setSuffix(" dpi")
        self.ocr_status = label("", "muted", wrap=True)
        b_chk = QPushButton("Check OCR installation"); b_chk.clicked.connect(self.check_ocr)
        for k, w in (("When to OCR", self.ocr_mode), ("Languages", self.ocr_langs), ("Tesseract program", rp), ("Render quality", self.ocr_dpi), ("", b_chk), ("", self.ocr_status)):
            f.addRow(k, w) if not isinstance(w, QHBoxLayout) else f.addRow(k, w)
        # AI
        f = tab("Local AI")
        self.ai_on = QCheckBox("Use local AI (Ollama) to help extraction"); self.ai_host = QLineEdit(); self.ai_model = QLineEdit()
        self.ai_timeout = QSpinBox(); self.ai_timeout.setRange(5, 1800); self.ai_timeout.setSuffix(" s")
        self.ai_remote = QCheckBox("Allow a server that is NOT on this computer (not recommended - documents would leave this PC)")
        self.ai_status = label("", "muted", wrap=True); b_ai = QPushButton("Check connection and model"); b_ai.clicked.connect(self.check_ai)
        self.ai_help = label(AI_HELP.format(model="qwen2.5:7b-instruct"), "small", wrap=True)
        for k, w in (("", self.ai_on), ("Server address", self.ai_host), ("Model name", self.ai_model), ("Timeout", self.ai_timeout), ("", self.ai_remote), ("", b_ai), ("", self.ai_status), ("", self.ai_help)):
            f.addRow(k, w)
        # Limits
        f = tab("Limits")
        self.l_mb = QSpinBox(); self.l_mb.setRange(1, 500); self.l_mb.setSuffix(" MB"); self.l_pages = QSpinBox(); self.l_pages.setRange(1, 1000)
        self.l_batch = QSpinBox(); self.l_batch.setRange(1, 5000); self.l_mp = QSpinBox(); self.l_mp.setRange(5, 500); self.l_mp.setSuffix(" megapixels")
        for k, w in (("Largest file", self.l_mb), ("Most pages per file", self.l_pages), ("Most files per import", self.l_batch), ("Largest image", self.l_mp)):
            f.addRow(k, w)
        # Export
        f = tab("Export defaults")
        self.x_fmt = QComboBox(); self.x_fmt.addItem("Excel (.xlsx)", "xlsx"); self.x_fmt.addItem("CSV (.csv)", "csv")
        self.x_delim = QComboBox(); self.x_delim.addItem("Comma ,", ","); self.x_delim.addItem("Semicolon ;", ";"); self.x_delim.addItem("Tab", "\t")
        self.x_appr = QCheckBox("Export only approved documents by default"); self.x_lines = QCheckBox("Include line items by default")
        self.x_dir = QLineEdit(); self.x_dir.setPlaceholderText("Default: <data folder>/exports")
        for k, w in (("Format", self.x_fmt), ("CSV separator", self.x_delim), ("", self.x_appr), ("", self.x_lines), ("Export folder", self.x_dir)):
            f.addRow(k, w)
        # Data
        self.data_tab = DataTab(ctx, win); self.tabs.addTab(self.data_tab, "Data && backup")
        # Templates
        w = QWidget(); self.tabs.addTab(w, "Templates && fields"); tl = QVBoxLayout(w)
        tl.addWidget(label("Templates teach DocuMind the labels of a particular supplier or layout (JSON files). Custom fields add extra columns found by keyword.", "muted", wrap=True))
        self.tpl = label("", "small", wrap=True); tl.addWidget(self.tpl)
        b_t = QPushButton("Open templates folder"); b_t.clicked.connect(lambda: QDesktopServices.openUrl(QUrl.fromLocalFile(str(ctx.paths.templates)))); tl.addWidget(b_t)
        tl.addWidget(label("Custom fields", "h2"))
        self.cf = QTableWidget(0, 4); self.cf.setHorizontalHeaderLabels(["Key (a-z, 0-9, _)", "Label", "Type (text/date/money)", "Keywords (comma separated)"])
        self.cf.horizontalHeader().setSectionResizeMode(QHeaderView.Stretch); self.cf.setAccessibleName("Custom fields")
        tl.addWidget(self.cf, 1)
        rr = QHBoxLayout(); a = QPushButton("Add field"); d = QPushButton("Remove selected")
        a.clicked.connect(lambda: self.cf.insertRow(self.cf.rowCount())); d.clicked.connect(lambda: self.cf.removeRow(self.cf.currentRow()))
        rr.addWidget(a); rr.addWidget(d); rr.addStretch(); tl.addLayout(rr)
        # footer
        foot = QHBoxLayout(); self.msg = label("", "muted", wrap=True)
        b_save = QPushButton("Save settings"); b_save.setProperty("variant", "primary"); b_save.clicked.connect(self.save)
        foot.addWidget(self.msg, 1); foot.addWidget(b_save); lay.addLayout(foot)
        self._name_inputs()
        self.load()

    def _name_inputs(self) -> None:
        """Give every input the text of its form label as accessible name (screen readers)."""
        for f in self._forms:
            for r in range(f.rowCount()):
                lab, fld = f.itemAt(r, QFormLayout.LabelRole), f.itemAt(r, QFormLayout.FieldRole)
                text = lab.widget().text() if lab is not None and lab.widget() is not None else ""
                if not text or fld is None:
                    continue
                targets = [fld.widget()] if fld.widget() is not None else [fld.layout().itemAt(i).widget() for i in range(fld.layout().count())]
                for w in targets:
                    if w is not None and not w.accessibleName():
                        w.setAccessibleName(text)

    # ---- load/save ------------------------------------------------------------------
    def refresh(self) -> None:
        self.data_tab.load()
        self.tpl.setText("Templates found: " + (", ".join(t.name for t in self.ctx.pack.templates) or "none") + f"\nYour templates go in: {self.ctx.paths.templates}")

    def load(self) -> None:
        c = self.ctx.cfg
        self.theme.setCurrentIndex(self.theme.findData(c.theme)); self.date_order.setCurrentIndex(self.date_order.findData(c.date_order))
        self.quiet.setChecked(c.suppress_ambiguous_date_warnings); self.cur.setText(c.default_currency or ""); self.tips.setChecked(c.show_tips)
        self.ocr_mode.setCurrentIndex(self.ocr_mode.findData(c.ocr.mode)); self.ocr_langs.setText(c.ocr.languages); self.ocr_path.setText(c.ocr.tesseract_path or "")
        self.ocr_dpi.setValue(c.ocr.dpi)
        self.ai_on.setChecked(c.ai.enabled); self.ai_host.setText(c.ai.host); self.ai_model.setText(c.ai.model); self.ai_timeout.setValue(c.ai.timeout_s)
        self.ai_remote.setChecked(c.ai.allow_remote_host)
        self.l_mb.setValue(c.limits.max_file_mb); self.l_pages.setValue(c.limits.max_pages); self.l_batch.setValue(c.limits.max_batch_files); self.l_mp.setValue(c.limits.max_image_megapixels)
        self.x_fmt.setCurrentIndex(self.x_fmt.findData(c.export.format)); self.x_delim.setCurrentIndex(self.x_delim.findData(c.export.csv_delimiter))
        self.x_appr.setChecked(c.export.approved_only); self.x_lines.setChecked(c.export.include_line_items); self.x_dir.setText(c.export.folder or "")
        self.cf.setRowCount(0)
        for cd in c.custom_fields:
            r = self.cf.rowCount(); self.cf.insertRow(r)
            for i, v in enumerate((cd.key, cd.label, cd.type, ", ".join(cd.keywords))):
                self.cf.setItem(r, i, QTableWidgetItem(v))
        self.data_tab.load()

    def collect(self) -> AppConfig:
        d = self.ctx.cfg.model_dump()
        d.update(theme=self.theme.currentData(), date_order=self.date_order.currentData(), suppress_ambiguous_date_warnings=self.quiet.isChecked(),
                 default_currency=(self.cur.text().strip().upper() or None), show_tips=self.tips.isChecked())
        d["ocr"].update(mode=self.ocr_mode.currentData(), languages=self.ocr_langs.text().strip() or "eng", tesseract_path=self.ocr_path.text().strip() or None, dpi=self.ocr_dpi.value())
        d["ai"].update(enabled=self.ai_on.isChecked(), host=self.ai_host.text().strip(), model=self.ai_model.text().strip(), timeout_s=self.ai_timeout.value(),
                       allow_remote_host=self.ai_remote.isChecked())
        d["limits"].update(max_file_mb=self.l_mb.value(), max_pages=self.l_pages.value(), max_batch_files=self.l_batch.value(), max_image_megapixels=self.l_mp.value())
        d["export"].update(format=self.x_fmt.currentData(), csv_delimiter=self.x_delim.currentData(), approved_only=self.x_appr.isChecked(),
                           include_line_items=self.x_lines.isChecked(), folder=self.x_dir.text().strip() or None)
        fields = []
        for r in range(self.cf.rowCount()):
            cells = [(self.cf.item(r, i).text().strip() if self.cf.item(r, i) else "") for i in range(4)]
            if not any(cells):
                continue
            fields.append(CustomFieldDef(key=cells[0], label=cells[1], type=cells[2] or "text", keywords=[k.strip() for k in cells[3].split(",") if k.strip()]).model_dump())
        d["custom_fields"] = fields
        new = AppConfig.model_validate(d)
        self.data_tab.apply_to(new)
        return new

    def save(self) -> None:
        try:
            new = self.collect()
            if new.ai.enabled:
                OllamaClient(new.ai)  # validates host policy
        except ValidationError as e:
            er = e.errors()[0]
            self.msg.setText("Please fix: " + ".".join(str(x) for x in er["loc"]) + " - " + er["msg"]); return
        except Exception as e:
            self.msg.setText(getattr(e, "user_message", str(e))); return
        restart = new.data_dir != self.ctx.cfg.data_dir or new.custom_fields != self.ctx.cfg.custom_fields
        self.ctx.update_config(new)
        self.win.apply_theme(new.theme)
        self.msg.setText("Saved." + (" Restart DocuMind for the data-folder / custom-field change to take effect." if restart else ""))
        self.win.notify("Settings saved")
        self.win.refresh_readiness()

    # ---- checks -------------------------------------------------------------------------
    def _pick_tess(self) -> None:
        f, _ = QFileDialog.getOpenFileName(self, "Locate tesseract", str(Path.home()), "Program (tesseract*);;All files (*)")
        if f:
            self.ocr_path.setText(f)

    def check_ocr(self) -> None:
        try:
            self.ctx.update_config(self.collect(), save=False)
        except Exception as e:
            self.ocr_status.setText(getattr(e, "user_message", str(e))); return
        self.ocr_status.setText("Checking…")

        def done(st):
            self.ocr_status.setText(f"{'Ready. ' if st.available else 'Problem: '}{st.message if not st.available else ''}" + (f"\n{st.version} · languages: {', '.join(st.languages)}" if st.version else "")
                                    + ("" if st.has_osd or not st.version else "\nTip: install the 'osd' data file to auto-rotate sideways scans."))
            self.win.refresh_readiness()
        run_async(lambda: self.ctx.ocr.status(refresh=True), done, lambda m: self.ocr_status.setText(m), parent=self)

    def check_ai(self) -> None:
        try:
            new = self.collect()
            client = OllamaClient(new.ai)
        except Exception as e:
            self.ai_status.setText(getattr(e, "user_message", str(e))); return
        self.ai_status.setText("Checking…")
        run_async(client.check, lambda st: self.ai_status.setText(("Ready. " if st.ready else "Problem: ") + (st.message if not st.ready else "") + (f"  (Ollama {st.version}; models: {', '.join(st.models) or 'none'})" if st.reachable else "")),
                  lambda m: self.ai_status.setText(m), parent=self)
