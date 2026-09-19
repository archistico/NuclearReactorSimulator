@echo off
setlocal EnableExtensions
cd /d "%~dp0"
if errorlevel 1 exit /b 1

echo Nuclear Reactor Simulator - M10 Final VR2 RP1C C4
echo Runtime Configuration Impact Assessment Planning 1 Amendment 1
echo Execution Host Provenance
echo.
echo This package is planning-only. It changes no production source or existing tests.
echo It adds same-host provenance requirements before Branch A2 implementation.
echo.
echo Run from PowerShell:
echo   .\scripts\run-m10-final-vr2-engineering-repair-planning1-rp1c-c4-runtime-configuration-impact-assessment-planning1-amendment1-host-provenance.cmd
echo.
echo Return the complete generated artifact folder before implementing A2.
exit /b 0
