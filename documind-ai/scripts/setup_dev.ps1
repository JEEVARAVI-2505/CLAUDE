<#
 .SYNOPSIS  Create a virtual environment and install DocuMind AI for development (Windows PowerShell 5.1+ / 7).
 .USAGE     Run from the documind-ai folder:  powershell -ExecutionPolicy Bypass -File scripts\setup_dev.ps1
 Needs Python 3.11, 3.12 or 3.13 (64-bit). Python 3.12 is recommended.
#>
Set-Location (Split-Path -Parent $PSScriptRoot)

function Find-Python {
  foreach ($v in "3.12", "3.13", "3.11") {
    try { $out = & py "-$v" -c "import sys;print(sys.version_info[:2])" 2>$null; if ($LASTEXITCODE -eq 0) { return @("py", "-$v") } } catch {}
  }
  foreach ($name in "python", "python3") {
    try {
      $c = Get-Command $name -ErrorAction Stop
      if ($c.Source -like "*WindowsApps*") { continue }   # the Microsoft Store stub, not a real Python
      $v = & $name -c "import sys;print('%d.%d' % sys.version_info[:2])" 2>$null
      if ($LASTEXITCODE -eq 0 -and $v -match "^3\.(11|12|13)$") { return @($name) }
    } catch {}
  }
  return $null
}

$py = Find-Python
if (-not $py) {
  Write-Host ""
  Write-Host "Python 3.11-3.13 was not found on this PC." -ForegroundColor Red
  Write-Host "1. Download Python 3.12 (64-bit) from https://www.python.org/downloads/windows/"
  Write-Host "2. In the installer TICK 'Add python.exe to PATH', then click Install Now."
  Write-Host "3. Close this window, open a new PowerShell in this folder and run this script again."
  exit 1
}
Write-Host "Using: $($py -join ' ')"
$ErrorActionPreference = "Stop"
& $py[0] $py[1..($py.Length)] -m venv .venv
& .\.venv\Scripts\python.exe -m pip install --upgrade pip
& .\.venv\Scripts\python.exe -m pip install -r requirements-dev.txt
& .\.venv\Scripts\python.exe -m pip install -e . --no-deps
Write-Host "`nDone. Start the app with:  .\.venv\Scripts\python.exe -m documind" -ForegroundColor Green
Write-Host "Run the tests with:        .\.venv\Scripts\python.exe -m pytest"
