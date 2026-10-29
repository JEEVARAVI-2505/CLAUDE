<#
 .SYNOPSIS  Reproducible Windows build: tests -> PyInstaller one-folder -> prune -> smoke test -> licences -> installer.
 .USAGE     powershell -ExecutionPolicy Bypass -File scripts\build_windows.ps1 [-SkipTests] [-NoInstaller]
 Needs: .venv from setup_dev.ps1. Inno Setup 6 (iscc.exe) is optional; without it the portable folder in dist\DocuMind is the output.
 Not signed: see docs\RELEASE_CHECKLIST.md for optional code signing.
#>
param([switch]$SkipTests, [switch]$NoInstaller)
$ErrorActionPreference = "Stop"
Set-Location (Split-Path -Parent $PSScriptRoot)
$py = ".\.venv\Scripts\python.exe"
if (-not (Test-Path $py)) { throw "Run scripts\setup_dev.ps1 first." }

if (-not $SkipTests) { & $py -m pytest -q; if ($LASTEXITCODE -ne 0) { throw "Tests failed - not building." } }

Remove-Item -Recurse -Force build, dist -ErrorAction SilentlyContinue
& $py scripts\make_icon.py
& $py -m PyInstaller --noconfirm --clean documind.spec
if ($LASTEXITCODE -ne 0) { throw "PyInstaller failed." }

& $py scripts\prune_build.py dist\DocuMind --smoke
if ($LASTEXITCODE -ne 0) { throw "Pruned build failed its smoke test." }

# licence texts shipped next to the program (Qt LGPL needs the text + the ability to relink, which the one-folder layout allows)
New-Item -ItemType Directory -Force dist\DocuMind\licenses | Out-Null
& $py scripts\collect_licenses.py dist\DocuMind\licenses
Copy-Item THIRD_PARTY_NOTICES.md, LICENSE, CHANGELOG.md dist\DocuMind\ -Force
Copy-Item docs\USER_GUIDE.md, docs\INSTALLATION.md, docs\TROUBLESHOOTING.md, docs\PRIVACY.md dist\DocuMind\ -Force

$size = [math]::Round((Get-ChildItem dist\DocuMind -Recurse | Measure-Object Length -Sum).Sum / 1MB, 1)
Write-Host "Portable build: dist\DocuMind ($size MB unpacked)" -ForegroundColor Green

if (-not $NoInstaller) {
  $iscc = (Get-Command iscc -ErrorAction SilentlyContinue).Source
  if (-not $iscc) { foreach ($c in "C:\Program Files (x86)\Inno Setup 6\ISCC.exe", "C:\Program Files\Inno Setup 6\ISCC.exe") { if (Test-Path $c) { $iscc = $c } } }
  if ($iscc) { & $iscc installer\documind.iss; Write-Host "Installer written to dist\installer" -ForegroundColor Green }
  else { Write-Warning "Inno Setup not found; skipped installer. Install Inno Setup 6 (free) or distribute dist\DocuMind." }
}
Get-ChildItem dist -Recurse -Include *.exe | Get-FileHash -Algorithm SHA256 | Format-Table Hash, Path -AutoSize
