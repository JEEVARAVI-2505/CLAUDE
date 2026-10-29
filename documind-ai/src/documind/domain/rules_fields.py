"""Rules-based extraction of header (scalar) fields from page text.

Label vocabulary comes from the JSON rule pack. Nothing here guesses a value
without recording that it did (``heuristic`` flag) and nothing is invented:
if no evidence is found the field stays ``None``.
"""

from __future__ import annotations

import re
from collections import Counter
from datetime import date

from .dates import find_dates, iso
from .money import CURRENCY_SYMBOLS, format_decimal, to_decimal
from .models import DocKind, FieldValue
from .validation import gstin_checksum_ok
from .rules_text import (Line, after_label, amounts_in, find_label, first_id, first_segment, pct_in)
from .schemas import CustomFieldDef
from .templates import RulePack, TemplateDef

DATE_EXCLUDE = ("due", "delivery", "ship", "expiry", "valid", "print", "birth", "payment")
GSTIN_RX = re.compile(r"\b\d{2}[A-Z]{5}\d{4}[A-Z][1-9A-Z]Z[0-9A-Z]\b")
CODE_RX = re.compile(r"\b(INR|USD|EUR|GBP|AED|SGD|CAD|AUD|JPY|CNY|CHF|ZAR|SAR|QAR|MYR|NPR|BDT|LKR)\b")
SYMBOL_RX = re.compile(r"(₹|€|£|¥|(?<![A-Za-z])Rs\.?(?=\s*\d)|\$)")
TERMS_RX = re.compile(r"\b(net\s*\d{1,3}(?:\s*days)?|due\s+on\s+receipt|cash\s+on\s+delivery|advance(?:\s+payment)?|\d{1,3}\s*days(?:\s+(?:credit|net))?)\b", re.I)
ADDRESS_RX = re.compile(r"\b(?:road|street|floor|nagar|phone|tel|mob|email|gstin|pincode|plot|sector|lane|avenue|near)\b|@|www\.|http|opp\.", re.I)


def fv(name: str, value: str | None, raw: str | None, page: int | None, flags: list[str] | None = None,
       alt: str | None = None) -> FieldValue:
    if value is not None:
        value = re.sub(r"\s+", " ", value).strip()
    return FieldValue(name=name, value=value, extracted_value=value, raw=raw, page=page, source="rules",
                      flags=flags or [], alt=alt)


class ScalarExtractor:
    def __init__(self, lines: list[Line], pack: RulePack, order: str, kind: DocKind,
                 template: TemplateDef | None = None, custom: list[CustomFieldDef] | None = None) -> None:
        self.lines, self.pack, self.order, self.kind = lines, pack, order, kind
        self.template, self.custom = template, custom or []
        self.table_end = 0  # line index after the line-item table (set by caller)

    # ------------------------------------------------------------------ generic
    def _hits(self, key: str, max_start: int | None = None):
        for label in self.pack.labels.get(key, []):
            hits = []
            for i, ln in enumerate(self.lines):
                span = find_label(ln.text, label)
                if span and (max_start is None or span[0] <= max_start):
                    hits.append((i, span))
            if hits:
                yield label, hits

    def _next_plain(self, i: int, max_alpha_label: bool = True) -> str | None:
        if i + 1 < len(self.lines):
            nxt = self.lines[i + 1].text
            if ":" not in nxt[:25] or not max_alpha_label:
                return nxt
        return None

    # --------------------------------------------------------------- field types
    def text_id(self, name: str) -> FieldValue | None:
        for label, hits in self._hits(name):
            for i, (s, e) in hits:
                ln = self.lines[i]
                tok = first_id(first_segment(after_label(ln.text, e)))
                if not tok:
                    nxt = self._next_plain(i)
                    tok = first_id(first_segment(nxt)) if nxt else None
                if tok:
                    return fv(name, tok, tok, ln.page)
        return None

    def text_phrase(self, name: str, labels: list[str] | None = None) -> FieldValue | None:
        pairs = [(lab, hits) for lab, hits in self._hits(name)] if labels is None else []
        for label, hits in pairs:
            for i, (s, e) in hits:
                ln = self.lines[i]
                val = first_segment(after_label(ln.text, e))
                if not val or not re.search(r"[A-Za-z]{2}", val):
                    nxt = self._next_plain(i)
                    val = first_segment(nxt) if nxt else ""
                if val and re.search(r"[A-Za-z]{2}", val) and len(val) <= 80:
                    return fv(name, val, val, ln.page)
        return None

    def date_field(self, name: str, exclude: tuple[str, ...] = ()) -> FieldValue | None:
        for label, hits in self._hits(name):
            for i, (s, e) in hits:
                ln = self.lines[i]
                if exclude and any(x in ln.low[max(0, s - 14):s] for x in exclude):
                    continue
                found = find_dates(after_label(ln.text, e), self.order)
                if not found and i + 1 < len(self.lines):
                    found = find_dates(self.lines[i + 1].text[:40], self.order)
                if found:
                    d = found[0]
                    return fv(name, iso(d.value), d.raw, ln.page, list(d.flags))
        return None

    def money_field(self, name: str, exclude_words: list[str] | None = None) -> FieldValue | None:
        found: FieldValue | None = None
        other_values: set = set()
        for label, hits in self._hits(name, max_start=25):
            cands = []
            for i, (s, e) in hits:
                ln = self.lines[i]
                if exclude_words and label in ("total", "discount"):
                    window = ln.low[max(0, s - 12):e + 14].replace(label, " ", 1)
                    if any(w in window for w in exclude_words):
                        continue
                am = amounts_in(after_label(ln.text, e, keep_sign=True))
                if not am and i + 1 < len(self.lines) and sum(c.isalpha() for c in self.lines[i + 1].text) <= 3:
                    am = amounts_in(self.lines[i + 1].text)
                if am:
                    cands.append((i, am[-1], ln.page))
            if not cands:
                continue
            if found is not None:  # a lower-priority label (e.g. "Total" after "Balance Due"): only used to detect disagreement
                other_values |= {c[1].parsed.value for c in cands}
                continue
            _, a, page = cands[-1]
            flags: list[str] = []
            distinct = {c[1].parsed.value for c in cands}
            alt = None
            if len(distinct) > 1:
                flags.append("multiple_candidates")
                alt = ", ".join(sorted({format_decimal(v) or "" for v in distinct if v != a.parsed.value}))
            if a.parsed.ambiguous:
                flags.append("ambiguous_number")
            found = fv(name, format_decimal(a.parsed.value), a.raw, page, flags, alt)
            other_values.add(a.parsed.value)
            if name != "total":
                break
        if found is not None and name == "total" and len(other_values) > 1 and "multiple_candidates" not in found.flags:
            mine = to_decimal(found.value)
            diff = sorted({format_decimal(v) or "" for v in other_values if v != mine})
            found.flags.append("multiple_candidates")
            found.alt = ", ".join(diff)
        return found

    # ------------------------------------------------------------------ specials
    def currency(self) -> FieldValue | None:
        for _, hits in self._hits("currency"):
            for i, (s, e) in hits:
                rest = after_label(self.lines[i].text, e)
                m = CODE_RX.search(rest.upper())
                if m:
                    return fv("currency", m.group(1), m.group(1), self.lines[i].page)
        counts: Counter[str] = Counter()
        first_page: dict[str, int] = {}
        ambiguous_only: dict[str, bool] = {}
        for ln in self.lines:
            for m in CODE_RX.finditer(ln.text):
                counts[m.group(1)] += 1
                first_page.setdefault(m.group(1), ln.page)
            for m in SYMBOL_RX.finditer(ln.text):
                sym = m.group(1).lower().rstrip(".")
                code = CURRENCY_SYMBOLS.get(sym) or CURRENCY_SYMBOLS.get(sym + ".")
                if code:
                    counts[code] += 1
                    first_page.setdefault(code, ln.page)
                    ambiguous_only[code] = ambiguous_only.get(code, True) and sym in ("$", "¥")
        if not counts:
            if self.template and self.template.default_currency:
                return fv("currency", self.template.default_currency, None, None, ["template_default"])
            return None
        best, _ = counts.most_common(1)[0]
        others = [c for c in counts if c != best]
        flags = []
        if others:
            flags.append("conflicting_currency")
        if ambiguous_only.get(best) and not any(m.group(1) == best for ln in self.lines for m in CODE_RX.finditer(ln.text)):
            flags.append("ambiguous_symbol")
        return fv("currency", best, best, first_page.get(best), flags, ", ".join(sorted(counts)) if others else None)

    @staticmethod
    def _repair_gstin(tok: str) -> str | None:
        """Fix OCR letter/digit swaps in a GSTIN; only accepted if the mod-36 checksum then validates."""
        if len(tok) != 15:
            return None
        to_digit = {"O": "0", "Q": "0", "I": "1", "L": "1", "S": "5", "B": "8", "Z": "2"}
        chars = list(tok.upper())
        for i in (0, 1, 7, 8, 9, 10):
            chars[i] = to_digit.get(chars[i], chars[i])
        cand = "".join(chars)
        return cand if cand != tok.upper() and gstin_checksum_ok(cand) else None

    def tax_id(self) -> FieldValue | None:
        found: list[tuple[str, int]] = []
        repaired = False
        for ln in self.lines:
            for m in GSTIN_RX.finditer(ln.text.upper()):
                if m.group(0) not in [f[0] for f in found]:
                    found.append((m.group(0), ln.page))
            if not found:
                for tok in re.findall(r"\b[0-9A-Z]{15}\b", ln.text.upper()):
                    fixed = self._repair_gstin(tok)
                    if fixed:
                        found.append((fixed, ln.page))
                        repaired = True
                        break
        if found and repaired:
            return fv("tax_id", found[0][0], found[0][0], found[0][1], ["ocr_repair"])
        if found:
            alt = ", ".join(f[0] for f in found[1:]) or None
            return fv("tax_id", found[0][0], found[0][0], found[0][1],
                      ["multiple_candidates"] if alt else [], alt)
        for _, hits in self._hits("tax_id"):
            for i, (s, e) in hits:
                tok = first_id(first_segment(after_label(self.lines[i].text, e)))
                if tok and len(tok) >= 5:
                    return fv("tax_id", tok, tok, self.lines[i].page)
        return None

    def payment_terms(self) -> FieldValue | None:
        for _, hits in self._hits("payment_terms"):
            for i, (s, e) in hits:
                val = first_segment(after_label(self.lines[i].text, e))
                if val and re.search(r"[A-Za-z0-9]{2}", val) and len(val) <= 80:
                    return fv("payment_terms", val, val, self.lines[i].page)
        for ln in self.lines:
            m = TERMS_RX.search(ln.text)
            if m:
                return fv("payment_terms", m.group(0), m.group(0), ln.page, ["heuristic"])
        return None

    def tax_block(self) -> tuple[FieldValue | None, FieldValue | None]:
        """Document-level tax total & rate (explicit label, else sum of components)."""
        total = self.money_field("tax_total")
        rate = None
        if total:
            for _, hits in self._hits("tax_total", max_start=25):
                for i, _ in hits:
                    p = pct_in(self.lines[i].text)
                    if p:
                        rate = fv("tax_rate", p.rstrip("0").rstrip(".") if "." in p else p, p + "%", self.lines[i].page)
                        break
                if rate:
                    break
            return total, rate
        comps = [c for c in self.pack.tax_components]
        pat = re.compile(r"^\W*(?:(?:total|add|plus|output)\s*:?\s*)?(" + "|".join(re.escape(c) for c in sorted(comps, key=len, reverse=True)) + r")\b", re.I)
        found = []
        for i, ln in enumerate(self.lines):
            if i < self.table_end:
                continue
            m = pat.match(ln.text)
            if not m or re.search(r"(invoice|number|\bno\b|gstin|registration|\bid\b|rate\s*:?$)", ln.low):
                continue
            am = amounts_in(ln.text[m.end():])
            if am:
                found.append((m.group(1).lower(), pct_in(ln.text), am[-1], ln.page))
        if not found:
            return None, None
        from decimal import Decimal
        tot = sum((f[2].parsed.value for f in found), Decimal(0))
        raw = "; ".join(f"{f[0].upper()} {f[2].raw}" for f in found)
        rates = [Decimal(f[1]) for f in found if f[1]]
        rate_fv = None
        if rates and len(rates) == len(found):
            rsum = sum(rates, Decimal(0))
            rate_fv = fv("tax_rate", format_decimal(rsum), "+".join(f[1] + "%" for f in found if f[1]), found[0][3],
                         ["derived_sum"] if len(found) > 1 else [])
        return fv("tax_total", format_decimal(tot), raw, found[0][3], ["derived_sum"] if len(found) > 1 else []), rate_fv

    # -------------------------------------------------------------------- parties
    def _looks_like_name(self, text: str) -> bool:
        t = text.strip()
        low = t.lower()
        letters = sum(c.isalpha() for c in t)
        if not (3 <= len(t) <= 70) or letters < 3 or sum(c.isdigit() for c in t) / max(len(t), 1) > 0.2:
            return False
        if ADDRESS_RX.search(t) or low.strip(" :") in self.pack.title_words:
            return False
        if any(low.startswith(w) for w in self.pack.title_words):
            return False
        if re.search(r"\b(invoice|date|number|no\.?|bill to|ship to|total|page)\b\s*[:#]", low) or find_dates(t):
            return False
        return len(t.split()) <= 8

    def letterhead(self) -> FieldValue | None:
        top = [ln for ln in self.lines if ln.page == self.lines[0].page][:20] if self.lines else []
        suffixes = self.pack.company_suffixes
        for ln in top:
            if self._looks_like_name(ln.text) and any(re.search(rf"(?<![a-z]){re.escape(s)}(?![a-z])", ln.low) for s in suffixes):
                return fv("supplier_name", ln.text.strip(), ln.text.strip(), ln.page, ["heuristic"])
        for ln in top:
            if self._looks_like_name(ln.text):
                return fv("supplier_name", ln.text.strip(), ln.text.strip(), ln.page, ["heuristic"])
        return None

    def parties(self) -> tuple[FieldValue | None, FieldValue | None]:
        supplier = self.text_phrase("supplier_name")
        customer = self.text_phrase("customer_name")
        head = self.letterhead()
        if self.kind == DocKind.PURCHASE_ORDER:
            if customer is None and head and (supplier is None or head.value != supplier.value):
                customer = fv("customer_name", head.value, head.raw, head.page, ["heuristic"])
        elif supplier is None and self.kind != DocKind.UNKNOWN:
            supplier = head  # letterhead guesses only for recognised invoices / credit notes
        if self.template and self.template.supplier_name:
            supplier = fv("supplier_name", self.template.supplier_name, None, None, ["template_fixed"])
        if supplier and customer and supplier.value and supplier.value == customer.value:
            customer = None
        return supplier, customer

    def customs(self) -> dict[str, FieldValue]:
        out: dict[str, FieldValue] = {}
        for c in self.custom:
            for kw in c.keywords:
                hit = next(((i, find_label(ln.text, kw)) for i, ln in enumerate(self.lines) if find_label(ln.text, kw)), None)
                if not hit:
                    continue
                i, span = hit
                rest = after_label(self.lines[i].text, span[1], keep_sign=c.type == "money")
                page = self.lines[i].page
                key = f"x_{c.key}"
                if c.type == "date":
                    d = find_dates(rest, self.order)
                    if d:
                        out[key] = fv(key, iso(d[0].value), d[0].raw, page, list(d[0].flags)); break
                elif c.type == "money":
                    a = amounts_in(rest)
                    if a:
                        out[key] = fv(key, format_decimal(a[-1].parsed.value), a[-1].raw, page); break
                else:
                    val = first_segment(rest)
                    if val:
                        out[key] = fv(key, val[:200], val[:200], page); break
        return out


def detect_kind(lines: list[Line], pack: RulePack, forced: str | None = None) -> DocKind:
    if forced:
        return DocKind(forced)
    head = " ".join(ln.low for ln in lines[:25])
    for kind in ("credit_note", "purchase_order", "invoice"):
        if any(m in head for m in pack.kind_markers.get(kind, [])):
            return DocKind(kind)
    whole = " ".join(ln.low for ln in lines)
    for kind in ("credit_note", "purchase_order", "invoice"):
        if any(m in whole for m in pack.kind_markers.get(kind, [])):
            return DocKind(kind)
    return DocKind.UNKNOWN


__all__ = ["ScalarExtractor", "detect_kind", "fv", "date"]
