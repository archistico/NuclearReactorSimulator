@echo off
setlocal EnableExtensions
set "ROOT=%~dp0"
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo Applying M10.9.6.4 - Initial Challenge Packs...
echo Removing stale build and focused-audit outputs...
for /d /r %%D in (bin obj) do @if exist "%%D" rd /s /q "%%D"
if exist "artifacts\m1096-initial-challenge-packs" rd /s /q "artifacts\m1096-initial-challenge-packs"

echo.
echo M10.9.6.1 Hotfix 1, M10.9.6.2 Hotfix 1 and M10.9.6.3 Hotfix 1 are validated prerequisites.
echo M10.9.6.4 composes six versioned challenge packs only from existing validated scenario/check/fault owners.
echo Demand remains observational, scoring arithmetic remains M10.9.6.3-owned and failure semantics remain challenge-specific.
echo It adds no UI, new fault physics, command authority, protection ownership or Simulation change.
echo.
echo Run:
echo   dotnet build
echo   dotnet test
echo   scripts\run-m1096-initial-challenge-pack-audit.cmd
echo.
echo If all gates are green, M10.9.6.4 is VALIDATED and M10.9.6.5 replay/checkpoint/determinism closure is next.
exit /b 0
