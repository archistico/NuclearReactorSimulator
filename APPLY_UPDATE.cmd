@echo off
setlocal EnableExtensions
set "ROOT=%~dp0"
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo Applying M10.9.4.1-H.24 Requalification 1 over the user-validated H.28 runtime...
echo Removing stale build and post-H.28 H.24 requalification outputs...
for /d /r %%D in (bin obj) do @if exist "%%D" rd /s /q "%%D"
if exist "artifacts\h24-post-h28-four-node-committed-long-horizon-cross-profile-requalification" rd /s /q "artifacts\h24-post-h28-four-node-committed-long-horizon-cross-profile-requalification"

echo.
echo H.24 Requalification 1 candidate applied. No numerical runtime retuning is introduced.
echo H.28 is VALIDATED as bounded-but-costly; H.29 remains blocked until the long-horizon regression is green.
echo Run:
echo   dotnet build
echo   dotnet test
echo   scripts\run-four-node-post-h28-committed-long-horizon-requalification-audit.cmd
exit /b 0
