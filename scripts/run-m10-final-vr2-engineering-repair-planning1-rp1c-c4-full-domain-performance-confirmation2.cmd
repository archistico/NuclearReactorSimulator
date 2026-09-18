@echo off
setlocal EnableExtensions
cd /d "%~dp0\.."

set "REPORT_DIR=%CD%\artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation2"

echo ============================================================
echo M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1C C4 FULL-DOMAIN PERFORMANCE CONFIRMATION 2
echo ============================================================
echo Evidence-only same-host confirmation under AMBIENT-UNSET.
echo FDPC1 negative history is preserved. Engineering-negative FDPC2 outcomes remain evidence.
echo No RP1C selection, production/runtime change, production repair, threshold change,
echo exact-v9 change, VR3, P3-R1 or second replacement-long authorization.
echo.

echo [1/4] Static returned-planning, host-scope, candidate/corpus and FDPC2 contract audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10-final-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation2.ps1"
if errorlevel 1 goto :fail

echo.
echo [2/4] Ordinary Release gate before focused FDPC2 evidence...
call eng\ci-ordinary.cmd
if errorlevel 1 goto :fail

echo.
echo [3/4] Same-host ambient/unset FDPC2 evidence collection: five fresh processes...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\invoke-m10-final-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation2.ps1" -RepositoryRoot "%CD%"
if errorlevel 1 goto :focused_fail

echo.
echo [4/4] Evidence integrity, complete exact-v9/seam matrices, host consistency and engineering classification...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\adjudicate-m10-final-vr2-engineering-repair-planning1-rp1c-c4-full-domain-performance-confirmation2.ps1"
if errorlevel 1 goto :adjudication_fail

for %%F in (
  01-contract-and-provenance.txt
  02-execution-host-provenance.txt
  03-host-consistency-audit.txt
  04-cross-process-run-summary.csv
  05-cross-process-seam-side-summary.csv
  06-confirmation-evidence-adjudication.txt
  07-rp1c-c4-full-domain-performance-confirmation2-summary.txt
) do if not exist "%REPORT_DIR%\%%F" goto :missing

echo.
echo M10 Final VR2 RP1C C4 Full-Domain Performance Confirmation 2 completed.
echo Classification is evidence only; this runner does not authorize RP1C selection or any production/runtime change.
echo Return the full "%REPORT_DIR%" folder before RP1C selection planning or production repair.
exit /b 0

:focused_fail
echo.
echo Full-Domain Performance Confirmation 2 FAILED infrastructure/evidence collection controls.
echo Preserve and return any files already written under "%REPORT_DIR%".
echo This is not automatically an engineering verdict against C4 or AMBIENT-UNSET.
exit /b 1

:adjudication_fail
echo.
echo Full-Domain Performance Confirmation 2 evidence integrity adjudication FAILED.
echo Preserve and return the complete "%REPORT_DIR%" folder.
exit /b 1

:missing
echo.
echo Full-Domain Performance Confirmation 2 FAILED: required aggregate evidence artifact is missing.
echo Preserve and return the complete "%REPORT_DIR%" folder.
exit /b 1

:fail
echo.
echo Full-Domain Performance Confirmation 2 FAILED before controlled evidence completion.
echo No RP1C selection or production/runtime authority is granted.
exit /b 1
