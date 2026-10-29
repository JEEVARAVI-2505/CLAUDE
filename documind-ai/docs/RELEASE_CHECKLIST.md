# Release checklist

## Before building
- [ ] `python -m pytest` green on the release commit (record counts: passed / skipped / failed). Re-run `python scripts/run_benchmark.py` and commit ACCURACY_REPORT.md.
- [ ] Version bumped in `src/documind/__init__.py`, `pyproject.toml`, `installer/version_info.txt`, `installer/documind.iss`; CHANGELOG.md updated (also copied to resources/help).
- [ ] `pip list --outdated`; read upstream security notices for pypdf, pypdfium2, Pillow, Qt, pydantic; update pins in requirements.txt + pyproject; re-run tests.
- [ ] `python scripts/license_inventory.py` compared with THIRD_PARTY_NOTICES.md; any new package has an approved licence (no GPL/AGPL/unclear).
- [ ] EULA / installation agreement reviewed by a lawyer; placeholders filled.
## Build (on a clean Windows 10/11 x64 VM)
- [ ] `scripts\setup_dev.ps1`, `scripts\build_windows.ps1` completes: tests, PyInstaller, prune, **smoke test OK**, licences collected, installer built.
- [ ] Record unpacked size and installer size in the release notes (measure; do not estimate).
- [ ] Optional: code-sign `DocuMind.exe` and the installer (timestamp it). Record that unsigned builds may trigger SmartScreen.
## Clean-machine verification (not done in development)
- [ ] Fresh Windows VM with no Python: install, first-run wizard, demo documents, export, backup/restore, uninstall (data kept).
- [ ] Tesseract absent -> scanned file gives the "OCR not available" message; installed -> scan works; Hindi/other pack if sold.
- [ ] Ollama absent/stopped/model missing -> clear messages, rules-only continues.
- [ ] Unicode user name and path with non-ASCII characters; path on a different drive; read-only program folder.
- [ ] 125%/150% display scaling; keyboard-only run through; Narrator reads field names.
- [ ] Antivirus (Windows Defender) does not quarantine the build; if it does, report the false positive and sign.
- [ ] Network disabled: full workflow works. Network monitor shows no outbound traffic from DocuMind.exe.
## Publish
- [ ] Installer SHA-256 recorded on the handover sheet; release notes list known issues from PROGRESS.md.
- [ ] Tag the commit; archive the exact dependency versions and the test/benchmark output.
