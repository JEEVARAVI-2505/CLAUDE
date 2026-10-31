# Architecture

```
App (WPF/MVVM)  ──►  Engine  ──►  Data (EF Core/SQLite)  ──►  Core (entities, rules)
        └──────────────►  Integrations (interfaces + mocks)           ▲
Tests ───────────────────────────────────────────────────────────────┘
```
* **Core** has no dependencies: entities, DTOs, service interfaces and the pure business rules (`PhoneNumberParser`, `EmailRules`, `NameCaser`, `UrlRules`, `DateRules`, `CompanyRules`,
  `StringSimilarity` — Levenshtein / OSA / Jaro-Winkler / Soundex / name similarity, `ContactQuality`, `ContactKeys`, `ContactDiff`).
* **Data** owns the schema (13 tables, FK / unique / check constraints, 32 indexes, EF Core migrations) and cross-cutting services: settings (JSON per property), audit, backup/restore (SQLite online-backup API,
  integrity check on restore), dashboard queries, contact repository. Short-lived `DbContext`s from a factory; every mutation + its audit row commit in one transaction.
* **Engine** is UI-free: `Import` (CSV/XLSX readers, `ColumnMapper`, `RowClassifier`, `ImportService`), `Cleaning`, `Validation`, `DuplicateDetection` (blocking + scoring + clustering + merge planning), `BulkUpdate`
  (planner + service), `Automation` (rule model, evaluator, service), `Reporting` (7 builders + Excel/CSV/own PDF writer), `BatchUndoService`, `Licensing`.
* **App** is MVVM with CommunityToolkit.Mvvm. `ViewModelBase.RunAsync` is the single place that turns exceptions into friendly dialogs, log entries and audit rows. Heavy work uses `Progress<T>` + `CancellationToken`.
* **Integrations** defines `ICrmConnector` and `ICredentialStore`; v1.0 ships clearly labelled development mocks and a DPAPI store that refuses (rather than falling back to plain text) on non-Windows.

## Key design decisions
* **Derived keys.** Contacts store `EmailKey`, `PhoneKey`, `AlternatePhoneKey`, `NameKey` (indexed) so duplicate checks during imports never scan the table. Quality flags (`EmailState`, `PhoneState`, `IsIncomplete`) are persisted so the dashboard is a handful of indexed `COUNT`s.
* **Safe by construction.** Anything that changes many records: backup → one transaction → audit rows with previous/new values → undo via `BatchUndoService`. Values edited after a plan was made are skipped, never overwritten.
* **Duplicate scale.** Blocking keys (email, phone, sorted name tokens, Soundex pair) limit comparisons: 50,500 contacts → ~500 comparisons instead of 1.27 billion. Over-large blocks (e.g. a shared mailbox) are skipped and reported.
* **Duplicate scoring** (documented in `DuplicateDetector.Compare`, golden-tested): email 0.90 (+0.05 name ≥ .85, +0.05 phone); phone 0.82 + 0.13·nameSim; name-only 0.8·sim; with company 0.6·name + 0.4·company. Exact = every field both records have agrees (and one is an identifier).
* **Import** is a single transaction (all or nothing); only non-imported rows are stored in `ImportRecords` to keep the DB small. A `ProposeUpdate` row stores the incoming values for later review in Bulk Update.
* **No hard-coded rules for matching data**: thresholds, required fields, policies and validation switches come from Settings; automation rules are data (JSON in `AutomationRules`).
* **Reports** are tabular `ReportData` rendered by three exporters; the PDF writer is ~170 lines of dependency-free code (Helvetica, A4 landscape, repeating headers, page numbers).

## Database (SQLite, WAL)
Contacts · Companies · CustomFields · ContactCustomFieldValues · ImportJobs · ImportRecords · DuplicateGroups · DuplicateMatches · AutomationRules · AutomationExecutions · AuditLogs · ApplicationSettings · Backups.
Enums are stored as readable strings; timestamps as UTC. Parameterised access only (EF); LIKE patterns escape `% _ \`; sort columns are whitelisted.

## Testing
`dotnet test` — 330 tests: golden values for algorithms (e.g. Jaro-Winkler MARTHA/MARHTA = 0.9611), real SQLite files (constraints, transactions, rollback, backup/restore), real CSV/XLSX files (empty, corrupted, truncated, wrong extension, BOM, delimiters),
policy matrices for bulk update, the 1,000-contacts / 200-incoming scenario, 50k-row import/scan benchmarks (`Category=Performance`). WPF views are not unit-tested; `tools/lint_xaml.py` statically checks resource keys and bindings.
