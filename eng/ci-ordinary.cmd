@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

echo [CI ORDINARY] Restoring packages...
dotnet restore || exit /b 1

echo [CI ORDINARY] Building Release with warnings-as-errors...
dotnet build --configuration Release --no-restore || exit /b 1

echo [CI ORDINARY] Running complete ordinary suite...
dotnet test --configuration Release --no-build || exit /b 1

echo [CI ORDINARY] Running current frozen-evidence baseline contracts...
call eng\ci-current-evidence.cmd || exit /b 1

echo [CI ORDINARY] PASSED.
exit /b 0
