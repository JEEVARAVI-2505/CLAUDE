<#
 .SYNOPSIS  Report whether this PC is ready for DocuMind AI (Python deps, Tesseract OCR, Ollama, disk, permissions).
 Read-only: changes nothing and sends nothing anywhere.
#>
$ok = $true
function Show($name, $good, $detail) { $mark = if ($good) { "[ OK ]" } else { "[WARN]" }; Write-Host ("{0} {1,-26} {2}" -f $mark, $name, $detail) }

$os = Get-CimInstance Win32_OperatingSystem
Show "Windows" ($os.Version -ge "10.0") "$($os.Caption) $($os.Version) $($os.OSArchitecture)"
$ram = [math]::Round($os.TotalVisibleMemorySize / 1MB, 1); Show "Memory" ($ram -ge 4) "$ram GB (4 GB minimum; 8 GB+ if you use a local AI model)"
$drive = Get-PSDrive -Name ($env:LOCALAPPDATA.Substring(0,1)); $free = [math]::Round($drive.Free / 1GB, 1); Show "Free disk (profile drive)" ($free -ge 2) "$free GB"

$data = Join-Path $env:LOCALAPPDATA "DocuMindAI"
try { New-Item -ItemType Directory -Force $data | Out-Null; $t = Join-Path $data ".write-test"; "x" | Set-Content $t; Remove-Item $t; Show "Data folder writable" $true $data } catch { Show "Data folder writable" $false $_.Exception.Message; $ok = $false }

$tess = (Get-Command tesseract -ErrorAction SilentlyContinue).Source
if (-not $tess) { foreach ($c in "C:\Program Files\Tesseract-OCR\tesseract.exe", "C:\Program Files (x86)\Tesseract-OCR\tesseract.exe") { if (Test-Path $c) { $tess = $c } } }
if ($tess) { $v = (& $tess --version 2>&1 | Select-Object -First 1); $langs = (& $tess --list-langs 2>&1 | Select-Object -Skip 1) -join ", "; Show "Tesseract OCR" $true "$v  languages: $langs" } else { Show "Tesseract OCR" $false "not found - needed only for scans/photos (docs\INSTALLATION.md)" }

$ollama = (Get-Command ollama -ErrorAction SilentlyContinue).Source
if ($ollama) {
  Show "Ollama" $true (& $ollama --version 2>&1 | Select-Object -First 1)
  try { $tags = Invoke-RestMethod -Uri "http://127.0.0.1:11434/api/tags" -TimeoutSec 4; Show "Ollama models" ($tags.models.Count -gt 0) (($tags.models | ForEach-Object name) -join ", ") } catch { Show "Ollama server" $false "installed but not running (start the Ollama app)" }
} else { Show "Ollama" $false "not installed - optional; DocuMind works in rules-only mode" }

Write-Host "`nOptional pieces are WARN, not errors. Nothing was installed or uploaded." -ForegroundColor DarkGray
