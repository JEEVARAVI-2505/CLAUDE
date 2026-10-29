# Third-party notices and licence inventory

Versions below are the exact versions the test suite in this repository was run against (read from the installed package metadata with
`scripts/license_inventory.py` on 2026-10-29). Licence identifiers come from each package's own metadata. **Official-source and
obligation columns are the author's summary and must be re-checked by whoever distributes the product** (this environment could not reach
the upstream sites to re-verify, and nothing here is legal advice).

## Bundled in the installer (runtime dependencies, transitive closure)

| Package | Version | Licence (from metadata) | Official source | What the licence requires of us |
|---|---|---|---|---|
| PySide6-Essentials (Qt for Python; Qt 6 libraries) | 6.12.0 | LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only (we use it under **LGPL-3.0**) | https://pypi.org/project/PySide6-Essentials/ · https://www.qt.io/qt-for-python | Ship the LGPL text and copyright notices; link dynamically (done: one-folder build, Qt DLLs are separate files); let the user replace the Qt libraries (do not lock them); state that Qt is used and where source can be obtained. Using only QtCore/QtGui/QtWidgets (no GPL-only add-on modules). |
| shiboken6 | 6.12.0 | LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only (use LGPL-3.0) | same project | Same as above. |
| pydantic | 2.14.2 | MIT | https://github.com/pydantic/pydantic | Keep copyright + licence text. |
| pydantic-core | 2.50.2 | MIT | https://github.com/pydantic/pydantic-core | Keep copyright + licence text. |
| annotated-types | 0.8.0 | MIT | https://github.com/annotated-types/annotated-types | Keep notice. |
| typing-extensions | 4.16.0 | PSF-2.0 | https://github.com/python/typing_extensions | Keep notice. |
| typing-inspection | 0.4.4 | MIT | https://github.com/pydantic/typing-inspection | Keep notice. |
| pypdf | 6.21.4 | BSD-3-Clause | https://github.com/py-pdf/pypdf | Keep notice; no endorsement use of names. |
| pypdfium2 (+ PDFium binaries) | 5.14.0 | BSD-3-Clause OR Apache-2.0 for the wrapper; PDFium itself BSD-3-Clause/Apache-2.0 with third-party notices (FreeType, etc.) | https://github.com/pypdfium2-team/pypdfium2 | Ship the bundled "dependency licenses" file (collected by `scripts/collect_licenses.py`). |
| Pillow | 13.0.0 | MIT-CMU (HPND-style) | https://github.com/python-pillow/Pillow | Keep notice. Its bundled codec libraries have their own permissive notices (included in its licence files). |
| openpyxl | 3.1.5 | MIT | https://foss.heptapod.net/openpyxl/openpyxl | Keep notice. |
| et-xmlfile | 2.0.0 | MIT | https://foss.heptapod.net/openpyxl/et_xmlfile | Keep notice. |
| CPython runtime + stdlib (inside the PyInstaller bundle) | 3.12/3.13 | PSF-2.0 | https://www.python.org/ | Keep PSF licence text. |
| PyInstaller bootloader | 6.22.3 (build tool) | GPL-2.0-or-later **with a special exception** permitting use for any program, including proprietary | https://pyinstaller.org | The exception covers the bootloader included in the output; no GPL obligations pass to DocuMind. Re-confirm at release. |

**Deliberately not used:** PyMuPDF/MuPDF (AGPL-3.0 or commercial licence) - rejected so no source-disclosure obligation arises. `cryptography`
is not bundled (so AES-encrypted PDFs that need a password are rejected, and owner-password-only AES files fall back to PDFium).
Qt Charts / Data Visualization / WebEngine (GPL-only or heavy) are not used; charts are drawn with QPainter.

## Installed separately by the customer / installer (NOT redistributed by DocuMind)

| Component | Licence (author's understanding - verify before relying) | Notes |
|---|---|---|
| Tesseract OCR 5.x and `tessdata` language files | Apache-2.0 | Download from the official project or the Windows installer it recommends. Not bundled. The third-party Windows installer carries its own bundle of notices - read them if you ever redistribute it. |
| Ollama | MIT | Not bundled; contacts only the configured local server. |
| AI model weights (e.g. Qwen2.5, Llama 3.x, Mistral, Gemma, Phi) | **Differs per model and per size** (Apache-2.0, MIT, or custom "community"/use-policy licences with attribution, naming and user-count conditions) | **Not verified from this environment (registries were unreachable).** Before offering any model to a customer, read the licence on the model's official page, check commercial-use, redistribution and attribution terms, and record the model, version and licence in the customer handover. DocuMind never downloads a model by itself and ships none. |

## Network behaviour of components
* DocuMind itself: connects only to the Ollama address configured in Settings (loopback by default). Evidence: `tests/security/test_local_only.py`.
* Tesseract: invoked as a local process; it does not need or use the network.
* Ollama: the customer's Ollama installation may check for updates or download models when the customer asks it to; that is Ollama's behaviour, outside DocuMind.

## Obligations checklist for each release
1. Run `python scripts/license_inventory.py` and update the tables above if any version changed.
2. Run `python scripts/collect_licenses.py <folder>` - the build script does this and ships the folder as `licenses\`.
3. Keep the one-folder (not one-file) layout so the LGPL "replace the library" right stays practical.
4. Re-read the PyInstaller, Qt and pypdfium2 licence pages for changes before shipping.
