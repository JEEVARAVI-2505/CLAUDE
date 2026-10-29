# Acceptance tests and measuring accuracy on a customer's documents

Passing the automated suite proves the code behaves as designed; it does **not** prove the product works on a customer's documents. Do this per customer.

## A. Installation acceptance (15 min, tick each)
| # | Check | Pass |
|---|---|---|
| A1 | `DocuMind.exe --smoke-test` prints `SMOKE OK` | ☐ |
| A2 | Start the app from the Start menu; wizard completes; data folder is where the handover sheet says | ☐ |
| A3 | Settings > OCR > Check shows ready (if scans are in scope) with the needed languages | ☐ |
| A4 | Settings > Local AI > Check shows ready (only if AI was agreed) | ☐ |
| A5 | Load demo documents; all reach "Needs review"; open each; page preview works | ☐ |
| A6 | Approve one, export to Excel and CSV; open in Excel; dates/numbers are real; no cell begins with `=` | ☐ |
| A7 | Create backup; restore it into the same install; counts match | ☐ |
| A8 | Delete the demo documents (History > Delete permanently) | ☐ |
| A9 | Unplug network (or disable adapter): import + process + export still work (rules-only) | ☐ |
| A10 | Customer's own printer/scanner file (PDF/JPG) imports | ☐ |

## B. Accuracy on the customer's documents (the number that matters)
1. Ask for **30-100 recent real documents** (copies), covering the suppliers that matter and both digital and scanned. Record counts per type/scan quality.
2. Process them all. For each document, an experienced staff member records, per field, whether the extracted value was **correct / wrong / missing** *before* any
   correction (the History > Review audit trail shows which fields they had to correct - `Corrected` state = it was wrong or missing).
3. Compute per field: correct ÷ total, and the share of documents that needed **zero** corrections. Report the counts, not just percentages, and the sample size.
4. Accept or reject against thresholds **agreed in writing before the test** (example: invoice number and total correct on ≥ 90% of digital documents; every arithmetic
   discrepancy flagged). Do not use thresholds the synthetic benchmark suggests.
5. Record failures by cause (layout/vocabulary -> template; scan quality -> rescan; handwriting -> manual). Re-test after templates.

## C. Reference: automated benchmark (synthetic, optimistic)
See ACCURACY_REPORT.md: synthetic documents with known answers, rules-only, real Tesseract on scans. It shows regressions and the effect of scan degradation. It does not predict customer accuracy.

## D. What the automated suite covers (run `python -m pytest`)
Valid/empty/corrupt/password PDFs; image-only PDFs; PNG/JPG/TIFF (multi-page); extension spoofing; large/bomb files; Unicode and malicious names; ambiguous
dates, currencies, European/Indian number formats; Decimal arithmetic, discounts, tax-inclusive, credit notes, round-off; malformed AI output, missing Ollama,
missing model, timeouts, cancellation; duplicates; migrations and rollback; concurrency and lock errors; XLSX/CSV round trip, Unicode, formula injection; backup,
tamper, zip-slip, interrupted restore; deletion and retention; no-network proof; GUI start, resize, import, errors, cancel, theme contrast (WCAG AA), screen-reader names.

## E. Not covered anywhere in this repository (do before selling)
Clean-Windows install on supported versions (10 and 11), antivirus behaviour, long/UNC paths, high-DPI scaling on real screens, GPU/CPU performance with real AI
models, real scanner output, Tesseract language packs other than English, a real Ollama model's output quality, large (1,000+ document) databases.
