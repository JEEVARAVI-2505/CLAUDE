"""Run blocking work off the GUI thread and report back through Qt signals."""

from __future__ import annotations

from typing import Any, Callable

from PySide6.QtCore import QObject, QRunnable, QThreadPool, Signal

from ..domain.exceptions import DocuMindError


class _Signals(QObject):
    done = Signal(object)
    failed = Signal(str)


class _Task(QRunnable):
    def __init__(self, fn: Callable[[], Any]) -> None:
        super().__init__()
        self.fn, self.signals = fn, _Signals()
        self.setAutoDelete(False)  # kept alive in _keep until its callbacks have run

    def run(self) -> None:
        try:
            try:
                result = self.fn()
            except DocuMindError as e:
                self.signals.failed.emit(e.user_message)
            except Exception as e:  # last-resort guard so a worker can never crash the app
                self.signals.failed.emit(f"Unexpected error ({type(e).__name__}). See Help > Diagnostics.")
            else:
                self.signals.done.emit(result)
        except RuntimeError:
            pass  # the window/receiver was closed while the task ran


_keep: set[_Task] = set()


def run_async(fn: Callable[[], Any], on_done: Callable[[Any], None] | None = None,
              on_error: Callable[[str], None] | None = None, parent: QObject | None = None) -> None:
    """Execute ``fn`` in the global thread pool; callbacks run on the GUI thread."""
    task = _Task(fn)
    _keep.add(task)

    def finish(cb):
        def inner(arg):
            _keep.discard(task)
            if cb:
                cb(arg)
        return inner

    # queued connections to a QObject living in the GUI thread
    anchor = parent or _anchor()
    task.signals.moveToThread(anchor.thread())
    task.signals.done.connect(finish(on_done))
    task.signals.failed.connect(finish(on_error))
    QThreadPool.globalInstance().start(task)


_anchor_obj: QObject | None = None


def _anchor() -> QObject:
    global _anchor_obj
    if _anchor_obj is None:
        _anchor_obj = QObject()
    return _anchor_obj
