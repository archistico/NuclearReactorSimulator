@echo off
setlocal EnableExtensions

set "ROOT=%~dp0"
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo Applying M10.9.4.1-H.28 Requalification 2 over user-validated H.28.1-G...
echo Restoring the original H.28 cost ceilings over the optimized runtime; no numerical runtime code is changed by this package.
echo Removing stale build and H.28 audit outputs...
for /d /r %%D in (bin obj) do (
    if exist "%%D" rmdir /s /q "%%D"
)
if exist "artifacts\h28-four-node-performance-cost-operational-soak" rmdir /s /q "artifacts\h28-four-node-performance-cost-operational-soak"

echo.
echo H.28 Requalification 2 candidate applied. H.29 remains blocked; standard current-v2 remains ExplicitCommittedState at 10 ms.
echo Run:
echo   dotnet build
echo   dotnet test
echo   scripts\run-four-node-performance-cost-operational-soak-audit.cmd
exit /b 0
