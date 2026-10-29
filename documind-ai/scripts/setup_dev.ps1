<#
 .SYNOPSIS  Create a virtual environment and install DocuMind AI for development (Windows PowerShell 5.1+ / 7).
 .USAGE     powershell -ExecutionPolicy Bypass -File scripts\setup_dev.ps1
 Requires Python 3.11, 3.12 or 3.13 (64-bit) on PATH. Python 3.12 is recommended for packaging.
#>
$ErrorActionPreference = "Stop"
Set-Location (Split-Path -Parent $PSScriptRoot)

$py = Get-Command py -ErrorAction SilentlyContinue
if ($py) { $ver = & py -3.12 --version 2>$null; $cmd = if ($LASTEXITCODE -eq 0) { "py -3.12" } else { "py -3" } } else { $cmd = "python" }
Write-Host "Using: $cmd"
Invoke-Expression "$cmd -m venv .venv"
& .\.venv\Scripts\python.exe -m pip install --upgrade pip
& .\.venv\Scripts\python.exe -m pip install -r requirements-dev.txt
& .\.venv\Scripts\python.exe -m pip install -e . --no-deps
Write-Host "`nDone. Next:  .\scripts\check_environment.ps1   then   .\.venv\Scripts\python.exe -m pytest" -ForegroundColor Green
