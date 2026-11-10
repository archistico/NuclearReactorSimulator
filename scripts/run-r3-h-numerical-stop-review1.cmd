@echo off
setlocal EnableExtensions
cd /d "%~dp0.."
echo R3 H Numerical Stop Review 1 - frozen-data arithmetic only, zero plant evaluations.
python ".\eng\review-r3-h-numerics1.py"
if errorlevel 1 exit /b 1
echo Evidence: artifacts\r3-h-numerics1. R3 remains RED.
exit /b 0
