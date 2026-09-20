@echo off
setlocal EnableExtensions
cd /d "%~dp0.."

echo ============================================================
echo M10 FINAL - VR2 R3 POST-REPAIR DYNAMIC-EQUILIBRIUM REPLANNING 1
echo ============================================================
echo Planning/audit only. Family B causal closure remains frozen.
echo No production, seed, controller-state, threshold, physics or R3 PASS change.
echo.

set "OUT=%CD%\artifacts\r3-dyn-eq-plan1"

powershell -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-r3-post-repair-dynamic-equilibrium-replanning1.ps1" -ArtifactDirectory "%OUT%"
if errorlevel 1 goto :fail

echo.
echo R3 Post-Repair Dynamic-Equilibrium Replanning 1 completed.
echo Next authorized activity: R3-POST-REPAIR-DYNAMIC-EQUILIBRIUM-RESIDUAL-DIAGNOSTIC1.
exit /b 0

:fail
echo.
echo R3 Post-Repair Dynamic-Equilibrium Replanning 1 FAILED.
echo Stop here. Do not retune, modify seed/controller state, or start Requalification 3.
exit /b 1
