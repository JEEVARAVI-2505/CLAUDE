"""Editable header-field rows showing extracted / corrected / validated state and provenance."""

from __future__ import annotations

from decimal import Decimal
from typing import Callable

from PySide6.QtCore import Qt, Signal
from PySide6.QtWidgets import QGridLayout, QHBoxLayout, QLabel, QLineEdit, QPushButton, QScrollArea, QToolButton, QVBoxLayout, QWidget

from ...domain.models import DocumentRecord, FieldValue
from ...domain.money import to_decimal
from ...domain.schemas import SCALAR_FIELDS, CustomFieldDef
from .common import Chip, label, repolish

STATE_CHIP = {"extracted": ("Read by software", "info"), "corrected": ("You changed it", "primary"), "validated": ("You approved it", "success"),
              "missing": ("Not found", "neutral")}


def display_value(value: str | None, ftype: str) -> str:
    """Show money with two decimals (storage keeps the canonical form)."""
    if value is None:
        return ""
    if ftype == "money":
        d = to_decimal(value)
        if d is not None and d == d.quantize(Decimal("0.01")):
            return f"{d:.2f}"
    return value


class FieldRow(QWidget):
    def __init__(self, name: str, text: str, hint: str, ftype: str, on_edit: Callable[[str, str], None],
                 on_revert: Callable[[str], None], on_page: Callable[[int], None]) -> None:
        super().__init__()
        self.name, self.ftype, self._on_edit, self._on_page = name, ftype, on_edit, on_page
        self._orig = ""
        self._page: int | None = None
        v = QVBoxLayout(self); v.setContentsMargins(0, 4, 0, 4); v.setSpacing(3)
        head = QHBoxLayout(); head.setSpacing(6)
        self.lbl = label(text, "muted")
        self.chip = Chip("Not found", "neutral"); self.chip.setMinimumWidth(104)
        self.page = QToolButton(); self.page.setText("p.–"); self.page.setToolTip("Show the page this value came from")
        self.page.clicked.connect(lambda: self._page and on_page(self._page))
        self.rev = QToolButton(); self.rev.setText("Undo my change"); self.rev.setToolTip("Go back to what the software read"); self.rev.setAccessibleName(f"Revert {text}")
        self.rev.clicked.connect(lambda: on_revert(name))
        head.addWidget(self.lbl, 1); head.addWidget(self.rev); head.addWidget(self.page); head.addWidget(self.chip)
        self.edit = QLineEdit(); self.edit.setPlaceholderText(hint); self.edit.setAccessibleName(text)
        self.edit.editingFinished.connect(self._finished)
        self.note = label("", "small", wrap=True)
        v.addLayout(head); v.addWidget(self.edit); v.addWidget(self.note)

    def _finished(self) -> None:
        if self.edit.text().strip() != self._orig:
            self._on_edit(self.name, self.edit.text())

    def set_value(self, fv: FieldValue | None, editable: bool) -> None:
        value = display_value(fv.value if fv else None, self.ftype)
        self._orig = value
        self.edit.blockSignals(True); self.edit.setText(value); self.edit.blockSignals(False)
        self.edit.setReadOnly(not editable)
        state = fv.state if fv else "missing"
        txt, kind = STATE_CHIP[state]
        self.chip.set(txt, kind)
        flagged = bool(fv and any(f in fv.flags for f in ("ambiguous_date", "ambiguous_number", "multiple_candidates", "conflict_ai",
                                                           "ambiguous_symbol", "conflicting_currency", "ocr_repair", "heuristic")))
        self.edit.setProperty("state", "check" if flagged and state == "extracted" else state)
        repolish(self.edit)
        self._page = fv.page if fv else None
        self.page.setText(f"p.{fv.page}" if fv and fv.page else "p.–")
        self.page.setEnabled(bool(fv and fv.page))
        self.rev.setVisible(bool(fv and fv.origin == "corrected" and editable))
        notes = []
        if fv and fv.raw and fv.raw != fv.value and fv.raw != value:
            notes.append(f"Source text: “{fv.raw}”")
        if fv and fv.alt:
            notes.append(f"Other value seen: {fv.alt}")
        if fv and fv.flags:
            notes.append("Check: " + ", ".join(f.replace("_", " ") for f in fv.flags))
        if fv and fv.source != "rules":
            notes.append({"llm": "found by AI model", "rules+llm": "rules and AI agree", "user": "entered by you"}.get(fv.source, ""))
        self.note.setText(" · ".join(n for n in notes if n))
        self.note.setVisible(bool(notes))


class FieldsPanel(QScrollArea):
    edited = Signal(str, str)
    reverted = Signal(str)
    jump = Signal(int)

    def __init__(self, custom: list[CustomFieldDef]) -> None:
        super().__init__()
        self.setWidgetResizable(True)
        body = QWidget(); self.setWidget(body)
        lay = QVBoxLayout(body); lay.setContentsMargins(10, 10, 10, 10); lay.setSpacing(2)
        self.rows: dict[str, FieldRow] = {}
        hints = {"date": "YYYY-MM-DD", "money": "0.00", "rate": "%", "currency": "INR"}
        group = None
        titles = {"header": "Document", "totals": "Totals"}
        for s in SCALAR_FIELDS:
            if s.group != group:
                group = s.group
                lay.addSpacing(6); lay.addWidget(label(titles.get(group, ""), "h2"))
            self._add(lay, s.name, s.label, hints.get(s.type, ""), s.type)
        if custom:
            lay.addSpacing(6); lay.addWidget(label("Custom fields", "h2"))
            for c in custom:
                self._add(lay, f"x_{c.key}", c.label, "", c.type)
        lay.addStretch()

    def _add(self, lay: QVBoxLayout, name: str, text: str, hint: str, ftype: str = "text") -> None:
        r = FieldRow(name, text, hint, ftype, lambda n, t: self.edited.emit(n, t), lambda n: self.reverted.emit(n), lambda p: self.jump.emit(p))
        self.rows[name] = r
        lay.addWidget(r)

    def set_record(self, rec: DocumentRecord | None, editable: bool) -> None:
        for name, row in self.rows.items():
            row.set_value(rec.fields.get(name) if rec else None, editable and rec is not None)

    def focus_field(self, name: str) -> None:
        r = self.rows.get(name)
        if r:
            self.ensureWidgetVisible(r)
            r.edit.setFocus()
