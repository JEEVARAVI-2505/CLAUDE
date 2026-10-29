# Security notes

**Status:** self-reviewed with automated tests; **no independent penetration test or audit has been done.** Do not describe the product as "secure" or
"compliant" to customers - describe the specific controls below.

## Threat model
Documents and OCR text are **untrusted**. The user is trusted. The machine is the customer's. No remote attacker has network access to DocuMind because it opens no port.

## Controls and where they are tested
| Risk | Control | Test |
|---|---|---|
| Malicious/mis-labelled files | Content sniffing (magic bytes) must match the extension; size/page/pixel limits; Pillow decompression-bomb cap; password PDFs refused | `test_files_and_pdf.py` |
| Path traversal / unsafe names | Stored path is computed (`files/<xx>/<sha256>.<ext>`), never from the file name; every read/delete is checked to be inside the data folder; `safe_filename()` for derived names; zip-slip checks on restore | `test_files_and_pdf.py`, `test_backup_retention.py` |
| Prompt injection | Document text is placed inside `<document>` markers with a system rule that it is data; AI output must match a strict schema, each value must literally occur in the document, nothing is executed; rules extractor ignores instructions | `test_ollama_and_ai.py`, `test_local_only.py` |
| Code execution | No `eval`/`exec`/`shell=True`/pickle anywhere (test scans the sources); Tesseract is run with an argument list, fixed options, a timeout | `test_no_eval_or_exec_or_shell_in_sources` |
| SQL injection | Parameterised SQL only; LIKE wildcards escaped | `test_search_is_parameterised...` |
| Formula injection | CSV prefix `'`; XLSX text stored as string cells; control characters stripped | `test_formula_injection_neutralised...` |
| Data exfiltration | No telemetry; AI host must be loopback; environment proxies ignored; test blocks all sockets | `test_local_only.py` |
| Resource abuse | Page/size/pixel/batch limits; AI response cap; AI timeout; OCR timeout; zip entry/size caps | various |
| Data corruption | WAL + `synchronous=FULL`, transactions, integrity checks at start, atomic file replacement, verified backups, rollback on failed restore | `test_database.py`, `test_backup_retention.py` |
| Secrets | None stored; no credentials in code or logs; log redaction | `test_log_redaction` |

## Known gaps
* Backups/exports/stored originals are not encrypted by the app (use BitLocker).
* The installer and EXE are unsigned unless you sign them; users may see SmartScreen warnings.
* PDF parsing relies on pypdf and PDFium; keep them updated (security fixes land upstream). Re-run `pip list --outdated` and the test suite before each release.
* A remote AI host can be enabled by an administrator; then documents leave the PC to that host - the setting says so.
* Windows-specific behaviour (file locking, antivirus, long paths, Unicode paths) has **not** been tested in this repository's CI; see RELEASE_CHECKLIST.md.

## Reporting
Send diagnostics and a description to the installer's support contact. Do not send documents unless asked and the customer agrees.
