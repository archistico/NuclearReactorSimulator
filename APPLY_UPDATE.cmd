@echo off
setlocal EnableExtensions
set "ROOT=%~dp0"
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo Applying M10.9.4.1-I.1 Hotfix 1 Profile Compatibility / Legacy Retirement Inventory over validated H.30...
echo Removing stale build and I.1 audit outputs...
for /d /r %%D in (bin obj) do @if exist "%%D" rd /s /q "%%D"
if exist "artifacts\i1-profile-compatibility-legacy-retirement-inventory" rd /s /q "artifacts\i1-profile-compatibility-legacy-retirement-inventory"

echo.
echo I.1 Hotfix 1 candidate applied. H.30 remains authoritative and Phase H stays closed as OPT-IN ONLY.
echo Exact v2 remains ExplicitCommittedState default/rollback/reference.
echo Exact v3 remains the qualified corrected opt-in path.
echo No exact-version profile is deleted by I.1; historical audit-only modes are inventory-only retirement candidates.
echo Run:
echo   dotnet build
echo   dotnet test
echo   scripts\run-profile-compatibility-legacy-retirement-inventory-audit.cmd
exit /b 0
