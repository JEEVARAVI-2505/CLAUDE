# Troubleshooting

| Problem | What to do |
|---|---|
| “Scanned document… OCR is not available” | Install Tesseract (Help › Install OCR) or enable OCR in Settings. |
| Numbers look wrong on a scan | Re-scan at 300 dpi, upright and flat. The OCR score on the Review page (engine's own score, not a probability) is low for hard pages. |
| “The file is named .pdf but its contents are PNG” | The file extension does not match the file. Rename it correctly or re-save it. |
| “Password-protected” | Open the PDF with its password and print it to a new PDF, then import that. |
| “Another document… same number” | Possible duplicate. Compare, then reject or approve. |
| Dates warn “could be day-first or month-first” | Confirm the date, or set the regional order in Settings › General. |
| AI says model not installed | Run `ollama pull <model>` in a terminal. |
| AI times out | Use a smaller model or increase the timeout in Settings › Local AI. Processing continues in rules-only mode. |
| “The database is busy” | Close other DocuMind windows; wait a moment and retry. |
| Export fails | Close the file if it is open in Excel; check free disk space and folder permissions. |
| App will not start / data damaged | Restore the latest backup (Settings › Data & backup), or see docs/TROUBLESHOOTING.md. |

Still stuck? Help › **Export diagnostics**, read the file (it contains no document text), then send it to your installer.
