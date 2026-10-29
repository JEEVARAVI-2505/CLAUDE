"""Design tokens and stylesheet.

Deliberately plain: neutral greys, one blue accent, 1px borders, 6px radii, no
gradients, shadows or animation (cheaper to paint and easier to read). Every
text/background pair is checked for WCAG AA contrast in tests/gui.
"""

from __future__ import annotations

from PySide6.QtGui import QColor, QPalette
from PySide6.QtWidgets import QApplication

LIGHT = {
    "bg": "#F5F6F8", "surface": "#FFFFFF", "surface2": "#F0F2F5", "border": "#DDE1E7", "text": "#1C2128", "muted": "#59616E",
    "primary": "#1F5FBF", "primary_hover": "#194D9C", "on_primary": "#FFFFFF", "primary_soft": "#E6EFFB", "primary_text": "#194D9C",
    "success": "#1B7536", "success_soft": "#E3F3E8", "warning": "#85550A", "warning_soft": "#FBF0D9",
    "danger": "#AE2A22", "danger_soft": "#FCE8E6", "info": "#1F5FBF", "info_soft": "#E6EFFB",
    "side": "#FFFFFF", "focus": "#1F5FBF",
}
DARK = {
    "bg": "#15171C", "surface": "#1D2026", "surface2": "#252932", "border": "#32373F", "text": "#E7E9ED", "muted": "#9DA5B3",
    "primary": "#6CA0F0", "primary_hover": "#8AB5F4", "on_primary": "#0A1A33", "primary_soft": "#1F2E47", "primary_text": "#9CC0F6",
    "success": "#5CC27C", "success_soft": "#17301F", "warning": "#E8B14F", "warning_soft": "#352A12",
    "danger": "#F0867F", "danger_soft": "#3C1D1B", "info": "#6CA0F0", "info_soft": "#1F2E47",
    "side": "#1D2026", "focus": "#8AB5F4",
}
FONT_STACK = '"Segoe UI", "Noto Sans", "DejaVu Sans", Arial, sans-serif'


def tokens(name: str) -> dict[str, str]:
    return DARK if name == "dark" else LIGHT


def build_qss(t: dict[str, str]) -> str:
    return f"""
* {{ font-family: {FONT_STACK}; font-size: 10pt; }}
QWidget {{ color: {t['text']}; }}
QMainWindow, QDialog, #Page, QStackedWidget {{ background: {t['bg']}; }}
QToolTip {{ background: {t['surface']}; color: {t['text']}; border: 1px solid {t['border']}; padding: 4px 6px; }}
QLabel {{ background: transparent; }}
QLabel[role="title"] {{ font-size: 15pt; font-weight: 600; }}
QLabel[role="h2"] {{ font-size: 11.5pt; font-weight: 600; }}
QLabel[role="muted"] {{ color: {t['muted']}; }}
QLabel[role="small"] {{ color: {t['muted']}; font-size: 9pt; }}
QLabel[role="metric"] {{ font-size: 20pt; font-weight: 600; }}

#Sidebar {{ background: {t['side']}; border-right: 1px solid {t['border']}; }}
QToolButton[nav="true"] {{ color: {t['muted']}; background: transparent; border: none; border-left: 3px solid transparent; border-radius: 0;
  padding: 8px 12px; text-align: left; font-weight: 500; }}
QToolButton[nav="true"]:hover {{ background: {t['surface2']}; color: {t['text']}; }}
QToolButton[nav="true"]:checked {{ background: {t['primary_soft']}; color: {t['primary_text']}; border-left: 3px solid {t['primary']}; font-weight: 600; }}
QToolButton[nav="true"]:focus {{ outline: none; border: 2px solid {t['focus']}; }}

QFrame[card="true"] {{ background: {t['surface']}; border: 1px solid {t['border']}; border-radius: 6px; }}
QFrame[card="strip"] {{ background: {t['surface']}; border: 1px solid {t['border']}; border-radius: 6px; }}
QFrame[divider="true"] {{ background: {t['border']}; border: none; min-width: 1px; max-width: 1px; }}
QFrame[banner="warning"] {{ background: {t['warning_soft']}; border: 1px solid {t['warning']}; border-radius: 6px; }}
QFrame[banner="info"] {{ background: {t['info_soft']}; border: 1px solid {t['info']}; border-radius: 6px; }}
QFrame[banner="danger"] {{ background: {t['danger_soft']}; border: 1px solid {t['danger']}; border-radius: 6px; }}
QLabel[chip="true"] {{ border-radius: 3px; padding: 1px 7px; font-weight: 600; font-size: 8.5pt; }}
QLabel[chip="success"] {{ background: {t['success_soft']}; color: {t['success']}; }}
QLabel[chip="warning"] {{ background: {t['warning_soft']}; color: {t['warning']}; }}
QLabel[chip="danger"] {{ background: {t['danger_soft']}; color: {t['danger']}; }}
QLabel[chip="info"] {{ background: {t['info_soft']}; color: {t['info']}; }}
QLabel[chip="neutral"] {{ background: {t['surface2']}; color: {t['muted']}; }}
QLabel[chip="primary"] {{ background: {t['primary_soft']}; color: {t['primary_text']}; }}

QPushButton {{ background: {t['surface']}; color: {t['text']}; border: 1px solid {t['border']}; border-radius: 5px; padding: 6px 14px; }}
QPushButton:hover {{ background: {t['surface2']}; border-color: {t['muted']}; }}
QPushButton:pressed {{ background: {t['primary_soft']}; }}
QPushButton:disabled {{ color: {t['muted']}; background: {t['surface2']}; border-color: {t['surface2']}; }}
QPushButton:focus, QToolButton:focus, QComboBox:focus, QLineEdit:focus, QSpinBox:focus, QDateEdit:focus, QPlainTextEdit:focus,
QTextBrowser:focus, QTableWidget:focus, QListWidget:focus {{ border: 2px solid {t['focus']}; }}
QPushButton[variant="primary"] {{ background: {t['primary']}; color: {t['on_primary']}; border: 1px solid {t['primary']}; font-weight: 600; }}
QPushButton[variant="primary"]:hover {{ background: {t['primary_hover']}; border-color: {t['primary_hover']}; }}
QPushButton[variant="primary"]:disabled {{ background: {t['surface2']}; color: {t['muted']}; border-color: {t['surface2']}; }}
QPushButton[variant="danger"] {{ color: {t['danger']}; border-color: {t['danger']}; }}
QPushButton[variant="danger"]:hover {{ background: {t['danger_soft']}; }}
QPushButton[variant="link"] {{ background: transparent; border: none; color: {t['primary_text']}; padding: 4px 6px; }}
QPushButton[variant="link"]:hover {{ text-decoration: underline; background: transparent; }}
QPushButton[step="true"] {{ text-align: left; padding: 12px 16px; font-size: 11pt; font-weight: 600; background: {t['surface']}; border: 1px solid {t['border']}; border-left: 4px solid {t['primary']}; border-radius: 6px; }}
QPushButton[step="true"]:hover {{ background: {t['primary_soft']}; }}
QToolButton {{ background: {t['surface']}; border: 1px solid {t['border']}; border-radius: 5px; padding: 3px 8px; color: {t['text']}; }}
QToolButton:hover {{ background: {t['surface2']}; }}

QLineEdit, QComboBox, QSpinBox, QDoubleSpinBox, QDateEdit, QPlainTextEdit, QTextBrowser {{ background: {t['surface']}; border: 1px solid {t['border']};
  border-radius: 5px; padding: 5px 8px; selection-background-color: {t['primary']}; selection-color: {t['on_primary']}; }}
QLineEdit[state="corrected"] {{ border-color: {t['primary']}; background: {t['primary_soft']}; }}
QLineEdit[state="validated"] {{ border-color: {t['success']}; }}
QLineEdit[state="check"] {{ background: {t['warning_soft']}; border-color: {t['warning']}; }}
QLineEdit[state="missing"] {{ border: 1px dashed {t['muted']}; }}
QComboBox::drop-down {{ border: none; width: 20px; }}
QComboBox QAbstractItemView {{ background: {t['surface']}; border: 1px solid {t['border']}; selection-background-color: {t['primary']}; selection-color: {t['on_primary']}; }}

QTableWidget, QTableView, QListWidget {{ background: {t['surface']}; alternate-background-color: {t['surface2']}; border: 1px solid {t['border']}; border-radius: 6px;
  gridline-color: {t['border']}; selection-background-color: {t['primary_soft']}; selection-color: {t['text']}; }}
QListWidget::item {{ padding: 7px 8px; border-bottom: 1px solid {t['border']}; }}
QListWidget::item:selected {{ background: {t['primary_soft']}; color: {t['text']}; border-left: 3px solid {t['primary']}; }}
QHeaderView::section {{ background: {t['surface2']}; color: {t['muted']}; padding: 6px 8px; border: none; border-bottom: 1px solid {t['border']}; font-weight: 600; }}
QTabWidget::pane {{ border: 1px solid {t['border']}; border-radius: 6px; background: {t['surface']}; top: -1px; }}
QTabBar::tab {{ background: transparent; color: {t['muted']}; padding: 7px 14px; margin-right: 2px; border-bottom: 2px solid transparent; font-weight: 500; }}
QTabBar::tab:selected {{ color: {t['primary_text']}; border-bottom: 2px solid {t['primary']}; font-weight: 600; }}
QTabBar::tab:hover:!selected {{ color: {t['text']}; }}
QProgressBar {{ background: {t['surface2']}; border: none; border-radius: 3px; text-align: center; color: {t['text']}; font-size: 8pt; max-height: 14px; }}
QProgressBar::chunk {{ background: {t['primary']}; border-radius: 3px; }}
QScrollBar:vertical {{ background: transparent; width: 10px; margin: 0; }}
QScrollBar::handle:vertical {{ background: {t['border']}; border-radius: 4px; min-height: 28px; }}
QScrollBar::handle:vertical:hover {{ background: {t['muted']}; }}
QScrollBar:horizontal {{ background: transparent; height: 10px; margin: 0; }}
QScrollBar::handle:horizontal {{ background: {t['border']}; border-radius: 4px; min-width: 28px; }}
QScrollBar::add-line, QScrollBar::sub-line {{ width: 0; height: 0; }}
QScrollArea {{ border: none; background: transparent; }}
QGroupBox {{ border: 1px solid {t['border']}; border-radius: 6px; margin-top: 12px; padding: 12px; font-weight: 600; }}
QGroupBox::title {{ subcontrol-origin: margin; left: 10px; padding: 0 4px; color: {t['muted']}; }}
QMenu {{ background: {t['surface']}; border: 1px solid {t['border']}; padding: 4px; }}
QMenu::item {{ padding: 6px 20px; }}
QMenu::item:selected {{ background: {t['primary_soft']}; }}
QStatusBar {{ background: {t['surface']}; color: {t['muted']}; border-top: 1px solid {t['border']}; }}
#DropZone {{ background: {t['surface']}; border: 1px dashed {t['muted']}; border-radius: 6px; }}
#DropZone[hover="true"] {{ background: {t['primary_soft']}; border: 1px solid {t['primary']}; }}
"""


def apply_theme(app: QApplication, name: str) -> dict[str, str]:
    t = tokens(name)
    app.setStyle("Fusion")
    pal = QPalette()
    for role, key in ((QPalette.Window, "bg"), (QPalette.WindowText, "text"), (QPalette.Base, "surface"), (QPalette.AlternateBase, "surface2"),
                      (QPalette.Text, "text"), (QPalette.Button, "surface"), (QPalette.ButtonText, "text"), (QPalette.Highlight, "primary"),
                      (QPalette.HighlightedText, "on_primary"), (QPalette.ToolTipBase, "surface"), (QPalette.ToolTipText, "text"),
                      (QPalette.PlaceholderText, "muted"), (QPalette.Link, "primary_text")):
        pal.setColor(role, QColor(t[key]))
    app.setPalette(pal)
    app.setStyleSheet(build_qss(t))
    return t


def contrast_ratio(fg: str, bg: str) -> float:
    def lum(c: str) -> float:
        q = QColor(c)
        v = [x / 12.92 if x <= 0.03928 else ((x + 0.055) / 1.055) ** 2.4 for x in (q.redF(), q.greenF(), q.blueF())]
        return 0.2126 * v[0] + 0.7152 * v[1] + 0.0722 * v[2]
    a, b = lum(fg), lum(bg)
    return (max(a, b) + 0.05) / (min(a, b) + 0.05)
