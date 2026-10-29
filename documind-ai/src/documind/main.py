"""Application entry point."""

from __future__ import annotations

import sys
import traceback

from . import APP_NAME, __version__


def smoke_test() -> int:
    """Headless self-check used by the build scripts: temp profile, process one generated invoice, render the window."""
    import os
    import tempfile
    from pathlib import Path

    home = Path(tempfile.mkdtemp(prefix="documind-smoke-"))
    os.environ["DOCUMIND_CONFIG_DIR"], os.environ["DOCUMIND_DATA_DIR"] = str(home / "cfg"), str(home / "data")
    from PySide6.QtWidgets import QApplication

    from .application.context import AppContext
    from .samples import generate, write_sample
    from .ui.main_window import BatchBridge, MainWindow

    app = QApplication([])
    bridge = BatchBridge()
    ctx = AppContext.create(on_batch_event=bridge.push)
    doc = generate(1, "gst_classic")
    pdf = write_sample(doc, home / "smoke.pdf")
    res = ctx.docs.import_one(pdf)
    ctx.batch.enqueue([res.doc_id])
    ok = ctx.batch.wait_idle(120)
    rec = ctx.repo.get(res.doc_id)
    win = MainWindow(ctx, bridge)
    win.show()
    for k in ("dashboard", "import", "review", "history", "export", "settings", "help"):
        win.show_page(k)
        app.processEvents()
    good = ok and rec.get("invoice_number") == doc.number and rec.get("total") == doc.truth["total"]
    ctx.shutdown()
    print(f"SMOKE {'OK' if good else 'FAILED'} version={__version__} invoice={rec.get('invoice_number')} total={rec.get('total')}")
    return 0 if good else 1


def main(argv: list[str] | None = None) -> int:
    if "--smoke-test" in (argv if argv is not None else sys.argv):
        return smoke_test()
    from PySide6.QtCore import Qt
    from PySide6.QtWidgets import QApplication, QMessageBox

    from .application.context import AppContext
    from .domain.exceptions import DocuMindError
    from .ui.dialogs.wizard import SetupWizard
    from .ui.icons import app_icon
    from .ui.main_window import BatchBridge, MainWindow

    app = QApplication(argv if argv is not None else sys.argv)
    app.setApplicationName(APP_NAME)
    app.setApplicationVersion(__version__)
    app.setOrganizationName("DocuMind")
    app.setWindowIcon(app_icon())
    bridge = BatchBridge()
    try:
        ctx = AppContext.create(on_batch_event=bridge.push)
    except DocuMindError as e:
        QMessageBox.critical(None, APP_NAME, f"DocuMind could not start.\n\n{e.user_message}\n\n{e.detail or ''}")
        return 2
    except Exception:
        QMessageBox.critical(None, APP_NAME, "DocuMind could not start because of an unexpected error:\n\n" + traceback.format_exc(limit=3))
        return 3
    win = MainWindow(ctx, bridge)
    win.show()
    if not ctx.cfg.first_run_done:
        SetupWizard(ctx, win).exec()
    return app.exec()


if __name__ == "__main__":
    raise SystemExit(main())
