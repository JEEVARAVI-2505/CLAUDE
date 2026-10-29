# Changelog

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
