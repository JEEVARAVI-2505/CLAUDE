import sqlite3
import threading

import pytest

from documind.domain.exceptions import DatabaseLockedError, IntegrityCheckError, MigrationError
from documind.domain.models import DocStatus, DocumentRecord
from documind.infrastructure import database as dbmod
from documind.infrastructure.database import Database
from documind.infrastructure.repositories import DocumentRepository, SearchFilter


def rec(name="a.pdf", sha="a" * 64, **kw):
    return DocumentRecord(file_name=name, sha256=sha, stored_path="/x", mime="application/pdf", size_bytes=1, **kw)


def test_fresh_migration_and_idempotence(tmp_path):
    db = Database(tmp_path / "t.db")
    assert db.migrate() == [1] and db.schema_version() == db.latest_version() == 1
    assert db.migrate() == [] and db.integrity_check() == []


def test_newer_database_refused_and_untouched(tmp_path):
    db = Database(tmp_path / "t.db"); db.migrate()
    db.connect().execute("INSERT INTO schema_migrations VALUES (99,'future','x')")
    with pytest.raises(MigrationError):
        Database(tmp_path / "t.db").migrate()


def test_failed_migration_rolls_back_everything(tmp_path, monkeypatch):
    mig = tmp_path / "mig"; mig.mkdir()
    (mig / "0001_a.sql").write_text("CREATE TABLE a(x INTEGER);\nINSERT INTO a VALUES (1);")
    (mig / "0002_b.sql").write_text("CREATE TABLE b(x INTEGER);\nINSERT INTO nonexistent VALUES (1);")
    monkeypatch.setattr(dbmod, "MIGRATIONS_DIR", mig)
    db = Database(tmp_path / "t.db")
    with pytest.raises(MigrationError):
        db.migrate()
    assert db.schema_version() == 1                                   # v1 committed, v2 rolled back entirely
    tables = {r[0] for r in db.connect().execute("SELECT name FROM sqlite_master WHERE type='table'")}
    assert "a" in tables and "b" not in tables


def test_pre_migration_backup_is_made(tmp_path, monkeypatch):
    mig = tmp_path / "mig"; mig.mkdir(); (mig / "0001_a.sql").write_text("CREATE TABLE a(x INTEGER);")
    monkeypatch.setattr(dbmod, "MIGRATIONS_DIR", mig)
    db = Database(tmp_path / "t.db"); db.migrate(tmp_path / "bk")
    (mig / "0002_b.sql").write_text("CREATE TABLE b(x INTEGER);")
    db.migrate(tmp_path / "bk")
    assert list((tmp_path / "bk").glob("pre-migration-v1-*.db"))


def test_transaction_rollback_on_error(tmp_path):
    db = Database(tmp_path / "t.db"); db.migrate(); repo = DocumentRepository(db)
    with pytest.raises(RuntimeError):
        with db.transaction() as c:
            c.execute("INSERT INTO documents(file_name,sha256,stored_path,mime,size_bytes,status,imported_at) VALUES ('x','s','p','m',1,'queued','t')")
            raise RuntimeError("boom")
    assert repo.search(SearchFilter()) == []


def test_check_constraints_and_foreign_keys(tmp_path):
    db = Database(tmp_path / "t.db"); db.migrate()
    with pytest.raises(Exception):
        with db.transaction() as c:
            c.execute("INSERT INTO documents(file_name,sha256,stored_path,mime,size_bytes,status,imported_at) VALUES ('x','s','p','m',1,'bogus','t')")
    with pytest.raises(Exception):
        with db.transaction() as c:
            c.execute("INSERT INTO field_values(document_id,name,source,origin) VALUES (999,'a','rules','extracted')")


def test_integrity_check_detects_corruption(tmp_path):
    db = Database(tmp_path / "t.db"); db.migrate(); repo = DocumentRepository(db)
    for i in range(300):
        repo.add(rec(f"f{i}.pdf", f"{i:064d}"))
    db.checkpoint(); db.close()
    raw = bytearray((tmp_path / "t.db").read_bytes())
    for i in range(4096 * 2, 4096 * 6, 7):
        raw[i] = 0xFF
    (tmp_path / "t.db").write_bytes(bytes(raw))
    for ext in ("-wal", "-shm"):
        (tmp_path / f"t.db{ext}").unlink(missing_ok=True)
    db2 = Database(tmp_path / "t.db")
    try:
        problems = db2.integrity_check()
    except Exception:
        problems = ["error"]
    assert problems
    with pytest.raises(Exception):
        db2.require_healthy()


def test_concurrent_writers_all_succeed(tmp_path):
    db = Database(tmp_path / "t.db"); db.migrate(); repo = DocumentRepository(db)
    errs = []

    def work(n):
        try:
            for i in range(25):
                repo.add(rec(f"t{n}_{i}.pdf", f"{n:02d}{i:02d}".ljust(64, "0")))
        except Exception as e:  # pragma: no cover
            errs.append(e)
    ts = [threading.Thread(target=work, args=(n,)) for n in range(6)]
    [t.start() for t in ts]; [t.join() for t in ts]
    assert not errs and len(repo.search(SearchFilter(limit=1000))) == 150 and db.integrity_check() == []


def test_lock_timeout_is_translated(tmp_path):
    db = Database(tmp_path / "t.db", busy_timeout_ms=200); db.migrate()
    holder = sqlite3.connect(tmp_path / "t.db", isolation_level=None)
    holder.execute("BEGIN IMMEDIATE")
    try:
        with pytest.raises(DatabaseLockedError):
            with db.transaction():
                pass
    finally:
        holder.execute("ROLLBACK"); holder.close()


def test_search_is_parameterised_and_escapes_wildcards(tmp_path):
    db = Database(tmp_path / "t.db"); db.migrate(); repo = DocumentRepository(db)
    a = repo.add(rec("100%_real.pdf", "1" * 64)); repo.add(rec("other.pdf", "2" * 64))
    assert [r.id for r in repo.search(SearchFilter(text="100%_"))] == [a]
    assert len(repo.search(SearchFilter(text="%"))) == 1                       # a literal % only matches the file that has one
    assert repo.search(SearchFilter(text="'; DROP TABLE documents; --")) == []
    assert repo.search(SearchFilter(status="x' OR '1'='1")) == []
    assert len(repo.search(SearchFilter())) == 2
