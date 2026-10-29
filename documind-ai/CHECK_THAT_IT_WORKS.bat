@echo off
cd /d "%~dp0"
echo Checking DocuMind on this computer (about 30 seconds)...
".venv\Scripts\python.exe" -m documind --smoke-test
echo.
echo If you see "SMOKE OK" above, the program works on this PC.
pause
