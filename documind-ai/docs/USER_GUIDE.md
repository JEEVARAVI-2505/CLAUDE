# User guide

DocuMind reads **invoices, purchase orders and credit notes** (PDF, PNG, JPG, TIFF), shows what it found next to the page, checks the arithmetic, and
lets you approve and export clean data. **A person must check every document before it is relied on.**

## The flow
1. **Import & Queue** (Ctrl+2): drop files or folders. Wrong file types, damaged, password-protected, empty or oversized files are refused with a reason; a bad file
   never stops the others. Files with identical content are skipped by default. Pause, cancel, retry from the queue.
2. **Review** (Ctrl+3): left = documents, middle = page preview (zoom, rotate, page numbers), right = values. Each value shows **Extracted** (read by the
   software), **Corrected** (you changed it), **Validated** (you approved the document) or **Missing**. Click **p.N** to jump to the page it came from; the
   small grey line shows the exact source text, and anything uncertain (ambiguous date, OCR repair, two competing values) is highlighted amber.
3. **Checks tab**: recomputed line totals, subtotal, tax and total; missing required fields; due date before invoice date; possible duplicate; GSTIN check
   digit. DocuMind **never changes a value to make the maths work** - you decide. Errors must be fixed or approved with a written reason (saved in History).
4. **Approve** (Ctrl+Enter) / **Reject** / **Re-open** / **Re-run** (discards your corrections after asking).
5. **Export** (Ctrl+5): choose and reorder columns, Excel or CSV. By default only approved documents are exported, dates and numbers are real
   dates/numbers, and cells starting with `= + - @` are neutralised so a spreadsheet cannot run them.
6. **History** (Ctrl+4): search/filter, open, export selected, delete permanently.

## Reading the warnings
| Message | Meaning | What to do |
|---|---|---|
| Line N: qty x price = X but the line total is Y | Arithmetic differs (also covers discounts and tax-inclusive lines) | Compare with the page; correct the wrong cell, or approve with a reason if the document itself is wrong. |
| Total differs by a small amount (likely round-off) | Within one currency unit; many invoices round to whole rupees | Usually fine. |
| Date could be day-first or month-first | e.g. 03/04/2025 | Confirm; set the regional order in Settings. |
| Another document from the same supplier has the same number | Possible duplicate | Compare, then reject or approve. |
| OCR letter/digit confusion was corrected | e.g. O read as 0 where the checksum proves it | Confirm against the page. |
| AI and rules disagree | Both values are shown | Pick the right one by typing it. |

## Scans and photos
Need Tesseract OCR (see INSTALLATION.md). Scan at 300 dpi, upright, flat, no shadows. Sideways and slightly skewed pages are straightened
automatically; heavy noise, handwriting, stamps over text, or tiny print will reduce accuracy - the Review page shows the OCR engine's own score
(not a probability) and warns when it is low.

## Keyboard
Ctrl+1..6 pages · F1 help · Ctrl+O import · Ctrl+E export · Ctrl+Enter approve · Alt+Up/Down previous/next · PgUp/PgDn preview page · Ctrl+wheel zoom · Tab/Shift+Tab move.

## Limits
Default 50 MB and 100 pages per file, 500 files per import, 50 megapixel images (Settings > Limits).

## What DocuMind does not do
It does not send email, upload documents, give tax or legal advice, or guarantee accuracy. It reads documents in English-style layouts out of the box;
other layouts need a template (Professional package) or a local AI model.
