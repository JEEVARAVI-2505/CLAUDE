"""Small reusable widgets: cards, chips, metric strip, empty state."""

from __future__ import annotations

from PySide6.QtCore import Qt
from PySide6.QtWidgets import QFrame, QHBoxLayout, QLabel, QPushButton, QVBoxLayout, QWidget


def repolish(w: QWidget) -> None:
    w.style().unpolish(w)
    w.style().polish(w)
    w.update()


def label(text: str = "", role: str | None = None, wrap: bool = False) -> QLabel:
    lb = QLabel(text)
    if role:
        lb.setProperty("role", role)
    lb.setWordWrap(wrap)
    return lb


class Card(QFrame):
    def __init__(self, parent: QWidget | None = None, title: str | None = None) -> None:
        super().__init__(parent)
        self.setProperty("card", "true")
        self.lay = QVBoxLayout(self)
        self.lay.setContentsMargins(16, 14, 16, 14)
        self.lay.setSpacing(8)
        if title:
            self.lay.addWidget(label(title, "h2"))


class Chip(QLabel):
    """Status label. Always text, never colour alone."""

    def __init__(self, text: str = "", kind: str = "neutral", parent: QWidget | None = None) -> None:
        super().__init__(text, parent)
        self.setProperty("chip", kind)
        self.setAlignment(Qt.AlignCenter)

    def set(self, text: str, kind: str) -> None:
        self.setText(text)
        self.setProperty("chip", kind)
        repolish(self)


STATUS_CHIP = {
    "queued": ("Queued", "neutral"), "processing": ("Processing", "info"), "needs_review": ("Needs review", "warning"),
    "approved": ("Approved", "success"), "rejected": ("Rejected", "neutral"), "failed": ("Failed", "danger"),
}


class MetricStrip(QFrame):
    """One bordered strip of figures separated by thin dividers."""

    def __init__(self, items: list[tuple[str, str]], parent: QWidget | None = None) -> None:
        super().__init__(parent)
        self.setProperty("card", "strip")
        row = QHBoxLayout(self)
        row.setContentsMargins(0, 12, 0, 12)
        row.setSpacing(0)
        self.values: dict[str, QLabel] = {}
        for i, (key, title) in enumerate(items):
            if i:
                d = QFrame()
                d.setProperty("divider", "true")
                row.addWidget(d)
            cell = QVBoxLayout()
            cell.setContentsMargins(18, 0, 18, 0)
            cell.setSpacing(2)
            v = label("0", "metric")
            v.setAccessibleName(title)
            self.values[key] = v
            cell.addWidget(v)
            cell.addWidget(label(title, "muted"))
            row.addLayout(cell, 1)

    def set_value(self, key: str, value: int) -> None:
        lb = self.values[key]
        lb.setText(f"{value:,}")
        lb.setAccessibleDescription(f"{lb.accessibleName()}: {value}")


class EmptyState(QWidget):
    """Plain, left-aligned message with one action (no decorative art)."""

    def __init__(self, title: str, text: str, button: str | None = None, parent: QWidget | None = None) -> None:
        super().__init__(parent)
        lay = QVBoxLayout(self)
        lay.setAlignment(Qt.AlignCenter)
        lay.setSpacing(6)
        self.setMinimumHeight(150)
        t = label(title, "h2")
        t.setAlignment(Qt.AlignCenter)
        d = label(text, "muted", wrap=True)
        d.setAlignment(Qt.AlignCenter)
        d.setMaximumWidth(440)
        lay.addWidget(t)
        lay.addWidget(d, 0, Qt.AlignHCenter)
        self.button: QPushButton | None = None
        if button:
            self.button = QPushButton(button)
            self.button.setProperty("variant", "primary")
            lay.addSpacing(6)
            lay.addWidget(self.button, 0, Qt.AlignHCenter)
