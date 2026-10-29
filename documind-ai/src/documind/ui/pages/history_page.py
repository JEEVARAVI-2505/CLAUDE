"""Searchable processing history with filters and permanent deletion."""

from __future__ import annotations

from PySide6.QtCore import QDate, Qt
from PySide6.QtWidgets import (QAbstractItemView, QCheckBox, QComboBox, QDateEdit, QHBoxLayout, QHeaderView, QLineEdit, QMessageBox,
                               QPushButton, QTableWidget, QTableWidgetItem, QVBoxLayout, QWidget)

from ...application.context import AppContext
from ...domain.exceptions import DocuMindError
from ...infrastructure.repositories import SearchFilter
from ..widgets.common import EmptyState, STATUS_CHIP, label

COLS = ["Imported", "File", "Type", "Supplier", "Number", "Doc date", "Total", "Status", "Checks"]


class HistoryPage(QWidget):
    def __init__(self, ctx: AppContext, win) -> None:
        super().__init__()
        self.setObjectName("Page")
        self.ctx, self.win = ctx, win
        lay = QVBoxLayout(self); lay.setContentsMargins(28, 22, 28, 22); lay.setSpacing(10)
        lay.addWidget(label("History", "title"))
        f = QHBoxLayout()
        self.q = QLineEdit(); self.q.setPlaceholderText("Search file name, supplier or invoice number…"); self.q.setClearButtonEnabled(True)
        self.q.setAccessibleName("Search history"); self.q.textChanged.connect(lambda _t: self.refresh())
        self.status = QComboBox(); self.status.setAccessibleName("Filter by status")
        self.status.addItem("Any status", "")
        for k, (t, _c) in STATUS_CHIP.items():
            self.status.addItem(t, k)
        self.kind = QComboBox(); self.kind.setAccessibleName("Filter by document type")
        for t, v in (("Any type", ""), ("Invoice", "invoice"), ("Purchase order", "purchase_order"), ("Credit note", "credit_note"), ("Unknown", "unknown")):
            self.kind.addItem(t, v)
        self.use_dates = QCheckBox("Date range")
        self.d1 = QDateEdit(QDate.currentDate().addMonths(-1)); self.d2 = QDateEdit(QDate.currentDate())
        for d in (self.d1, self.d2):
            d.setCalendarPopup(True); d.setDisplayFormat("yyyy-MM-dd"); d.setEnabled(False)
            d.dateChanged.connect(lambda _d: self.refresh())
        self.d1.setAccessibleName("From date"); self.d2.setAccessibleName("To date")
        self.use_dates.toggled.connect(lambda on: (self.d1.setEnabled(on), self.d2.setEnabled(on), self.refresh()))
        for c in (self.status, self.kind):
            c.currentIndexChanged.connect(lambda _i: self.refresh())
        f.addWidget(self.q, 1)
        for w in (self.status, self.kind, self.use_dates, self.d1, label("to", "muted"), self.d2):
            f.addWidget(w)
        lay.addLayout(f)
        self.table = QTableWidget(0, len(COLS)); self.table.setHorizontalHeaderLabels(COLS)
        self.table.verticalHeader().setVisible(False); self.table.setSelectionBehavior(QAbstractItemView.SelectRows)
        self.table.setEditTriggers(QAbstractItemView.NoEditTriggers); self.table.setAlternatingRowColors(True)
        self.table.horizontalHeader().setSectionResizeMode(1, QHeaderView.Stretch); self.table.horizontalHeader().setSectionResizeMode(3, QHeaderView.Stretch)
        self.table.setAccessibleName("Processing history"); self.table.horizontalHeader().setDefaultAlignment(Qt.AlignLeft | Qt.AlignVCenter)
        self.table.itemDoubleClicked.connect(lambda _i: self.open())
        lay.addWidget(self.table, 1)
        self.empty = EmptyState("No matching documents", "Documents you process are kept here until you delete them or the retention policy removes them.")
        lay.addWidget(self.empty)
        bar = QHBoxLayout()
        self.count = label("", "muted")
        b_open = QPushButton("Open in Review"); b_open.setProperty("variant", "primary"); b_open.clicked.connect(self.open)
        b_exp = QPushButton("Export selected…"); b_exp.clicked.connect(self.export_selected)
        b_del = QPushButton("Delete permanently…"); b_del.setProperty("variant", "danger"); b_del.clicked.connect(self.delete)
        bar.addWidget(self.count, 1)
        for b in (b_exp, b_del, b_open):
            bar.addWidget(b)
        lay.addLayout(bar)

    def refresh(self) -> None:
        flt = SearchFilter(text=self.q.text(), status=self.status.currentData() or None, kind=self.kind.currentData() or None, limit=2000,
                           date_from=self.d1.date().toString("yyyy-MM-dd") if self.use_dates.isChecked() else None,
                           date_to=self.d2.date().toString("yyyy-MM-dd") if self.use_dates.isChecked() else None)
        rows = self.ctx.repo.search(flt)
        self.table.setRowCount(len(rows))
        for r, s in enumerate(rows):
            vals = [s.imported_at[:16].replace("T", " "), s.file_name, s.kind.replace("_", " "), s.supplier or "", s.doc_number or "", s.doc_date or "",
                    f"{s.total or ''} {s.currency or ''}".strip(), STATUS_CHIP.get(s.status, (s.status,))[0],
                    ", ".join(x for x in (f"{s.errors} error(s)" if s.errors else "", f"{s.warnings} warning(s)" if s.warnings else "") if x) or ("-" if s.status == "failed" else "OK")]
            for c, v in enumerate(vals):
                it = QTableWidgetItem(v)
                if c == 6:
                    it.setTextAlignment(Qt.AlignRight | Qt.AlignVCenter)
                self.table.setItem(r, c, it)
            self.table.item(r, 0).setData(Qt.UserRole, s.id)
        self.count.setText(f"{len(rows)} document(s)")
        self.empty.setVisible(not rows); self.table.setVisible(bool(rows))

    def _ids(self) -> list[int]:
        return [self.table.item(i.row(), 0).data(Qt.UserRole) for i in self.table.selectionModel().selectedRows()]

    def open(self) -> None:
        ids = self._ids()
        if ids:
            self.win.open_review(ids[0])

    def export_selected(self) -> None:
        ids = self._ids()
        if not ids:
            self.win.notify("Select one or more documents first."); return
        self.win.pages["export"].export_ids(ids)

    def delete(self) -> None:
        ids = self._ids()
        if not ids:
            self.win.notify("Select one or more documents to delete."); return
        box = QMessageBox(self); box.setIcon(QMessageBox.Warning); box.setWindowTitle("Delete permanently")
        box.setText(f"Permanently delete {len(ids)} document(s)?")
        box.setInformativeText("The stored original file is overwritten and removed, and all extracted data, corrections and history for it are erased from the "
                               "database. Exported files and backups you made earlier are NOT touched. This cannot be undone.")
        yes = box.addButton("Delete permanently", QMessageBox.DestructiveRole); box.addButton("Cancel", QMessageBox.RejectRole)
        box.exec()
        if box.clickedButton() is not yes:
            return
        failed = 0
        for i in ids:
            try:
                self.ctx.docs.delete_permanently(i)
            except DocuMindError:
                failed += 1
        self.win.notify(f"Deleted {len(ids) - failed} document(s)." + (f" {failed} could not be deleted." if failed else ""))
        self.refresh()
