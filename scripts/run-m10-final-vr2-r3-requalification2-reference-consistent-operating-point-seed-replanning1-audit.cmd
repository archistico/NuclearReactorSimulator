@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."
set "REPORT_DIR=%CD%\artifacts\m10-final-physical-reference-vr2-r3-requalification2-reference-consistent-operating-point-seed-replanning1"

if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
if exist "%REPORT_DIR%" goto :cleanup_fail

echo ============================================================
echo M10 FINAL - VR2 R3 REQUALIFICATION 2 REFERENCE-CONSISTENT OPERATING-POINT SEED REPLANNING 1
echo ============================================================
echo Planning only. No production seed repair, new exact-version identity, threshold change or R4 authority.
echo.

echo [1/1] Returned-Diagnostic2, frozen-baseline and candidate-construction scope audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-r3-requalification2-reference-consistent-operating-point-seed-replanning1.ps1"
if errorlevel 1 goto :fail

mkdir "%REPORT_DIR%" >nul 2>nul
if not exist "%REPORT_DIR%" goto :create_fail

> "%REPORT_DIR%\01-contract-and-provenance.txt" (
echo status=PASS-AS-AUTHORED
echo planning-id=R3-REQUALIFICATION2-REFERENCE-CONSISTENT-OPERATING-POINT-SEED-REPLANNING1
echo predecessor=LEGACY-FORWARD-MODE2-INVERSE-SEED-CONSISTENCY-GAP
echo returned-diagnostic2-artifacts=4
echo production-src-changed=False
echo tests-changed=False
)

> "%REPORT_DIR%\02-target-state-and-candidate-construction-contract.txt" (
echo status=PASS-AS-AUTHORED
echo target-state-source=DIAGNOSTIC2-MODE1-RESOLVED-RAW-SEED
echo target-node-count=12
echo candidate-representation=CONSERVED-INVENTORY
echo candidate-construction-language=C#
echo candidate-construction-scope=TEST-ONLY
echo runtime-dynamic-steps=0
echo candidate-hydraulic-head-count=8
echo c4-resolver-change=FORBIDDEN
echo c4-payload-change=FORBIDDEN
echo canonical-exact-v9-change=FORBIDDEN
echo production-seed-seam-change=FORBIDDEN
)

> "%REPORT_DIR%\03-replanning-summary.txt" (
echo status=PASS-AS-AUTHORED
echo next-authorized-gate=R3-REFERENCE-CONSISTENT-RAW-SEED-CANDIDATE-CONSTRUCTION1
echo candidate-construction-is-production=False
echo production-seed-repair-authorized=False
echo new-exact-version-identity-authorized=False
echo threshold-change-authorized=False
echo r3-passed=False
echo r4-planning-authorized=False
echo post-candidate-successor=R3-REFERENCE-CONSISTENT-SEED-INTEGRATION-PLANNING1
)

> "%REPORT_DIR%\04-preexecution-review.txt" (
echo status=PASS-AS-AUTHORED
echo frozen-target-state-vector-required=True
echo candidate-mass-energy-vector-must-be-emitted=True
echo candidate-target-roundtrip-must-be-emitted=True
echo candidate-hydraulic-head-comparison-must-be-emitted=True
echo no-runtime-preconditioning=True
echo no-dynamic-simulation=True
echo returned-evidence-required-before-integration-planning=True
echo r4-remains-blocked=True
)

echo.
echo R3 Requalification 2 Reference-Consistent Operating-Point Seed Replanning 1 completed PASS-AS-AUTHORED.
echo Return the full "%REPORT_DIR%" folder before Candidate Construction 1.
exit /b 0

:cleanup_fail
echo Replanning 1 FAILED: stale artifact directory could not be removed.
exit /b 1
:create_fail
echo Replanning 1 FAILED: artifact directory could not be created.
exit /b 1
:fail
echo Replanning 1 audit FAILED.
exit /b 1
