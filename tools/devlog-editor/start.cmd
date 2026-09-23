@echo off
cd /d "%~dp0..\.."
".tooling\day01-tts\venv\Scripts\python.exe" "tools\devlog-editor\server.py"
if errorlevel 1 pause
