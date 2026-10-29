# Progress log

## Done (0.9.0 release candidate)
- Engine: domain (money/date parsing, validation, JSON-driven rules extractor, templates), application services, infrastructure (SQLite+migrations, PDF/image readers, Tesseract, Ollama, exports).
- GUI: flat light/dark design, Overview, Import & Queue, Review (preview + fields + line items + checks + history), History, Export, Settings, Help, first-run wizard.
- Packaging scripts (PowerShell), PyInstaller spec (unused Qt modules excluded), prune script + `--smoke-test`, Inno Setup definition, licence collection.
- Docs: installation, user, admin, privacy, security, troubleshooting, handover, acceptance, cost model/packages, release checklist, EULA draft, third-party notices, accuracy report.

## Verified in this environment (Linux x86_64, Python 3.13, PySide6 6.12 offscreen, real Tesseract 5.3.4 eng+osd)
- Full suite: see "Test results" below (exact counts recorded at the end of the last run).
- Real OCR on synthetic scans incl. rotated 90/180/270, skewed, noisy (tests + benchmark).
- Frozen PyInstaller build starts, processes a generated invoice and renders every page (`DocuMind --smoke-test` -> `SMOKE OK`).
- Measured weight: window visible in ~0.7 s; ~123 MB peak memory at start, ~138 MB after opening every page (Qt itself is most of it); Linux build 159 MB unpacked / 48 MB xz-compressed. **Windows sizes will differ and have not been measured.**

## NOT verified (known gaps - do not claim otherwise)
1. **No Windows build or install was run.** The PowerShell scripts, spec behaviour on Windows, Inno Setup script, icon generation, SmartScreen behaviour, antivirus, long/UNC paths and clean-VM first run are untested.
2. **No real Ollama model was available** (registry unreachable). The AI client is tested against a protocol-compatible stub (success, malformed output, missing model, timeout, down, remote host blocked, proxy ignored, injection/grounding). Output quality of any real model is unknown; model licences were not verifiable online.
3. **No real customer documents.** Benchmark uses synthetic invoices written by the same project (upper bound). See docs/ACCURACY_REPORT.md for the numbers and what they do and do not mean.
4. Only English OCR was tested. Other language packs, handwriting, stamps, ruled tables, multi-column layouts: untested.
5. No accessibility test with a real screen reader; keyboard use and accessible names are covered by automated checks only. High-DPI scaling untested on real displays.
6. No independent security review. No load test beyond hundreds of rows.
7. Backups, exports and stored originals are not encrypted (documented).
8. Support for `cryptography`-dependent AES PDFs is limited to PDFium's empty-password fallback.
9. The application icon is a simple vector monogram drawn in code, not professionally designed artwork (no image tools were available).

## Findings fixed during development (evidence the tests earn their keep)
Amount parser (`Rs.` prefix, non-ASCII digits); GSTIN OCR repair guarded by checksum; row alignment when an optional column is empty and an HSN code is swallowed; letterhead guess on non-invoices; duplicate queue rows in the UI; missing accessible names on Settings inputs; worker signal teardown race; **totals that disagree between labels were not flagged** (found by the benchmark's error-injection, now flagged and regression-tested).

## Next
- Build and verify on a clean Windows 10/11 VM (docs/RELEASE_CHECKLIST.md); record real installer size.
- Pilot with 5-10 target firms; measure accuracy on their documents (docs/ACCEPTANCE_TESTS.md B) and willingness to pay before further investment.
- Evaluate 1-2 local models on real samples; record licences; decide whether AI mode is worth offering.
- Lawyer review of EULA; decide on code signing.
- Optional: encrypted backups; templates for the first customers' suppliers; professional icon.

## Test results (last full run, Linux, Python 3.13, 2026-10-29)
`python -m pytest` -> **211 passed, 2 skipped, 0 failed** (29 s). Skipped: two permission tests that cannot run as root.
Frozen build: `pyinstaller documind.spec` -> `prune_build.py --smoke` -> `SMOKE OK`; 159 MB unpacked, 48 MB xz-compressed (Linux; Windows not measured).
Benchmark (docs/ACCURACY_REPORT.md): synthetic, rules-only; digital PDFs 98 docs, scans 49 docs per condition (real Tesseract); error injection 98/98 flagged, 0/98 false errors.
