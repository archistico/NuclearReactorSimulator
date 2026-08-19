@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo [CI LONG] Clean restore/build first...
dotnet restore || exit /b 1
dotnet build --configuration Release --no-restore || exit /b 1

echo [CI LONG] Long-running gameplay/reference journey gate...
call scripts\run-gameplay-long-tests.cmd || exit /b 1

echo [CI LONG] Operational-envelope gate...
call scripts\run-operational-envelope-audit.cmd || exit /b 1

echo [CI LONG] Reference-plant scale gate...
call scripts\run-reference-plant-scale-audit.cmd || exit /b 1

echo [CI LONG] Phase-I corrected 300-second pre-H.30 requalification...
call scripts\run-phase-i-corrected-300s-healthy-reference-requalification-audit.cmd || exit /b 1

echo [CI LONG] PASSED.
exit /b 0
