# LeadPilot AI

Windows desktop software (C# / WPF / .NET 8) that helps a business **find prospects, collect public business information, remove duplicates, score relevance, and manage follow-ups** - all stored locally in SQLite.

It never fabricates leads, contact details, verification results or buying signals: when a source has no data, the field stays empty and the screen says so.

* User guide: [docs/USER_GUIDE.md](docs/USER_GUIDE.md)
* Sources, limits and costs (what is free, what is not): [docs/DATA_SOURCES_AND_COSTS.md](docs/DATA_SOURCES_AND_COSTS.md)
* Security design and where each property is tested: [docs/SECURITY.md](docs/SECURITY.md)
* Selling it (installer, licence keys, signing): [docs/VENDOR_GUIDE.md](docs/VENDOR_GUIDE.md)
* Acceptance criteria and evidence: [docs/ACCEPTANCE.md](docs/ACCEPTANCE.md)

## Layout
```
src/LeadPilot.Domain          entities and enums (no dependencies)
src/LeadPilot.Application     interfaces, scoring, duplicate engine, services, ALL view-models (MVVM)
src/LeadPilot.Data            SQLite, migrations, repositories
src/LeadPilot.Discovery       OpenStreetMap provider, website extractor, robots.txt, DNS/website checks
src/LeadPilot.AI              optional AI clients (Ollama, OpenAI-compatible) - everything works without them
src/LeadPilot.Infrastructure  import/export, backup, secrets (DPAPI), licensing, logging
src/LeadPilot.Desktop         WPF views + composition root only (net8.0-windows)
tools/LeadPilot.LicenseTool   vendor-side licence key generator
installer/                    Inno Setup script, EULA, privacy, release notes
tests/LeadPilot.Tests         xUnit + FluentAssertions
```

## Build and test
```bash
dotnet test LeadPilot.sln                 # any OS; everything except the WPF project
dotnet build src/LeadPilot.Desktop        # Windows (WPF) - CI does this on windows-latest
powershell -File installer\build-installer.ps1   # Windows: produces artifacts\LeadPilotAI_Setup.exe
```
The WPF project needs the Windows Desktop SDK, so it is built on Windows (GitHub Actions: `.github/workflows/leadpilot.yml`). A static test (`XamlSanityTests`) checks every XAML binding, resource key and command against the real view-models on any OS.

## Free tools only
.NET 8 SDK, Inno Setup, GitHub Actions. No paid service is required to build or run; optional AI / web-search use *your own* keys.
