@echo off
setlocal EnableExtensions
cd /d "%~dp0.."
echo H bounded scaled local-model design - no plant or nonlinear search calls.
python ".\eng\design-r3-h-local-model1.py"
if errorlevel 1 exit /b 1
echo Evidence: artifacts\r3-h-local-model1. R3 remains RED.
exit /b 0
