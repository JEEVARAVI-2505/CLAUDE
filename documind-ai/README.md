# DocuMind AI Professional

Local-first **invoice and purchase-order processing for Windows**. Import PDFs and scans, extract fields and line items, independently check the
arithmetic, review and correct with a full audit trail, then export clean Excel/CSV. Everything runs on the customer's PC: no accounts, no cloud, no
paid AI API, no telemetry. A local AI model (Ollama) is optional; the product is fully usable without it.

> **Status: release candidate 0.9.0.** Engine, GUI, persistence, backup/restore, export, packaging scripts and documentation are implemented and tested on
> Linux (offscreen Qt, real Tesseract 5.3.4). **Not yet verified:** a Windows installer build, clean-machine install, a real Ollama model, real customer
> documents. See `docs/RELEASE_CHECKLIST.md` and `PROGRESS.md`. Extraction results are not guaranteed correct; a person must review every document.

## Quick start (development)
```powershell
powershell -ExecutionPolicy Bypass -File scripts\setup_dev.ps1     # venv + pinned dependencies
.\.venv\Scripts\python.exe -m documind                              # run the app
.\.venv\Scripts\python.exe -m pytest                                # run the tests
powershell -ExecutionPolicy Bypass -File scripts\build_windows.ps1  # tests -> build -> prune -> smoke test -> installer
```
Linux/macOS (tests and development): `python -m venv .venv && .venv/bin/pip install -r requirements-dev.txt && QT_QPA_PLATFORM=offscreen .venv/bin/python -m pytest`

## What is inside
```
src/documind/
  domain/          pure logic, no GUI/DB/AI: money & date parsing, field registry, validation, rules extractor, templates, errors
  application/     services: processing pipeline, AI merge, batch queue, review, export, backup/restore, retention, diagnostics, context (composition root)
  infrastructure/  SQLite + migrations + repositories, file validation/storage, PDF/image readers, Tesseract wrapper, Ollama client, CSV/XLSX writers
  ui/              PySide6 app: main window, pages (lazy), widgets, dialogs, flat design system (light/dark), drawn icons
  resources/       rule pack (JSON), template examples, in-app help (Markdown)
tests/             unit, integration, security, gui, benchmark helpers      scripts/   PowerShell setup/build, prune, benchmark, licences
installer/         Inno Setup script, version info                          docs/      guides, privacy, security, licensing, pricing, acceptance
```
Layering rule: `domain` imports nothing from the others; `application` uses `infrastructure` through plain classes; `ui` calls `application` only.
Differences from the proposed tree: `domain/` has extra modules (rules_*, money, dates, templates); `application/` has `batch`, `review`, `retention`, `diagnostics`, `context`, `issues`, `llm_extraction`; `infrastructure/` has `image_prep`, `export_common`, `file_storage`; samples generator lives in `src/documind/samples*.py` because demo mode ships with the app.

## Design decisions worth knowing
* **No PyMuPDF** (AGPL). PDF text: pypdf; rendering: pypdfium2 (BSD/Apache); images: Pillow; OCR: Tesseract (not bundled).
* **AI is a helper, not the source of truth.** Strict schema; every value must literally appear in the document; conflicts are shown, not resolved; no confidence score is shown because none is calibrated.
* **Values are never silently "fixed".** Validation reports differences; the user decides. Corrections are kept separate from extracted values and audited.
* **Light on resources:** pages are built on first use, no animations or effects, one worker thread, build excludes unused Qt modules. See PROGRESS.md for measured numbers.
* **Python:** 3.11-3.13 supported by the pins; tested on 3.13; use 3.12 for packaging on Windows.

## Documents
`docs/INSTALLATION.md` · `USER_GUIDE.md` · `ADMIN_GUIDE.md` · `PRIVACY.md` · `SECURITY.md` · `TROUBLESHOOTING.md` · `CUSTOMER_HANDOVER.md` · `ACCEPTANCE_TESTS.md` ·
`ACCURACY_REPORT.md` · `COST_MODEL.md` · `RELEASE_CHECKLIST.md` · `EULA_DRAFT.md` · `THIRD_PARTY_NOTICES.md`
