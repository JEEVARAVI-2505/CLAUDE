"""Step 3: export approved documents to Excel / CSV, by what the person wants and for which period."""

from __future__ import annotations

from datetime import date, timedelta
from pathlib import Path

from PySide6.QtCore import QDate, Qt, QUrl
from PySide6.QtGui import QDesktopServices
from PySide6.QtWidgets import (QCheckBox, QComboBox, QDateEdit, QFileDialog, QGroupBox, QHBoxLayout, QListWidget, QListWidgetItem,
                               QPushButton, QVBoxLayout, QWidget)

from ...application.context import AppContext
from ...infrastructure.export_common import PRESETS, available_columns
from ..widgets.common import Card, label
from ..widgets.guide import GuideBar
from ..workers import run_async


def month_bounds(offset: int) -> tuple[str, str]:
    today = date.today()
    first = today.replace(day=1)
    if offset:
        first = (first - timedelta(days=1)).replace(day=1)
        last = first.replace(day=28) + timedelta(days=4)
        last = last - timedelta(days=last.day)
        return first.isoformat(), last.isoformat()
    nxt = (first.replace(day=28) + timedelta(days=4)).replace(day=1)
    return first.isoformat(), (nxt - timedelta(days=1)).isoformat()


class ExportPage(QWidget):
    def __init__(self, ctx: AppContext, win) -> None:
        super().__init__()
        self.setObjectName("Page")
        self.ctx, self.win = ctx, win
        lay = QVBoxLayout(self); lay.setContentsMargins(24, 20, 24, 20); lay.setSpacing(12)
        lay.addWidget(label("Export to Excel", "title"))
        self.guide = GuideBar(ctx, "export"); lay.addWidget(self.guide)

        c1 = Card(title="1. What do you want?")
        self.preset = QComboBox(); self.preset.setAccessibleName("What do you want to export")
        for k, (name, _d, _c, _l, _s) in PRESETS.items():
            self.preset.addItem(name, k)
        self.preset.addItem("Choose my own columns", "custom")
        self.preset.currentIndexChanged.connect(self._preset_changed)
        self.desc = label("", "muted", wrap=True)
        c1.lay.addWidget(self.preset); c1.lay.addWidget(self.desc)
        lay.addWidget(c1)

        c2 = Card(title="2. Which period? (by the date on the document)")
        self.period = QComboBox(); self.period.setAccessibleName("Period")
        for t, v in (("All approved documents", "all"), ("This month", "this"), ("Last month", "last"), ("I will choose the dates", "custom")):
            self.period.addItem(t, v)
        self.d1 = QDateEdit(QDate.currentDate().addMonths(-1)); self.d2 = QDateEdit(QDate.currentDate())
        for d in (self.d1, self.d2):
            d.setCalendarPopup(True); d.setDisplayFormat("yyyy-MM-dd"); d.setVisible(False)
        self.d1.setAccessibleName("From date"); self.d2.setAccessibleName("To date")
        self.period.currentIndexChanged.connect(self._period_changed)
        self.to_lbl = label("to", "muted"); self.to_lbl.setVisible(False)
        self.fmt = QComboBox(); self.fmt.addItem("Excel file (.xlsx)", "xlsx"); self.fmt.addItem("CSV file (.csv)", "csv"); self.fmt.setAccessibleName("File type")
        self.approved = QCheckBox("Only documents I approved"); self.lines = QCheckBox("Include a sheet with line items")
        row = QHBoxLayout(); row.addWidget(self.period); row.addWidget(self.d1); row.addWidget(self.to_lbl); row.addWidget(self.d2); row.addSpacing(16)
        row.addWidget(label("File type", "muted")); row.addWidget(self.fmt); row.addStretch()
        c2.lay.addLayout(row)
        opt = QHBoxLayout(); opt.addWidget(self.approved); opt.addWidget(self.lines); opt.addStretch(); c2.lay.addLayout(opt)
        self.count = label("", "muted", wrap=True); c2.lay.addWidget(self.count)
        lay.addWidget(c2)

        self.adv = QGroupBox("Advanced: choose and reorder the columns"); self.adv.setCheckable(True); self.adv.setChecked(False)
        al = QVBoxLayout(self.adv)
        self.cols = QListWidget(); self.cols.setAccessibleName("Columns to export (tick to include)"); self.cols.setDragDropMode(QListWidget.InternalMove)
        self.cols.setMaximumHeight(170)
        al.addWidget(self.cols)
        mv = QHBoxLayout(); up = QPushButton("Move up"); dn = QPushButton("Move down")
        up.clicked.connect(lambda: self._move(-1)); dn.clicked.connect(lambda: self._move(1))
        for w in (up, dn):
            mv.addWidget(w)
        mv.addStretch(); al.addLayout(mv)
        self.adv.toggled.connect(lambda on: [w.setVisible(on) for w in (self.cols,)])
        lay.addWidget(self.adv)

        self.note = label("Dates and amounts are real Excel dates and numbers. Text that starts with = + - or @ is saved as plain text, so Excel can never run it as a formula.", "small", wrap=True)
        lay.addWidget(self.note)
        lay.addStretch()
        foot = QHBoxLayout(); foot.addStretch()
        self.go = QPushButton("Create the Excel file…"); self.go.setProperty("variant", "primary"); self.go.setMinimumHeight(38)
        self.go.clicked.connect(lambda: self.export_ids(None)); foot.addWidget(self.go)
        lay.addLayout(foot)
        self._built = False

    # ------------------------------------------------------------------------------------
    def refresh(self) -> None:
        ex = self.ctx.cfg.export
        self.guide.refresh()
        if not self._built:
            self._built = True
            cat = available_columns(self.ctx.cfg.custom_fields)
            for k in cat:
                it = QListWidgetItem(cat[k][0]); it.setData(Qt.UserRole, k)
                it.setFlags(it.flags() | Qt.ItemIsUserCheckable); it.setCheckState(Qt.Unchecked)
                self.cols.addItem(it)
            self.fmt.setCurrentIndex(self.fmt.findData(ex.format)); self.approved.setChecked(ex.approved_only); self.lines.setChecked(ex.include_line_items)
            self.cols.setVisible(False)
            self._preset_changed()
        self.update_count()

    def _preset_changed(self) -> None:
        key = self.preset.currentData()
        if key == "custom":
            self.desc.setText("Open 'Columns' below and tick what you need.")
            self.adv.setChecked(True)
            self.cols.setVisible(True)
            return
        name, desc, cols, lines, summary = PRESETS[key]
        self.desc.setText(desc)
        self._set_columns(cols)
        self.lines.setChecked(lines)

    def _set_columns(self, cols: list[str]) -> None:
        cat = available_columns(self.ctx.cfg.custom_fields)
        taken = [self.cols.takeItem(0) for _ in range(self.cols.count())]
        items = {it.data(Qt.UserRole): it for it in taken}
        for k in cols + [k for k in cat if k not in cols]:
            it = items.get(k)
            if it is None:
                continue
            it.setCheckState(Qt.Checked if k in cols else Qt.Unchecked)
            self.cols.addItem(it)

    def _period_changed(self) -> None:
        custom = self.period.currentData() == "custom"
        self.d1.setVisible(custom); self.d2.setVisible(custom); self.to_lbl.setVisible(custom)
        self.update_count()

    def period_range(self) -> tuple[str | None, str | None]:
        v = self.period.currentData()
        if v == "this":
            return month_bounds(0)
        if v == "last":
            return month_bounds(1)
        if v == "custom":
            return self.d1.date().toString("yyyy-MM-dd"), self.d2.date().toString("yyyy-MM-dd")
        return None, None

    def update_count(self) -> None:
        from ...infrastructure.repositories import SearchFilter
        a, b = self.period_range()
        n = len(self.ctx.repo.search(SearchFilter(status="approved", date_from=a, date_to=b, limit=5000)))
        waiting = self.ctx.repo.stats(None)["needs_review"]
        msg = f"{n} approved document(s) match." if n else "No approved documents match yet."
        if waiting:
            msg += f"  {waiting} more are still waiting for your check (step 2) and are not included."
        self.count.setText(msg)
        self.go.setEnabled(n > 0 or self.preset.currentData() == "custom")

    def _move(self, d: int) -> None:
        r = self.cols.currentRow()
        if 0 <= r + d < self.cols.count():
            it = self.cols.takeItem(r); self.cols.insertItem(r + d, it); self.cols.setCurrentRow(r + d)

    def selected_columns(self) -> list[str]:
        return [self.cols.item(i).data(Qt.UserRole) for i in range(self.cols.count()) if self.cols.item(i).checkState() == Qt.Checked]

    def export_ids(self, ids: list[int] | None) -> None:
        cols = self.selected_columns()
        if not cols:
            self.win.notify("Choose at least one column."); return
        key = self.preset.currentData()
        fmt = self.fmt.currentData()
        default = self.ctx.exports.default_path(fmt)
        path, _ = QFileDialog.getSaveFileName(self, "Save the file", str(default), "Excel (*.xlsx)" if fmt == "xlsx" else "CSV (*.csv)")
        if not path:
            return
        if not path.lower().endswith("." + fmt):
            path += "." + fmt
        a, b = self.period_range()
        kwargs = dict(fmt=fmt, columns=cols, doc_ids=ids, approved_only=self.approved.isChecked() or ids is None, include_lines=self.lines.isChecked(),
                      date_from=a, date_to=b, preset=None if key == "custom" else key)
        self.go.setEnabled(False); self.win.notify("Creating the file…")

        def done(res):
            self.go.setEnabled(True)
            msg = f"Saved {res.documents} document(s)" + (f" and {res.line_rows} line item(s)" if res.line_rows else "") + "."
            if res.skipped_unapproved:
                msg += f" {res.skipped_unapproved} not-yet-approved document(s) were left out."
            self.win.notify(msg)
            QDesktopServices.openUrl(QUrl.fromLocalFile(str(Path(res.paths[0]).parent)))

        def fail(m):
            self.go.setEnabled(True); self.win.error("The file could not be created", m)
        run_async(lambda: self.ctx.exports.export(Path(path), **kwargs), done, fail, parent=self)
