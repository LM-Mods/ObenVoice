@echo off
setlocal
cd /d "%~dp0"
if not exist "Logs" mkdir "Logs"
if not exist "cache" mkdir "cache"
set "PYTHONNOUSERSITE=1"
set "PYTHONUTF8=1"
set "HF_HOME=%~dp0model-cache"
set "HF_HUB_OFFLINE=1"
set "TRANSFORMERS_OFFLINE=1"
set "HF_HUB_DISABLE_TELEMETRY=1"
set "TTS_ROOT=%~dp0"
set "OBENVOICE_ROOT=%~dp0"
set "TTS_LOG_DIR=%~dp0Logs"
set "TTS_CACHE_DIR=%~dp0cache"
"%~dp0runtime\python.exe" "%~dp0backend\tts_server.py" %*
endlocal
