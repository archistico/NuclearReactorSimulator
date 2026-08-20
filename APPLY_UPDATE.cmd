@echo off
setlocal EnableExtensions
set "ROOT=%~dp0"
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo Applying M10.9.6.3 Hotfix 1 - Missing Parent Challenge Namespace Test Compile Fix...
echo Removing stale build and focused-audit outputs...
for /d /r %%D in (bin obj) do @if exist "%%D" rd /s /q "%%D"
if exist "artifacts\m1096-multidimensional-scoring" rd /s /q "artifacts\m1096-multidimensional-scoring"

echo.
echo M10.9.6.1 Hotfix 1 and M10.9.6.2 Hotfix 1 are validated prerequisites.
echo M10.9.6.3 scoring semantics are unchanged; Hotfix 1 only restores the missing parent challenge namespace in the focused test.
echo Standard v1 guidance and plant-control-authority modifiers are explicit and neutral.
echo It adds no challenge pack, UI, command authority, protection ownership or physics.
echo.
echo Run:
echo   dotnet build
echo   dotnet test
echo   scripts\run-m1096-multidimensional-scoring-audit.cmd
echo.
echo If all gates are green, M10.9.6.3 is VALIDATED and M10.9.6.4 initial challenge packs are next.
exit /b 0
