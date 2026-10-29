"""Exports approved (or chosen) documents to XLSX / CSV."""

from __future__ import annotations

from dataclasses import dataclass, field
from decimal import Decimal
from datetime import datetime
from pathlib import Path
from typing import Any

from .. import __version__
from ..config import AppConfig, Paths
from ..domain.exceptions import ExportError
from ..domain.models import DocStatus
from ..domain.money import to_decimal
from ..infrastructure.export_common import (LINE_COLUMNS, PRESETS, available_columns, doc_row, line_rows)
from ..infrastructure.export_csv import write_csv
from ..infrastructure.export_xlsx import write_xlsx
from ..infrastructure.file_storage import safe_filename
from ..infrastructure.repositories import DocumentRepository, SearchFilter


ZERO = Decimal(0)


def build_summary(recs) -> list[dict[str, Any]]:
    """Totals by month and by supplier. Currencies are never mixed: every row carries its own currency."""
    by_month: dict[tuple[str, str], list] = {}
    by_supplier: dict[tuple[str, str], list] = {}
    for r in recs:
        cur = r.get("currency") or "(no currency)"
        sub, tax, tot = (to_decimal(r.get(k)) or ZERO for k in ("subtotal", "tax_total", "total"))
        month = (r.get("invoice_date") or "")[:7] or "(no date)"
        supplier = r.get("supplier_name") or "(no supplier)"
        for table, key in ((by_month, (month, cur)), (by_supplier, (supplier, cur))):
            acc = table.setdefault(key, [0, ZERO, ZERO, ZERO])
            acc[0] += 1; acc[1] += sub; acc[2] += tax; acc[3] += tot
    rows: list[dict[str, Any]] = []
    for title, table in (("By month", by_month), ("By supplier", by_supplier)):
        for (name, cur), (n, sub, tax, tot) in sorted(table.items()):
            rows.append({"view": title, "name": name, "currency": cur, "documents": n, "subtotal": sub, "tax": tax, "total": tot})
    return rows


@dataclass
class ExportResult:
    paths: list[Path] = field(default_factory=list)
    documents: int = 0
    line_rows: int = 0
    skipped_unapproved: int = 0


class ExportService:
    def __init__(self, cfg: AppConfig, repo: DocumentRepository, paths: Paths) -> None:
        self.cfg, self.repo, self.paths = cfg, repo, paths

    def default_path(self, fmt: str) -> Path:
        folder = Path(self.cfg.export.folder) if self.cfg.export.folder else self.paths.exports
        folder.mkdir(parents=True, exist_ok=True)
        return folder / safe_filename(f"documind-export-{datetime.now():%Y%m%d-%H%M%S}.{fmt}")

    def export(self, dest: Path, *, fmt: str | None = None, columns: list[str] | None = None, doc_ids: list[int] | None = None,
               approved_only: bool | None = None, include_lines: bool | None = None, date_from: str | None = None,
               date_to: str | None = None, preset: str | None = None, include_summary: bool | None = None) -> ExportResult:
        ex = self.cfg.export
        if preset:
            if preset not in PRESETS:
                raise ExportError("Unknown export type.")
            _l, _d, pcols, plines, psummary = PRESETS[preset]
            columns = columns or pcols
            include_lines = plines if include_lines is None else include_lines
            include_summary = psummary if include_summary is None else include_summary
        fmt = fmt or ex.format
        approved_only = ex.approved_only if approved_only is None else approved_only
        include_lines = ex.include_line_items if include_lines is None else include_lines
        catalog = available_columns(self.cfg.custom_fields)
        keys = [c for c in (columns or ex.columns) if c in catalog]
        if not keys:
            raise ExportError("Choose at least one column to export.")
        dest = Path(dest)
        if dest.is_dir():
            raise ExportError("Choose a file name, not a folder.")
        if not dest.parent.is_dir():
            raise ExportError("The destination folder does not exist.")
        if doc_ids is None:
            sums = self.repo.search(SearchFilter(status=DocStatus.APPROVED.value if approved_only else None, date_from=date_from,
                                                 date_to=date_to, limit=5000))
            doc_ids = [s.id for s in sums if s.status in ("approved", "needs_review")]
            doc_ids.reverse()
        recs = [self.repo.get(i) for i in doc_ids]
        res = ExportResult()
        if approved_only:
            kept = [r for r in recs if r.status == DocStatus.APPROVED]
            res.skipped_unapproved = len(recs) - len(kept)
            recs = kept
        rows: list[dict[str, Any]] = [doc_row(r, keys, catalog, approved_only_fields=approved_only) for r in recs]
        lines = [lr for r in recs for lr in line_rows(r)] if include_lines else []
        res.documents, res.line_rows = len(rows), len(lines)
        headers = [catalog[k][0] for k in keys]
        kinds = [catalog[k][1] for k in keys]
        lkeys = list(LINE_COLUMNS)
        lheaders, lkinds = [LINE_COLUMNS[k][0] for k in lkeys], [LINE_COLUMNS[k][1] for k in lkeys]
        if fmt == "xlsx":
            issues = [{"document_id": r.id, "source_file": r.file_name, "severity": i.severity.value, "code": i.code, "message": i.message}
                      for r in recs for i in r.issues]
            sheets = [("Documents", headers, keys, kinds, rows)]
            if include_summary and recs:
                sheets.append(("Summary", ["View", "Name", "Currency", "Documents", "Taxable value", "Tax", "Total"],
                               ["view", "name", "currency", "documents", "subtotal", "tax", "total"],
                               ["text", "text", "text", "int", "money", "money", "money"], build_summary(recs)))
            if include_lines:
                sheets.append(("Line items", lheaders, lkeys, lkinds, lines))
            sheets.append(("Validation", ["Document ID", "Source file", "Severity", "Code", "Message"],
                           ["document_id", "source_file", "severity", "code", "message"], ["int", "text", "text", "text", "text"], issues))
            sheets.append(("About", ["Item", "Value"], ["k", "v"], ["text", "text"],
                           [{"k": "Generated by", "v": f"DocuMind AI Professional {__version__}"},
                            {"k": "Generated at", "v": datetime.now().strftime("%Y-%m-%d %H:%M")},
                            {"k": "Note", "v": "Values were extracted by software and reviewed by a person. Verify before relying on them."}]))
            write_xlsx(dest, sheets)
            res.paths.append(dest)
        elif fmt == "csv":
            write_csv(dest, headers, keys, rows, ex.csv_delimiter)
            res.paths.append(dest)
            if include_lines:
                lp = dest.with_name(dest.stem + "_lines" + dest.suffix)
                write_csv(lp, lheaders, lkeys, lines, ex.csv_delimiter)
                res.paths.append(lp)
        else:
            raise ExportError("Unknown export format.")
        return res
