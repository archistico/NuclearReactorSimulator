@echo off
setlocal
set "ROOT=%~dp0.."
echo ============================================================
echo M10 FINAL - VR2 R3 SHORT EXACT-V9-EQUIVALENT SHADOW / COMPOSITION REQUALIFICATION - PLANNING 1
echo ============================================================
echo Planning/audit only. No R3 execution, production mutation, exact-v9 reinterpretation, R4 execution, VR3 or P3-R1 authority.
echo.
pwsh -NoProfile -ExecutionPolicy Bypass -File "%ROOT%\eng\validate-m10-final-vr2-r3-short-exact-v9-equivalent-shadow-composition-requalification-planning1.ps1"
if errorlevel 1 (
  echo.
  echo R3 Planning 1 FAILED.
  exit /b 1
)
echo.
echo R3 Planning 1 completed PASS-AS-AUTHORED.
echo Return the full "%ROOT%\artifacts\m10-final-physical-reference-vr2-r3-short-exact-v9-equivalent-shadow-composition-requalification-planning1" folder before R3 execution.
exit /b 0
