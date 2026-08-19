@echo off
setlocal EnableExtensions
set "ROOT=%~dp0"
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo Applying M10.9.4.1-H.29 Production Activation Candidate over validated H.24 Requalification 1 post-H.28...
echo Removing stale build and H.29 candidate audit outputs...
for /d /r %%D in (bin obj) do @if exist "%%D" rd /s /q "%%D"
if exist "artifacts\h29-four-node-production-activation-candidate" rd /s /q "artifacts\h29-four-node-production-activation-candidate"

echo.
echo H.29 candidate applied. Exact v2 remains ExplicitCommittedState and authoritative.
echo Exact v3 is candidate-only; H.30 retains the final activation decision.
echo No H.9, P060/F040, H.20/H.22, hysteresis, physical-coefficient or 10 ms timestep retuning is introduced.
echo Run:
echo   dotnet build
echo   dotnet test
echo   scripts\run-four-node-production-activation-candidate-audit.cmd
exit /b 0
