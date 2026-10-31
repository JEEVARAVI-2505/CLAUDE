<#
.SYNOPSIS  Clean build, test, self-contained publish, portable zip and (if Inno Setup is installed) the installer.
.EXAMPLE   .\build\publish.ps1 -Version 1.0.0
Requires: .NET 10 SDK (https://dotnet.microsoft.com). Optional: Inno Setup 6 (free) for the installer - `choco install innosetup`.
#>
param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0",
    [switch]$SkipTests,
    [switch]$SkipInstaller
)
$ErrorActionPreference = "Stop"
Set-Location (Split-Path $PSScriptRoot -Parent)

Write-Host "== .NET SDK ==" ; dotnet --version
if (-not $SkipTests) { Write-Host "== Tests ==" ; dotnet test CRM_Master.sln -c $Configuration ; if ($LASTEXITCODE -ne 0) { throw "Tests failed - not publishing." } }

Write-Host "== Clean ==" ; Remove-Item dist -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "== Publish (self-contained, win-x64) =="
dotnet publish CRM_Master.App -c $Configuration -r win-x64 --self-contained true -p:Version=$Version -p:DebugType=none -p:DebugSymbols=false -o dist/publish
if ($LASTEXITCODE -ne 0) { throw "Publish failed." }
if (Test-Path CRM_Master.App\branding.json) { Copy-Item CRM_Master.App\branding.json dist\publish\ -Force }

$exe = "dist\publish\CRM_Master.exe"
if (-not (Test-Path $exe)) { throw "CRM_Master.exe was not produced." }
Write-Host ("Built {0} ({1:N1} MB, folder {2:N0} MB)" -f $exe, ((Get-Item $exe).Length / 1MB), ((Get-ChildItem dist\publish -Recurse | Measure-Object Length -Sum).Sum / 1MB))

Write-Host "== Portable zip =="
Compress-Archive -Path dist\publish\* -DestinationPath "dist\CRM_MASTER_portable_$Version.zip" -Force

if (-not $SkipInstaller) {
    $iscc = (Get-Command iscc -ErrorAction SilentlyContinue)?.Source
    if (-not $iscc) { foreach ($p in "$env:ProgramFiles(x86)\Inno Setup 6\ISCC.exe", "$env:ProgramFiles\Inno Setup 6\ISCC.exe", "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe") { if (Test-Path $p) { $iscc = $p; break } } }
    if ($iscc) {
        Write-Host "== Installer ($iscc) =="
        & $iscc "/DAppVersion=$Version" installer\CRM_Master.iss
        if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed." }
        Get-ChildItem dist\installer\*.exe | ForEach-Object { Write-Host ("Installer: {0} ({1:N1} MB)" -f $_.FullName, ($_.Length / 1MB)) }
    } else { Write-Warning "Inno Setup not found - installer not built. Install it (free) and re-run, or use the portable zip." }
}
Write-Host "Done. Unsigned binaries trigger Windows SmartScreen warnings - see Documentation\PACKAGING.md."
