"""Human review: field/line corrections, approval, rejection, audit history."""

from __future__ import annotations

from ..config import AppConfig
from ..domain.dates import iso, parse_date
from ..domain.exceptions import ApprovalBlockedError, InvalidValueError
from ..domain.models import DocKind, DocStatus, DocumentRecord, FieldValue
from ..domain.money import KNOWN_CODES, format_decimal, parse_amount
from ..domain.schemas import LINE_BY_NAME, LINE_FIELDS, SCALAR_BY_NAME
from ..infrastructure.repositories import DocumentRepository
from .issues import revalidate


def normalize_input(ftype: str, text: str, order: str) -> str | None:
    t = text.strip()
    if not t:
        return None
    if ftype == "date":
        d = parse_date(t, order)
        if d.value is None:
            raise InvalidValueError(f"'{t}' is not a valid date. Use e.g. 2025-04-30 or 30/04/2025.")
        return iso(d.value)
    if ftype in ("money", "qty", "rate"):
        a = parse_amount(t.rstrip("%"))
        if a.value is None:
            raise InvalidValueError(f"'{t}' is not a valid number.")
        return format_decimal(a.value)
    if ftype == "currency":
        c = t.upper()
        if c not in KNOWN_CODES and not (len(c) == 3 and c.isalpha()):
            raise InvalidValueError("Enter a 3-letter currency code such as INR, USD or EUR.")
        return c
    return " ".join(t.split())[:300]


class ReviewService:
    def __init__(self, cfg: AppConfig, repo: DocumentRepository) -> None:
        self.cfg, self.repo = cfg, repo

    def _editable(self, doc_id: int) -> DocumentRecord:
        rec = self.repo.get(doc_id)
        if rec.status in (DocStatus.APPROVED, DocStatus.PROCESSING, DocStatus.QUEUED):
            raise ApprovalBlockedError("Re-open this document before editing it." if rec.status == DocStatus.APPROVED
                                       else "This document has not finished processing yet.")
        return rec

    def _save(self, rec: DocumentRecord) -> DocumentRecord:
        revalidate(self.repo, self.cfg, rec)
        self.repo.save_record(rec)
        return rec

    # ---- edits --------------------------------------------------------------------
    def correct_field(self, doc_id: int, name: str, text: str, actor: str = "user") -> DocumentRecord:
        spec = SCALAR_BY_NAME.get(name)
        ftype = spec.type if spec else "text"
        if not spec and not name.startswith("x_"):
            raise InvalidValueError("Unknown field.")
        rec = self._editable(doc_id)
        new = normalize_input(ftype, text, self.cfg.date_order)
        cur = rec.fields.get(name) or FieldValue(name=name, source="user")
        if cur.value == new and cur.origin == "extracted" and not cur.confirmed:
            return rec
        if cur.value == new:
            return rec
        old = cur.value
        cur.value, cur.origin, cur.source, cur.flags, cur.confirmed, cur.alt = new, "corrected", "user", [], False, None
        rec.fields[name] = cur
        self.repo.audit(doc_id, actor, "corrected", field=name, old=old, new=new)
        return self._save(rec)

    def correct_cell(self, doc_id: int, row: int, name: str, text: str, actor: str = "user") -> DocumentRecord:
        if name not in LINE_BY_NAME:
            raise InvalidValueError("Unknown column.")
        rec = self._editable(doc_id)
        if not 0 <= row < len(rec.items):
            raise InvalidValueError("That line no longer exists.")
        new = normalize_input(LINE_BY_NAME[name].type, text, self.cfg.date_order)
        cur = rec.items[row].get(name) or FieldValue(name=name, source="user")
        if cur.value == new:
            return rec
        old = cur.value
        cur.value, cur.origin, cur.source, cur.flags, cur.confirmed = new, "corrected", "user", [], False
        rec.items[row][name] = cur
        self.repo.audit(doc_id, actor, "corrected", field=name, row=row, old=old, new=new)
        return self._save(rec)

    def add_line(self, doc_id: int, actor: str = "user") -> DocumentRecord:
        rec = self._editable(doc_id)
        rec.items.append({s.name: FieldValue(name=s.name, origin="corrected", source="user") for s in LINE_FIELDS if s.name == "description"})
        self.repo.audit(doc_id, actor, "line_added", row=len(rec.items) - 1)
        return self._save(rec)

    def delete_line(self, doc_id: int, row: int, actor: str = "user") -> DocumentRecord:
        rec = self._editable(doc_id)
        if not 0 <= row < len(rec.items):
            raise InvalidValueError("That line no longer exists.")
        gone = rec.items.pop(row)
        self.repo.audit(doc_id, actor, "line_deleted", row=row, old=(gone.get("description").value if gone.get("description") else None))
        return self._save(rec)

    def revert_field(self, doc_id: int, name: str, actor: str = "user") -> DocumentRecord:
        rec = self._editable(doc_id)
        fv = rec.fields.get(name)
        if not fv or fv.origin != "corrected":
            return rec
        old = fv.value
        fv.value, fv.origin, fv.source, fv.confirmed = fv.extracted_value, "extracted", "rules", False
        if fv.value is None:
            rec.fields.pop(name)
        self.repo.audit(doc_id, actor, "reverted", field=name, old=old, new=fv.extracted_value)
        return self._save(rec)

    def set_kind(self, doc_id: int, kind: DocKind, actor: str = "user") -> DocumentRecord:
        rec = self._editable(doc_id)
        if rec.kind != kind:
            self.repo.audit(doc_id, actor, "kind_changed", old=rec.kind.value, new=kind.value)
            rec.kind = kind
            self._save(rec)
        return rec

    # ---- decisions ------------------------------------------------------------------
    def approve(self, doc_id: int, actor: str = "user", override_reason: str | None = None) -> DocumentRecord:
        rec = self._editable(doc_id)
        errors = [i for i in rec.issues if i.severity.value == "error"]
        if errors and not (override_reason and override_reason.strip()):
            raise ApprovalBlockedError(f"{len(errors)} validation error(s) remain. Fix them, or approve with a written reason.")
        for fv in rec.fields.values():
            if fv.value is not None:
                fv.confirmed = True
        for row in rec.items:
            for fv in row.values():
                if fv.value is not None:
                    fv.confirmed = True
        rec.status = DocStatus.APPROVED
        self.repo.save_record(rec)
        self.repo.set_status(doc_id, DocStatus.APPROVED)
        self.repo.audit(doc_id, actor, "approved", note=(f"override: {override_reason.strip()}" if errors else None))
        return self.repo.get(doc_id)

    def reject(self, doc_id: int, reason: str, actor: str = "user") -> DocumentRecord:
        rec = self.repo.get(doc_id)
        self.repo.set_status(doc_id, DocStatus.REJECTED)
        self.repo.audit(doc_id, actor, "rejected", note=reason.strip()[:300] or None)
        return self.repo.get(rec.id or doc_id)

    def reopen(self, doc_id: int, actor: str = "user") -> DocumentRecord:
        rec = self.repo.get(doc_id)
        for fv in list(rec.fields.values()) + [c for r in rec.items for c in r.values()]:
            fv.confirmed = False
        rec.status = DocStatus.NEEDS_REVIEW
        self.repo.save_record(rec)
        self.repo.set_status(doc_id, DocStatus.NEEDS_REVIEW)
        self.repo.audit(doc_id, actor, "reopened")
        return self.repo.get(doc_id)
