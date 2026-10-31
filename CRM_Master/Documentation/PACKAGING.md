# Packaging & distribution

## Produce the build
| Goal | Command | Needs |
|---|---|---|
| Self-contained app folder + portable zip + installer | `.\build\publish.ps1 -Version 1.0.0` | Windows, .NET 10 SDK, Inno Setup 6 (free) |
| App folder + zip only | `build/publish.sh 1.0.0` (Linux/macOS/WSL) | .NET 10 SDK |
| Everything on a hosted Windows runner | push to GitHub → workflow *CRM MASTER* → artifact `CRM_MASTER_Windows` | GitHub Actions |

Output: `dist/publish/CRM_Master.exe` (self-contained x64, ~150 MB folder — bundles .NET and WPF; the user installs nothing),
`dist/CRM_MASTER_portable_<version>.zip`, `dist/installer/CRM_MASTER_Setup_<version>.exe`.
WPF cannot be trimmed or published as NativeAOT; single-file publishing is intentionally not used (native SQLite/WPF DLLs).

## Installer (Inno Setup, `installer/CRM_Master.iss`)
Per-user install by default (no admin rights; "all users" is offered), Start-menu shortcut, desktop shortcut (task, ticked by default), uninstaller, launch-after-install.
User data is **not** in the install folder; uninstall asks whether to delete `%LOCALAPPDATA%\CRM Master` (default: keep). Upgrades install over the old version and never touch data (EF migrations upgrade the database on first start).
Replace `installer/EULA_EVALUATION.txt` with your real licence agreement before commercial release. Fill in `branding.json` (copy `CRM_Master.App/branding.json.example`) so the About screen shows your developer/support details.

## First-launch behaviour
Creates `%LOCALAPPDATA%\CRM Master`, applies migrations, seeds the six starter automation rules, shows the setup dialog, optionally imports the sample dataset (`SampleData\sample-contacts.csv`; `sample-update.csv` is provided for the Bulk Update demo).

## Windows warnings you should expect
**The EXE and installer are unsigned.** Windows SmartScreen / browsers may show "Windows protected your PC" or "unknown publisher" and antivirus products may be suspicious of new unsigned executables. This is normal for unsigned software
and does not mean the file is unsafe, but customers will notice it. Mitigations (future cost): an **OV/EV code-signing certificate** (paid, typically yearly; EV builds reputation fastest) with `signtool` and a timestamp server,
or Azure Trusted Signing / a reseller certificate. Add the signing step to `build/publish.ps1` for both `CRM_Master.exe` and the installer. Until then, distribute checksums (`Get-FileHash`) and tell testers what to expect.

## Release checklist
1. `dotnet test` green, `python3 tools/lint_xaml.py` clean. 2. Run `Documentation/WINDOWS_SMOKE_TEST.md` on a clean Windows 10 and 11 machine (no .NET installed). 3. Update version, `THIRD_PARTY.md` (re-check licences). 4. Fill `branding.json`, replace the EULA.
5. Build, sign (when available), install on a clean VM, upgrade-install over the previous version, uninstall. 6. Publish with checksums.
