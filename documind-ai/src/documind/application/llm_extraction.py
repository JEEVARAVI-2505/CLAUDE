"""Turn untrusted LLM JSON into validated, *grounded* field values and merge with rules output.

An AI value is accepted only if its text literally appears in the document.
Disagreements are surfaced for the reviewer, never silently resolved.
"""

from __future__ import annotations

import json
import re
from typing import Any

from pydantic import BaseModel, ValidationError

from ..domain.dates import iso, parse_date
from ..domain.exceptions import AiMalformedOutputError
from ..domain.models import DocKind, FieldValue, PageText, Severity
from ..domain.money import CURRENCY_SYMBOLS, KNOWN_CODES, format_decimal, parse_amount
from ..domain.rules_extractor import RulesResult
from ..domain.schemas import LINE_FIELDS, SCALAR_BY_NAME, CustomFieldDef, build_llm_model
from ..domain.validation import validate


def _norm(s: str) -> str:
    return re.sub(r"\s+", " ", s).strip().lower()


def extract_json_object(text: str) -> str:
    """Find the first balanced {...} (models sometimes wrap JSON in prose/fences)."""
    s = text.strip()
    s = re.sub(r"^```(?:json)?|```$", "", s, flags=re.M).strip()
    start = s.find("{")
    if start < 0:
        raise AiMalformedOutputError("The AI model did not return JSON.")
    depth, in_str, esc = 0, False, False
    for i in range(start, len(s)):
        ch = s[i]
        if in_str:
            if esc:
                esc = False
            elif ch == "\\":
                esc = True
            elif ch == '"':
                in_str = False
        elif ch == '"':
            in_str = True
        elif ch == "{":
            depth += 1
        elif ch == "}":
            depth -= 1
            if depth == 0:
                return s[start:i + 1]
    raise AiMalformedOutputError("The AI model returned incomplete JSON.")


def parse_payload(text: str, custom: list[CustomFieldDef]) -> BaseModel:
    model = build_llm_model(custom)
    try:
        return model.model_validate(json.loads(extract_json_object(text)))
    except AiMalformedOutputError:
        raise
    except (ValueError, ValidationError) as e:
        raise AiMalformedOutputError("The AI model's answer did not match the required format.", detail=str(e)[:400]) from e


class Merger:
    def __init__(self, pages: list[PageText], order: str) -> None:
        self.pages, self.order = pages, order
        self.page_norm = [(p.page, _norm(p.text)) for p in pages]
        self.doc_norm = " ".join(t for _, t in self.page_norm)
        self.discarded = 0

    def locate(self, raw: str) -> int | None:
        n = _norm(raw)
        for page, t in self.page_norm:
            if n and n in t:
                return page
        return None

    def to_field(self, name: str, raw: str | None, ftype: str) -> FieldValue | None:
        if raw is None or not str(raw).strip():
            return None
        raw = str(raw).strip()
        page = self.locate(raw)
        if page is None:
            self.discarded += 1
            return FieldValue(name=name, flags=["ungrounded_ai"], raw=raw, source="llm")
        value: str | None
        flags: list[str] = []
        if ftype == "date":
            pd = parse_date(raw, self.order)
            value, flags = iso(pd.value), list(pd.flags)
        elif ftype in ("money", "qty", "rate"):
            pa = parse_amount(raw.rstrip("%"))
            value = format_decimal(pa.value)
            flags = ["ambiguous_number"] if pa.ambiguous else []
        elif ftype == "currency":
            key = raw.strip().lower()
            value = raw.upper() if raw.upper() in KNOWN_CODES else CURRENCY_SYMBOLS.get(key)
        else:
            value = re.sub(r"\s+", " ", raw)[:300]
        if value is None:
            self.discarded += 1
            return FieldValue(name=name, flags=["ungrounded_ai"], raw=raw, source="llm")
        return FieldValue(name=name, value=value, extracted_value=value, raw=raw, page=page, source="llm", flags=flags)


def merge(payload: BaseModel, rules: RulesResult, pages: list[PageText], order: str, custom: list[CustomFieldDef],
          validate_opts) -> tuple[DocKind, dict[str, FieldValue], list[dict[str, FieldValue]], list[str]]:
    m = Merger(pages, order)
    data: dict[str, Any] = payload.model_dump()
    notes: list[str] = []
    kind = rules.kind
    if kind == DocKind.UNKNOWN and data.get("document_kind") and data["document_kind"] != "unknown":
        kind = DocKind(data["document_kind"])
    fields = {k: v.model_copy(deep=True) for k, v in rules.fields.items()}
    specs = {**{n: s.type for n, s in SCALAR_BY_NAME.items()}, **{f"x_{c.key}": c.type for c in custom}}
    for name, ftype in specs.items():
        item = data.get(name)
        if not item:
            continue
        ai = m.to_field(name, item.get("value"), ftype)
        if ai is None:
            continue
        cur = fields.get(name)
        if ai.value is None:  # ungrounded
            if cur is not None and "ungrounded_ai" not in cur.flags:
                cur.flags.append("ungrounded_ai")
            continue
        if cur is None or cur.value is None:
            fields[name] = ai
        elif cur.value == ai.value:
            cur.source = "rules+llm"
        else:
            cur.flags.append("conflict_ai")
            cur.alt = ai.value
    if kind == DocKind.PURCHASE_ORDER:
        fields.pop("invoice_number", None)
    # --- line items -----------------------------------------------------------
    ai_rows: list[dict[str, FieldValue]] = []
    for row in data.get("line_items", []):
        cells: dict[str, FieldValue] = {}
        for spec in LINE_FIELDS:
            fvv = m.to_field(spec.name, row.get(spec.name), spec.type)
            if fvv is not None and fvv.value is not None:
                cells[spec.name] = fvv
        if "line_total" in cells or "description" in cells:
            ai_rows.append(cells)
    items = rules.items
    if ai_rows:
        def errs(rows: list[dict[str, FieldValue]]) -> int:
            return sum(1 for i in validate(kind, {}, rows, validate_opts) if i.code == "LINE_MISMATCH" and i.severity == Severity.ERROR)
        if not rules.items:
            items = ai_rows
        elif len(ai_rows) != len(rules.items):
            notes.append(f"AI found {len(ai_rows)} line items but rules found {len(rules.items)}; showing the set with fewer arithmetic errors.")
            items = ai_rows if errs(ai_rows) < errs(rules.items) else rules.items
    if m.discarded:
        notes.append(f"{m.discarded} AI-suggested value(s) were discarded because they do not appear in the document.")
    return kind, fields, items, notes
