@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "METHOD=NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance.M10974FingerprintV1SchemaAnchorTests.FingerprintV1_PopulatedExactVersionFixtureMatchesFrozenGoldenHash"
set "NRS_FINGERPRINT_V1_DIAGNOSTICS_DIR=%ROOT%\artifacts\ci\fingerprint-v1-local"

if exist "%NRS_FINGERPRINT_V1_DIAGNOSTICS_DIR%" rmdir /s /q "%NRS_FINGERPRINT_V1_DIAGNOSTICS_DIR%"
mkdir "%NRS_FINGERPRINT_V1_DIAGNOSTICS_DIR%" || exit /b 1

echo ============================================================
echo M10.9.7.4 FINGERPRINT V1 CROSS-HOST DETERMINISM DIAGNOSTIC 1
echo ============================================================
echo Test-only capture. Golden hash and fingerprint-v1 semantics remain unchanged.

echo [1/4] Static diagnostic contract audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10974-fingerprint-v1-cross-host-diagnostic1.ps1" || exit /b 1

echo [2/4] Restoring Application.Tests...
dotnet restore "%PROJECT%" || exit /b 1

echo [3/4] Building Application.Tests Release...
dotnet build "%PROJECT%" --configuration Release --no-restore || exit /b 1

echo [4/4] Running frozen H29 step-128 fingerprint anchor with diagnostic capture...
dotnet test --project "%PROJECT%" --configuration Release --no-build -- --filter-method "%METHOD%" --parallel none --xunit-info || exit /b 1

if not exist "%NRS_FINGERPRINT_V1_DIAGNOSTICS_DIR%\fingerprint-v1-cross-host-diagnostic.zip" (
  echo Diagnostic ZIP was not produced.
  exit /b 1
)
if not exist "%NRS_FINGERPRINT_V1_DIAGNOSTICS_DIR%\fingerprint-v1-cross-host-summary.txt" (
  echo Diagnostic summary was not produced.
  exit /b 1
)

echo.
echo Fingerprint V1 Cross-Host Determinism Diagnostic 1 completed.
echo Return:
echo %NRS_FINGERPRINT_V1_DIAGNOSTICS_DIR%\fingerprint-v1-cross-host-diagnostic.zip
exit /b 0
