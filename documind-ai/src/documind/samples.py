"""Synthetic invoice / PO generator with known ground truth.

All parties, addresses and numbers are fictional. Used for demo mode, tests and
the accuracy benchmark. Because the generator and the extractor were written by
the same team, benchmark scores on this data are optimistic upper bounds, not
real-world accuracy (see docs/ACCEPTANCE_TESTS.md).
"""

from __future__ import annotations

import random
from dataclasses import dataclass, field
from datetime import date, timedelta
from decimal import ROUND_HALF_UP, Decimal
from pathlib import Path

from .domain.money import format_decimal
from .samples_pdf import paginate, render_page_image, write_image_pdf, write_text_pdf

Q = Decimal("0.01")
SUPPLIERS = ["Sunrise Building Supplies Pvt Ltd", "Kaveri Hardware Traders", "Harbor Point Industrial Co.",
             "Ashoka Steel Distributors LLP", "BlueRiver Office Mart", "Mehta & Sons Electricals",
             "Greenfield Agro Industries Ltd", "Orchid Packaging Pvt Ltd", "Meridian Tools & Fasteners Ltd"]
CUSTOMERS = ["Lakshmi Constructions", "Patel Infra Projects Pvt Ltd", "Zenith Interiors LLP",
             "Coastal Contractors & Co", "Redwood Procurement Services", "Anand Wholesale Mart"]
STREETS = ["14 Industrial Estate Road", "Plot 22, Sector 5", "8 Station Lane", "101 Market Street, 2nd Floor"]
CITIES = [("Pune", "27"), ("Bengaluru", "29"), ("Chennai", "33"), ("Ahmedabad", "24"), ("Delhi", "07")]
CATALOG = [
    ("Cement OPC 53 Grade 50kg bag", "Bag", "2523", (320, 420)), ("TMT Steel Bar 12mm Fe500D", "Kg", "7214", (55, 75)),
    ("River Sand (washed)", "Cum", "2505", (1400, 2200)), ("Exterior Emulsion Paint 20L", "Pcs", "3208", (3200, 4800)),
    ("Copper Cable 2.5 sq mm 90m coil", "Roll", "8544", (1800, 2600)), ("PVC Pipe 4 inch 6m", "Pcs", "3917", (480, 760)),
    ("A4 Copier Paper 75gsm (500 sheets)", "Ream", "4802", (210, 290)), ("LED Panel Light 15W", "Nos", "9405", (350, 640)),
    ("Safety Helmet ISI marked", "Nos", "6506", (180, 320)), ("Site supervision service", "Hrs", "9954", (600, 1500)),
]


def gstin_with_check(body14: str) -> str:
    alpha = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"
    s = 0
    for i, ch in enumerate(body14):
        v = alpha.index(ch) * (1 if i % 2 == 0 else 2)
        s += v // 36 + v % 36
    return body14 + alpha[(36 - s % 36) % 36]


@dataclass
class Item:
    desc: str
    unit: str
    hsn: str
    qty: Decimal
    price: Decimal
    disc_pct: Decimal
    line_total: Decimal


@dataclass
class SampleDoc:
    layout: str
    kind: str
    supplier: str
    customer: str
    gstin: str
    number: str
    po_number: str | None
    doc_date: date
    due_date: date | None
    currency: str
    items: list[Item]
    tax_mode: str  # split | igst | vat | sales | none
    tax_rate: Decimal
    subtotal: Decimal
    tax_total: Decimal
    total: Decimal
    round_off: Decimal
    terms: str | None
    number_style: str  # indian | western | european
    date_style: str  # dmy | iso | text | mdy
    seed: int = 0
    lines: list[str] = field(default_factory=list)
    shown: set[str] = field(default_factory=set)  # optional fields the layout actually printed

    @property
    def truth(self) -> dict[str, str | None]:
        return {
            "document_kind": self.kind, "supplier_name": self.supplier, "customer_name": self.customer,
            "invoice_number": None if self.kind == "purchase_order" else self.number,
            "po_number": self.number if self.kind == "purchase_order" else (self.po_number if "po_number" in self.shown else None),
            "invoice_date": self.doc_date.isoformat(), "due_date": self.due_date.isoformat() if self.due_date and "due_date" in self.shown else None,
            "currency": self.currency, "tax_id": self.gstin if "tax_id" in self.shown else None,
            "subtotal": format_decimal(self.subtotal), "tax_total": format_decimal(self.tax_total) if self.tax_mode != "none" else None,
            "total": format_decimal(self.total), "payment_terms": self.terms if "payment_terms" in self.shown else None,
        }

    @property
    def line_truth(self) -> list[dict[str, str | None]]:
        return [{"description": i.desc, "quantity": format_decimal(i.qty), "unit_price": format_decimal(i.price),
                 "line_total": format_decimal(i.line_total)} for i in self.items]


LAYOUTS = ["gst_classic", "gst_igst", "simple_us", "eu_vat", "purchase_order", "credit_note", "labels_below"]


def generate(seed: int, layout: str | None = None) -> SampleDoc:
    from .samples_layouts import render
    r = random.Random(seed)
    layout = layout or LAYOUTS[seed % len(LAYOUTS)]
    kind = {"purchase_order": "purchase_order", "credit_note": "credit_note"}.get(layout, "invoice")
    city, state = r.choice(CITIES)
    body = state + "".join(r.choice("ABCDEFGHJKLMNPRSTUVWXY") for _ in range(5)) + f"{r.randrange(10000):04d}" + r.choice("ABCDEFGH") + "1Z"
    n_items = r.randint(2, 6)
    items: list[Item] = []
    style = {"gst_classic": "indian", "gst_igst": "indian", "simple_us": "western", "eu_vat": "european"}.get(layout, r.choice(["indian", "western"]))
    for d, unit, hsn, (lo, hi) in r.sample(CATALOG, n_items):
        qty = Decimal(r.choice([1, 2, 5, 10, 25, 40, 100]))
        price = Decimal(r.randint(lo * 100, hi * 100)) / 100
        disc = Decimal(r.choice([0, 0, 0, 5, 10])) if layout.startswith('gst') else Decimal(0)  # only gst layouts print a Disc% column
        net = (qty * price * (1 - disc / 100)).quantize(Q, ROUND_HALF_UP)
        items.append(Item(d, unit, hsn, qty, price, disc, net))
    subtotal = sum((i.line_total for i in items), Decimal(0))
    mode = {"gst_classic": "split", "gst_igst": "igst", "simple_us": "sales", "eu_vat": "vat"}.get(layout, r.choice(["split", "igst", "none"]))
    rate = {"split": Decimal(18), "igst": Decimal(18), "sales": Decimal(8), "vat": Decimal(20), "none": Decimal(0)}[mode]
    if mode == "split":
        half = (subtotal * rate / 200).quantize(Q, ROUND_HALF_UP)
        tax = half * 2
    else:
        tax = (subtotal * rate / 100).quantize(Q, ROUND_HALF_UP)
    gross = subtotal + tax
    round_off = Decimal(0)
    if layout.startswith("gst"):
        rounded = gross.quantize(Decimal(1), ROUND_HALF_UP)
        round_off, gross = rounded - gross, rounded
    d0 = date(2025, 1, 1) + timedelta(days=r.randrange(0, 330))
    if layout == "simple_us":
        while d0.day <= 12:
            d0 += timedelta(days=1)  # keep US dates unambiguous (day > 12)
    doc = SampleDoc(
        layout=layout, kind=kind, supplier=r.choice(SUPPLIERS), customer=r.choice(CUSTOMERS),
        gstin=("DE" + f"{r.randrange(10**9):09d}") if layout == "eu_vat" else gstin_with_check(body), number=(f"PO-{r.randrange(1000, 9999)}" if kind == "purchase_order"
                                              else f"{'CN' if kind == 'credit_note' else 'INV'}-2025-{r.randrange(1, 9999):04d}"),
        po_number=f"PO-{r.randrange(1000, 9999)}" if kind == "invoice" and r.random() < 0.5 else None,
        doc_date=d0, due_date=None if kind != "invoice" else d0 + timedelta(days=r.choice([15, 30, 45])),
        currency={"simple_us": "USD", "eu_vat": "EUR"}.get(layout, "INR"), items=items, tax_mode=mode, tax_rate=rate,
        subtotal=subtotal, tax_total=tax, total=gross, round_off=round_off,
        terms=r.choice(["Net 30", "Net 15", "Due on receipt"]) if kind == "invoice" else None,
        number_style=style, date_style={"simple_us": "mdy", "eu_vat": "iso"}.get(layout, r.choice(["dmy", "text"])), seed=seed)
    if doc.kind == "invoice" and doc.due_date and doc.terms == "Net 30":
        doc.due_date = doc.doc_date + timedelta(days=30)
    doc.lines = render(doc, city)
    return doc


def write_sample(doc: SampleDoc, path: Path, as_scan: bool = False, **degrade) -> Path:
    """Write ``doc`` as a text PDF, or (as_scan) as an image-only PDF / PNG / JPEG / TIFF."""
    path = Path(path)
    pages = paginate(doc.lines)
    ext = path.suffix.lower()
    if ext == ".pdf" and not as_scan:
        write_text_pdf(pages, path)
    else:
        imgs = [render_page_image(p, seed=doc.seed, **degrade) for p in pages]
        if ext == ".pdf":
            write_image_pdf(imgs, path)
        else:
            fmt = {".png": "PNG", ".jpg": "JPEG", ".jpeg": "JPEG", ".tif": "TIFF", ".tiff": "TIFF"}[ext]
            imgs[0].convert("RGB" if fmt == "JPEG" else imgs[0].mode).save(path, fmt)
    return path


def write_demo_set(folder: Path) -> list[Path]:
    """A small, realistic demo batch (text PDFs plus one scan) for first-run demo mode."""
    folder.mkdir(parents=True, exist_ok=True)
    out = []
    for n, (seed, layout) in enumerate([(101, "gst_classic"), (102, "gst_igst"), (103, "simple_us"),
                                        (104, "purchase_order"), (105, "credit_note"), (106, "labels_below")]):
        out.append(write_sample(generate(seed, layout), folder / f"demo_{n + 1}_{layout}.pdf"))
    out.append(write_sample(generate(107, "gst_classic"), folder / "demo_7_scanned_invoice.png", as_scan=True))
    return out
