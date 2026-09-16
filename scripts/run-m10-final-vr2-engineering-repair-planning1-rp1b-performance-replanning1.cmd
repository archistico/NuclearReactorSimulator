@echo off
setlocal
cd /d "%~dp0.."

set ARTIFACT_DIR=artifacts\m10-final-physical-reference-vr2-engineering-repair-planning1-rp1b-performance-replanning1

if exist "%ARTIFACT_DIR%" rmdir /s /q "%ARTIFACT_DIR%"

 echo ============================================================
 echo M10 FINAL - VR2 ENGINEERING REPAIR PLANNING 1 - RP1B PERFORMANCE REPLANNING 1
 echo ============================================================
 echo Planning/adjudication only on immutable C3 and returned R3/R4 evidence.
 echo No C4, RP1C selection, production repair, tolerance change, exact-v9 change, VR3,
 echo P3-R1 execution or second replacement-long authorization.
 echo.
 echo [1/1] Static returned-Refinement4 evidence and performance-replanning contract audit...

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "eng\validate-m10-final-vr2-engineering-repair-planning1-rp1b-performance-replanning1.ps1"
if errorlevel 1 goto :fail

 echo.
 echo M10 Final VR2 Engineering Repair Planning 1 RP1B Performance Measurement Replanning 1 completed.
 echo This freezes only the interpretation of returned R3/R4 timing evidence and the later Refinement 5 protocol.
 echo Return the full ".\%ARTIFACT_DIR%" folder before implementing Refinement 5, C4 or RP1C.
 exit /b 0

:fail
 echo.
 echo M10 Final VR2 Engineering Repair Planning 1 RP1B Performance Measurement Replanning 1 FAILED.
 echo Preserve and return any files already written under ".\%ARTIFACT_DIR%".
 exit /b 2
