@echo off
setlocal
cd /d "%~dp0.."
echo ============================================================
echo M10 FINAL - VR2 RP1C C4 FULL-DOMAIN PERFORMANCE CONFIRMATION 2 PLANNING 1
echo ============================================================
echo Planning only on returned A2 evidence. Ambient/unset qualification profile on the frozen A2 host.
echo No FDPC2 implementation, RP1C selection, production/runtime change, threshold change, exact-v9 change,
echo VR3, P3-R1 or second replacement-long authorization.
echo.
echo [1/1] Static A2 evidence, compact-store, host-scope and FDPC2 planning-contract audit...
powershell -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation2-planning1.ps1"
if errorlevel 1 goto :fail
echo.
echo M10 Final VR2 RP1C C4 Full-Domain Performance Confirmation 2 Planning 1 completed.
echo This freezes only the future FDPC2 same-host ambient/unset design and evidence contract.
echo Return the full ".\artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation2-planning1" folder before implementing FDPC2 or opening RP1C selection.
exit /b 0
:fail
echo.
echo Full-Domain Performance Confirmation 2 Planning 1 FAILED.
echo No FDPC2 implementation, RP1C selection or production/runtime authority is granted.
exit /b 1
