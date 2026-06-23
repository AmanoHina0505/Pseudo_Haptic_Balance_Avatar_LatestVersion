@echo off
setlocal
set OUTDIR=%~dp0..\Recorded Data
if not exist "%OUTDIR%" mkdir "%OUTDIR%"
python "%~dp0udp_balance_receiver.py" --output "%OUTDIR%\quest_balance_latest.csv"
pause
