@echo off
setlocal EnableExtensions
set "ROOT=%~dp0"
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo Applying M10.9.4.1-H.30 Phase H Closure / Production Qualification Decision over validated H.29...
echo Removing stale build and H.30 closure audit outputs...
for /d /r %%D in (bin obj) do @if exist "%%D" rd /s /q "%%D"
if exist "artifacts\h30-phase-h-closure-production-qualification-decision" rd /s /q "artifacts\h30-phase-h-closure-production-qualification-decision"

echo.
echo H.30 closure candidate applied. Candidate decision: OPT-IN ONLY.
echo Exact v2 remains ExplicitCommittedState authoritative default/rollback/reference.
echo Exact v3 remains the qualified corrected opt-in path; the H.29 selector implementation is unchanged.
echo H.24 and H.28 are not rerun by the H.30 focused gate.
echo Run:
echo   dotnet build
echo   dotnet test
echo   scripts\run-phase-h-closure-production-qualification-decision-audit.cmd
exit /b 0
