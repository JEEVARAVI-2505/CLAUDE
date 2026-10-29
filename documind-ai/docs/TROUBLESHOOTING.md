# Troubleshooting (support staff)

| Symptom | Likely cause | Fix |
|---|---|---|
| SmartScreen "Windows protected your PC" | Unsigned installer | More info > Run anyway; or sign the build. |
| App won't start, message about data folder | Folder not writable / disk full / path too long | Choose another folder (`DOCUMIND_DATA_DIR`), free space, check permissions. |
| "database was created by a newer version" | Older app opened newer data | Install the newer version; data was not changed. |
| "failed its integrity check" | Disk fault, forced power-off | Restore latest backup; keep the damaged `documind.db*` for analysis. |
| "database is busy" | Another instance, backup, antivirus scanning | Close duplicates, add an AV exclusion for the data folder if constant. |
| Scanned file fails: "OCR is not available" | Tesseract missing / language missing | INSTALLATION.md section 3; Settings > OCR > Check. |
| OCR output poor | Low resolution, skew, noise, coloured background | Rescan 300 dpi; check Settings > OCR render quality; see score on Review page. |
| Sideways page not fixed | `osd` data missing | Install osd with Tesseract. |
| Everything flagged "ambiguous date" | Day/month order not set | Settings > Date order; tick the confirmation checkbox once sure. |
| Values missing for a supplier's invoices | Labels differ from default vocabulary | Add a template (JSON) or enable local AI. |
| AI: "not running" / "model not installed" / timeout | Ollama closed / model not pulled / PC too slow | Start Ollama; `ollama pull <model>`; use smaller model; raise timeout; or turn AI off. |
| Export fails | File open in Excel, no permission, disk full | Close the file; choose another folder; free space. |
| Restore refused: "checksum mismatch" | Backup copied badly / corrupted | Use an earlier backup; never edit the ZIP. |
| High memory / slow | Many pages at high DPI; local AI model | Lower OCR render quality; smaller model; process in smaller batches. |
| Unicode file names | - | Supported (tested); if a path > 260 chars fails, use a shorter folder or enable Windows long paths. |

Collect: Help > Diagnostics > Save. Reproduce with a *copy* of one document if the customer permits. Never ask for the whole data folder.
