@echo off
setlocal
cd /d "%~dp0.."

set ARTIFACT_DIR=artifacts\m10-final-frozen-evidence-ordinary-compaction1
if exist "%ARTIFACT_DIR%" rmdir /s /q "%ARTIFACT_DIR%"

echo ============================================================
echo M10 FINAL - FROZEN EVIDENCE ORDINARY COMPACTION 1
echo ============================================================
echo Maintenance-only frozen-evidence compaction after returned Host Provenance Amendment 1 PASS-AS-AUTHORED.
echo No A2 implementation, FDPC2, RP1C selection, production/runtime change, threshold change,
echo exact-v9 change, VR3, P3-R1 or second replacement-long authorization.
echo.
echo [1/1] Static compact-store, archive-integrity and authority audit...

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "eng\validate-m10-final-frozen-evidence-ordinary-compaction1.ps1"
if errorlevel 1 goto :fail

echo.
echo M10 Final Frozen Evidence Ordinary Compaction 1 completed.
echo Return the full ".\%ARTIFACT_DIR%" folder before implementing Runtime Configuration Impact Assessment 1.
exit /b 0

:fail
echo.
echo Frozen Evidence Ordinary Compaction 1 FAILED.
echo Preserve and return any files already written under ".\%ARTIFACT_DIR%".
exit /b 2
