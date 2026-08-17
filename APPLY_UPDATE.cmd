@echo off
setlocal EnableExtensions
cd /d "%~dp0"
if errorlevel 1 exit /b 1

echo Applying M10.9.4.1-H.18 Hotfix 1 turbine-inlet continuity / residual-floor split candidate...

echo Removing stale local build outputs so stacked ZIP timestamps cannot reuse an older assembly...
for /d /r %%D in (bin) do @if exist "%%D" rd /s /q "%%D"
for /d /r %%D in (obj) do @if exist "%%D" rd /s /q "%%D"

rem Compatibility cleanup retained from prior stacked packages.
for %%F in (
    "docs\adr\0110-electrical-protection-thresholds-are-derived-from-signed-current-v2-trajectories.md"
    "docs\adr\0111-evidence-derived-electrical-protection-uses-supervised-delayed-m5-functions.md"
) do (
    if exist "%%~F" del /q "%%~F"
)

echo M10.9.4.1-H.18 Hotfix 1 applied. Stale bin/obj outputs were removed; rebuild before testing.
exit /b 0
