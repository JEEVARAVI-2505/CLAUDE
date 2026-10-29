"""Column catalogue, typed row building and formula-injection protection."""

from __future__ import annotations

import re
from datetime import date
from decimal import Decimal
from typing import Any

from ..domain.models import DocumentRecord, Severity
from ..domain.money import to_decimal
from ..domain.schemas import LINE_FIELDS, SCALAR_FIELDS, CustomFieldDef

# key -> (header, type)
BASE_COLUMNS: dict[str, tuple[str, str]] = {
    "document_id": ("Document ID", "int"), "source_file": ("Source file", "text"), "document_kind": ("Document type", "text"),
    **{f.name: (f.label, f.type if f.type in ("date", "money", "rate") else "text") for f in SCALAR_FIELDS},
    "validation_status": ("Validation status", "text"), "review_status": ("Review status", "text"),
    "imported_at": ("Imported at", "text"), "extraction_mode": ("Extraction mode", "text"),
}
LINE_COLUMNS: dict[str, tuple[str, str]] = {
    "source_file": ("Source file", "text"), "document_id": ("Document ID", "int"), "supplier_name": ("Supplier", "text"),
    "invoice_number": ("Document no.", "text"), "line_no": ("Line", "int"),
    **{f.name: (f.label, "money" if f.type == "money" else "qty" if f.type == "qty" else "rate" if f.type == "rate" else "text") for f in LINE_FIELDS},
}
_DANGEROUS = ("=", "+", "-", "@", "\t", "\r")
_CTRL = re.compile(r"[\x00-\x08\x0b\x0c\x0e-\x1f]")


def available_columns(custom: list[CustomFieldDef]) -> dict[str, tuple[str, str]]:
    cols = dict(BASE_COLUMNS)
    for c in custom:
        cols[f"x_{c.key}"] = (c.label, "date" if c.type == "date" else "money" if c.type == "money" else "text")
    return cols


def clean_text(s: str) -> str:
    return _CTRL.sub("", s)


def neutralize_formula(s: str) -> str:
    """Prefix risky leading characters so spreadsheets never evaluate the cell (OWASP CSV injection)."""
    s = clean_text(s)
    return "'" + s if s.lstrip(" ")[:1] in _DANGEROUS else s


def validation_status(rec: DocumentRecord) -> str:
    if any(i.severity == Severity.ERROR for i in rec.issues):
        return "Errors"
    if any(i.severity == Severity.WARNING for i in rec.issues):
        return "Warnings"
    return "OK"


def typed(value: str | None, kind: str) -> Any:
    if value is None or value == "":
        return None
    if kind == "date":
        try:
            return date.fromisoformat(value)
        except ValueError:
            return value
    if kind in ("money", "qty", "rate"):
        d = to_decimal(value)
        return d if d is not None else value
    return value


def doc_row(rec: DocumentRecord, columns: list[str], catalog: dict[str, tuple[str, str]],
            approved_only_fields: bool = False) -> dict[str, Any]:
    out: dict[str, Any] = {}
    for key in columns:
        kind = catalog.get(key, (key, "text"))[1]
        if key == "document_id":
            out[key] = rec.id
        elif key == "source_file":
            out[key] = rec.file_name
        elif key == "document_kind":
            out[key] = rec.kind.value
        elif key == "validation_status":
            out[key] = validation_status(rec)
        elif key == "review_status":
            out[key] = rec.status.value
        elif key == "imported_at":
            out[key] = rec.imported_at.strftime("%Y-%m-%d %H:%M") if rec.imported_at else None
        elif key == "extraction_mode":
            out[key] = rec.info.mode
        else:
            fv = rec.fields.get(key)
            out[key] = None if fv is None or fv.value is None or (approved_only_fields and not fv.confirmed) else typed(fv.value, kind)
    return out


def line_rows(rec: DocumentRecord) -> list[dict[str, Any]]:
    rows = []
    for i, it in enumerate(rec.items, 1):
        r: dict[str, Any] = {"source_file": rec.file_name, "document_id": rec.id, "supplier_name": rec.get("supplier_name"),
                             "invoice_number": rec.get("invoice_number") or rec.get("po_number"), "line_no": i}
        for f in LINE_FIELDS:
            fv = it.get(f.name)
            r[f.name] = typed(fv.value if fv else None, LINE_COLUMNS[f.name][1])
        rows.append(r)
    return rows


def fmt_csv(v: Any) -> str:
    if v is None:
        return ""
    if isinstance(v, Decimal):
        return format(v, "f")
    if isinstance(v, date):
        return v.isoformat()
    if isinstance(v, str):
        return neutralize_formula(v)
    return str(v)
