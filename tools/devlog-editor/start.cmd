@echo off
cd /d "%~dp0..\.."
if not exist ".tooling\devlog-editor\venv\Scripts\pythonw.exe" (
  echo Run tools\devlog-editor\setup.cmd first.
  pause
  exit /b 1
)
start "" ".tooling\devlog-editor\venv\Scripts\pythonw.exe" "tools\devlog-editor\desktop.py"
