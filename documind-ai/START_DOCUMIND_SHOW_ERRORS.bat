@echo off
cd /d "%~dp0"
echo Running DocuMind with a visible window so errors can be seen...
".venv\Scripts\python.exe" -m documind
echo.
echo DocuMind closed. Press any key.
pause
