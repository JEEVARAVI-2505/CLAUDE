# Acceptance criteria - evidence

Legend: ✅ proven by automated tests / CI run · ⚠️ implemented and tested against fixtures, **not yet proven live** · ❌ not done

| # | Criterion | Status | Evidence |
|---|---|---|---|
| 1 | Installs on a clean Windows computer | ✅ | CI job `windows-installer` runs the real `LeadPilotAI_Setup.exe` silently on a fresh `windows-latest` VM and checks the installed files. (Not yet tried on your own Windows 10 *and* 11 PCs.) |
| 2 | Launches without a development environment | ✅ | Self-contained publish (.NET runtime included); CI launches the *installed* app and walks all 19 screens, saving a screenshot of each. |
| 3 | Creates and accesses a local database | ✅ | Installer runs `--init`; CI asserts `%LOCALAPPDATA%\LeadPilotAI\Data\leadpilot.db`, Backups, Logs, Exports exist. `DataTests` cover schema, keys, constraints, migrations. |
| 4 | Imports real Excel and CSV files | ✅ | `ImportExportTests` import generated .xlsx and .csv (quoted commas, semicolon delimiter, invalid rows, duplicates). Legacy .xls is refused with instructions (by design). |
| 5 | Identifies duplicate records | ✅ | `DuplicateDetectorTests` (hand-calculated scores), `IngestAndMergeTests` (flagging, merge, undo). |
| 6 | Real search through a configured discovery provider | ⚠️ | OpenStreetMap (Nominatim + Overpass) provider, orchestrator, paging, retries - tested against recorded-shape responses. The build sandbox had no internet, so a live call has **not** been made. Do one search on a connected PC before selling. |
| 7 | Retrieves actual available business information | ⚠️ | Same as 6; website extraction is tested on realistic HTML (JSON-LD, mailto/tel, contact page, robots.txt). |
| 8 | Preserves source URLs and timestamps | ✅ | `IngestAndMergeTests`, `EnrichmentVerificationTests` (per-field evidence with URL + time). |
| 9 | Detects incomplete records | ✅ | Grid filter, dashboard KPI, enrichment queue (`DataTests`, `ViewModelTests`). |
| 10 | Generates explainable lead scores | ✅ | `ScoringTests` (hand-calculated 70/100 golden case; every score carries per-factor reasons and the "relevance ≠ intent" note). |
| 11 | Stores discovered leads | ✅ | `ViewModelTests.Finder_scores_flags_known_leads_and_saves_selected_into_a_campaign`. |
| 12 | Exports to Excel and CSV (and PDF) | ✅ | `ImportExportTests` (round-trip, formula-injection guard, valid PDF structure). |
| 13 | Schedules follow-up tasks | ✅ | `DataTests`, `ViewModelTests` (overdue, snooze, complete, suggestions). |
| 14 | Accurate dashboard statistics | ✅ | `DataTests.Dashboard_numbers_match_the_data` - every figure computed from the database. |
| 15 | Recovers from API failures | ✅ | `OrchestratorTests`: failing provider retried 3×, others continue, offline source skipped with a message, failures logged in API usage. |
| 16 | Works offline for local features | ✅ | All tests run without network; only discovery/enrichment/AI/DNS need internet. |
| 17 | No fabricated leads or false verification claims | ✅ | Design + tests: empty fields stay empty; e-mail/phone statuses never exceed what was checked; AI output stays an unapplied suggestion; discovery shows "nothing was added" when sources fail. |

## Other deliverables
| Item | Status |
|---|---|
| `LeadPilotAI_Setup.exe` | ✅ built by CI (artifact, and a GitHub Release when a `leadpilot-v*` tag is pushed) |
| Installer is code-signed | ❌ **No.** Needs your certificate; the workflow signs automatically once secrets are added. SmartScreen may warn. |
| Paid licence keys | ⚠️ Mechanism is complete and tested, but you must generate your own key pair once (docs/VENDOR_GUIDE.md); until then builds run as free trial. |
| Payments / online activation | ❌ Not included (offline keys by design). |
| Licensed-data / CRM / ad-platform connectors, e-mail/WhatsApp sending | ❌ Not included in 1.0 (file import and drafts instead). |
| LiveCharts2 | Replaced by built-in WPF charts (no pre-release dependency). |
| EULA / privacy text | Templates - lawyer review needed. |
