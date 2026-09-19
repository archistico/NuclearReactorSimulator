@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."
set "REPORT_DIR=%CD%\artifacts\m10-final-physical-reference-vr2-r3-mode2-branch-continuity-fusion-repair-planning1"
if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
if exist "%REPORT_DIR%" goto :cleanup_fail
echo ============================================================
echo M10 FINAL - VR2 R3 MODE2 BRANCH-CONTINUITY FUSION REPAIR PLANNING 1
echo ============================================================
echo Planning only. No production repair, R3 PASS, R4, exact-v9/default or threshold authority.
echo.
echo [1/1] Returned-diagnostic, frozen-baseline and repair-scope planning audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-r3-mode2-branch-continuity-fusion-repair-planning1.ps1"
if errorlevel 1 goto :fail
mkdir "%REPORT_DIR%" >nul 2>nul
if not exist "%REPORT_DIR%" goto :create_fail
> "%REPORT_DIR%\01-contract-and-provenance.txt" (
echo status=PASS-AS-AUTHORED
echo planning-id=R3-MODE2-BRANCH-CONTINUITY-FUSION-REPAIR-PLANNING1
echo predecessor-r3=R3-SHADOW-COMPOSITION-BLOCKING
echo predecessor-diagnostic=PASS-DIAGNOSTIC-MODE2-FUSION-BYPASS-CONFIRMED
echo root-cause=H28.1-E-FUSED-PATH-NOT-MODE2-AWARE
echo returned-diagnostic-artifacts=4
echo production-src-changed=False
echo tests-changed=False
)
> "%REPORT_DIR%\02-repair-scope-and-regression-matrix.txt" (
echo status=PASS-AS-AUTHORED
echo selected-repair=MODE2-FUSION-ELIGIBILITY-GUARD
echo authorized-production-files=2
echo mode0-fused-path=PRESERVE
echo mode1-fused-path=PRESERVE
echo mode2-fused-path=INELIGIBLE
echo mode2-production-path=EXISTING-NONFUSED-RESOLVE-PLUS-DIAGNOSTIC
echo evaluate-branch-continuity-change=FORBIDDEN
echo resolver-change=FORBIDDEN
echo payload-change=FORBIDDEN
echo closure-enum-default-change=FORBIDDEN
)
> "%REPORT_DIR%\03-planning-summary.txt" (
echo status=PASS-AS-AUTHORED
echo next-authorized-gate=R3-MODE2-BRANCH-CONTINUITY-FUSION-REPAIR-IMPLEMENTATION1
echo production-repair-authorized-now=False
echo r3-passed=False
echo r4-planning-authorized=False
echo post-implementation-successor=R3-SHORT-EXACT-V9-EQUIVALENT-SHADOW-COMPOSITION-REQUALIFICATION2
)
> "%REPORT_DIR%\04-preexecution-review.txt" (
echo status=PASS-AS-AUTHORED
echo implementation-must-be-two-file-bounded=True
echo mode0-mode1-regression-must-pass=True
echo mode2-returned-state-equality-must-pass=True
echo ordinary-release-suite-must-pass=True
echo r3-120s-requalification-still-required-after-implementation=True
echo r4-remains-blocked=True
)
echo.
echo R3 Mode-2 Branch-Continuity Fusion Repair Planning 1 completed PASS-AS-AUTHORED.
echo Return the full "%REPORT_DIR%" folder before Repair Implementation 1.
exit /b 0
:cleanup_fail
echo Planning audit FAILED: stale artifact directory could not be removed.
exit /b 1
:create_fail
echo Planning audit FAILED: artifact directory could not be created.
exit /b 1
:fail
echo R3 fusion Repair Planning 1 audit FAILED.
exit /b 1
