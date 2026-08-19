@echo off
setlocal EnableExtensions
set "ROOT=%~dp0"
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo Applying M10.9.4.1-I.3 Hotfix 2 - Compact Frozen Evidence Contract Migration...
echo Removing stale build and I.3 runtime audit outputs...
for /d /r %%D in (bin obj) do @if exist "%%D" rd /s /q "%%D"
if exist "artifacts\i3-phase-i-authoritative-reference-trajectory-baseline" rd /s /q "artifacts\i3-phase-i-authoritative-reference-trajectory-baseline"

echo Local tests\NuclearReactorSimulator.Application.Tests\Scenarios\Gameplay\Evidence payloads are optional and are left untouched.
echo Ordinary tests now use eng\frozen-evidence\ordinary plus the compact large-payload hash manifest.
echo Candidate ZIPs intentionally do not bundle the generated/local Gameplay\Evidence directory.

echo Removing superseded root-level M10.9.4.1 chronology already archived under docs\history...
if exist "docs\M10_9_4_1_*.md" del /q "docs\M10_9_4_1_*.md"

echo.
echo H.30 Requalification 1 is validated: exact v3 corrected-commit is the authoritative production default.
echo Exact v2 explicit remains fail-closed rollback/reference.
echo I.3 Hotfix 2 preserves the authoritative 300 s reference contract and decouples ordinary tests from the excluded Gameplay\Evidence payload directory.
echo Run:
echo   dotnet build
echo   dotnet test
echo   scripts\run-phase-i-reference-trajectory-conservation-inventory-baseline-audit.cmd
exit /b 0
