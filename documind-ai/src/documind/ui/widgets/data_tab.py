"""Settings > Data, retention and backup/restore tab."""

from __future__ import annotations

from pathlib import Path

from PySide6.QtCore import Qt, QUrl
from PySide6.QtGui import QDesktopServices
from PySide6.QtWidgets import (QCheckBox, QFileDialog, QFormLayout, QGroupBox, QHBoxLayout, QInputDialog, QListWidget, QMessageBox,
                               QPushButton, QSpinBox, QVBoxLayout, QWidget)

from ...application.context import AppContext
from ..workers import run_async
from .common import label


class DataTab(QWidget):
    def __init__(self, ctx: AppContext, win) -> None:
        super().__init__()
        self.ctx, self.win = ctx, win
        lay = QVBoxLayout(self)
        g = QGroupBox("Where your data lives"); f = QVBoxLayout(g)
        self.path = label(str(ctx.paths.root), "muted", wrap=True)
        self.path.setTextInteractionFlags(Qt.TextSelectableByMouse)
        f.addWidget(self.path)
        f.addWidget(label("Database, stored originals, logs and backups are inside this folder. Nothing is uploaded anywhere. "
                          "Protect it with your Windows account permissions (and BitLocker for full-disk encryption).", "small", wrap=True))
        row = QHBoxLayout()
        b_open = QPushButton("Open folder"); b_open.clicked.connect(lambda: QDesktopServices.openUrl(QUrl.fromLocalFile(str(ctx.paths.root))))
        b_move = QPushButton("Choose a different data folder…"); b_move.clicked.connect(self.change_dir)
        row.addWidget(b_open); row.addWidget(b_move); row.addStretch(); f.addLayout(row)
        lay.addWidget(g)
        g2 = QGroupBox("Retention"); f2 = QFormLayout(g2)
        self.days = QSpinBox(); self.days.setRange(0, 36500); self.days.setSpecialValueText("Keep until I delete"); self.days.setSuffix(" days")
        self.days.setAccessibleName("Delete approved, rejected and failed documents after this many days")
        self.keep_src = QCheckBox("Keep original files (untick to remove the stored file but keep the extracted data)")
        f2.addRow("Auto-delete after", self.days); f2.addRow(self.keep_src)
        rr = QHBoxLayout(); b_prev = QPushButton("Apply retention now…"); b_prev.clicked.connect(self.apply_retention)
        b_erase = QPushButton("Erase ALL data…"); b_erase.setProperty("variant", "danger"); b_erase.clicked.connect(self.erase_all)
        rr.addWidget(b_prev); rr.addWidget(b_erase); rr.addStretch(); f2.addRow(rr)
        f2.addRow(label("Documents waiting for review are never deleted automatically. Deleted files are overwritten once; SSDs and backups may still "
                        "hold older copies (see docs/PRIVACY.md).", "small", wrap=True))
        lay.addWidget(g2)
        g3 = QGroupBox("Backup and restore"); f3 = QVBoxLayout(g3)
        self.b_folder = label("", "muted", wrap=True); f3.addWidget(self.b_folder)
        self.inc = QCheckBox("Include original document files in backups")
        self.keep = QSpinBox(); self.keep.setRange(1, 100); self.keep.setPrefix("Keep last "); self.keep.setSuffix(" backups")
        f3.addWidget(self.inc); f3.addWidget(self.keep)
        br = QHBoxLayout()
        b_new = QPushButton("Create backup now"); b_new.setProperty("variant", "primary"); b_new.clicked.connect(self.backup_now)
        b_rest = QPushButton("Restore from backup…"); b_rest.clicked.connect(self.restore)
        b_dir = QPushButton("Choose backup folder…"); b_dir.clicked.connect(self.pick_backup_dir)
        for b in (b_new, b_rest, b_dir):
            br.addWidget(b)
        br.addStretch(); f3.addLayout(br)
        self.list = QListWidget(); self.list.setMaximumHeight(110); self.list.setAccessibleName("Existing backups"); f3.addWidget(self.list)
        lay.addWidget(g3); lay.addStretch()
        self.new_data_dir: str | None = None
        self.new_backup_dir: str | None = None

    def load(self) -> None:
        c = self.ctx.cfg
        self.days.setValue(c.retention.days); self.keep_src.setChecked(c.retention.keep_source_files)
        self.inc.setChecked(c.backup.include_source_files); self.keep.setValue(c.backup.keep_last)
        self.b_folder.setText(f"Backups are saved to: {self.ctx.backup.folder()}")
        self.list.clear()
        for p in self.ctx.backup.list_backups()[:15]:
            self.list.addItem(f"{p.name}   ({p.stat().st_size / 1e6:.1f} MB)")

    def apply_to(self, cfg) -> None:
        cfg.retention.days = self.days.value(); cfg.retention.keep_source_files = self.keep_src.isChecked()
        cfg.backup.include_source_files = self.inc.isChecked(); cfg.backup.keep_last = self.keep.value()
        if self.new_data_dir:
            cfg.data_dir = self.new_data_dir
        if self.new_backup_dir:
            cfg.backup.folder = self.new_backup_dir

    def change_dir(self) -> None:
        d = QFileDialog.getExistingDirectory(self, "Choose a new data folder (use Backup/Restore to move existing data)", str(Path.home()))
        if d:
            self.new_data_dir = d
            self.path.setText(f"{self.ctx.paths.root}\n→ will switch to: {d} after you press Save and restart DocuMind.")

    def pick_backup_dir(self) -> None:
        d = QFileDialog.getExistingDirectory(self, "Choose where backups are saved", str(Path.home()))
        if d:
            self.new_backup_dir = d; self.b_folder.setText(f"Backups will be saved to: {d} (press Save to apply)")

    def backup_now(self) -> None:
        self.win.notify("Creating backup…")
        cfg = self.ctx.cfg.model_copy(deep=True); self.apply_to(cfg)
        self.ctx.cfg.backup = cfg.backup
        run_async(lambda: self.ctx.backup.create(),
                  lambda p: (self.win.notify(f"Backup saved: {p.name}"), self.load()), lambda m: self.win.error("Backup failed", m), parent=self)

    def restore(self) -> None:
        if not self.ctx.batch.wait_idle(0):
            self.win.error("Restore", "Wait for processing to finish (or cancel it) before restoring."); return
        f, _ = QFileDialog.getOpenFileName(self, "Choose a DocuMind backup", str(self.ctx.backup.folder()), "DocuMind backup (*.zip)")
        if not f:
            return
        self.win.notify("Checking backup…")

        def check():
            return self.ctx.backup.verify(Path(f))

        def confirm(man):
            ans = QMessageBox.question(self, "Restore backup?", f"This backup holds {man.document_count} document(s), made {man.created_at[:16].replace('T', ' ')} "
                                       f"by version {man.app_version}.\n\nYour current data will be replaced. A safety copy of the current data is made first "
                                       "and kept, so you can go back.\n\nContinue?")
            if ans != QMessageBox.Yes:
                return
            run_async(lambda: self.ctx.backup.restore(Path(f)), self._restored, lambda m: self.win.error("Restore failed", m), parent=self)
        run_async(check, confirm, lambda m: self.win.error("This backup cannot be used", m), parent=self)

    def _restored(self, rep) -> None:
        self.win.notify(f"Restored {rep.documents} document(s).")
        QMessageBox.information(self, "Restore complete", f"{rep.documents} document(s) restored.\n\nYour previous data was kept as:\n" +
                                "\n".join(str(p) for p in rep.kept_previous) + "\n\nOnce you have confirmed everything looks right, delete these copies "
                                "(Settings > Data) to free space and remove old data.")
        for p in self.win.pages.values():
            if hasattr(p, "refresh"):
                p.refresh()
        self.load()

    def apply_retention(self) -> None:
        cfg = self.ctx.cfg.model_copy(deep=True); self.apply_to(cfg)
        self.ctx.cfg.retention = cfg.retention
        n = len(self.ctx.retention.preview())
        if self.ctx.cfg.retention.days <= 0:
            self.win.notify("Set a number of days first."); return
        if n == 0:
            self.win.notify("No documents are older than the retention period."); return
        if QMessageBox.question(self, "Apply retention", f"Permanently delete {n} approved/rejected/failed document(s) older than {self.ctx.cfg.retention.days} days?") != QMessageBox.Yes:
            return
        run_async(self.ctx.retention.apply, lambda r: self.win.notify(f"Retention applied: {r.deleted} deleted, {r.source_files_removed} files removed."),
                  lambda m: self.win.error("Retention", m), parent=self)

    def erase_all(self) -> None:
        text, ok = QInputDialog.getText(self, "Erase all data", "This permanently deletes EVERY document, correction and history entry.\nType DELETE to confirm:")
        if ok and text.strip() == "DELETE":
            run_async(self.ctx.retention.delete_all_data, lambda n: self.win.notify(f"Erased {n} document(s)."), lambda m: self.win.error("Erase", m), parent=self)
