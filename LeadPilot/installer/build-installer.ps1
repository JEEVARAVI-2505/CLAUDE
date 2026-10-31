<#
 Builds LeadPilotAI_Setup.exe on Windows.  Requirements (all free): .NET 8 SDK, Inno Setup 6 (https://jrsoftware.org/isinfo.php).
 Usage:  powershell -ExecutionPolicy Bypass -File installer\build-installer.ps1 [-Version 1.0.0]
#>
param([string]$Version = "1.0.0")
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
Remove-Item -Recurse -Force publish, artifacts -ErrorAction SilentlyContinue
dotnet publish src\LeadPilot.Desktop\LeadPilot.Desktop.csproj -c Release -r win-x64 --self-contained true -p:Version=$Version -p:PublishReadyToRun=true -o publish
if ($LASTEXITCODE -ne 0) { throw "publish failed" }
$iscc = @("$env:ProgramFiles(x86)\Inno Setup 6\ISCC.exe", "$env:ProgramFiles\Inno Setup 6\ISCC.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) { throw "Inno Setup 6 not found. Install it from https://jrsoftware.org/isdl.php" }
& $iscc "/DMyAppVersion=$Version" installer\LeadPilotAI.iss
if ($LASTEXITCODE -ne 0) { throw "installer compile failed" }
Get-FileHash artifacts\LeadPilotAI_Setup.exe -Algorithm SHA256 | Format-List
Write-Host "Done: artifacts\LeadPilotAI_Setup.exe (NOT code-signed unless you sign it - see docs\VENDOR_GUIDE.md)"
