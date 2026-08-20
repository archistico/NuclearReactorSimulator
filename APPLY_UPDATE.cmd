@echo off
setlocal EnableExtensions
set "ROOT=%~dp0"
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo Applying M10.9.4.1-I.4 Hotfix 2 - Canonical Frozen-Evidence Contract Alignment...
echo Removing stale build and I.4 audit outputs...
for /d /r %%D in (bin obj) do @if exist "%%D" rd /s /q "%%D"
if exist "artifacts\i4-phase-i-known-limitations-legacy-retirement-review" rd /s /q "artifacts\i4-phase-i-known-limitations-legacy-retirement-review"

echo Local tests\NuclearReactorSimulator.Application.Tests\Scenarios\Gameplay\Evidence payloads are optional and are left untouched.
echo Candidate ZIPs do not bundle Gameplay\Evidence, artifacts, bin or obj.
echo Compact frozen prerequisites remain under eng\frozen-evidence\ordinary.

echo Removing superseded root-level M10.9.4.1 chronology already archived under docs\history...
if exist "docs\M10_9_4_1_*.md" del /q "docs\M10_9_4_1_*.md"

echo.
echo H.30 Requalification 1 remains validated with ACTIVATE.
echo I.3 Hotfix 2 is validated: the 300 s authoritative v3 reference, seven slopes and 19 regression budgets are frozen.
echo I.4 reviews current limitations and H.5/H.21 historical source retirement; candidate decision is DEFER-SOURCE-REMOVAL.
echo Run:
echo   dotnet build
echo   dotnet test
echo   scripts\run-phase-i-known-limitations-legacy-retirement-review-audit.cmd
exit /b 0
