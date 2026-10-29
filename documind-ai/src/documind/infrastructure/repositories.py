"""Persistence for documents, fields, issues and the audit trail (parameterised SQL only)."""

from __future__ import annotations

import json
import re
import sqlite3
from dataclasses import dataclass
from datetime import datetime, timedelta, timezone

from ..domain.exceptions import NotFoundError
from ..domain.models import DocKind, DocStatus, DocumentRecord, ExtractionInfo, FieldValue, ValidationIssue
from .database import Database, utcnow

SUFFIX_RX = re.compile(r"\b(pvt|private|ltd|limited|llp|llc|inc|corp|co|company|gmbh)\b\.?", re.I)


def normalize_name(name: str | None) -> str:
    if not name:
        return ""
    return re.sub(r"[^a-z0-9]+", " ", SUFFIX_RX.sub("", name.lower())).strip()


def like_escape(s: str) -> str:
    return s.replace("\\", "\\\\").replace("%", "\\%").replace("_", "\\_")


@dataclass
class SearchFilter:
    text: str = ""
    status: str | None = None
    kind: str | None = None
    supplier: str = ""
    date_from: str | None = None  # ISO date, compared with the document date (or import date if none)
    date_to: str | None = None
    min_errors: int | None = None
    limit: int = 500
    offset: int = 0


@dataclass
class Summary:
    id: int
    file_name: str
    status: str
    kind: str
    supplier: str | None
    doc_number: str | None
    doc_date: str | None
    total: str | None
    currency: str | None
    imported_at: str
    errors: int
    warnings: int
    error: str | None
    pages: int


def _row_summary(r: sqlite3.Row) -> Summary:
    return Summary(r["id"], r["file_name"], r["status"], r["kind"], r["supplier"], r["doc_number"], r["doc_date"],
                   r["total"], r["currency"], r["imported_at"], r["issue_errors"], r["issue_warnings"], r["error"], r["pages"])


class DocumentRepository:
    def __init__(self, db: Database) -> None:
        self.db = db

    # ---- create / read ---------------------------------------------------------
    def add(self, rec: DocumentRecord) -> int:
        with self.db.transaction() as c:
            cur = c.execute(
                "INSERT INTO documents(file_name, sha256, stored_path, mime, size_bytes, pages, status, kind, imported_at, info_json)"
                " VALUES (?,?,?,?,?,?,?,?,?,?)",
                (rec.file_name, rec.sha256, rec.stored_path, rec.mime, rec.size_bytes, rec.pages, rec.status.value,
                 rec.kind.value, utcnow(), rec.info.model_dump_json()))
            doc_id = int(cur.lastrowid)
            c.execute("INSERT INTO audit_log(document_id, ts, actor, action, note) VALUES (?,?,?,?,?)",
                      (doc_id, utcnow(), "system", "imported", None))
        return doc_id

    def get(self, doc_id: int) -> DocumentRecord:
        c = self.db.connect()
        r = c.execute("SELECT * FROM documents WHERE id=?", (doc_id,)).fetchone()
        if r is None:
            raise NotFoundError("That document no longer exists.")
        fields: dict[str, FieldValue] = {}
        rows: dict[int, dict[str, FieldValue]] = {}
        for f in c.execute("SELECT * FROM field_values WHERE document_id=? ORDER BY row_index, name", (doc_id,)):
            fv = FieldValue(name=f["name"], value=f["value"], extracted_value=f["extracted_value"], raw=f["raw"], page=f["page"],
                            source=f["source"], origin=f["origin"], confirmed=bool(f["confirmed"]),
                            flags=json.loads(f["flags"] or "[]"), alt=f["alt"])
            if f["row_index"] < 0:
                fields[f["name"]] = fv
            else:
                rows.setdefault(f["row_index"], {})[f["name"]] = fv
        issues = [ValidationIssue(code=i["code"], severity=i["severity"], message=i["message"], field=i["field"], row=i["row_index"],
                                  expected=i["expected"], actual=i["actual"])
                  for i in c.execute("SELECT * FROM validation_issues WHERE document_id=? ORDER BY id", (doc_id,))]
        return DocumentRecord(
            id=r["id"], file_name=r["file_name"], sha256=r["sha256"], stored_path=r["stored_path"], mime=r["mime"],
            size_bytes=r["size_bytes"], pages=r["pages"], status=DocStatus(r["status"]), kind=DocKind(r["kind"]),
            imported_at=_dt(r["imported_at"]), processed_at=_dt(r["processed_at"]), error=r["error"], fields=fields,
            items=[rows[k] for k in sorted(rows)], issues=issues, info=ExtractionInfo.model_validate_json(r["info_json"] or "{}"))

    def exists(self, doc_id: int) -> bool:
        return self.db.connect().execute("SELECT 1 FROM documents WHERE id=?", (doc_id,)).fetchone() is not None

    # ---- save ---------------------------------------------------------------------
    def save_record(self, rec: DocumentRecord, *, processed: bool = False) -> None:
        """Atomically replace fields/items/issues and refresh search columns."""
        assert rec.id is not None
        with self.db.transaction() as c:
            if c.execute("SELECT 1 FROM documents WHERE id=?", (rec.id,)).fetchone() is None:
                raise NotFoundError("That document no longer exists.")
            c.execute("DELETE FROM field_values WHERE document_id=?", (rec.id,))
            c.execute("DELETE FROM validation_issues WHERE document_id=?", (rec.id,))
            rows = [(rec.id, -1, fv) for fv in rec.fields.values()]
            rows += [(rec.id, i, fv) for i, item in enumerate(rec.items) for fv in item.values()]
            c.executemany(
                "INSERT INTO field_values(document_id,row_index,name,value,extracted_value,raw,page,source,origin,confirmed,flags,alt)"
                " VALUES (?,?,?,?,?,?,?,?,?,?,?,?)",
                [(d, i, fv.name, fv.value, fv.extracted_value, fv.raw, fv.page, fv.source, fv.origin, int(fv.confirmed),
                  json.dumps(fv.flags), fv.alt) for d, i, fv in rows])
            c.executemany(
                "INSERT INTO validation_issues(document_id,code,severity,message,field,row_index,expected,actual) VALUES (?,?,?,?,?,?,?,?)",
                [(rec.id, i.code, i.severity.value, i.message, i.field, i.row, i.expected, i.actual) for i in rec.issues])
            num = rec.get("invoice_number") or rec.get("po_number")
            c.execute(
                "UPDATE documents SET status=?, kind=?, error=?, pages=?, supplier=?, doc_number=?, doc_date=?, total=?, currency=?,"
                " issue_errors=?, issue_warnings=?, info_json=?, processed_at=COALESCE(?, processed_at) WHERE id=?",
                (rec.status.value, rec.kind.value, rec.error, rec.pages, rec.get("supplier_name"), num, rec.get("invoice_date"),
                 rec.get("total"), rec.get("currency"), rec.error_count, rec.warning_count, rec.info.model_dump_json(),
                 utcnow() if processed else None, rec.id))

    def set_status(self, doc_id: int, status: DocStatus, error: str | None = None) -> None:
        with self.db.transaction() as c:
            c.execute("UPDATE documents SET status=?, error=?, reviewed_at=CASE WHEN ? IN ('approved','rejected') THEN ? ELSE reviewed_at END,"
                      " approved_at=CASE WHEN ?='approved' THEN ? ELSE NULL END WHERE id=?",
                      (status.value, error, status.value, utcnow(), status.value, utcnow(), doc_id))

    def reset_stuck(self) -> int:
        """Documents left 'processing' by a crash go back to the queue."""
        with self.db.transaction() as c:
            return c.execute("UPDATE documents SET status='queued' WHERE status='processing'").rowcount

    # ---- queries ----------------------------------------------------------------------
    def find_by_sha(self, sha: str) -> list[Summary]:
        return [_row_summary(r) for r in self.db.connect().execute("SELECT * FROM documents WHERE sha256=? ORDER BY id", (sha,))]

    def find_business_duplicates(self, supplier: str | None, number: str | None, exclude_id: int) -> list[Summary]:
        if not supplier or not number:
            return []
        want = normalize_name(supplier)
        out = []
        for r in self.db.connect().execute(
                "SELECT * FROM documents WHERE doc_number=? COLLATE NOCASE AND id<>? AND status<>'rejected' AND status<>'failed'",
                (number, exclude_id)):
            if normalize_name(r["supplier"]) == want:
                out.append(_row_summary(r))
        return out

    def search(self, f: SearchFilter) -> list[Summary]:
        where, args = ["1=1"], []
        if f.text.strip():
            t = f"%{like_escape(f.text.strip())}%"
            where.append("(file_name LIKE ? ESCAPE '\\' OR supplier LIKE ? ESCAPE '\\' OR doc_number LIKE ? ESCAPE '\\')")
            args += [t, t, t]
        if f.supplier.strip():
            where.append("supplier LIKE ? ESCAPE '\\'")
            args.append(f"%{like_escape(f.supplier.strip())}%")
        if f.status:
            where.append("status=?")
            args.append(f.status)
        if f.kind:
            where.append("kind=?")
            args.append(f.kind)
        if f.date_from:
            where.append("COALESCE(doc_date, substr(imported_at,1,10)) >= ?")
            args.append(f.date_from)
        if f.date_to:
            where.append("COALESCE(doc_date, substr(imported_at,1,10)) <= ?")
            args.append(f.date_to)
        if f.min_errors is not None:
            where.append("issue_errors >= ?")
            args.append(f.min_errors)
        sql = f"SELECT * FROM documents WHERE {' AND '.join(where)} ORDER BY imported_at DESC, id DESC LIMIT ? OFFSET ?"
        args += [max(1, min(f.limit, 5000)), max(0, f.offset)]
        return [_row_summary(r) for r in self.db.connect().execute(sql, args)]

    def ids_by_status(self, status: DocStatus) -> list[int]:
        return [r[0] for r in self.db.connect().execute("SELECT id FROM documents WHERE status=? ORDER BY id", (status.value,))]

    # ---- stats (calculated from saved rows) ----------------------------------------------
    def stats(self, since_iso: str | None) -> dict[str, int]:
        c, a = self.db.connect(), ([since_iso] if since_iso else [])
        w = " WHERE imported_at >= ?" if since_iso else ""
        counts = {s: 0 for s in ("queued", "processing", "needs_review", "approved", "rejected", "failed")}
        for r in c.execute(f"SELECT status, COUNT(*) n FROM documents{w} GROUP BY status", a):
            counts[r["status"]] = r["n"]
        disc = c.execute(f"SELECT COUNT(*) FROM documents{w + (' AND' if w else ' WHERE')} issue_errors>0 AND status NOT IN ('failed','rejected')", a).fetchone()[0]
        counts["total"] = sum(v for k, v in counts.items())
        counts["with_discrepancies"] = disc
        counts["processed"] = counts["needs_review"] + counts["approved"] + counts["rejected"]
        return counts

    def per_day(self, days: int) -> list[tuple[str, int]]:
        start = (datetime.now(timezone.utc) - timedelta(days=days - 1)).strftime("%Y-%m-%d")
        got = {r[0]: r[1] for r in self.db.connect().execute(
            "SELECT substr(imported_at,1,10) d, COUNT(*) FROM documents WHERE substr(imported_at,1,10) >= ? GROUP BY d", (start,))}
        base = datetime.now(timezone.utc).date()
        return [((base - timedelta(days=days - 1 - i)).isoformat(), got.get((base - timedelta(days=days - 1 - i)).isoformat(), 0)) for i in range(days)]

    def top_suppliers(self, limit: int = 5) -> list[tuple[str, int]]:
        return [(r[0], r[1]) for r in self.db.connect().execute(
            "SELECT supplier, COUNT(*) n FROM documents WHERE supplier IS NOT NULL AND status IN ('needs_review','approved')"
            " GROUP BY supplier COLLATE NOCASE ORDER BY n DESC, supplier LIMIT ?", (limit,))]

    # ---- audit -----------------------------------------------------------------------------
    def audit(self, doc_id: int, actor: str, action: str, field: str | None = None, row: int | None = None,
              old: str | None = None, new: str | None = None, note: str | None = None) -> None:
        with self.db.transaction() as c:
            c.execute("INSERT INTO audit_log(document_id,ts,actor,action,field,row_index,old_value,new_value,note) VALUES (?,?,?,?,?,?,?,?,?)",
                      (doc_id, utcnow(), actor, action, field, row, old, new, note))

    def audit_trail(self, doc_id: int) -> list[sqlite3.Row]:
        return list(self.db.connect().execute("SELECT * FROM audit_log WHERE document_id=? ORDER BY id", (doc_id,)))

    # ---- deletion ---------------------------------------------------------------------------
    def delete(self, doc_id: int, reason: str, actor: str) -> str | None:
        """Remove every row for a document; returns its stored file path (caller deletes the file)."""
        with self.db.transaction() as c:
            r = c.execute("SELECT stored_path FROM documents WHERE id=?", (doc_id,)).fetchone()
            if r is None:
                raise NotFoundError("That document no longer exists.")
            c.execute("DELETE FROM documents WHERE id=?", (doc_id,))  # cascades fields, issues, audit
            c.execute("INSERT INTO deletion_log(deleted_at, document_id, reason, actor) VALUES (?,?,?,?)", (utcnow(), doc_id, reason, actor))
        return r["stored_path"]

    def older_than(self, days: int, statuses: tuple[str, ...]) -> list[int]:
        cutoff = (datetime.now(timezone.utc) - timedelta(days=days)).strftime("%Y-%m-%dT%H:%M:%S")
        q = ",".join("?" * len(statuses))
        return [r[0] for r in self.db.connect().execute(
            f"SELECT id FROM documents WHERE imported_at < ? AND status IN ({q})", (cutoff, *statuses))]

    def references_path(self, stored_path: str, exclude_id: int) -> bool:
        return self.db.connect().execute("SELECT 1 FROM documents WHERE stored_path=? AND id<>?", (stored_path, exclude_id)).fetchone() is not None


def _dt(s: str | None) -> datetime | None:
    return datetime.fromisoformat(s.replace("Z", "+00:00")) if s else None
