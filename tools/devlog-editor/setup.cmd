@echo off
cd /d "%~dp0..\.."
if not exist ".tooling\devlog-editor\venv\Scripts\python.exe" py -3.12 -m venv ".tooling\devlog-editor\venv"
if errorlevel 1 exit /b 1
".tooling\devlog-editor\venv\Scripts\python.exe" -m pip install -r "tools\devlog-editor\requirements-desktop.txt"
if errorlevel 1 exit /b 1
".tooling\devlog-editor\venv\Scripts\python.exe" "tools\devlog-editor\prepare_tts_assets.py"
if errorlevel 1 exit /b 1
