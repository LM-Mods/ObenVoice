@echo off
setlocal
cd /d "%~dp0"
if not exist "Obenseuer.exe" (
  echo Obenseuer.exe was not found. Place this file in the Obenseuer game folder.
  pause
  exit /b 1
)
if not exist "ObenVoice\run_backend.bat" (
  echo ObenVoice\run_backend.bat was not found. Install ObenVoice Core first.
  pause
  exit /b 1
)
start "ObenVoice backend" /min "%~dp0ObenVoice\run_backend.bat"
start "Obenseuer" "%~dp0Obenseuer.exe"
endlocal
