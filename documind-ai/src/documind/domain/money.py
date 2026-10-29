"""Locale-tolerant amount parsing and currency detection.

All arithmetic uses :class:`decimal.Decimal`; floats are never used for money.
"""

from __future__ import annotations

import re
from dataclasses import dataclass
from decimal import Decimal, InvalidOperation

CURRENCY_SYMBOLS: dict[str, str] = {
    "₹": "INR", "rs": "INR", "rs.": "INR", "inr": "INR",
    "€": "EUR", "eur": "EUR", "£": "GBP", "gbp": "GBP",
    "usd": "USD", "us$": "USD", "aed": "AED", "sgd": "SGD", "cad": "CAD",
    "aud": "AUD", "jpy": "JPY", "¥": "JPY", "cny": "CNY", "chf": "CHF",
    "zar": "ZAR", "sar": "SAR", "qar": "QAR", "myr": "MYR", "npr": "NPR",
    "bdt": "BDT", "lkr": "LKR", "$": "USD",
}
AMBIGUOUS_SYMBOLS = {"$", "¥"}
KNOWN_CODES = {v for v in CURRENCY_SYMBOLS.values()}
ZERO_DECIMAL = {"JPY"}

_NUM = re.compile(r"^\(?[-+]?\d[\d.,]*\)?-?$")


@dataclass(frozen=True)
class ParsedAmount:
    value: Decimal | None
    ambiguous: bool = False
    note: str | None = None


def clean_number_token(raw: str) -> str:
    """Strip currency markers and whitespace around a numeric token."""
    s = raw.strip().replace(" ", " ")
    s = re.sub(r"(?i)(?<![a-z])(rs\.?|inr|usd|eur|gbp|aed|sgd|cad|aud|us\$)(?![a-z])", "", s)
    s = re.sub(r"[₹€£¥$]", "", s)
    return s.strip().replace(" ", "")


def parse_amount(raw: str | None) -> ParsedAmount:
    """Parse "1,234.56", "1.234,56", "1,23,456.78", "(12.50)", "12.50-" ...

    Returns ``value=None`` when the text is not a number. ``ambiguous`` is set
    when the separator reading is a guess (e.g. "1,234" could be 1.234).
    """
    if raw is None:
        return ParsedAmount(None)
    s = clean_number_token(raw)
    if not s or not s.isascii() or not _NUM.match(s):
        return ParsedAmount(None)
    negative = False
    if s.startswith("(") and s.endswith(")"):
        negative, s = True, s[1:-1]
    if s.endswith("-"):
        negative, s = True, s[:-1]
    if s.startswith("-"):
        negative, s = True, s[1:]
    elif s.startswith("+"):
        s = s[1:]
    if not s or not s[0].isdigit() or not s[-1].isdigit():
        return ParsedAmount(None)

    ambiguous = False
    note = None
    last_dot, last_comma = s.rfind("."), s.rfind(",")
    if last_dot >= 0 and last_comma >= 0:
        dec_sep = "." if last_dot > last_comma else ","
        grp_sep = "," if dec_sep == "." else "."
        if s.count(dec_sep) > 1:
            return ParsedAmount(None)
        int_part, frac = s.rsplit(dec_sep, 1)
        int_part = int_part.replace(grp_sep, "")
    elif last_comma >= 0:
        parts = s.split(",")
        frac_len = len(parts[-1])
        if len(parts) > 2 or frac_len == 3:
            # thousands grouping (Western "1,234,567" or Indian "1,23,456")
            int_part, frac = "".join(parts), ""
            if len(parts) == 2 and frac_len == 3:
                ambiguous, note = True, "single comma read as thousands separator"
        else:
            int_part, frac = parts[0], parts[1]
            ambiguous, note = frac_len != 2, "comma read as decimal separator"
    elif last_dot >= 0:
        parts = s.split(".")
        if len(parts) > 2:  # "1.234.567" European grouping
            int_part, frac = "".join(parts), ""
        else:
            int_part, frac = parts[0], parts[1]
            if len(frac) == 3 and len(int_part) <= 3 and int_part != "0":
                ambiguous, note = True, "single dot read as decimal separator"
    else:
        int_part, frac = s, ""
    try:
        value = Decimal(f"{int_part}.{frac}" if frac else int_part)
    except InvalidOperation:
        return ParsedAmount(None)
    return ParsedAmount(-value if negative else value, ambiguous, note)


def format_decimal(value: Decimal | None) -> str | None:
    """Canonical plain-string form used for storage (no exponent, no grouping)."""
    if value is None:
        return None
    text = format(value, "f")
    if "." in text:
        text = text.rstrip("0").rstrip(".")
    return text or "0"


def to_decimal(text: str | None) -> Decimal | None:
    """Parse a *canonical stored* decimal string; None on empty/invalid."""
    if text is None or str(text).strip() == "":
        return None
    try:
        return Decimal(str(text).strip())
    except InvalidOperation:
        return None


def detect_currency_marker(token: str) -> tuple[str, bool] | None:
    """Return (ISO code, symbol_is_ambiguous) for a currency marker token."""
    key = token.strip().lower()
    if key in CURRENCY_SYMBOLS:
        return CURRENCY_SYMBOLS[key], key in AMBIGUOUS_SYMBOLS
    return None
