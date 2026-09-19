@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "REPORT_DIR=%CD%\artifacts\m10974-exact-v9-cross-host-determinism-contract-v2"

if exist "%REPORT_DIR%" rmdir /s /q "%REPORT_DIR%"
mkdir "%REPORT_DIR%" || exit /b 1

> "%REPORT_DIR%\00-status.txt" echo status=STARTED
>> "%REPORT_DIR%\00-status.txt" echo scope=cross-host-determinism-contract-v2
>> "%REPORT_DIR%\00-status.txt" echo physics-change=False
>> "%REPORT_DIR%\00-status.txt" echo tolerance-change=False
>> "%REPORT_DIR%\00-status.txt" echo vr2-r3-change=False

echo ============================================================
echo M10.9.7.4 EXACT-V9 CROSS-HOST DETERMINISM CONTRACT V2
echo ============================================================
echo V1 raw remains exact same-host evidence.
echo V2 presentation-canonical is the frozen cross-host contract.
echo No physics, tolerance or VR2/R3 change.
echo.
set "OBSOLETE_RUNTIME_ALIGNMENT_WORKFLOW=.github\workflows\exact-v9-runtime-alignment-diagnostic.yml"
if exist "%OBSOLETE_RUNTIME_ALIGNMENT_WORKFLOW%" (
    echo [0/5] Removing superseded one-shot runtime-alignment workflow...
    del /f /q "%OBSOLETE_RUNTIME_ALIGNMENT_WORKFLOW%"
    if exist "%OBSOLETE_RUNTIME_ALIGNMENT_WORKFLOW%" goto :fail
)
>> "%REPORT_DIR%\00-status.txt" echo superseded-runtime-alignment-workflow-absent=PASS

echo [1/5] Static contract and frozen-boundary validation...
powershell -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10974-exact-v9-cross-host-determinism-contract-v2.ps1"
if errorlevel 1 goto :fail
>> "%REPORT_DIR%\00-status.txt" echo static-validator=PASS

echo.
echo [2/5] Restore and Debug build warnings-as-errors...
dotnet restore
if errorlevel 1 goto :fail
dotnet build --configuration Debug --no-restore -warnaserror
if errorlevel 1 goto :fail
>> "%REPORT_DIR%\00-status.txt" echo build=PASS

echo.
echo [3/5] Frozen V1 schema/culture focused regression...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- ^
  --filter-method "NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance.M10974FingerprintV1SchemaAnchorTests.*" ^
  --parallel none
if errorlevel 1 goto :fail
>> "%REPORT_DIR%\00-status.txt" echo fingerprint-v1-focused=PASS

echo.
echo [4/5] Cross-host V2 canonicalization focused regression...
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- ^
  --filter-method "NuclearReactorSimulator.Application.Tests.ControlRoom.MissionPerformance.M10974FingerprintV2CrossHostContractTests.*" ^
  --parallel none
if errorlevel 1 goto :fail
>> "%REPORT_DIR%\00-status.txt" echo fingerprint-v2-focused=PASS

echo.
echo [5/5] Exact-V9 authoritative gate under V2 cross-host contract...
set "NRS_M10_FINAL_V9_ACTIVATION_PREREQUISITES_PASSED=1"
set "NRS_M10_FINAL_V9_ACTIVATION_DECISION=1"
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- ^
  --explicit only ^
  --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalExactV9ProductionActivationDecisionTests.AuthoritativeExactV9_DefaultAndMissionPathsRemainHealthyConservativeDeterministicAndFailClosed" ^
  --parallel none
set "TEST_EXIT=%ERRORLEVEL%"
set "NRS_M10_FINAL_V9_ACTIVATION_DECISION="
set "NRS_M10_FINAL_V9_ACTIVATION_PREREQUISITES_PASSED="
if not "%TEST_EXIT%"=="0" goto :fail
>> "%REPORT_DIR%\00-status.txt" echo exact-v9-authoritative-v2=PASS

copy /y "eng\m10974-exact-v9-cross-host-determinism-v2-contract.json" "%REPORT_DIR%\01-contract.json" >nul
copy /y "docs\M10974_EXACT_V9_CROSS_HOST_DETERMINISM_CONTRACT_V2.md" "%REPORT_DIR%\02-decision.md" >nul
>> "%REPORT_DIR%\00-status.txt" echo status=PASS
>> "%REPORT_DIR%\00-status.txt" echo frozen-cross-host-v2=99B9D27A8F5791A194771D698E8A0740F7024058C172D645E2DF74F1B3C09E73
>> "%REPORT_DIR%\00-status.txt" echo historical-raw-v1=7880AD580179B936C584EB0055BE663E0A1CFA65C5191B0DB8A7F3C514DB5418

echo.
echo Exact-V9 Cross-Host Determinism Contract V2 completed locally.
echo Push this exact candidate unchanged and require hosted ordinary-ci GREEN.
exit /b 0

:fail
set "NRS_M10_FINAL_V9_ACTIVATION_DECISION="
set "NRS_M10_FINAL_V9_ACTIVATION_PREREQUISITES_PASSED="
>> "%REPORT_DIR%\00-status.txt" echo status=FAIL
echo.
echo Exact-V9 Cross-Host Determinism Contract V2 FAILED.
exit /b 1
