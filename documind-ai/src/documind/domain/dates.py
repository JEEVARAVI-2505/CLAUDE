"""Date parsing with explicit ambiguity handling (DMY vs MDY)."""

from __future__ import annotations

import re
from dataclasses import dataclass, field
from datetime import date

MONTHS = {m: i for i, m in enumerate(
    ["jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec"], 1)}
_MON = r"(jan(?:uary)?|feb(?:ruary)?|mar(?:ch)?|apr(?:il)?|may|jun(?:e)?|jul(?:y)?|aug(?:ust)?|sep(?:t(?:ember)?)?|oct(?:ober)?|nov(?:ember)?|dec(?:ember)?)"

RE_ISO = re.compile(r"\b(\d{4})[-/.](\d{1,2})[-/.](\d{1,2})\b")
RE_NUMERIC = re.compile(r"\b(\d{1,2})[/\-.](\d{1,2})[/\-.](\d{4}|\d{2})\b")
RE_DMY_TEXT = re.compile(rf"\b(\d{{1,2}})(?:st|nd|rd|th)?[\s\-.,]*{_MON}\.?[\s\-.,]*(\d{{4}}|\d{{2}})\b", re.I)
RE_MDY_TEXT = re.compile(rf"\b{_MON}\.?\s+(\d{{1,2}})(?:st|nd|rd|th)?,?\s+(\d{{4}})\b", re.I)


@dataclass
class ParsedDate:
    value: date | None
    raw: str | None = None
    ambiguous: bool = False
    flags: list[str] = field(default_factory=list)


def _mk(y: int, m: int, d: int) -> date | None:
    try:
        return date(y, m, d)
    except ValueError:
        return None


def _year(y: str, flags: list[str]) -> int:
    n = int(y)
    if len(y) == 2:
        flags.append("two_digit_year")
        return 2000 + n if n < 70 else 1900 + n
    return n


def find_dates(text: str, order: str = "DMY") -> list[ParsedDate]:
    """Find every date in ``text`` in left-to-right order."""
    hits: list[tuple[int, ParsedDate]] = []
    taken: list[tuple[int, int]] = []

    def free(span: tuple[int, int]) -> bool:
        return all(span[1] <= a or span[0] >= b for a, b in taken)

    for rx, kind in ((RE_ISO, "iso"), (RE_DMY_TEXT, "dmy_text"), (RE_MDY_TEXT, "mdy_text"), (RE_NUMERIC, "num")):
        for m in rx.finditer(text):
            if not free(m.span()):
                continue
            flags: list[str] = []
            amb = False
            if kind == "iso":
                d = _mk(int(m[1]), int(m[2]), int(m[3]))
            elif kind == "dmy_text":
                d = _mk(_year(m[3], flags), MONTHS[m[2][:3].lower()], int(m[1]))
            elif kind == "mdy_text":
                d = _mk(int(m[3]), MONTHS[m[1][:3].lower()], int(m[2]))
            else:
                a, b = int(m[1]), int(m[2])
                y = _year(m[3], flags)
                if a > 12 and b <= 12:
                    d = _mk(y, b, a)
                elif b > 12 and a <= 12:
                    d = _mk(y, a, b)
                else:
                    amb = a != b and a <= 12 and b <= 12
                    d = _mk(y, b, a) if order == "DMY" else _mk(y, a, b)
            if d is None:
                continue
            if amb:
                flags.append("ambiguous_date")
            taken.append(m.span())
            hits.append((m.start(), ParsedDate(d, m[0], amb, flags)))
    return [p for _, p in sorted(hits, key=lambda t: t[0])]


def infer_date_order(text: str, default: str = "DMY") -> str:
    """Infer DMY/MDY from unambiguous numeric dates in the document, else default."""
    dmy = mdy = 0
    for m in RE_NUMERIC.finditer(text):
        a, b = int(m[1]), int(m[2])
        if a > 12 >= b:
            dmy += 1
        elif b > 12 >= a:
            mdy += 1
    if dmy and not mdy:
        return "DMY"
    if mdy and not dmy:
        return "MDY"
    return default


def parse_date(raw: str | None, order: str = "DMY") -> ParsedDate:
    if not raw:
        return ParsedDate(None, raw)
    found = find_dates(raw, order)
    return found[0] if found else ParsedDate(None, raw)


def iso(d: date | None) -> str | None:
    return d.isoformat() if d else None
