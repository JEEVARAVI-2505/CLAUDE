"""Low-level text helpers shared by the rules extractors."""

from __future__ import annotations

import re
from dataclasses import dataclass

from .money import ParsedAmount, parse_amount

_CUR = r"(?:Rs\.?|INR|USD|EUR|GBP|AED|SGD|CAD|AUD|₹|\$|€|£)"
RE_PCT = re.compile(r"(\d+(?:\.\d+)?)\s*%")
RE_AMOUNT = re.compile(rf"(?<![\w/.\-])(\(?-?)\s?({_CUR}\s*)?(\d[\d.,]*\d|\d)(\)?)(-?)(?![\w/%\-])", re.I)
RE_ID = re.compile(r"[A-Za-z0-9][A-Za-z0-9\-/_.#]*")


@dataclass
class Line:
    page: int
    text: str

    @property
    def low(self) -> str:
        return self.text.lower()


@dataclass
class Amount:
    raw: str
    parsed: ParsedAmount
    currency_marker: str | None
    start: int


def to_lines(pages) -> list[Line]:
    out: list[Line] = []
    for p in pages:
        for raw in p.text.replace("\r", "\n").split("\n"):
            t = raw.replace(" ", " ").replace("\t", "  ").rstrip()
            if t.strip():
                out.append(Line(p.page, t.strip() if len(t) - len(t.lstrip()) > 40 else t.lstrip()))
    return out


def label_regex(label: str) -> tuple[re.Pattern[str], bool]:
    colon = label.endswith(":")
    core = label[:-1] if colon else label
    words = [re.escape(w) for w in re.split(r"\s+", core.strip()) if w]
    body = r"[\s.\-]*".join(words)
    if colon:
        body += r"(?=\s*:)"
    return re.compile(rf"(?<![A-Za-z]){body}(?![A-Za-z])", re.I), colon


def find_label(line: str, label: str) -> tuple[int, int] | None:
    rx, _ = label_regex(label)
    m = rx.search(line)
    return (m.start(), m.end()) if m else None


def after_label(line: str, end: int, keep_sign: bool = False) -> str:
    rest = line[end:]
    pat = r"^[\s:#=]+" if keep_sign else r"^[\s:#.=\-–—]+"
    return re.sub(pat, "", rest)


def first_segment(text: str) -> str:
    """Cut at the first run of 3+ spaces (next column / next label)."""
    return re.split(r"\s{3,}", text.strip(), maxsplit=1)[0].strip()


def amounts_in(text: str) -> list[Amount]:
    cleaned = RE_PCT.sub(lambda m: " " * len(m.group(0)), text)
    out: list[Amount] = []
    for m in RE_AMOUNT.finditer(cleaned):
        raw = (m.group(1) + (m.group(2) or "") + m.group(3) + m.group(4) + m.group(5)).strip()
        pa = parse_amount(raw)
        if pa.value is None:
            continue
        out.append(Amount(raw, pa, (m.group(2) or "").strip() or None, m.start()))
    return out


def first_id(text: str) -> str | None:
    for m in RE_ID.finditer(text):
        tok = m.group(0).rstrip(".,;:")
        if any(c.isdigit() for c in tok) and len(tok) <= 40:
            return tok
    return None


def pct_in(text: str) -> str | None:
    m = RE_PCT.search(text)
    return m.group(1) if m else None
