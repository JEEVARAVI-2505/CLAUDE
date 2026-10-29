# Changelog

## 0.9.1 - usability release
- Home shows the 3-step job with live counts and a single 'what to do next'. Plain-language labels throughout.
- Export by purpose (purchase register, full detail, PO list, payments due) and by period; Summary sheet with totals by month and supplier (currencies never mixed).
- 'Approve all with no warnings' (asks first, audited). Setup wizard asks how dates are written, once.
- Double-click launchers for Windows (`START_DOCUMIND.bat`). How-to banners per step (can be hidden).
- End-to-end month-end test (`tests/integration/test_use_case.py`) and a GUI test of the guided workflow.

## 0.9.0 - release candidate (not yet validated on customer documents)
First complete build.
- Import PDF/PNG/JPG/TIFF with content sniffing, size/page limits, duplicate detection (file hash + supplier/number).
- Embedded PDF text, local Tesseract OCR (auto-rotate, deskew, denoise), rules-only extraction driven by JSON rule packs and templates.
- Optional local Ollama model with strict JSON schema, grounding check and conflict display.
- Independent arithmetic validation (lines, subtotal, tax, total, round-off, credit notes, GSTIN check digit).
- Review screen with preview, field provenance, corrections, audit history, approve/reject/re-open.
- SQLite with migrations, integrity checks, WAL; versioned verified backups and safe restore with rollback.
- Excel/CSV export with typed cells and formula-injection protection; retention and permanent deletion.
- Light/dark UI, dashboard, first-run wizard, demo mode, diagnostics export.
