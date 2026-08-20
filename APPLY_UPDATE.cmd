@echo off
setlocal EnableExtensions
set "ROOT=%~dp0"
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo Applying M10.9.5.1 - Contextual Command Consequence Model / Consequence Semantics and Catalog...
echo Removing stale build and M10.9.5.1 generated outputs...
for /d /r %%D in (bin obj) do @if exist "%%D" rd /s /q "%%D"

echo.
echo M10.9.4.1 / Phase I is the validated baseline and remains closed.
echo M10.9.5.1 changes Application presentation metadata/tests/docs only; no plant physics, exact-version identity or command-dispatch owner is changed.
echo.
echo Run:
echo   dotnet build
echo   dotnet test
echo   scripts\run-m1095-command-consequence-catalog-audit.cmd
echo.
echo If all three gates are green, M10.9.5.1 is validated and M10.9.5.2 dependency-chain projection is next.
exit /b 0
