@echo off
setlocal
title DocuMind AI Professional
cd /d "%~dp0"

if exist ".venv\Scripts\pythonw.exe" goto run

echo.
echo  DocuMind AI - first-time setup. This runs once and takes a few minutes.
echo  It needs an internet connection now; after setup it works fully offline.
echo.
call :findpy
if defined PY goto havepy

echo  Python is not installed on this computer. Installing Python 3.12 for you...
winget install --id Python.Python.3.12 -e --scope user --accept-package-agreements --accept-source-agreements
call :findpy
if defined PY goto havepy

echo.
echo  Python could not be installed automatically.
echo  Please install it yourself: https://www.python.org/downloads/windows/
echo  Tick "Add python.exe to PATH" in the installer, then double-click this file again.
goto fail

:havepy
echo  Using Python: %PY%
%PY% -m venv .venv
if errorlevel 1 goto fail
".venv\Scripts\python.exe" -m pip install --upgrade pip
".venv\Scripts\python.exe" -m pip install -r requirements.txt
if errorlevel 1 goto fail
".venv\Scripts\python.exe" -m pip install -e . --no-deps
if errorlevel 1 goto fail
echo.
echo  Setup finished. Opening DocuMind...

:run
start "" ".venv\Scripts\pythonw.exe" -m documind
exit /b 0

:fail
echo.
echo  Something went wrong. Take a photo or copy the text above and send it to your installer.
rmdir /s /q ".venv" >nul 2>&1
pause
exit /b 1

:findpy
set "PY="
for %%V in (3.12 3.13 3.11) do (
  if not defined PY (
    py -%%V -c "pass" >nul 2>&1
    if not errorlevel 1 set "PY=py -%%V"
  )
)
if not defined PY if exist "%LOCALAPPDATA%\Programs\Python\Python312\python.exe" set PY="%LOCALAPPDATA%\Programs\Python\Python312\python.exe"
if not defined PY (
  python -c "import sys;sys.exit(0 if (3,11)<=sys.version_info[:2]<=(3,13) else 1)" >nul 2>&1
  if not errorlevel 1 set "PY=python"
)
exit /b 0
