@echo off
setlocal EnableExtensions
set "ROOT=%~dp0"
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo Applying M10.9.4.1-H.30 Requalification 1 Hotfix 1 - Operations Namespace Compile Fix...
echo Removing stale build and H.30 RQ1 Hotfix 1 audit outputs...
for /d /r %%D in (bin obj) do @if exist "%%D" rd /s /q "%%D"
if exist "artifacts\h30-rq1-production-policy-rereview" rd /s /q "artifacts\h30-rq1-production-policy-rereview"

echo Removing superseded root-level M10.9.4.1 chronology now archived under docs\history...
if exist "docs\M10_9_4_1_*.md" del /q "docs\M10_9_4_1_*.md"

echo.
echo I.2 remains authoritative until this candidate is explicitly validated.
echo Candidate policy: ACTIVATE exact v3 corrected-commit as desktop default; preserve exact v2 as fail-closed rollback/reference.
echo Documentation has been consolidated: current docs stay in docs root/current, detailed M10.9.4.1 chronology is under docs\history\m10.9.4.1.
echo Run:
echo   dotnet build
echo   dotnet test
echo   scripts\run-h30-rq1-production-policy-rereview-audit.cmd
exit /b 0
