@echo off
setlocal EnableExtensions
cd /d "%~dp0.."
echo R3 Equilibrium Reconstruction Planning 1 - planning / frozen evidence review only.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-r3-equilibrium-reconstruction-planning1.ps1"
if errorlevel 1 goto :fail
echo Evidence: artifacts\r3-eq-h-plan1
echo Next: H test-only feasibility implementation and preexecution review.
exit /b 0
:fail
echo Planning audit FAILED. Do not change production or start a candidate search.
exit /b 1
