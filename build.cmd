@echo off
chcp 65001 >nul
setlocal
cd /d "%~dp0"
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\build.ps1" %*
if errorlevel 1 (
  echo.
  echo 构建失败，请查看上面的提示。
  pause
  exit /b 1
)
echo.
echo 构建完成，请打开 dist 文件夹。
pause
