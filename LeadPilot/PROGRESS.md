# LeadPilot AI - progress log

## Done (all with automated tests unless noted)
| Phase | Delivered |
|---|---|
| 1 Architecture, shell, MVVM, navigation, SQLite, setup | 7 projects + tests; 15-table schema with migrations; WPF shell with sidebar, light/dark theme, first-run wizard |
| 2 Import, manual entry, database, search | CSV/XLSX import wizard (preview → mapping → validation → duplicate detection → report), manual lead, paged/sortable/filterable grid, bulk actions |
| 3 Discovery | Provider interface; **OpenStreetMap provider (real, free)**; orchestrator with paging, rate limits, retries, failure isolation, API-usage log; website extractor (JSON-LD, meta, mailto/tel, contact page) with robots.txt + SSRF guard; source URL + timestamp per field |
| 4 Enrichment, verification, duplicates, scoring | Evidence-gated enrichment; separate e-mail/phone/website statuses; weighted duplicate engine (High/Medium/Low) with merge + undo; explainable 0-100 relevance score with custom weights |
| 5 Follow-ups, assistant, drafts, analytics | Tasks/overdue/suggestions; rule-based assistant over real records; e-mail/WhatsApp drafts (handoff, never sent); dashboard + analytics from live data |
| 6 Licensing, backup, security, installer | ECDSA offline licences + vendor tool; AES-256-GCM backups; DPAPI secrets; roles/PINs/audit; Inno Setup installer + CI that installs, launches every screen, uninstalls |

## Known limitations (see also docs/DATA_SOURCES_AND_COSTS.md)
* The WPF layer is compiled and smoke-tested only on the Windows CI runner, not on a developer's real PC by the author.
* Live internet sources (OpenStreetMap, websites) are tested against recorded-format fixtures and fakes; they could not be called from the build sandbox. First real-world run should be done on a connected PC.
* No bundled licensed-data / CRM / ad-platform connectors; no SMTP/WhatsApp sending; no social-media access (by design).
* Charts are built-in WPF bars/columns (not LiveCharts2) to avoid a pre-release UI dependency.
* The trial clock is offline and can be defeated by a determined user; paid licences are cryptographically signed.
* Installer is unsigned until you supply a certificate.

## Next
1. Run on a real Windows 10 and 11 PC and fix visual polish found there.
2. Add authorised verification-provider and CRM connectors as customers ask.
3. Code-sign, then publish a release tag `leadpilot-v1.0.0`.
