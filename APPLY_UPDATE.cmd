@echo off
setlocal EnableExtensions
set "ROOT=%~dp0"
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo Applying M10.9.4.1-I.3 Hotfix 4 Classifier Fix 1 - Targeted-Train Reverse-Flow Classification...
echo Removing stale build and Hotfix 4 comparison outputs...
for /d /r %%D in (bin obj) do @if exist "%%D" rd /s /q "%%D"
if exist "artifacts\i3-hotfix4-explicit-vs-corrected-branch-discontinuity-comparison" rd /s /q "artifacts\i3-hotfix4-explicit-vs-corrected-branch-discontinuity-comparison"

echo.
echo Script Fix 1 applied. I.2 remains the authoritative validated baseline and I.3 remains unvalidated.
echo The Hotfix 4 diagnostic C# code and acceptance criteria are unchanged.
echo The focused launcher now uses the .NET 10 Microsoft.Testing.Platform --project contract and native xUnit v3 MTP filters.
echo Run:
echo   dotnet build
echo   dotnet test
echo   scripts\run-phase-i-explicit-vs-corrected-branch-discontinuity-comparison-audit.cmd
exit /b 0
