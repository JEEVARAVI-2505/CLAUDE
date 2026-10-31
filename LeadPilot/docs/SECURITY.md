# Security and privacy design

| Area | What is done | Where tested |
|---|---|---|
| SQL injection | Every statement is parameterised; sort columns come from a whitelist; LIKE wildcards are escaped. | `DataTests.Sql_injection_text_is_stored_and_searched_literally` |
| Database integrity | Foreign keys on, CHECK constraints, WAL, transactions (atomic insert, merge, undo), versioned migrations (`PRAGMA user_version`). | `DataTests`, `IngestAndMergeTests` |
| Secrets | API keys in `secrets.dat`, each value DPAPI-encrypted (current Windows user). Never in settings JSON, logs or exports. | `SecurityTests`, `ViewModelTests.Integrations_…` |
| Backups | zip → AES-256-GCM, PBKDF2-SHA256 (600 000 iterations, random salt); integrity check + schema check + safety copy before restore. | `SecurityTests` |
| Licences | ECDSA P-256 signed, offline; only the public key ships. Tampered/forged/expired/other-PC keys rejected. | `LicenseTests`, `LicenseToolTests` |
| File import | Size (50 MB) and row (100 000) caps; formulas are never evaluated; macros ignored; legacy `.xls` refused. | `ImportExportTests` |
| Export | Cells starting with `= + - @` are stored as text (formula-injection guard). | `ImportExportTests` |
| Web access | robots.txt/Crawl-delay, per-host rate limit, timeouts, 2 MB cap, HTML only, **SSRF guard** (loopback, private, link-local, CGNAT, unique-local refused, also after redirects). | `WebsiteExtractorTests` |
| Permissions | Roles Sales / Manager / Admin enforced in the view-models and services; optional PBKDF2 PINs; sign-ins and failures audited. This is app-level control, not a substitute for Windows account security. | `DataTests`, `ViewModelTests` |
| Audit log | Merges, deletes, imports, exports, sign-ins, retention, delete-all. | `DataTests`, `ViewModelTests` |
| Retention / erasure | Configurable purge of closed leads; one-click delete-all (typed confirmation, admin only); export and backup for data portability. | `DataTests` |
| No hidden transmission | No telemetry/analytics. Network use is listed in API Integrations and the installer's privacy page. `OpenUrl` only allows http(s), mailto and local folders. | review |
| Honest labelling | No "verified" claim from format/DNS checks; scores are labelled as relevance not intent. | `EnrichmentVerificationTests`, `ScoringTests` |

Not in scope for 1.0: database-file encryption at rest (use Windows BitLocker/EFS for disk-level protection), code-signing (needs your certificate), SMTP/WhatsApp delivery.
