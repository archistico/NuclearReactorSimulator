@echo off
setlocal EnableExtensions
set "ROOT=%~dp0"
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo Applying M10.9.5.2 - Explicit Dependency-Chain Projection...
echo Removing stale build outputs...
for /d /r %%D in (bin obj) do @if exist "%%D" rd /s /q "%%D"

echo.
echo M10.9.5.1 is the validated baseline.
echo M10.9.5.2 changes Application presentation metadata/tests/docs only; no plant physics, command dispatch or Avalonia UI is changed.
echo.
echo Run:
echo   dotnet build
echo   dotnet test
echo   scripts\run-m1095-command-dependency-chain-audit.cmd
echo.
echo If all three gates are green, M10.9.5.2 is validated and M10.9.5.3 UI integration is next.
exit /b 0
