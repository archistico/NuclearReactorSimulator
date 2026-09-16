@echo off
setlocal
cd /d "%~dp0.."

set ARTIFACT_DIR=artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1b-c4-planning1

if exist "%ARTIFACT_DIR%" rmdir /s /q "%ARTIFACT_DIR%"

 echo ============================================================
 echo M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1B C4 PLANNING 1 REV2
 echo ============================================================
 echo Planning REV2 only on validated returned Refinement 5 evidence.
 echo No C4 implementation, RP1C selection, production repair, threshold change,
 echo exact-v9 change, VR3, P3-R1 or second replacement-long authorization.
 echo.
 echo [1/1] Static returned-evidence, source-attribution and C4 planning-contract audit...

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "eng\validate-m10-final-vr2-engineering-repair-planning1-rp1b-c4-planning1.ps1"
if errorlevel 1 goto :fail

 echo.
 echo M10 Final VR2 Engineering Repair Planning 1 RP1B C4 Planning 1 REV2 completed.
 echo This freezes only the separately versioned test-only C4 design and evidence contract.
 echo Return the full ".\%ARTIFACT_DIR%" folder before implementing C4 or planning RP1C.
 exit /b 0

:fail
 echo.
 echo M10 Final VR2 Engineering Repair Planning 1 RP1B C4 Planning 1 REV2 FAILED.
 echo Preserve and return any files already written under ".\%ARTIFACT_DIR%".
 exit /b 2
