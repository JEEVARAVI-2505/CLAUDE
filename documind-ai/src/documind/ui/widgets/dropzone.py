"""Large, keyboard-accessible drag-and-drop target."""

from __future__ import annotations

from pathlib import Path

from PySide6.QtCore import QUrl, Qt, Signal
from PySide6.QtWidgets import QLabel, QVBoxLayout, QFrame, QWidget

from ..icons import icon_pixmap
from .common import label, repolish


class DropZone(QFrame):
    files_dropped = Signal(list)
    browse_requested = Signal()

    def __init__(self, parent: QWidget | None = None) -> None:
        super().__init__(parent)
        self.setObjectName("DropZone")
        self.setAcceptDrops(True)
        self.setFocusPolicy(Qt.StrongFocus)
        self.setCursor(Qt.PointingHandCursor)
        self.setMinimumHeight(170)
        self.setAccessibleName("Drop zone for invoices and purchase orders")
        self.setAccessibleDescription("Press Enter or Space to choose files, or drag files onto this area.")
        lay = QVBoxLayout(self)
        lay.setAlignment(Qt.AlignCenter)
        lay.setSpacing(6)
        self.icon = QLabel()
        self.icon.setPixmap(icon_pixmap("import", "#8B5CF6", 44))
        self.icon.setAlignment(Qt.AlignCenter)
        self.title = label("Drop invoices or purchase orders here", "h2")
        self.title.setAlignment(Qt.AlignCenter)
        self.sub = label("or click to browse  ·  PDF, PNG, JPG, TIFF  ·  scanned or digital", "muted")
        self.sub.setAlignment(Qt.AlignCenter)
        for w in (self.icon, self.title, self.sub):
            lay.addWidget(w)

    def set_limits_text(self, mb: int) -> None:
        self.sub.setText(f"or click to browse  ·  PDF, PNG, JPG, TIFF  ·  up to {mb} MB each")

    def _hover(self, on: bool) -> None:
        self.setProperty("hover", "true" if on else "false")
        repolish(self)

    def dragEnterEvent(self, e) -> None:
        if e.mimeData().hasUrls():
            e.acceptProposedAction()
            self._hover(True)

    def dragLeaveEvent(self, _e) -> None:
        self._hover(False)

    def dropEvent(self, e) -> None:
        self._hover(False)
        paths: list[str] = []
        for u in e.mimeData().urls():
            if isinstance(u, QUrl) and u.isLocalFile():
                paths.append(u.toLocalFile())
        if paths:
            self.files_dropped.emit(expand_paths(paths))
            e.acceptProposedAction()

    def mouseReleaseEvent(self, e) -> None:
        if e.button() == Qt.LeftButton:
            self.browse_requested.emit()

    def keyPressEvent(self, e) -> None:
        if e.key() in (Qt.Key_Return, Qt.Key_Enter, Qt.Key_Space):
            self.browse_requested.emit()
        else:
            super().keyPressEvent(e)


def expand_paths(paths: list[str], limit: int = 5000) -> list[str]:
    """Expand dropped folders (one level of recursion safe-guarded) into supported files."""
    exts = {".pdf", ".png", ".jpg", ".jpeg", ".tif", ".tiff"}
    out: list[str] = []
    for p in paths:
        pp = Path(p)
        if pp.is_dir():
            for f in sorted(pp.rglob("*")):
                if f.is_file() and f.suffix.lower() in exts and not f.is_symlink():
                    out.append(str(f))
                    if len(out) >= limit:
                        return out
        else:
            out.append(str(pp))
    return out[:limit]
