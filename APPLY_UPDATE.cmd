@echo off
setlocal EnableExtensions
set "ROOT=%~dp0"
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo Applying M10.9.7.3 Hotfix 2 REV2 - Desktop Host Failure ^& Session Save Integrity...
echo Removing stale build and focused-audit outputs...
for /d /r %%D in (bin obj) do @if exist "%%D" rd /s /q "%%D"
if exist "artifacts\m10973-desktop-host-session-integrity" rd /s /q "artifacts\m10973-desktop-host-session-integrity"

echo.
echo Baseline: M10.9.7.3 Hotfix 1 REV2 VALIDATED on 2026-08-21 after automated and manual HMI gates.
echo Original Hotfix 2 was not validated: build failed only on eight xUnit1051 cancellation-token analyzer violations in new async tests.
echo Hotfix 2 REV1 was not validated: ordinary tests exposed three follow-up contract defects after compilation succeeded.
echo Hotfix 2 REV2 explicitly classifies InvalidDataException, avoids backup cleanup on the new-file path, and aligns the historical archive-boundary regression with the centralized policy.
echo Expected numerical step failures pause and report; unknown programming exceptions are not swallowed.
echo SAVE now selects the destination before export and uses temp-sibling durable write plus safe local-filesystem replace/move.
echo Existing archives are never truncate-written by NRS before the replacement is complete.
echo MISSION, F1-F8/no-F9, scoring, protection, plant-command authority, physics and archive schema are unchanged.
echo.
echo Run:
echo   dotnet build
echo   dotnet test
echo   scripts\run-m10973-desktop-host-session-integrity-audit.cmd
echo Then complete:
echo   docs\M10_9_7_3_HOTFIX2_MANUAL_VALIDATION_CHECKLIST.md
exit /b 0
