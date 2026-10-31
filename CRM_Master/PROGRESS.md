# CRM MASTER — progress log, checklist and known issues

Branch work is committed as `CRM-<stage>.<task>: message`. Test totals: **330 automated tests, all passing** (`dotnet test CRM_Master.sln`).

## Status legend
✅ implemented **and** automatically tested · 🟡 implemented and compiled, **not run on Windows** (needs `Documentation/WINDOWS_SMOKE_TEST.md`) · ⬜ not built

## Checklist (against the product brief)
| Area | Status | Notes |
|---|---|---|
| Solution / projects / DI / MVVM / SQLite | ✅ engine+data+DI graph (`CompositionTests`) · 🟡 WPF wiring | .NET 10, 6 projects + tests; WPF project builds on Linux and cross-publishes |
| 13 required tables, FKs, indexes, constraints, EF migrations | ✅ | `DatabaseTests` (schema, FK enforcement, check constraints, rollback) |
| Import XLSX / XLSM / CSV, mapping, validation, dup pre-check, progress, cancel, history | ✅ engine · 🟡 UI | empty / corrupt / truncated / renamed / locked / BOM / delimiters covered; 50k rows: CSV 14.5 s, XLSX 18.6 s |
| Intelligent column mapping | ✅ | names, typos, data sniffing, custom fields |
| Data cleaning (+ before/after, apply, undo/backup) | ✅ engine · 🟡 UI | |
| Duplicate detection (exact/phone/name/company/fuzzy, thresholds, classes, merge, separate, export) | ✅ engine · 🟡 UI | 50,500 contacts scanned in 0.5 s (blocking) |
| Contact management (CRUD, search, filter, sort, paging, bulk select, export, history) | ✅ repository · 🟡 UI | search over 50k rows: 142 ms |
| Bulk update (6 policies, field approval, audit, report, undo) | ✅ engine · 🟡 UI | 1,000 existing / 200 incoming scenario tested |
| Validation engine (configurable, suggestions) | ✅ engine · 🟡 UI | syntax-only email claims; documented in UI |
| Automation rules (builder, history, 6 starters, manual run, undo) | ✅ engine · 🟡 UI | |
| Reports × (Excel, CSV, PDF) × 7 types with filters | ✅ | files re-read in tests; PDF structure (xref) verified |
| Audit log, filters, batch undo, retention | ✅ engine · 🟡 UI | |
| Backup / restore | ✅ | online backup, integrity check, safety copy, prune |
| Dashboard from real data + empty state | ✅ queries · 🟡 charts/UI | custom-drawn charts (no library) |
| Settings, About, first-run setup, sample data | ✅ settings/sample pipeline · 🟡 UI | sample dataset imports/dedupes/validates in `SampleDataTests` |
| Security: DPAPI store (no plaintext), safe SQL, formula-injection-safe exports, no telemetry | ✅ | DPAPI round-trip only runs on Windows (test asserts refusal elsewhere) |
| Integrations architecture + mocks | ✅ | mocks are labelled "does NOT connect"; no real connector |
| Light + dark theme design system | 🟡 | |
| Release build, self-contained EXE | ✅ **built**: `dist/publish/CRM_Master.exe` is a real PE32+ x86-64 GUI executable (cross-published on Linux) · 🟡 not launched | |
| Installer (Inno Setup) | ⬜ script written, **never compiled** (Inno Setup is Windows-only) | use `build/publish.ps1` or the CI workflow |
| Code signing | ⬜ | unsigned → SmartScreen warning (see PACKAGING.md) |
| User guide, README, third-party notices, architecture, packaging, licensing docs | ✅ | |

## Known limitations / not built
* The WPF UI has not been executed anywhere yet. `tools/lint_xaml.py` checks resource keys and bindings statically, but visual layout, dark-theme contrast, DPI behaviour and the exact feel of the drag-and-drop zone are unverified. **Run the smoke test.**
* No real CRM connectors (HubSpot / Zoho / Salesforce / Pipedrive): only development mocks. OAuth connectors are future work.
* Licensing is a local, non-tamper-proof 30-day evaluation notice; no activation system (see LICENSING.md).
* Phone validation uses built-in rules (India-first, North America, generic ITU length rules for ~110 countries), not a full numbering-plan library.
* `.xls` (old Excel) unsupported; `.xlsm` macros are never run. Whole file is read into memory (limit 300 MB; 50k rows is comfortable, very large Excel files are slow).
* PDF reports: Latin text only (other scripts print as `?`); Excel/CSV are full Unicode.
* Custom fields are imported, shown and edited, and copied during merges, but are not part of bulk-update, cleaning or the contact export.
* Automation triggers *contact created / edited* fire from the contact editor, not from bulk operations; *import* triggers fire during import.
* Duplicate scan skips very large blocks of identical keys (reported in the scan note) — e.g. one shared email on hundreds of contacts.
* Single local user (no accounts/roles). One running instance per Windows user. Changing the database location needs a restart.
* Windows only (WPF). Not accessibility-audited. English only.
* `FluentAssertions` is held at 6.12.1 and ClosedXML's `SixLabors.Fonts` at 1.0.0 to stay within free licences — do not upgrade blindly (see THIRD_PARTY.md).

## Environment notes
Built with the Ubuntu-packaged .NET SDK 10.0.114 (Microsoft's installer hosts were not reachable); it includes the WindowsDesktop SDK, so WPF compiles and `win-x64` self-contained publish works. `dotnet-ef 10.0.14` generated the migration.

## What's next (suggested)
1. Run the Windows smoke test; fix whatever it finds. 2. Compile the installer on Windows (or via the GitHub workflow). 3. Buy/obtain a code-signing certificate. 4. Replace the EULA, fill `branding.json`. 5. Real licensing provider. 6. First real connector (HubSpot, OAuth + official API).
