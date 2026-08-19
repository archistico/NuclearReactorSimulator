@echo off
setlocal EnableExtensions
set "ROOT=%~dp0"
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo Applying M10.9.4.1-I.2 Audit Consolidation / CI Baseline Hardening over validated I.1 Hotfix 1...
echo Removing stale build and I.2 audit outputs...
for /d /r %%D in (bin obj) do @if exist "%%D" rd /s /q "%%D"
if exist "artifacts\i2-phase-i-audit-consolidation-ci-baseline" rd /s /q "artifacts\i2-phase-i-audit-consolidation-ci-baseline"

echo.
echo I.2 candidate applied. I.1 Hotfix 1 remains authoritative and Phase H stays closed as OPT-IN ONLY.
echo Exact v2 remains ExplicitCommittedState default/rollback/reference.
echo Exact v3 remains the qualified corrected opt-in path.
echo H.5/H.21 historical modes are not current-CI dependencies, but I.2 does not delete them.
echo Run:
echo   dotnet build
echo   dotnet test
echo   scripts\run-phase-i-audit-consolidation-ci-baseline-audit.cmd
exit /b 0
