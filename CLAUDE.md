# Project: AI Building Studio (WPF / .NET 8)
- Language: C# 12, .NET 8, WPF, MVVM (CommunityToolkit.Mvvm). Nullable enabled. Warnings as errors.
- Engines are UI-free class libraries. The WPF app only references them via interfaces + DI.
- Single source of truth: Core.Model.BuildingModel. All engines read/write it; never duplicate geometry.
- Units: internal = millimetres (double), angles = radians, Y-up in 3D, plan is X-Y. Convert only at the UI/IO edge.
- Every engine function is deterministic and pure where possible. Randomness takes an explicit seed.
- Tests first: xUnit + FluentAssertions. Every structural/design/BOQ formula needs a hand-calculated golden test with source clause noted (e.g. "IS 456 cl. 26.2.1").
- Rules (Vastu, bylaws) live in JSON rule packs under /rules, never hardcoded.
- Use only free, permissively licensed NuGet packages. Record each in THIRD_PARTY.md with license.
- Never claim structural safety from AI output. Show an "Engineer must verify" banner on all structural reports.
- Commit after each task: "<phase>.<task>: message". Do not start the next task until tests pass.
- Keep a PROGRESS.md log: what was done, what is next, known issues.
- Keep files < 400 lines, classes focused, public APIs documented.

## Build environment notes
- Linux containers: build WPF with Microsoft's SDK (not Ubuntu's source-built SDK, which lacks the WindowsDesktop SDK).
  `EnableWindowsTargeting` is set for App.Wpf so it compiles on Linux; it only runs on Windows.
- `dotnet test BuildingStudio.sln` runs every engine test on any OS.
