@echo off
setlocal EnableExtensions
set "ROOT=%~dp0"
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo Applying M10.9.4.1-I.3 Hotfix 5 Compile Fix 1 - Recording Fingerprint Namespace Import...
echo Removing stale build and Hotfix 5 audit outputs...
for /d /r %%D in (bin obj) do @if exist "%%D" rd /s /q "%%D"
if exist "artifacts\i3-hotfix5-corrected-300s-healthy-reference-requalification" rd /s /q "artifacts\i3-hotfix5-corrected-300s-healthy-reference-requalification"

echo.
echo I.2 remains authoritative. I.3 remains unvalidated and H.30 remains OPT-IN ONLY.
echo Hotfix 5 Compile Fix 1 adds only the missing Recording namespace import; the exact-v3 300 s gate is otherwise unchanged.
echo Run:
echo   dotnet build
echo   dotnet test
echo   scripts\run-phase-i-corrected-300s-healthy-reference-requalification-audit.cmd
exit /b 0
