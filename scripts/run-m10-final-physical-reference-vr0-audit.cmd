@echo off
setlocal
cd /d "%~dp0.."

echo ============================================================
echo M10 FINAL - VR0 REFERENCE / PROVENANCE CONTRACT FREEZE
echo ============================================================
echo Documentation/reference-contract-only audit.
echo No VR1-VR4 execution, production repair, P3-R1 or second-long authorization.
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-physical-reference-vr0.ps1"
if errorlevel 1 exit /b 1

echo.
echo M10 Final VR0 Reference / Provenance Contract Freeze completed.
exit /b 0
