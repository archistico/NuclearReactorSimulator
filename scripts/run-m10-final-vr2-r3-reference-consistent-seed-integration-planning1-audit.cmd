@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."
set "REPORT_DIR=%CD%\artifacts\m10-final-physical-reference-vr2-r3-reference-consistent-seed-integration-planning1"

if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
if exist "%REPORT_DIR%" goto :cleanup_fail

echo ============================================================
echo M10 FINAL - VR2 R3 REFERENCE-CONSISTENT SEED INTEGRATION PLANNING 1
echo ============================================================
echo Planning only. Canonical exact-v9 remains frozen; no production integration or R4 authority.
echo.

echo [1/1] Returned-candidate, frozen-baseline and integration-seam planning audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-r3-reference-consistent-seed-integration-planning1.ps1"
if errorlevel 1 goto :fail

mkdir "%REPORT_DIR%" >nul 2>nul
if not exist "%REPORT_DIR%" goto :create_fail

> "%REPORT_DIR%\01-contract-and-provenance.txt" (
echo status=PASS-AS-AUTHORED
echo planning-id=R3-REFERENCE-CONSISTENT-SEED-INTEGRATION-PLANNING1
echo predecessor=PASS-CANDIDATE-CONSTRUCTION-EVIDENCE-COMPLETE
echo returned-candidate-artifacts=5
echo candidate-nodes=12
echo phase-matches=12
echo production-src-changed=False
echo tests-changed=False
)

> "%REPORT_DIR%\02-integration-seam-and-fast-gate-contract.txt" (
echo status=PASS-AS-AUTHORED
echo integration-design=OPT-IN-CONSERVED-INVENTORY-SEED-SEAM
echo authorized-production-files=3
echo new-seed-subtype=OperationalFluidNodeSeed.ConservedInventory
echo canonical-exact-v9-method-body=PRESERVE
echo new-opt-in-candidate-factory=REQUIRED
echo candidate-closure-mode=ReferenceConsistentTabulatedInverseDomain
echo candidate-seed-preconditioning-ms=20
echo fast-running-steps=100
echo runtime-step-ms=10
echo fast-health-envelope=UNCHANGED-EXACT-V9
echo rollback-steps-max=0
echo c4-resolver-change=FORBIDDEN
echo c4-payload-change=FORBIDDEN
echo threshold-change=FORBIDDEN
)

> "%REPORT_DIR%\03-planning-summary.txt" (
echo status=PASS-AS-AUTHORED
echo next-authorized-gate=R3-REFERENCE-CONSISTENT-SEED-INTEGRATION-IMPLEMENTATION1
echo production-seed-integration-authorized-now=False
echo canonical-exact-v9-change-authorized=False
echo new-exact-version-identity-authorized=False
echo r3-passed=False
echo r4-planning-authorized=False
echo post-implementation-successor=R3-SHORT-EXACT-V9-EQUIVALENT-SHADOW-COMPOSITION-REQUALIFICATION3
)

> "%REPORT_DIR%\04-preexecution-review.txt" (
echo status=PASS-AS-AUTHORED
echo frozen-12-node-vector-required=True
echo generic-conserved-inventory-seam-required=True
echo active-closure-must-resolve-conserved-inventory=True
echo canonical-exact-v9-source-must-remain-unchanged=True
echo first-100-running-steps-health-violations-max=0
echo first-100-running-steps-rollbacks-max=0
echo ordinary-release-suite-required=True
echo full-r3-120s-requalification-still-required=True
echo r4-remains-blocked=True
)

echo.
echo R3 Reference-Consistent Seed Integration Planning 1 completed PASS-AS-AUTHORED.
echo Return the full "%REPORT_DIR%" folder before Seed Integration Implementation 1.
exit /b 0

:cleanup_fail
echo Seed Integration Planning 1 FAILED: stale artifact directory could not be removed.
exit /b 1
:create_fail
echo Seed Integration Planning 1 FAILED: artifact directory could not be created.
exit /b 1
:fail
echo Seed Integration Planning 1 audit FAILED.
exit /b 1
