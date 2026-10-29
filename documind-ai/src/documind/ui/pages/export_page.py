"""Excel / CSV export with configurable columns."""

from __future__ import annotations

import os
import sys
from pathlib import Path

from PySide6.QtCore import QDate, Qt, QUrl
from PySide6.QtGui import QDesktopServices
from PySide6.QtWidgets import (QCheckBox, QComboBox, QDateEdit, QFileDialog, QHBoxLayout, QLabel, QListWidget, QListWidgetItem,
                               QPushButton, QVBoxLayout, QWidget)

from ...application.context import AppContext
from ...infrastructure.export_common import available_columns
from ..widgets.common import Card, label
from ..workers import run_async


class ExportPage(QWidget):
    def __init__(self, ctx: AppContext, win) -> None:
        super().__init__()
        self.setObjectName("Page")
        self.ctx, self.win = ctx, win
        lay = QVBoxLayout(self); lay.setContentsMargins(28, 22, 28, 22); lay.setSpacing(12)
        lay.addWidget(label("Export", "title"))
        lay.addWidget(label("Send reviewed data to Excel or CSV. By default only documents you approved are exported.", "muted"))
        row = QHBoxLayout(); row.setSpacing(16)
        c1 = Card(); c1.lay.addWidget(label("Columns", "h2"))
        self.cols = QListWidget(); self.cols.setAccessibleName("Columns to export (check to include)"); self.cols.setDragDropMode(QListWidget.InternalMove)
        c1.lay.addWidget(self.cols, 1)
        mv = QHBoxLayout(); up = QPushButton("Move up"); dn = QPushButton("Move down"); allb = QPushButton("Select all")
        up.clicked.connect(lambda: self._move(-1)); dn.clicked.connect(lambda: self._move(1)); allb.clicked.connect(self._all)
        for b in (up, dn, allb):
            mv.addWidget(b)
        c1.lay.addLayout(mv)
        row.addWidget(c1, 3)
        c2 = Card(); c2.lay.addWidget(label("Options", "h2"))
        self.fmt = QComboBox(); self.fmt.addItem("Excel workbook (.xlsx)", "xlsx"); self.fmt.addItem("CSV (.csv)", "csv"); self.fmt.setAccessibleName("Format")
        self.approved = QCheckBox("Only approved documents"); self.lines = QCheckBox("Include line items")
        self.use_dates = QCheckBox("Limit to document dates"); self.d1 = QDateEdit(QDate.currentDate().addMonths(-1)); self.d2 = QDateEdit(QDate.currentDate())
        for d in (self.d1, self.d2):
            d.setCalendarPopup(True); d.setDisplayFormat("yyyy-MM-dd"); d.setEnabled(False)
        self.use_dates.toggled.connect(lambda on: (self.d1.setEnabled(on), self.d2.setEnabled(on), self.update_count()))
        self.approved.toggled.connect(lambda _o: self.update_count())
        for w in (label("Format", "muted"), self.fmt, self.approved, self.lines, self.use_dates, self.d1, self.d2):
            c2.lay.addWidget(w)
        self.count = label("", "muted", wrap=True); c2.lay.addWidget(self.count)
        self.note = label("Text cells that start with = + - @ are stored as plain text so a spreadsheet can never run them as a formula.", "small", wrap=True)
        c2.lay.addWidget(self.note); c2.lay.addStretch()
        self.go = QPushButton("Export…"); self.go.setProperty("variant", "primary"); self.go.clicked.connect(lambda: self.export_ids(None))
        c2.lay.addWidget(self.go)
        row.addWidget(c2, 2)
        lay.addLayout(row, 1)
        self._built = False

    def refresh(self) -> None:
        ex = self.ctx.cfg.export
        if not self._built:
            self._built = True
            cat = available_columns(self.ctx.cfg.custom_fields)
            order = [c for c in ex.columns if c in cat] + [c for c in cat if c not in ex.columns]
            for k in order:
                it = QListWidgetItem(cat[k][0]); it.setData(Qt.UserRole, k)
                it.setFlags(it.flags() | Qt.ItemIsUserCheckable)
                it.setCheckState(Qt.Checked if k in ex.columns else Qt.Unchecked)
                self.cols.addItem(it)
            self.fmt.setCurrentIndex(self.fmt.findData(ex.format)); self.approved.setChecked(ex.approved_only); self.lines.setChecked(ex.include_line_items)
        self.update_count()

    def update_count(self) -> None:
        s = self.ctx.repo.stats(None)
        n = s["approved"] if self.approved.isChecked() else s["approved"] + s["needs_review"]
        self.count.setText(f"{n} document(s) available. " + ("" if self.approved.isChecked() else "Unreviewed documents are marked by their review status column."))

    def _move(self, d: int) -> None:
        r = self.cols.currentRow()
        if 0 <= r + d < self.cols.count():
            it = self.cols.takeItem(r); self.cols.insertItem(r + d, it); self.cols.setCurrentRow(r + d)

    def _all(self) -> None:
        for i in range(self.cols.count()):
            self.cols.item(i).setCheckState(Qt.Checked)

    def selected_columns(self) -> list[str]:
        return [self.cols.item(i).data(Qt.UserRole) for i in range(self.cols.count()) if self.cols.item(i).checkState() == Qt.Checked]

    def export_ids(self, ids: list[int] | None) -> None:
        cols = self.selected_columns()
        if not cols:
            self.win.notify("Choose at least one column."); return
        fmt = self.fmt.currentData()
        default = self.ctx.exports.default_path(fmt)
        path, _ = QFileDialog.getSaveFileName(self, "Save export", str(default), "Excel (*.xlsx)" if fmt == "xlsx" else "CSV (*.csv)")
        if not path:
            return
        if not path.lower().endswith("." + fmt):
            path += "." + fmt
        kwargs = dict(fmt=fmt, columns=cols, doc_ids=ids, approved_only=self.approved.isChecked(), include_lines=self.lines.isChecked(),
                      date_from=self.d1.date().toString("yyyy-MM-dd") if self.use_dates.isChecked() else None,
                      date_to=self.d2.date().toString("yyyy-MM-dd") if self.use_dates.isChecked() else None)
        self.go.setEnabled(False); self.win.notify("Exporting…")

        def done(res):
            self.go.setEnabled(True)
            msg = f"Exported {res.documents} document(s)" + (f" and {res.line_rows} line(s)" if res.line_rows else "") + "."
            if res.skipped_unapproved:
                msg += f" {res.skipped_unapproved} unapproved document(s) were left out."
            self.win.notify(msg)
            QDesktopServices.openUrl(QUrl.fromLocalFile(str(Path(res.paths[0]).parent)))

        def fail(m):
            self.go.setEnabled(True); self.win.error("Export failed", m)
        run_async(lambda: self.ctx.exports.export(Path(path), **kwargs), done, fail, parent=self)
