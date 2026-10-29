import os
import time
from pathlib import Path

import pytest
from PySide6.QtCore import QPoint, Qt
from PySide6.QtTest import QTest
from PySide6.QtWidgets import QApplication, QMessageBox

from documind.application.context import AppContext
from documind.config import ConfigManager
from documind.domain.models import DocStatus
from documind.ui.main_window import BatchBridge, MainWindow, NAV
from documind.ui.styles.theme import DARK, LIGHT, contrast_ratio
from tests.helpers import make_png, sample_pdf


@pytest.fixture()
def win(qtbot, tmp_home, monkeypatch):
    monkeypatch.setattr(QMessageBox, "warning", lambda *a, **k: QMessageBox.Ok)       # never block on modal dialogs
    monkeypatch.setattr(QMessageBox, "question", lambda *a, **k: QMessageBox.Yes)
    bridge = BatchBridge(); ctx = AppContext.create(ConfigManager(), on_batch_event=bridge.push)
    w = MainWindow(ctx, bridge); qtbot.addWidget(w); w.show()
    yield w
    ctx.shutdown()


def wait_idle(qtbot, w, timeout=120):
    qtbot.waitUntil(lambda: w.ctx.batch.wait_idle(0.01), timeout=timeout * 1000)
    qtbot.wait(150)


def test_startup_builds_only_dashboard_and_shows_empty_state(win):
    assert list(win.pages.keys()) == ["dashboard"]
    d = win.pages["dashboard"]
    assert d.empty.isVisible() and d.metrics.values["review"].text() == "0"
    assert win.windowTitle() == "DocuMind AI Professional"


def test_every_page_opens_without_error(win):
    for key, *_ in NAV:
        win.show_page(key)
        assert win.stack.currentWidget() is win.pages[key] and win.nav_buttons[key].isChecked()


def test_resizing_to_minimum_and_large_does_not_break_layout(win, qtbot):
    for key, *_ in NAV:
        win.show_page(key)
        for size in ((960, 620), (1920, 1080), (1100, 700)):
            win.resize(*size)
            qtbot.waitUntil(lambda: win.stack.currentWidget().width() > 400, timeout=3000)   # layout settles asynchronously
            assert win.width() >= 960 and win.height() >= 620


def test_keyboard_shortcuts_navigate(win, qtbot):
    win.activateWindow()
    for i, (key, *_r) in enumerate(NAV[:6], 1):
        QTest.keyClick(win, getattr(Qt, f"Key_{i}"), Qt.ControlModifier)
        assert win.stack.currentWidget() is win.pages[key]


def test_import_process_review_approve_export_through_the_ui(win, qtbot, tmp_home, monkeypatch):
    from PySide6.QtWidgets import QFileDialog
    d, p = sample_pdf(tmp_home, 7, "gst_classic", name="ui.pdf")
    imp = win.pages["import"]; win.show_page("import")
    imp.handle_paths([str(p)])
    qtbot.waitUntil(lambda: imp.table.rowCount() == 1, timeout=10000); wait_idle(qtbot, win)
    qtbot.waitUntil(lambda: imp.table.item(0, 2).text() == "Ready for review", timeout=5000)
    win.show_page("dashboard"); assert win.pages["dashboard"].metrics.values["review"].text() == "1"
    win.open_review(imp.table.item(0, 0).data(Qt.UserRole)); rv = win.pages["review"]
    qtbot.waitUntil(lambda: rv.rec is not None, timeout=5000)
    assert rv.fields.rows["invoice_number"].edit.text() == d.number and rv.lines.rowCount() == len(d.items)
    ed = rv.fields.rows["supplier_name"].edit
    ed.setText("Edited By Human"); ed.editingFinished.emit()
    assert rv.rec.fields["supplier_name"].state == "corrected" and rv.fields.rows["supplier_name"].chip.text() == "Corrected"
    rv.approve()
    assert win.ctx.repo.get(rv.rec.id if rv.rec else 1).status in (DocStatus.APPROVED, DocStatus.NEEDS_REVIEW)
    assert win.ctx.repo.stats(None)["approved"] == 1
    monkeypatch.setattr(QFileDialog, "getSaveFileName", lambda *a, **k: (str(tmp_home / "out.xlsx"), ""))
    ex = win.pages["export"]; win.show_page("export"); ex.export_ids(None)
    qtbot.waitUntil(lambda: (tmp_home / "out.xlsx").exists(), timeout=10000)


def test_bad_files_show_clear_messages_in_queue(win, qtbot, tmp_home):
    bad = tmp_home / "bad.pdf"; bad.write_bytes(b"%PDF-1.4 broken")
    spoof = tmp_home / "spoof.pdf"; make_png(spoof.with_suffix(".png")); spoof.write_bytes(spoof.with_suffix(".png").read_bytes())
    txt = tmp_home / "n.txt"; txt.write_text("x")
    imp = win.pages["import"]; win.show_page("import")
    imp.handle_paths([str(bad), str(spoof), str(txt)])
    qtbot.waitUntil(lambda: imp.table.rowCount() == 3, timeout=10000)
    msgs = [imp.table.item(r, 4).text() for r in range(3)]
    assert all(imp.table.item(r, 2).text() == "Not imported" for r in range(3))
    assert any("damaged" in m or "valid" in m for m in msgs) and any("contents" in m for m in msgs) and any("not supported" in m for m in msgs)


def test_processing_failure_and_retry_in_ui(win, qtbot, tmp_home):
    p = sample_pdf(tmp_home, 8, name="f.pdf")[1]
    r = win.ctx.docs.import_one(p); Path(win.ctx.repo.get(r.doc_id).stored_path).write_bytes(b"%PDF-1.4 x")
    imp = win.pages["import"]; win.show_page("import"); imp.add_results([r]); win.ctx.batch.enqueue([r.doc_id]); wait_idle(qtbot, win)
    qtbot.waitUntil(lambda: imp.table.item(0, 2).text() == "Failed", timeout=5000)
    assert imp.table.item(0, 4).text()
    qtbot.waitUntil(lambda: win.pages["dashboard"].metrics.values["failed"].text() == "1", timeout=5000)
    imp.table.selectRow(0); imp.retry(); wait_idle(qtbot, win)
    qtbot.waitUntil(lambda: imp.table.item(0, 2).text() == "Failed", timeout=5000)
    assert win.ctx.repo.get(r.doc_id).status == DocStatus.FAILED


def test_pause_and_cancel_from_the_queue(win, qtbot, tmp_home):
    ids = [win.ctx.docs.import_one(sample_pdf(tmp_home, s, name=f"c{s}.pdf")[1]).doc_id for s in (11, 12, 13)]
    imp = win.pages["import"]; win.show_page("import")
    imp.toggle_pause(); assert win.ctx.batch.paused and imp.b_pause.text() == "Resume"
    win.ctx.batch.enqueue(ids); qtbot.wait(300)
    assert all(win.ctx.repo.get(i).status == DocStatus.QUEUED for i in ids)
    imp.cancel_all(); wait_idle(qtbot, win)
    qtbot.waitUntil(lambda: {imp.table.item(r, 2).data(Qt.UserRole) for r in range(imp.table.rowCount())} == {"cancelled"}, timeout=5000)
    assert not win.ctx.batch.paused


def test_theme_toggle_persists_and_restyles(win, qtbot):
    assert win.ctx.cfg.theme == "light"
    win.toggle_theme()
    assert win.ctx.cfg.theme == "dark" and QApplication.instance().palette().window().color().name().upper() == DARK["bg"]
    assert ConfigManager().load().theme == "dark"
    win.toggle_theme(); assert win.ctx.cfg.theme == "light"


def test_settings_validation_messages_and_save(win, qtbot):
    win.show_page("settings"); s = win.pages["settings"]
    s.ocr_langs.setText("eng; rm -rf /"); s.save()
    assert "languages" in s.msg.text().lower()
    s.ocr_langs.setText("eng"); s.l_mb.setValue(25); s.save()
    assert s.msg.text().startswith("Saved") and win.ctx.cfg.limits.max_file_mb == 25
    s.ai_host.setText("http://example.com"); s.ai_on.setChecked(True); s.save()
    assert "privacy" in s.msg.text().lower() or "this computer" in s.msg.text().lower()


def test_review_screen_handles_no_selection_and_missing_source_file(win, qtbot, tmp_home):
    win.show_page("review"); rv = win.pages["review"]
    assert rv.rec is None and not rv.b_approve.isEnabled()
    r = win.ctx.docs.import_one(sample_pdf(tmp_home, 14)[1]); win.ctx.batch.enqueue([r.doc_id]); wait_idle(qtbot, win)
    os.remove(win.ctx.repo.get(r.doc_id).stored_path)
    win.open_review(r.doc_id); qtbot.wait(200)
    assert "no longer stored" in rv.viewer.canvas.text()


def test_wizard_completes_and_marks_first_run(win, qtbot):
    from documind.ui.dialogs.wizard import SetupWizard
    w = SetupWizard(win.ctx, None); qtbot.addWidget(w)
    for _ in range(w.stack.count() - 1):
        w.go(1)
    assert w.next.text() == "Finish" and "Step" in w.dots.text()
    w.want_demo.setChecked(False); w.go(1)
    assert win.ctx.cfg.first_run_done


@pytest.mark.parametrize("theme", [LIGHT, DARK], ids=["light", "dark"])
def test_theme_text_contrast_meets_wcag_aa(theme):
    t = theme
    pairs = [("text", "bg"), ("text", "surface"), ("muted", "surface"), ("muted", "bg"), ("muted", "surface2"), ("on_primary", "primary"),
             ("primary_text", "primary_soft"), ("primary_text", "surface"), ("success", "success_soft"), ("warning", "warning_soft"),
             ("danger", "danger_soft"), ("info", "info_soft"), ("text", "primary_soft"), ("danger", "surface"), ("muted", "side")]
    low = [(a, b, round(contrast_ratio(t[a], t[b]), 2)) for a, b in pairs if contrast_ratio(t[a], t[b]) < 4.5]
    assert low == []


def test_accessible_names_on_interactive_controls(win):
    missing = []
    for key, *_ in NAV:
        win.show_page(key)
        from PySide6.QtWidgets import QAbstractButton, QComboBox, QLineEdit, QTableWidget
        for kind in (QComboBox, QLineEdit, QTableWidget):
            for w in win.pages[key].findChildren(kind):
                if w.objectName().startswith("qt_") or (w.parent() is not None and w.parent().metaObject().className() in ("QSpinBox", "QDateEdit", "QComboBox")):
                    continue                                         # Qt-internal editors of spin boxes / pickers
                named = w.accessibleName() or w.toolTip() or (w.placeholderText() if isinstance(w, QLineEdit) else "")
                if not named:
                    missing.append((key, type(w).__name__))
    assert missing == [], missing
