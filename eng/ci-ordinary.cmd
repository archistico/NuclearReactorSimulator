@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

rem Keep local and hosted ordinary CI build semantics identical inside this entry point.
set "CI=true"

echo [CI ORDINARY] Validating stable CI contract V2.1.1 hosted-failure-capture...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-github-ordinary-ci-stable-contract-v2_1_1.ps1" || exit /b 1

echo [CI ORDINARY] Restoring packages...
dotnet restore || exit /b 1

echo [CI ORDINARY] Building Release with warnings-as-errors...
dotnet build --configuration Release --no-restore || exit /b 1

echo [CI ORDINARY] Running complete ordinary suite deterministically...
rem Microsoft.Testing.Platform modules are serialized and xUnit collection parallelism is disabled.
rem No test is filtered, retried, skipped or allowed to fail. Detailed output preserves the first real failure.
dotnet test --configuration Release --no-build --max-parallel-test-modules 1 --output Detailed -- --parallel none --xunit-info || exit /b 1

echo [CI ORDINARY] Running current frozen-evidence baseline contracts...
call eng\ci-current-evidence.cmd || exit /b 1

echo [CI ORDINARY] PASSED.
exit /b 0
