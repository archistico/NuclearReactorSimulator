@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "CLASS=NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance.M10974FingerprintV1SchemaAnchorTests"
set "ARTIFACT_DIR=%ROOT%\artifacts\m10974-fingerprint-v1-cross-host-determinism-hotfix1-rev2"
set "NRS_FINGERPRINT_V1_DIAGNOSTICS_DIR=%ROOT%\artifacts\ci\fingerprint-v1-hotfix1-rev2-local"

if exist "%ARTIFACT_DIR%" rmdir /s /q "%ARTIFACT_DIR%"
mkdir "%ARTIFACT_DIR%" || exit /b 1
if exist "%NRS_FINGERPRINT_V1_DIAGNOSTICS_DIR%" rmdir /s /q "%NRS_FINGERPRINT_V1_DIAGNOSTICS_DIR%"
mkdir "%NRS_FINGERPRINT_V1_DIAGNOSTICS_DIR%" || exit /b 1

echo ============================================================
echo M10.9.7.4 FINGERPRINT V1 CROSS-HOST DETERMINISM HOTFIX 1 REV2
echo ============================================================
echo Invariant live presentation plus frozen V1 compatibility preservation.
echo No V1/Exact-V9 golden change. No physics or VR2/R3 change.

echo [1/5] Static contract and returned-evidence audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10974-fingerprint-v1-cross-host-determinism-hotfix1.ps1" || goto :fail

echo [2/5] Restoring Application.Tests...
dotnet restore "%PROJECT%" || goto :fail

echo [3/5] Building Application.Tests Release...
dotnet build "%PROJECT%" --configuration Release --no-restore || goto :fail

echo [4/5] Running focused fingerprint schema/culture regression...
dotnet test --project "%PROJECT%" --configuration Release --no-build --minimum-expected-tests 2 -- --filter-class "%CLASS%" --parallel none --xunit-info || goto :fail

echo [5/5] Running full ordinary CI stable contract locally, including current Exact-V9 evidence audit...
call ".\eng\ci-ordinary.cmd" || goto :fail

if not exist "%NRS_FINGERPRINT_V1_DIAGNOSTICS_DIR%\fingerprint-v1-cross-host-diagnostic.zip" (
  echo ERROR: expected local fingerprint diagnostic ZIP was not produced.
  goto :fail
)
copy /y "%NRS_FINGERPRINT_V1_DIAGNOSTICS_DIR%\fingerprint-v1-cross-host-diagnostic.zip" "%ARTIFACT_DIR%\02-local-fingerprint-v1-cross-host-diagnostic.zip" >nul || goto :fail
if exist "%NRS_FINGERPRINT_V1_DIAGNOSTICS_DIR%\fingerprint-v1-cross-host-summary.txt" (
  copy /y "%NRS_FINGERPRINT_V1_DIAGNOSTICS_DIR%\fingerprint-v1-cross-host-summary.txt" "%ARTIFACT_DIR%\03-local-fingerprint-v1-cross-host-summary.txt" >nul || goto :fail
)

> "%ARTIFACT_DIR%\01-hotfix-summary.txt" echo status=PASS
>>"%ARTIFACT_DIR%\01-hotfix-summary.txt" echo revision=2
>>"%ARTIFACT_DIR%\01-hotfix-summary.txt" echo classification=PRESENTATION-CULTURE-DRIFT-REPAIRED-WITH-V1-COMPATIBILITY-PRESERVED
>>"%ARTIFACT_DIR%\01-hotfix-summary.txt" echo fingerprint-algorithm=sha256-control-room-snapshot-v1
>>"%ARTIFACT_DIR%\01-hotfix-summary.txt" echo frozen-v1-fingerprint=63643e5506a6b99f8106950ecb25a5243e9755b3bc96bf2a60e96c219216f362
>>"%ARTIFACT_DIR%\01-hotfix-summary.txt" echo pre-repair-hosted-payload-fingerprint=3e11375d2e0abce2ccbb4d35434f7e51724d5977341319f7dad188d7c9e593d4
>>"%ARTIFACT_DIR%\01-hotfix-summary.txt" echo exact-v9-transitive-anchor=7880AD580179B936C584EB0055BE663E0A1CFA65C5191B0DB8A7F3C514DB5418
>>"%ARTIFACT_DIR%\01-hotfix-summary.txt" echo focused-culture-regression=PASS
>>"%ARTIFACT_DIR%\01-hotfix-summary.txt" echo local-ordinary-ci=PASS
>>"%ARTIFACT_DIR%\01-hotfix-summary.txt" echo local-current-evidence-exact-v9=PASS
>>"%ARTIFACT_DIR%\01-hotfix-summary.txt" echo hosted-ordinary-ci=PENDING
>>"%ARTIFACT_DIR%\01-hotfix-summary.txt" echo r3=RED
>>"%ARTIFACT_DIR%\01-hotfix-summary.txt" echo vr2-r3-production-change=False

echo.
echo Fingerprint V1 Cross-Host Determinism Hotfix 1 REV2 completed.
echo Local qualification is GREEN, including the frozen Exact-V9 transitive anchor.
echo Return the complete folder:
echo %ARTIFACT_DIR%
echo Then push this exact REV2 candidate and require hosted ordinary-ci GREEN before closing the CI block.
exit /b 0

:fail
echo.
echo Fingerprint V1 Cross-Host Determinism Hotfix 1 REV2 FAILED.
exit /b 1
