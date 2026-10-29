"""One-line "what to do here" banner shown at the top of each workflow screen (can be switched off)."""

from __future__ import annotations

from PySide6.QtWidgets import QFrame, QHBoxLayout, QLabel, QPushButton, QWidget

from ...application.context import AppContext

STEPS = {
    "import": (1, "Add your documents",
               "Drag invoices or purchase orders into the box below, or click it to choose files. PDFs, photos and scans all work. "
               "You do not need to do anything else; reading starts by itself."),
    "review": (2, "Check what was read, then approve",
               "Each value is shown next to the page it came from. Amber means 'please look at this one'. Fix anything wrong by typing over it, "
               "then press Approve. Documents with no warnings can be approved together with one click."),
    "export": (3, "Send the approved data to Excel",
               "Choose what you want (for example a purchase register for your accountant) and which period. Only documents you approved are included."),
}


class GuideBar(QFrame):
    def __init__(self, ctx: AppContext, key: str, parent: QWidget | None = None) -> None:
        super().__init__(parent)
        self.ctx = ctx
        step, title, text = STEPS[key]
        self.setProperty("banner", "info")
        row = QHBoxLayout(self)
        row.setContentsMargins(12, 8, 8, 8)
        row.setSpacing(10)
        badge = QLabel(f"Step {step} of 3")
        badge.setProperty("chip", "primary")
        badge.setProperty("role", "small")
        self.msg = QLabel(f"<b>{title}.</b> {text}")
        self.msg.setWordWrap(True)
        hide = QPushButton("Hide tips")
        hide.setProperty("variant", "link")
        hide.setToolTip("You can turn tips back on in Settings > General")
        hide.clicked.connect(self._hide)
        row.addWidget(badge)
        row.addWidget(self.msg, 1)
        row.addWidget(hide)
        self.refresh()

    def _hide(self) -> None:
        self.ctx.cfg.show_tips = False
        try:
            self.ctx.cfg_mgr.save(self.ctx.cfg)
        except Exception:
            pass
        self.setVisible(False)

    def refresh(self) -> None:
        self.setVisible(self.ctx.cfg.show_tips)
