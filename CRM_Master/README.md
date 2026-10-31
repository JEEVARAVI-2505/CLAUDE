# CRM MASTER — Intelligent CRM Data Management & Automation Suite

A Windows desktop application (C# / .NET 10 / WPF / SQLite) that automates repetitive CRM data work: import spreadsheets, clean and
validate contacts, find and merge duplicates, bulk-update records safely, run automation rules, produce reports and keep a full audit
trail. Everything runs **locally and offline**. There is no AI service, no cloud dependency, no subscription and no telemetry.

> **Honest status.** The engine, database and reporting layers are covered by 330 automated tests that run on any OS.
> The WPF user interface **compiles and publishes** (a real self-contained `CRM_Master.exe` is produced) but it could **not be launched**
> in the Linux build environment used to create it. Before shipping, run through
> [`Documentation/WINDOWS_SMOKE_TEST.md`](Documentation/WINDOWS_SMOKE_TEST.md) on a Windows 10/11 machine. See [`PROGRESS.md`](PROGRESS.md)
> for exactly what is done, tested, unverified and not built.

## Features
| Module | What it does |
|---|---|
| Dashboard | Live KPIs, growth and data-quality charts, recent imports / automation / errors — all queried from SQLite (empty-state when no data) |
| Import Data | `.xlsx`, `.xlsm`, `.csv/.tsv/.txt`; drag-and-drop; preview; **intelligent column mapping** (names, typos, data sniffing); custom fields; dry-run duplicate check; progress, cancel (full rollback) |
| Data Cleaning | Trim/collapse spaces, name capitalisation, email / phone / website normalisation, issue detection, exact-duplicate detection; before/after preview; backup + **undo** |
| Duplicate Detection | Email / phone / name / company / fuzzy matching with configurable thresholds; Exact / High / Possible / Low classes; side-by-side compare; field-level merge; "mark as separate"; never auto-merges uncertain matches |
| Contacts | Search, filter, sort, paging (tested at 50,000), bulk select, add / edit / delete, profile with history, export Excel/CSV/PDF |
| Bulk Update | Match incoming rows, field-level proposals, 6 update policies, approve per field, backup, audit, report, undo |
| Data Validation | Configurable rules, severities, suggested corrections (syntax checks only — never claims an email is verified) |
| Automation Rules | Trigger / condition / action rule builder, execution history, run-now, 6 starter rules |
| Reports | 7 reports × Excel / CSV / PDF with date, category and status filters |
| Activity Logs | Searchable audit trail (previous/new values), batch undo, retention |
| Settings | Theme (light/dark), folders, thresholds, validation defaults, backup & restore, DB location |

## Requirements
* Run: Windows 10 / 11 x64 (self-contained build bundles .NET — nothing to install).
* Build: [.NET 10 SDK](https://dotnet.microsoft.com/download). Tests run on Windows, Linux and macOS. The WPF project compiles on Linux (`EnableWindowsTargeting`) but only runs on Windows.

## Build, test, publish
```bash
dotnet build CRM_Master.sln -c Release
dotnet test  CRM_Master.sln -c Release          # 330 tests (engine, database, reporting; no UI)
python3 tools/lint_xaml.py                      # static check of XAML resources and bindings
```
Windows (builds the installer too, needs free Inno Setup 6):
```powershell
.\build\publish.ps1 -Version 1.0.0
```
Linux/macOS (portable folder + zip only): `build/publish.sh 1.0.0`. Details: [`Documentation/PACKAGING.md`](Documentation/PACKAGING.md).
A GitHub Actions workflow (`.github/workflows/crm-master.yml`) builds the installer on a Windows runner.

## Where data lives
`%LOCALAPPDATA%\CRM Master\` — `Data\crm_master.db` (SQLite), `Backups\`, `Logs\`. Never the install folder. Uninstalling keeps it unless you choose otherwise.

## Project structure
```
CRM_Master.sln
CRM_Master.Core          entities, DTOs, interfaces, business rules (phone / email / name / URL / date / similarity / quality)
CRM_Master.Data          EF Core + SQLite: DbContext, configurations, migrations, repositories & services (audit, backup, settings, dashboard)
CRM_Master.Engine        Import, Cleaning, Validation, DuplicateDetection, BulkUpdate, Automation, Reporting, Licensing, undo
CRM_Master.Integrations  connector interfaces, development-only mock adapters, DPAPI credential store
CRM_Master.App           WPF (MVVM): Views, ViewModels, Services, Converters, Controls, Themes, Assets
CRM_Master.Tests         xUnit + FluentAssertions 6 (330 tests)
Documentation/           user guide, architecture, packaging, licensing, Windows smoke test
build/  installer/  tools/
```

## Third-party software
Only free, permissively licensed packages (MIT / Apache-2.0 / MS-PL). See [`THIRD_PARTY.md`](THIRD_PARTY.md) (also shown in the About screen).
**FluentAssertions is pinned to 6.12.1 (Apache-2.0) and ClosedXML's SixLabors.Fonts to 1.0.0 on purpose** — newer versions change licence terms.

## Not included / limitations
Read [`PROGRESS.md`](PROGRESS.md) → "Known limitations". Highlights: unsigned binaries (SmartScreen warning), no real CRM connectors yet (mock adapters only), evaluation licence is informational, PDF is Latin-text only, `.xls` is not supported.
