# Install OCR (needed for scans and photos)

Digital PDFs that already contain text work without OCR. Scanned PDFs and images need **Tesseract OCR** (free, Apache-2.0 licence), which DocuMind does not install by itself.

1. Download the Windows installer from the Tesseract project page recommended in the installation guide (docs/INSTALLATION.md) and run it.
2. In the installer, tick **Additional language data** for each language you need (English is included). Keep **osd** (orientation detection) ticked so sideways scans are rotated automatically.
3. Back in DocuMind open *Settings › OCR (scans)* and press **Check OCR installation**. If it is not found, use *Browse…* to point to `tesseract.exe` (normally `C:\Program Files\Tesseract-OCR`).
4. For several languages enter e.g. `eng+hin` in *Languages*.

Tips for better results: scan at 300 dpi, keep pages upright and flat, avoid shadows, use black-and-white or grey rather than heavy colour backgrounds.
