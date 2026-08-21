@echo off
setlocal EnableExtensions
set "ROOT=%~dp0"
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo Applying M10.9.7.2 Hotfix 3 REV1 - JsonDocument Parse Exception-Type Test Alignment...
echo Removing stale build and focused-audit outputs...
for /d /r %%D in (bin obj) do @if exist "%%D" rd /s /q "%%D"
if exist "artifacts\m10972-hotfix3-persistence-payload-integrity" rd /s /q "artifacts\m10972-hotfix3-persistence-payload-integrity"

echo.
echo Baseline: M10.9.7.2 Hotfix 2 REV1 VALIDATED; original Hotfix 3 is SUPERSEDED / NOT VALIDATED.
echo Persistence runtime is identical to Hotfix 3; REV1 only aligns the malformed-scenario JsonException regression assertion with the public exception contract.
echo Malformed JSON across all persistence adapters fails as InvalidDataException; future schema versions remain NotSupportedException.
echo String-enum schema migration and stream-based persistence remain deferred.
echo No replay authority, MISSION activation, scoring, challenge, protection, physics or plant command authority change is included.
echo.
echo Run:
echo   dotnet build
echo   dotnet test
echo   scripts\run-m10972-persistence-payload-integrity-audit.cmd
exit /b 0
