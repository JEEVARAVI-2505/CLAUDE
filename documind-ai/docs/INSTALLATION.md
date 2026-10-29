# Installation guide (for the person installing)

## Requirements
Windows 10/11 64-bit · 4 GB RAM (8 GB+ for a local AI model) · 2 GB free disk + document space · a normal user account (no administrator rights are needed
for DocuMind itself; installing Tesseract/Ollama may ask for them).

## 1. Check the PC (optional, read-only)
```powershell
powershell -ExecutionPolicy Bypass -File scripts\check_environment.ps1
```

## 2. Install DocuMind
Run `DocuMind-Setup-<version>.exe` (installs for the current user) or copy the `DocuMind` folder anywhere and run `DocuMind.exe` (portable).
Unsigned installers may show a Windows SmartScreen warning ("More info" > "Run anyway") - tell the customer why before they see it.
Data is created on first start in `%LOCALAPPDATA%\DocuMindAI`. The program folder is read-only to the app; uninstalling does **not** delete data.

## 3. OCR for scans and photos (needed only for non-digital documents)
1. Download Tesseract for Windows from the official project's recommended Windows installer page (https://tesseract-ocr.github.io/tessdoc/Installation.html).
2. Install to the default folder (`C:\Program Files\Tesseract-OCR`). Tick **Additional language data** you need and keep **osd** (auto-rotate).
3. DocuMind finds it automatically. Check: *Settings > OCR (scans) > Check OCR installation*. For several languages use `eng+hin`.
DocuMind never downloads or bundles Tesseract.

## 4. Optional local AI (Ollama)
Do this **only** if the customer needs it and the PC is capable, and after the customer has accepted the model's licence (see THIRD_PARTY_NOTICES.md).
1. Install Ollama from https://ollama.com/download (free).
2. In a terminal: `ollama pull qwen2.5:7b-instruct` (several GB; internet needed *once* for this download only; model choice is yours).
3. *Settings > Local AI*: tick "Use local AI", model name as pulled, press **Check connection and model**.
4. If processing is too slow, use a smaller model or turn AI off - rules-only mode remains fully functional.

## 5. First run
The wizard checks the data folder, OCR and AI. Load the demo documents to confirm everything works, then delete them from History.

## 6. Verify
`DocuMind.exe --smoke-test` (from a command prompt in the program folder) generates a fictional invoice, processes it and prints `SMOKE OK`.

## 7. Updating
Close DocuMind, make a backup (*Settings > Data & backup*), run the new installer over the old one (data is untouched; the database upgrades itself
and keeps a pre-migration copy in `backups\`). If anything looks wrong, restore the backup with the previous version.

## 8. Uninstalling and erasing
Uninstall removes the program only. To remove customer data too, delete `%LOCALAPPDATA%\DocuMindAI` (and any backup folders you created). Read
PRIVACY.md first about copies on SSDs and in backups.

## Dependencies: what is and is not inside the installer
Inside: Python runtime, Qt (LGPL), pydantic, pypdf, pypdfium2/PDFium, Pillow, openpyxl. **Not** inside: Tesseract, Ollama, any AI model.
