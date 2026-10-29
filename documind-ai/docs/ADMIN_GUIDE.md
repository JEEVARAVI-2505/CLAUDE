# Administrator guide

## Locations
| What | Where (default) | Change |
|---|---|---|
| Settings file | `%APPDATA%\DocuMindAI\config.json` | env `DOCUMIND_CONFIG_DIR` |
| Data folder (database, stored originals, logs, temp, templates, exports, backups) | `%LOCALAPPDATA%\DocuMindAI` | Settings > Data & backup (restart), or env `DOCUMIND_DATA_DIR` |
| Database | `<data>\documind.db` (+ `-wal`, `-shm` while running) | - |
| Stored originals | `<data>\files\<xx>\<sha256>.<ext>` (content-addressed; original file name is kept only in the database) | - |
| Logs | `<data>\logs\documind.log` (rotating, 5 x 1 MB, redacted) | - |
| Templates (JSON) | `<data>\templates\*.json` | see below |
| Backups | `<data>\backups` or the folder chosen in Settings | Settings |

Moving the data folder: make a backup, change the folder, restart, **restore** the backup (DocuMind does not move data silently).

## Backups and recovery
* *Settings > Data & backup > Create backup now* writes a ZIP: `manifest.json` (format version, app version, schema version, counts, SHA-256 of every file), a
  consistent database snapshot (SQLite online backup API) and, optionally, the original files. It is verified after writing. The last N are kept.
* **Restore** validates everything first (ZIP safety, manifest, every checksum, database integrity, version compatibility). It then makes a safety backup of the
  current data, stages the new data, swaps it in, and on any failure puts the previous data back. The previous database/files stay as `*.pre-restore-<time>`
  until you delete them (Settings > Data) - they contain customer data.
* Backups are **not encrypted**; store them on an encrypted drive and restrict access.
* If the database is damaged and no backup exists: copy `documind.db*` away, then `sqlite3 documind.db ".recover"` can sometimes salvage rows (outside support scope).
* Interrupted work: on next start documents left "processing" return to the queue; a half-written backup/export never replaces a good file (`.part` + atomic rename).

## Retention and deletion
Settings > Data & backup: auto-delete approved/rejected/failed documents after N days (0 = never); optionally remove only stored originals. Documents waiting for
review are never auto-deleted. *Delete permanently* overwrites the stored file once, removes the file and **all** database rows for the document (fields, issues,
audit trail) and records a content-free line in `deletion_log`; the SQLite `secure_delete` option and a WAL truncate are used. Limits: SSD wear levelling, shadow
copies, previously made exports and backups, and OS caches may retain earlier copies. For high-sensitivity data use full-disk encryption.

## Templates (Professional)
A JSON file in `<data>\templates` (see `src\documind\resources\templates\example-single-vendor.json`):
```json
{"name": "Kaveri layout", "match_keywords": ["kaveri hardware traders"], "document_kind": "invoice", "date_order": "DMY",
 "default_currency": "INR", "extra_labels": {"invoice_number": ["challan no"], "po_number": ["indent no"]}}
```
The template with the most keyword hits in the document text is used. Unknown keys make the file be ignored and reported at start. Default vocabulary is in
`resources\rules\labels_default.json` (not code). Custom fields (Settings > Templates & fields) are located by label keywords - no regular expressions are accepted.

## Windows permissions
The app writes only under its data folder and exports/backups you choose. Give the daily user Modify rights on the data folder; remove access for other accounts
(`icacls "%LOCALAPPDATA%\DocuMindAI" /inheritance:r /grant:r "%USERNAME%:(OI)(CI)F"`). Enable BitLocker for at-rest protection.

## Network
No listener is opened. The only outbound connection is to the AI address in Settings (default `127.0.0.1:11434`); a non-local address is refused unless an
administrator ticks the explicit override. Environment proxy variables are ignored for that connection.

## Diagnostics
Help > Diagnostics shows versions, settings summary, OCR/AI status, database integrity and a redacted log tail (no document text, no extracted values). Read it, then save and send.

## Command line
`DocuMind.exe --smoke-test` self-check. No other switches. Set `DOCUMIND_DATA_DIR` for a portable/USB profile.
