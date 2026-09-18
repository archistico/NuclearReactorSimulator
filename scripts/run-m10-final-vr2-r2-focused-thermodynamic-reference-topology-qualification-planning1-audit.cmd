@echo off
setlocal
cd /d "%~dp0.."
echo ============================================================
echo M10 FINAL - VR2 R2 FOCUSED THERMODYNAMIC REFERENCE TOPOLOGY
echo QUALIFICATION - PLANNING 1
echo ============================================================
echo Planning/static audit only. No R2 execution, production change,
echo exact-v9 composition, VR3, P3-R1 or second replacement-long authority.
echo.
pwsh -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-r2-focused-thermodynamic-reference-topology-qualification-planning1.ps1"
if errorlevel 1 (
  echo.
  echo R2 Planning 1 FAILED static audit. No downstream authority is granted.
  exit /b 1
)
echo.
echo R2 Planning 1 completed PASS-AS-AUTHORED.
echo Return the full "artifacts\m10-final-physical-reference-vr2-r2-focused-thermodynamic-reference-topology-qualification-planning1" folder before R2 execution.
exit /b 0
