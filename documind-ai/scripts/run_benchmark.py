"""Accuracy benchmark on SYNTHETIC documents with known ground truth.

Measures the rules-only pipeline (no AI model is available in the build environment). Digital PDFs use embedded text;
scans go through the real Tesseract OCR. Because the same project wrote both the generator and the extractor, scores are an
upper bound for documents that look like these - NOT an estimate of accuracy on customers' real documents.

    python scripts/run_benchmark.py [--per-layout 14] [--scans 7] [--out docs/ACCURACY_REPORT.md]
"""
from __future__ import annotations

import argparse
import json
import platform
import sys
import tempfile
import time
from collections import defaultdict
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "src"))

from documind import __version__  # noqa: E402
from documind.config import OcrSettings  # noqa: E402
from documind.domain.models import PageText  # noqa: E402
from documind.domain.rules_extractor import extract_with_rules  # noqa: E402
from documind.domain.templates import load_pack  # noqa: E402
from documind.domain.validation import validate  # noqa: E402
from documind.infrastructure.ocr_engine import TesseractEngine  # noqa: E402
from documind.resource import resource_path  # noqa: E402
from documind.samples import LAYOUTS, generate, write_sample  # noqa: E402
from documind.samples_pdf import paginate, render_page_image  # noqa: E402
from documind.infrastructure.pdf_reader import open_source  # noqa: E402
from documind.config import LimitSettings  # noqa: E402

FIELDS = ["document_kind", "supplier_name", "customer_name", "invoice_number", "po_number", "invoice_date", "due_date", "currency",
          "tax_id", "subtotal", "tax_total", "total", "payment_terms"]
CONDITIONS = {"digital_pdf": {}, "scan_clean": {}, "scan_skew3": {"rotate": 3}, "scan_rot90": {"rotate": 90}, "scan_noisy": {"blur": 0.5, "noise": 1}}


def evaluate(doc, res) -> tuple[dict, dict]:
    stats = {f: [0, 0, 0] for f in FIELDS}   # tp, fp, fn
    for f in FIELDS:
        truth = doc.truth.get(f)
        got = res.kind.value if f == "document_kind" else (res.fields[f].value if f in res.fields else None)
        if truth is None and got is None:
            continue
        if got == truth:
            stats[f][0] += 1
        else:
            if got is not None:
                stats[f][1] += 1
            if truth is not None:
                stats[f][2] += 1
    rows_total = len(doc.line_truth)
    rows_ok = 0
    for a, b in zip(doc.line_truth, res.items):
        got = {k: v.value for k, v in b.items()}
        rows_ok += all(got.get(k) == v for k, v in a.items())
    return stats, {"rows_total": rows_total, "rows_ok": rows_ok, "rows_found": len(res.items)}


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--per-layout", type=int, default=14)
    ap.add_argument("--scans", type=int, default=7)
    ap.add_argument("--out", default="docs/ACCURACY_REPORT.md")
    a = ap.parse_args()
    pack = load_pack(resource_path("rules", "labels_default.json"))
    tmp = Path(tempfile.mkdtemp(prefix="dm-bench-"))
    ocr = TesseractEngine(OcrSettings(), tmp)
    ost = ocr.status()
    results = defaultdict(lambda: defaultdict(lambda: {f: [0, 0, 0] for f in FIELDS}))     # cond -> layout -> field -> counts
    lines = defaultdict(lambda: defaultdict(lambda: [0, 0, 0]))
    docs_n = defaultdict(lambda: defaultdict(int))
    arith = {"consistent_docs": 0, "false_error_docs": 0, "false_warning_docs": 0, "injected": 0, "detected": 0, "detected_error": 0}
    t0 = time.time()
    for layout_i, layout in enumerate(LAYOUTS):
        for k in range(a.per_layout):
            seed = 5000 + layout_i * 100 + k
            doc = generate(seed, layout)
            for cond, kw in CONDITIONS.items():
                if cond != "digital_pdf":
                    if not ost.available or k >= a.scans:
                        continue
                    img = render_page_image(doc.lines, **kw)
                    text = ocr.ocr_image(img).text
                else:
                    pdf = tmp / f"{seed}.pdf"; write_sample(doc, pdf)
                    src = open_source(pdf, "application/pdf", LimitSettings())
                    text = "\n".join(src.embedded_text(i) for i in range(src.page_count)); src.close()
                res = extract_with_rules([PageText(page=1, text=text)], pack)
                st, ln = evaluate(doc, res)
                docs_n[cond][layout] += 1
                for f in FIELDS:
                    for i in range(3):
                        results[cond][layout][f][i] += st[f][i]
                for i, key in enumerate(("rows_total", "rows_ok", "rows_found")):
                    lines[cond][layout][i] += ln[key]
                if cond == "digital_pdf":
                    issues = validate(res.kind, res.fields, res.items)
                    arith["consistent_docs"] += 1
                    arith["false_error_docs"] += any(i.severity.value == "error" for i in issues)
                    arith["false_warning_docs"] += any(i.code == "MULTIPLE_CANDIDATES" for i in issues)
                    # inject a one-digit error into the printed grand total / a line total and check it is flagged
                    bad = [(l[:-4] + str((int(l[-4]) + 3) % 10) + l[-3:]) if ("Total" in l and l.rstrip()[-4:-3].isdigit() and "Sub" not in l and "Qty" not in l) else l for l in doc.lines]
                    if bad != doc.lines:
                        r2 = extract_with_rules([PageText(page=1, text="\n".join(bad))], pack)
                        arith["injected"] += 1
                        iss2 = validate(r2.kind, r2.fields, r2.items)
                        arith["detected_error"] += any(i.severity.value == "error" for i in iss2)
                        arith["detected"] += any(i.severity.value == "error" or i.code == "MULTIPLE_CANDIDATES" for i in iss2)
    dt = time.time() - t0

    def prf(c):
        tp, fp, fn = c
        p = tp / (tp + fp) if tp + fp else None
        r = tp / (tp + fn) if tp + fn else None
        return p, r, tp + fn

    def pct(x):
        return "n/a" if x is None else f"{100 * x:.1f}%"

    out = ["# Accuracy report (synthetic data)", "",
           f"Generated by `scripts/run_benchmark.py` for DocuMind {__version__} on {time.strftime('%Y-%m-%d')} "
           f"({platform.platform()}, Python {platform.python_version()}, {ost.version or 'NO Tesseract'}, languages {','.join(ost.languages) or '-'}).", "",
           "## Read this first", "",
           "* **Synthetic data only.** Fictional invoices from `documind.samples` in 7 layouts. The generator and the extractor were written by the same team, "
           "so these numbers are an *upper bound for look-alike documents*. They are **not** an estimate of accuracy on your customers' real documents, "
           "which will be lower and must be measured on a customer sample (see ACCEPTANCE_TESTS.md).",
           "* **Rules-only mode.** No AI model was available in the build environment, so the Ollama path was tested only against a protocol stub. No AI accuracy is claimed.",
           "* Scans were produced by drawing the same text with a clean bitmap font then applying the listed degradation; real scanner noise, stamps, handwriting, "
           "logos, tables with ruling lines and multi-column layouts are not represented.",
           "* Metric: exact string match of the normalised value. Precision = correct / (correct + wrong values produced). Recall = correct / (fields that exist). "
           "A field whose truth is absent and which was not produced is not counted.", "",
           f"Run time {dt:.0f}s. Documents per condition: " + ", ".join(f"{c}={sum(v.values())}" for c, v in docs_n.items()) + ".", ""]
    for cond in docs_n:
        out += [f"## Condition: `{cond}`", "", "| Field | Precision | Recall | Field instances |", "|---|---|---|---|"]
        for f in FIELDS:
            tot = [0, 0, 0]
            for layout in results[cond]:
                for i in range(3):
                    tot[i] += results[cond][layout][f][i]
            p, r, n = prf(tot)
            out.append(f"| {f} | {pct(p)} | {pct(r)} | {n} |")
        lt = [sum(lines[cond][l][i] for l in lines[cond]) for i in range(3)]
        out += [f"| line items (exact row) | - | {pct(lt[1] / lt[0] if lt[0] else None)} | {lt[0]} rows (found {lt[2]}) |", ""]
        out += ["By document type (average recall over the fields above):", "", "| Layout | Docs | Mean field recall |", "|---|---|---|"]
        for layout in LAYOUTS:
            if layout in results[cond]:
                rs = [prf(results[cond][layout][f])[1] for f in FIELDS]
                rs = [x for x in rs if x is not None]
                out.append(f"| {layout} | {docs_n[cond][layout]} | {pct(sum(rs) / len(rs) if rs else None)} |")
        out.append("")
    out += ["## Arithmetic validation", "",
            f"* Internally consistent synthetic invoices flagged with an ERROR (false alarms): **{arith['false_error_docs']} of {arith['consistent_docs']}**.",
            f"* Consistent invoices that got a 'several different values for the total' warning: **{arith['false_warning_docs']} of {arith['consistent_docs']}**.",
            f"* Invoices where one digit of one printed total was altered: flagged (error, or warning that the document shows two different totals) in **{arith['detected']} of {arith['injected']}**; flagged as a hard ERROR in {arith['detected_error']}.",
            "* Not tested: errors that are internally consistent (e.g. wrong quantity and wrong line total together), swapped digits in values never cross-checked (invoice number, dates), and real-world rounding conventions other than those listed.", "",
            "## Known limitations", "",
            "* Scores depend on layout vocabulary in `resources/rules/labels_default.json`; unseen vocabularies need templates (Professional package).",
            "* Dates like 03/04/2025 are *flagged* as ambiguous rather than counted as errors; the benchmark resolves them with the configured day-first default.",
            "* Sample size is small (see counts) so differences of a few percent are not meaningful.", "",
            "<details><summary>Raw counts (JSON)</summary>", "", "```json", json.dumps({c: {l: v for l, v in d.items()} for c, d in results.items()}, indent=1), "```", "</details>"]
    Path(a.out).write_text("\n".join(out), encoding="utf-8")
    print(f"wrote {a.out} in {dt:.0f}s")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
