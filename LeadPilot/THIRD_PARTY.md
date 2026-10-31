# Third-party components

All are free to use commercially. Versions are pinned in the `.csproj` files.

| Package | Used for | Licence |
|---|---|---|
| CommunityToolkit.Mvvm | MVVM source generators | MIT |
| Microsoft.Data.Sqlite | local SQLite database | MIT |
| Microsoft.Extensions.DependencyInjection / Logging | composition root, logging | MIT |
| HtmlAgilityPack | HTML parsing for the website reader | MIT |
| ClosedXML | Excel read/write (uses DocumentFormat.OpenXml, MIT) | MIT |
| CsvHelper | CSV read/write | MS-PL / Apache-2.0 (dual) |
| DnsClient | DNS A/MX lookups for e-mail domain checks | Apache-2.0 |
| libphonenumber-csharp | phone parsing/validation | Apache-2.0 |
| System.Security.Cryptography.ProtectedData | Windows DPAPI for API keys | MIT |
| xUnit, FluentAssertions 6.x, Microsoft.NET.Test.Sdk | tests only (not shipped) | Apache-2.0 / MIT |
| Inno Setup 6 | builds the installer (build tool, not shipped) | Inno Setup licence (free, incl. commercial use) |

Data: map/business data from **OpenStreetMap** © OpenStreetMap contributors, ODbL 1.0 - attribution is stored with every lead that uses it.

Not used (and why): QuestPDF (community licence has revenue limits) - PDFs are written by a small built-in writer; LiveCharts2 - the charts are plain WPF
shapes so the app has no pre-release UI dependency; Google Places - its terms forbid storing results in your own database.
