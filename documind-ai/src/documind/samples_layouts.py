"""Text layouts for synthetic documents (monospace; max 100 columns)."""

from __future__ import annotations

from datetime import date
from decimal import Decimal


def money(v: Decimal, style: str, decimals: int = 2) -> str:
    s = f"{abs(v):.{decimals}f}"
    ip, _, fp = s.partition(".")
    if style == "indian":
        head, tail = ip[:-3], ip[-3:]
        parts = []
        while len(head) > 2:
            parts.insert(0, head[-2:])
            head = head[:-2]
        if head:
            parts.insert(0, head)
        ip = ",".join(parts + [tail]) if parts else tail
    else:
        ip = f"{int(ip):,}"
    if style == "european":
        out = ip.replace(",", ".") + "," + fp
    else:
        out = ip + "." + fp
    return ("-" if v < 0 else "") + out


def qty(v: Decimal) -> str:
    return str(int(v)) if v == v.to_integral() else str(v)


def dstr(d: date, style: str) -> str:
    if style == "iso":
        return d.isoformat()
    if style == "text":
        return d.strftime("%d %b %Y")
    if style == "mdy":
        return d.strftime("%m/%d/%Y")
    return d.strftime("%d/%m/%Y")


def two(left: str, right: str, width: int = 96, gap: int = 4) -> str:
    return left + " " * max(gap, width - len(left) - len(right)) + right


def right_block(label: str, value: str, width: int = 96, label_w: int = 18) -> str:
    return " " * (width - label_w - 14 - 2) + f"{label:<{label_w}}{value:>14}"


def cur(doc) -> str:
    return {"INR": "Rs. ", "USD": "$", "EUR": "EUR "}.get(doc.currency, "")


def _party(doc, city: str) -> list[str]:
    return [doc.supplier, "14 Industrial Estate Road", f"{city} - 4110{doc.seed % 90 + 10}", ""]


def render(doc, city: str) -> list[str]:
    fn = {"gst_classic": _gst, "gst_igst": _gst, "simple_us": _us, "eu_vat": _eu, "purchase_order": _po,
          "credit_note": _credit, "labels_below": _below}[doc.layout]
    return fn(doc, city)


def _gst(doc, city) -> list[str]:
    m, st = lambda v: money(v, doc.number_style), doc.date_style
    L = _party(doc, city)[:3]
    L += [f"GSTIN: {doc.gstin}", "", two("TAX INVOICE", "ORIGINAL FOR RECIPIENT"), ""]
    doc.shown |= {"tax_id", "due_date", "payment_terms"}
    L.append(two(f"Invoice No: {doc.number}", f"Invoice Date: {dstr(doc.doc_date, st)}", 80))
    if doc.po_number:
        doc.shown.add("po_number")
        L.append(two(f"PO Ref: {doc.po_number}", f"Due Date: {dstr(doc.due_date, st)}", 80))
    else:
        L.append(two("", f"Due Date: {dstr(doc.due_date, st)}", 80))
    L += ["", "Bill To:", doc.customer, "22 Residency Road", ""]
    L.append(f"{'Sl':<4}{'Description':<36}{'HSN':<6}{'Qty':>5} {'Unit':<5}{'Rate':>11}{'Disc%':>6}{'GST%':>6}{'Amount':>16}")
    for n, it in enumerate(doc.items, 1):
        L.append(f"{n:<4}{it.desc[:34]:<36}{it.hsn:<6}{qty(it.qty):>5} {it.unit:<5}{m(it.price):>11}"
                 f"{qty(it.disc_pct):>6}{qty(doc.tax_rate):>6}{m(it.line_total):>16}")
    L.append("-" * 96)
    L.append(right_block("Sub Total", m(doc.subtotal)))
    if doc.tax_mode == "split":
        half = doc.tax_total / 2
        L.append(right_block(f"CGST {qty(doc.tax_rate / 2)}%", m(half)))
        L.append(right_block(f"SGST {qty(doc.tax_rate / 2)}%", m(half)))
    else:
        L.append(right_block(f"IGST {qty(doc.tax_rate)}%", m(doc.tax_total)))
    L.append(right_block("Round Off", m(doc.round_off) if doc.round_off >= 0 else "-" + m(-doc.round_off)))
    L.append(right_block("Grand Total", f"Rs. {m(doc.total)}"))
    L += ["", f"Payment Terms: {doc.terms}", "Thank you for your business."]
    return L


def _us(doc, city) -> list[str]:
    m, st = lambda v: money(v, "western"), doc.date_style
    L = [doc.supplier, "8 Station Lane", f"{city}", "", "INVOICE", ""]
    doc.shown |= {"due_date", "payment_terms"}
    L += [f"Invoice #: {doc.number}", f"Date: {dstr(doc.doc_date, st)}", f"Due Date: {dstr(doc.due_date, st)}",
          f"Terms: {doc.terms}", "", "Bill To:", doc.customer, ""]
    L.append(f"{'Description':<44}{'Qty':>6}{'Unit Price':>16}{'Amount':>16}")
    for it in doc.items:
        d = it.disc_pct
        L.append(f"{it.desc[:42]:<44}{qty(it.qty):>6}{'$' + m(it.price):>16}{'$' + m(it.line_total):>16}")
    L.append("")
    L.append(right_block("Subtotal", "$" + m(doc.subtotal)))
    L.append(right_block(f"Sales Tax ({qty(doc.tax_rate)}%)", "$" + m(doc.tax_total)))
    L.append(right_block("Total", "$" + m(doc.total)))
    L.append(right_block("Balance Due", "$" + m(doc.total)))
    return L


def _eu(doc, city) -> list[str]:
    m, st = lambda v: money(v, "european"), doc.date_style
    doc.shown |= {"due_date", "payment_terms", "tax_id"}
    L = [doc.supplier, "Hauptstrasse 12", f"{city}", f"VAT No: {doc.gstin}", "", "INVOICE", ""]
    L += [f"Invoice Number: {doc.number}", f"Invoice Date: {dstr(doc.doc_date, st)}", f"Due Date: {dstr(doc.due_date, st)}",
          "", "Customer:", doc.customer, ""]
    L.append(f"{'Item':<40}{'Quantity':>9}{'Price':>14}{'VAT %':>8}{'Total':>16}")
    for it in doc.items:
        L.append(f"{it.desc[:38]:<40}{qty(it.qty):>9}{m(it.price):>14}{qty(doc.tax_rate):>8}{m(it.line_total):>16}")
    L.append("")
    L.append(right_block("Subtotal", m(doc.subtotal)))
    L.append(right_block(f"VAT {qty(doc.tax_rate)}%", m(doc.tax_total)))
    L.append(right_block("Total Amount Due", "EUR " + m(doc.total)))
    L += ["", f"Payment Terms: {doc.terms}", "Currency: EUR"]
    return L


def _po(doc, city) -> list[str]:
    m, st = lambda v: money(v, doc.number_style), doc.date_style
    L = [doc.customer, "22 Residency Road", f"{city}", "", "PURCHASE ORDER", ""]
    L += [f"PO Number: {doc.number}", f"Order Date: {dstr(doc.doc_date, st)}", "", "Vendor:", doc.supplier,
          f"GSTIN: {doc.gstin}", ""]
    doc.shown.add("tax_id")
    L.append(f"{'No':<4}{'Item Description':<40}{'Qty':>6}{'Rate':>14}{'Amount':>18}")
    for n, it in enumerate(doc.items, 1):
        L.append(f"{n:<4}{it.desc[:38]:<40}{qty(it.qty):>6}{m(it.price):>14}{m(it.line_total):>18}")
    L.append("")
    L.append(right_block("Sub Total", m(doc.subtotal)))
    if doc.tax_mode != "none":
        L.append(right_block(f"GST {qty(doc.tax_rate)}%", m(doc.tax_total)))
    L.append(right_block("Total", "Rs. " + m(doc.total)))
    return [x for x in L if x is not None]


def _credit(doc, city) -> list[str]:
    m, st = lambda v: money(v, doc.number_style), doc.date_style
    doc.shown.add("tax_id")
    L = [doc.supplier, "101 Market Street", f"{city}", f"GSTIN: {doc.gstin}", "", "CREDIT NOTE", "",
         f"Credit Note No: {doc.number}", f"Credit Note Date: {dstr(doc.doc_date, st)}", "", "Bill To:", doc.customer, ""]
    L.append(f"{'Description':<44}{'Qty':>6}{'Rate':>14}{'Amount':>18}")
    for it in doc.items:
        L.append(f"{it.desc[:42]:<44}{qty(it.qty):>6}{m(it.price):>14}{m(it.line_total):>18}")
    L.append("")
    L.append(right_block("Sub Total", m(doc.subtotal)))
    if doc.tax_mode != "none":
        L.append(right_block(f"GST {qty(doc.tax_rate)}%", m(doc.tax_total)))
    L.append(right_block("Total", "Rs. " + m(doc.total)))
    return L


def _below(doc, city) -> list[str]:
    m, st = lambda v: money(v, doc.number_style), doc.date_style
    doc.shown |= {"due_date", "payment_terms", "tax_id"}
    L = [doc.supplier, "Plot 22, Sector 5", city, f"GSTIN: {doc.gstin}", "", "Invoice", ""]
    L += ["Invoice Number", doc.number, "", "Invoice Date", dstr(doc.doc_date, st), "", "Due Date", dstr(doc.due_date, st), "",
          "Bill To", doc.customer, ""]
    L.append(f"{'Item':<40}{'Quantity':>9}{'Price':>14}{'Total':>18}")
    for it in doc.items:
        words = it.desc.split()
        L.append(f"{' '.join(words[:3]):<40}{qty(it.qty):>9}{m(it.price):>14}{m(it.line_total):>18}")
        if len(words) > 3:
            L.append(" ".join(words[3:]))
    L.append("")
    L.append(right_block("Subtotal", m(doc.subtotal)))
    if doc.tax_mode != "none":
        L.append(right_block(f"Tax {qty(doc.tax_rate)}%", m(doc.tax_total)))
    L.append(right_block("Total", "Rs. " + m(doc.total)))
    L += ["", f"Payment Terms: {doc.terms}"]
    return L
