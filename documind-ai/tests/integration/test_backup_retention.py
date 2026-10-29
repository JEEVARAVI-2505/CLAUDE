import json
import os
import shutil
import sqlite3
import zipfile
from pathlib import Path

import pytest

from documind.domain.exceptions import BackupError, RestoreValidationError
from documind.domain.models import DocStatus
from tests.helpers import make_ctx, process_all, sample_pdf


@pytest.fixture()
def ctx(tmp_home):
    c = make_ctx(); yield c; c.shutdown()


def seed(ctx, tmp, n=2, first=1):
    ids = [ctx.docs.import_one(sample_pdf(tmp, s, name=f"b{s}.pdf")[1]).doc_id for s in range(first, first + n)]
    return process_all(ctx, ids)


def count(ctx):
    return ctx.db.connect().execute("SELECT COUNT(*) FROM documents").fetchone()[0]


def test_backup_contents_manifest_and_verification(ctx, tmp_home):
    seed(ctx, tmp_home)
    z = ctx.backup.create()
    with zipfile.ZipFile(z) as zf:
        names = set(zf.namelist()); man = json.loads(zf.read("manifest.json"))
    assert "documind.db" in names and any(n.startswith("files/") for n in names)
    assert man["format_version"] == 1 and man["schema_version"] == 1 and man["document_count"] == 2 and man["app_version"]
    assert ctx.backup.verify(z).document_count == 2


def test_restore_roundtrip_replaces_data_and_keeps_previous(ctx, tmp_home):
    seed(ctx, tmp_home, 2, 1)
    z = ctx.backup.create()
    seed(ctx, tmp_home, 2, 11)
    assert count(ctx) == 4
    rep = ctx.backup.restore(z)
    assert count(ctx) == 2 and rep.documents == 2 and rep.safety_backup and rep.safety_backup.exists()
    assert rep.kept_previous and all(p.exists() for p in rep.kept_previous)
    assert ctx.db.integrity_check() == []
    for rid in [r.id for r in ctx.repo.search(__import__("documind.infrastructure.repositories", fromlist=["SearchFilter"]).SearchFilter())]:
        assert Path(ctx.repo.get(rid).stored_path).exists()
    assert ctx.backup.purge_previous_copies() >= 2


def test_restore_of_corrupt_or_tampered_backup_leaves_data_untouched(ctx, tmp_home):
    seed(ctx, tmp_home)
    z = ctx.backup.create(); before = count(ctx)
    bad = tmp_home / "tampered.zip"
    with zipfile.ZipFile(z) as src, zipfile.ZipFile(bad, "w") as dst:
        for n in src.namelist():
            data = src.read(n)
            if n.startswith("files/"):
                data = data[:-1] + bytes([data[-1] ^ 0xFF])            # flip one byte
            dst.writestr(n, data)
    with pytest.raises(RestoreValidationError):
        ctx.backup.restore(bad)
    junk = tmp_home / "junk.zip"; junk.write_bytes(b"not a zip")
    with pytest.raises(RestoreValidationError):
        ctx.backup.restore(junk)
    nomani = tmp_home / "nomani.zip"
    with zipfile.ZipFile(nomani, "w") as zf:
        zf.writestr("documind.db", b"x")
    with pytest.raises(RestoreValidationError):
        ctx.backup.restore(nomani)
    assert count(ctx) == before and ctx.db.integrity_check() == []


def test_backup_with_damaged_database_is_refused(ctx, tmp_home):
    seed(ctx, tmp_home, 1)
    z = ctx.backup.create(); man = json.loads(zipfile.ZipFile(z).read("manifest.json"))
    bad = tmp_home / "baddb.zip"
    junk = os.urandom(8192)
    import hashlib
    man["files"] = [f for f in man["files"] if f["path"] != "documind.db"] + [{"path": "documind.db", "sha256": hashlib.sha256(junk).hexdigest(), "size": len(junk)}]
    with zipfile.ZipFile(bad, "w") as zf:
        zf.writestr("manifest.json", json.dumps(man)); zf.writestr("documind.db", junk)
    with pytest.raises(RestoreValidationError):
        ctx.backup.verify(bad)


@pytest.mark.parametrize("evil", ["../../evil.txt", "/abs/evil.txt", "files/../../evil.txt", "C:/evil.txt", "..\\evil.txt"])
def test_zip_slip_member_names_refused(ctx, tmp_home, evil):
    seed(ctx, tmp_home, 1)
    z = ctx.backup.create(); bad = tmp_home / "slip.zip"
    with zipfile.ZipFile(z) as src, zipfile.ZipFile(bad, "w") as dst:
        for n in src.namelist():
            dst.writestr(n, src.read(n))
        dst.writestr(evil, b"owned")
    with pytest.raises(RestoreValidationError) as e:
        ctx.backup.restore(bad)
    assert "unsafe" in e.value.user_message
    assert not (tmp_home.parent / "evil.txt").exists()


def test_interrupted_restore_rolls_back_to_original(ctx, tmp_home, monkeypatch):
    seed(ctx, tmp_home, 2, 1)
    z = ctx.backup.create()
    seed(ctx, tmp_home, 1, 21)
    assert count(ctx) == 3
    calls = {"n": 0}
    real = os.replace

    def flaky(src, dst, *a, **k):
        if "restore-staging" in str(src):
            calls["n"] += 1
            if calls["n"] == 2:                                   # fail while moving the restored files into place
                raise OSError(5, "simulated I/O error / power loss")
        return real(src, dst, *a, **k)
    monkeypatch.setattr(os, "replace", flaky)
    with pytest.raises(BackupError):
        ctx.backup.restore(z)
    monkeypatch.setattr(os, "replace", real)
    assert count(ctx) == 3 and ctx.db.integrity_check() == []
    rid = ctx.repo.search(__import__("documind.infrastructure.repositories", fromlist=["SearchFilter"]).SearchFilter())[0].id
    assert Path(ctx.repo.get(rid).stored_path).exists()
    assert not list(ctx.paths.root.glob("restore-staging-*"))


def test_backup_rotation_keeps_last_n(ctx, tmp_home):
    seed(ctx, tmp_home, 1); ctx.cfg.backup.keep_last = 2
    import time
    for _ in range(4):
        ctx.backup.create(include_files=False); time.sleep(1.05)
    assert len(list(ctx.backup.folder().glob("documind-backup-2*.zip"))) == 2


def test_backup_disk_full_is_reported_and_cleans_up(ctx, tmp_home, monkeypatch):
    seed(ctx, tmp_home, 1)
    import errno
    real = zipfile.ZipFile.write
    monkeypatch.setattr(zipfile.ZipFile, "write", lambda *a, **k: (_ for _ in ()).throw(OSError(errno.ENOSPC, "full")))
    from documind.domain.exceptions import DiskFullError
    with pytest.raises(DiskFullError):
        ctx.backup.create()
    assert not list(ctx.backup.folder().glob("*.part")) and not list(ctx.backup.folder().glob("documind-backup-*.zip"))


# ---- deletion & retention -----------------------------------------------------------------
def test_permanent_delete_removes_rows_file_and_data(ctx, tmp_home):
    recs = seed(ctx, tmp_home, 2)
    a, b = recs
    marker = a.get("invoice_number")
    ctx.review.correct_field(a.id, "supplier_name", "SECRET-SUPPLIER-XYZ")
    path = Path(a.stored_path)
    ctx.docs.delete_permanently(a.id)
    assert not path.exists() and ctx.repo.search.__self__ is ctx.repo
    c = ctx.db.connect()
    for t in ("documents", "field_values", "validation_issues", "audit_log"):
        assert c.execute(f"SELECT COUNT(*) FROM {t} WHERE document_id=?", (a.id,)).fetchone()[0] == 0 if t != "documents" else c.execute("SELECT COUNT(*) FROM documents WHERE id=?", (a.id,)).fetchone()[0] == 0
    assert c.execute("SELECT COUNT(*) FROM deletion_log WHERE document_id=?", (a.id,)).fetchone()[0] == 1
    ctx.db.checkpoint(); ctx.db.close()
    blob = b"".join(p.read_bytes() for p in ctx.paths.root.glob("documind.db*"))
    assert b"SECRET-SUPPLIER-XYZ" not in blob and marker.encode() not in blob.replace(b.get("invoice_number").encode(), b"")
    assert Path(b.stored_path).exists()                                        # the other document is untouched


def test_deleting_one_of_two_identical_imports_keeps_shared_file(ctx, tmp_home):
    d, p = sample_pdf(tmp_home, 3, name="same.pdf")
    a = ctx.docs.import_one(p).doc_id; b = ctx.docs.import_one(p, skip_duplicates=False).doc_id
    assert ctx.repo.get(a).stored_path == ctx.repo.get(b).stored_path
    ctx.docs.delete_permanently(a)
    assert Path(ctx.repo.get(b).stored_path).exists()


def test_stored_path_traversal_refused(ctx, tmp_home):
    from documind.domain.exceptions import StorageError
    outside = tmp_home / "precious.txt"; outside.write_text("keep me")
    with pytest.raises(StorageError):
        ctx.store.delete(str(outside))
    with pytest.raises(StorageError):
        ctx.store.resolve(str(ctx.paths.files / ".." / ".." / "precious.txt"))
    assert outside.exists()


def test_retention_policy_deletes_only_finished_old_documents(ctx, tmp_home):
    a, b, c = seed(ctx, tmp_home, 3)
    ctx.review.approve(a.id); ctx.review.reject(b.id, "bad")
    old = "2020-01-01T00:00:00.000000Z"
    ctx.db.connect().execute("UPDATE documents SET imported_at=?", (old,))
    ctx.cfg.retention.days = 30
    rep = ctx.retention.apply()
    assert rep.deleted == 2 and ctx.repo.get(c.id).status == DocStatus.NEEDS_REVIEW      # pending review is never auto-deleted
    ctx.cfg.retention.days = 0
    assert ctx.retention.apply().deleted == 0


def test_retention_can_drop_source_files_but_keep_data(ctx, tmp_home):
    (a,) = seed(ctx, tmp_home, 1); ctx.review.approve(a.id)
    ctx.db.connect().execute("UPDATE documents SET imported_at='2020-01-01T00:00:00Z'")
    ctx.cfg.retention.days, ctx.cfg.retention.keep_source_files = 30, False
    rep = ctx.retention.apply()
    assert rep.source_files_removed == 1 and not Path(a.stored_path).exists() and ctx.repo.get(a.id).get("total")


def test_erase_all(ctx, tmp_home):
    seed(ctx, tmp_home, 2)
    assert ctx.retention.delete_all_data() == 2 and count(ctx) == 0 and not list(ctx.paths.files.rglob("*.pdf"))
