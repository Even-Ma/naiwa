@echo off
setlocal
cd /d "%~dp0"
if not exist "dist\Naiwa\*.exe" (
  powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\build.ps1"
  if errorlevel 1 (pause & exit /b 1)
)
for %%F in ("%~dp0dist\Naiwa\*.exe") do start "" "%%~fF" %*
