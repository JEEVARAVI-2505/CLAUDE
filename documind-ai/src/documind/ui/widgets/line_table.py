"""Editable line-item table with per-cell state colouring and row-level issue marking."""

from __future__ import annotations

from PySide6.QtCore import Qt, Signal
from PySide6.QtGui import QBrush, QColor
from PySide6.QtWidgets import QAbstractItemView, QHeaderView, QTableWidget, QTableWidgetItem

from ...domain.models import DocumentRecord, Severity
from ...domain.schemas import LINE_FIELDS
from .field_form import display_value


SHORT = {"description": "Description", "quantity": "Qty", "unit_price": "Price", "discount_pct": "Disc %", "discount_amount": "Disc",
         "tax_rate": "Tax %", "tax_amount": "Tax", "line_total": "Total"}


class LineTable(QTableWidget):
    cell_edited = Signal(int, str, str)
    jump = Signal(int)

    def __init__(self) -> None:
        super().__init__(0, len(LINE_FIELDS) + 1)
        self.setHorizontalHeaderLabels(["#"] + [SHORT.get(f.name, f.label) for f in LINE_FIELDS])
        self.verticalHeader().setVisible(False)
        self.setAlternatingRowColors(True)
        self.setSelectionBehavior(QAbstractItemView.SelectRows)
        self.horizontalHeader().setSectionResizeMode(QHeaderView.Interactive)
        self.horizontalHeader().setStretchLastSection(False)
        self.setColumnWidth(0, 36)
        for i, f in enumerate(LINE_FIELDS, 1):
            self.setColumnWidth(i, 150 if f.name == "description" else 68)
        self.horizontalHeader().setDefaultAlignment(Qt.AlignLeft | Qt.AlignVCenter)
        self.show_all = False
        self.itemChanged.connect(self._changed)
        self.cellDoubleClicked.connect(lambda r, c: None)
        self._busy = False
        self._editable = True
        self.setAccessibleName("Line items table")
        self.pages: dict[int, int] = {}
        self.colors = {"corrected": "#DFE9FF", "check": "#FFF0D0", "error": "#FDE2E2", "validated": "#DCF5E4"}
        self.itemSelectionChanged.connect(self._sel)

    def _sel(self) -> None:
        r = self.currentRow()
        if r in self.pages:
            self.jump.emit(self.pages[r])

    def set_colors(self, t: dict[str, str]) -> None:
        self.colors = {"corrected": t["info_soft"], "check": t["warning_soft"], "error": t["danger_soft"], "validated": t["success_soft"]}

    def set_record(self, rec: DocumentRecord | None, editable: bool) -> None:
        self._busy = True
        self._editable = editable
        optional = ("discount_pct", "discount_amount", "tax_rate", "tax_amount")
        for i, f in enumerate(LINE_FIELDS, 1):  # hide rarely used columns unless a row actually has a value in them
            used = rec is not None and any(it.get(f.name) is not None and it[f.name].value not in (None, "", "0") for it in rec.items)
            self.setColumnHidden(i, f.name in optional and not (self.show_all or used))
        self.setRowCount(0)
        self.pages = {}
        if rec:
            err_rows = {i.row for i in rec.issues if i.row is not None and i.severity == Severity.ERROR}
            warn_rows = {i.row for i in rec.issues if i.row is not None and i.severity == Severity.WARNING}
            self.setRowCount(len(rec.items))
            for r, item in enumerate(rec.items):
                n = QTableWidgetItem(str(r + 1)); n.setFlags(Qt.ItemIsEnabled | Qt.ItemIsSelectable)
                if r in err_rows:
                    n.setBackground(QBrush(QColor(self.colors["error"]))); n.setForeground(QBrush(QColor("#161A36"))); n.setToolTip("This line has an arithmetic error - see the Checks tab")
                elif r in warn_rows:
                    n.setBackground(QBrush(QColor(self.colors["check"]))); n.setForeground(QBrush(QColor("#161A36"))); n.setToolTip("Check this line - see the Checks tab")
                self.setItem(r, 0, n)
                for c, f in enumerate(LINE_FIELDS, 1):
                    fv = item.get(f.name)
                    it = QTableWidgetItem(display_value(fv.value if fv else None, f.type))
                    flags = Qt.ItemIsEnabled | Qt.ItemIsSelectable | (Qt.ItemIsEditable if editable else Qt.NoItemFlags)
                    it.setFlags(flags)
                    if f.type in ("money", "qty", "rate"):
                        it.setTextAlignment(Qt.AlignRight | Qt.AlignVCenter)
                    tint = None
                    if fv and fv.state == "corrected":
                        tint = self.colors["corrected"]
                    elif f.name == "line_total" and r in err_rows:
                        tint = self.colors["error"]
                    elif fv and any(x in fv.flags for x in ("ocr_repair", "alignment_uncertain", "partial_row", "ambiguous_number")):
                        tint = self.colors["check"]
                    if tint:
                        it.setBackground(QBrush(QColor(tint)))
                        it.setForeground(QBrush(QColor("#161A36")))
                    if fv and fv.raw and fv.raw != fv.value:
                        it.setToolTip(f"Source text: {fv.raw}")
                    self.setItem(r, c, it)
                    if fv and fv.page:
                        self.pages[r] = fv.page
        self._busy = False

    def _changed(self, it: QTableWidgetItem) -> None:
        if self._busy or it.column() == 0:
            return
        self.cell_edited.emit(it.row(), LINE_FIELDS[it.column() - 1].name, it.text())
