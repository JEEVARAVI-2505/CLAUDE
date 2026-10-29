"""Rules-based line-item table extraction (header-driven, right-aligned numbers)."""

from __future__ import annotations

import itertools
import re
from decimal import Decimal

from .money import format_decimal, parse_amount, to_decimal
from .models import FieldValue
from .rules_text import Line
from .templates import RulePack
from .validation import line_base

# (regex, column) in priority order; spans are consumed so "Tax Amt" is one column.
HEADER_PATTERNS: list[tuple[str, str]] = [
    (r"(?:tax|gst|vat|igst)\s*(?:amt|amount|value)", "tax_amount"),
    (r"(?:tax|gst|vat|igst|cgst|sgst)\s*(?:rate)?\s*%|rate\s*%", "tax_rate"),
    (r"disc(?:ount)?\s*(?:amt|amount|value)", "discount_amount"),
    (r"disc(?:ount)?\s*%", "discount_pct"),
    (r"disc(?:ount)?", "discount"),
    (r"unit\s*(?:price|cost|rate)|price|rate|mrp|cost", "unit_price"),
    (r"quantity|qty|nos|pcs", "quantity"),
    (r"line\s*total|amount|amt|total|value|net", "line_total"),
    (r"gst|vat|tax|igst", "tax_rate"),
]
OPTIONAL = ("discount_amount", "discount_pct", "discount", "tax_rate", "tax_amount")
RE_TOKEN_NUM = re.compile(r"^(?:Rs\.?|INR|₹|\$|€|£)?\(?-?\d[\d.,]*\)?-?%?$", re.I)


def parse_header(line: str, pack: RulePack) -> list[str] | None:
    low = line.lower()
    taken: list[tuple[int, int]] = []
    cols: list[tuple[int, str]] = []
    for pat, col in HEADER_PATTERNS:
        for m in re.finditer(rf"(?<![a-z])(?:{pat})(?![a-z])", low):
            if all(m.end() <= a or m.start() >= b for a, b in taken):
                taken.append(m.span())
                cols.append((m.start(), col))
    has_desc = any(re.search(rf"(?<![a-z]){re.escape(k)}(?![a-z])", low) for k in pack.line_header_keywords["desc"])
    cols.sort()
    seen: list[str] = []
    for _, c in cols:
        if c not in seen:
            seen.append(c)
    numeric = [c for c in seen]
    if has_desc and "line_total" in numeric and len(numeric) >= 2:
        return numeric
    return None


def _is_num(t: str) -> bool:
    return bool(RE_TOKEN_NUM.match(t)) and any(ch.isdigit() for ch in t)


OCR_ZERO = {")", "|", "]", "O", "o", "()", "(", "\u00b0"}


def _num_tokens(tokens: list[str], need: int, units: set[str], unit_col: bool) -> tuple[list[tuple[int, str]], int]:
    got: list[tuple[int, str]] = []
    i = len(tokens) - 1
    while i >= 0 and len(got) < need:
        t = tokens[i]
        if _is_num(t):
            got.append((i, t))
        elif t in OCR_ZERO and 0 < i < len(tokens) - 1 and _is_num(tokens[i + 1]) and (_is_num(tokens[i - 1]) or len(got) < need - 1):
            got.append((i, "0~" + t))  # lone OCR glyph inside the numeric block: read as 0, flagged for review
        elif t.lower().rstrip(".") in units or re.fullmatch(r"(?i)(rs\.?|inr|₹)", t) or (
                unit_col and re.fullmatch(r"[A-Za-z]{1,7}\.?", t) and i > 0 and _is_num(tokens[i - 1])):
            pass
        else:
            break
        i -= 1
    got.reverse()
    return got, (got[0][0] if got else len(tokens))


def _cell(name: str, tok: str, page: int, flags: list[str] | None = None) -> FieldValue:
    fl = list(flags or [])
    raw = tok
    if tok.startswith("0~"):
        raw, tok = tok[2:], "0"
        fl.append("ocr_repair")
    pa = parse_amount(tok.rstrip("%"))
    val = format_decimal(pa.value)
    fl += ["ambiguous_number"] if pa.ambiguous else []
    return FieldValue(name=name, value=val, extracted_value=val, raw=raw, page=page, source="rules", flags=fl)


def _assign(cols: list[str], toks: list[tuple[int, str]], page: int, flags: list[str]) -> dict[str, FieldValue]:
    row: dict[str, FieldValue] = {}
    for col, (_, tok) in zip(cols, toks):
        name = col
        if col == "discount":
            name = "discount_pct" if tok.endswith("%") else "discount_amount"
        row[name] = _cell(name, tok, page, flags)
    return row


def _arith_ok(row: dict[str, FieldValue]) -> bool:
    base, lt = line_base({k: v for k, v in row.items()}), to_decimal(row["line_total"].value) if "line_total" in row else None
    if base is None or lt is None:
        return False
    cands = [base]
    rate, tax = to_decimal(row.get("tax_rate").value) if "tax_rate" in row else None, \
        to_decimal(row.get("tax_amount").value) if "tax_amount" in row else None
    if rate is not None:
        cands.append(base * (1 + rate / 100))
    if tax is not None:
        cands.append(base + tax)
    return any(abs(c - lt) <= Decimal("0.02") for c in cands)


def extract_items(lines: list[Line], pack: RulePack) -> tuple[list[dict[str, FieldValue]], int]:
    """Return (rows, index of first line after the table)."""
    units = set(pack.units)
    hdr_i, cols = -1, None
    for i, ln in enumerate(lines):
        c = parse_header(ln.text, pack)
        if c:
            hdr_i, cols = i, c
            break
    if cols is None:
        return [], 0
    markers = tuple(pack.totals_row_markers)
    has_hsn = any(h in lines[hdr_i].low for h in pack.line_header_keywords["hsn"])
    hdr_low = re.sub(r"unit[ ]?(?:price|cost|rate)", " ", lines[hdr_i].low)
    unit_col = bool(re.search(r"(?<![a-z])(unit|uom)(?![a-z])", hdr_low))
    rows: list[dict[str, FieldValue]] = []
    end = len(lines)
    for j in range(hdr_i + 1, len(lines)):
        ln = lines[j]
        low = ln.low.strip()
        if parse_header(ln.text, pack):
            continue  # repeated header on next page
        if rows and low.startswith(markers):
            end = j
            break
        if low.startswith(("page ", "continued")) or re.fullmatch(r"-+|=+|_+", low):
            continue
        tokens = ln.text.split()
        got, cut = _num_tokens(tokens, len(cols), units, unit_col)
        desc_tokens = tokens[:cut]
        if not got:
            if rows and "description" in rows[-1] and not low.startswith(markers):
                prev = rows[-1]["description"]
                prev.value = prev.extracted_value = f"{prev.value} {ln.text.strip()}"[:300]
            continue
        if len(got) == 1 and not any(c.isalpha() for t in desc_tokens for c in t):
            continue
        flags: list[str] = []
        req = [c for c in cols if c not in OPTIONAL]
        opt = [c for c in cols if c in OPTIONAL]
        row = None
        if len(got) == len(cols):
            row = _assign(cols, got, ln.page, flags)
            if opt and not _arith_ok(row):
                row = None  # maybe an optional cell is empty and a code (HSN) was swallowed: try narrower assignments
        for k in range(min(len(got), len(cols)) if row is None else 0, len(req) - 1, -1):
            for n_opt in range(0, len(opt) + 1):
                if len(req) + n_opt != k:
                    continue
                tail = got[-k:]
                for keep in itertools.combinations(opt, n_opt):
                    sub = [c for c in cols if c in req or c in keep]
                    cand = _assign(sub, tail, ln.page, ["alignment_uncertain"])
                    if _arith_ok(cand):
                        row, got = cand, tail
                        break
                if row:
                    break
            if row:
                break
        if row is None:
            k = len(got)
            if k == len(cols):
                row = _assign(cols, got, ln.page, [])
            else:
                row = _assign(cols[-k:], got, ln.page, ["alignment_uncertain", "partial_row"])
        cut = got[0][0] if got else cut
        desc_tokens = tokens[:cut]
        if desc_tokens and re.fullmatch(r"\d{1,3}[.)]?", desc_tokens[0]) and len(desc_tokens) > 1:
            desc_tokens = desc_tokens[1:]
        if has_hsn and len(desc_tokens) > 1 and re.fullmatch(r"\d{4,8}", desc_tokens[-1]):
            desc_tokens = desc_tokens[:-1]
        desc = " ".join(desc_tokens).strip()
        if not desc and "line_total" not in row:
            continue
        row["description"] = FieldValue(name="description", value=desc or None, extracted_value=desc or None,
                                        raw=desc or None, page=ln.page, source="rules")
        rows.append(row)
    return rows, end
