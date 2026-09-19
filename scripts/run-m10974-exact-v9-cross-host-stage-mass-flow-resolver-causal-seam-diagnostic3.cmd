@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"
set "METHOD=NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalExactV9ProductionActivationDecisionTests.AuthoritativeExactV9_DefaultAndMissionPathsRemainHealthyConservativeDeterministicAndFailClosed"
set "ARTIFACT_DIR=%ROOT%\artifacts\m10974-exact-v9-cross-host-stage-mass-flow-resolver-causal-seam-diagnostic3"
set "NRS_FINGERPRINT_V1_DIAGNOSTICS_DIR=%ARTIFACT_DIR%\capture-root"

if exist "%ARTIFACT_DIR%" rmdir /s /q "%ARTIFACT_DIR%"
mkdir "%ARTIFACT_DIR%" || exit /b 1
mkdir "%NRS_FINGERPRINT_V1_DIAGNOSTICS_DIR%" || exit /b 1

> "%ARTIFACT_DIR%\00-status.txt" echo status=STARTED
>>"%ARTIFACT_DIR%\00-status.txt" echo authority=DIAGNOSTIC-ONLY
>>"%ARTIFACT_DIR%\00-status.txt" echo diagnostic2-adjudication=PASS-AS-AUTHORED
>>"%ARTIFACT_DIR%\00-status.txt" echo diagnostic2-first-divergent-owner=COMMANDED-STAGE-MASS-FLOW
>>"%ARTIFACT_DIR%\00-status.txt" echo first-divergent-step=126
>>"%ARTIFACT_DIR%\00-status.txt" echo golden-change=False
>>"%ARTIFACT_DIR%\00-status.txt" echo production-change=False
>>"%ARTIFACT_DIR%\00-status.txt" echo physics-change=False
>>"%ARTIFACT_DIR%\00-status.txt" echo vr2-r3-change=False

echo ============================================================
echo M10.9.7.4 EXACT-V9 CROSS-HOST STAGE MASS-FLOW RESOLVER CAUSAL-SEAM DIAGNOSTIC 3
echo ============================================================
echo Test/evidence only. Same step-126 reference capture as Diagnostic 2; all resolver arithmetic post-loop.
echo Frozen V1 and Exact-V9 anchors remain unchanged.
echo No production, physics, tolerance or VR2/R3 change.

echo [1/4] Static frozen-boundary and Diagnostic 3 contract audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-m10974-exact-v9-cross-host-stage-mass-flow-resolver-causal-seam-diagnostic3.ps1" || goto :fail

echo [2/4] Restoring Application.Tests...
dotnet restore "%PROJECT%" || goto :fail

echo [3/4] Building Application.Tests Debug to match hosted current-evidence audit...
dotnet build "%PROJECT%" --configuration Debug --no-restore || goto :fail

echo [4/4] Running frozen Exact-V9 gate with post-loop stage mass-flow resolver extraction...
set "NRS_M10_FINAL_V9_ACTIVATION_PREREQUISITES_PASSED=1"
set "NRS_M10_FINAL_V9_ACTIVATION_DECISION=1"
dotnet test --project "%PROJECT%" --configuration Debug --no-build -- ^
  --explicit only ^
  --filter-method "%METHOD%" ^
  --parallel none
set "TEST_EXIT=%ERRORLEVEL%"
set "NRS_M10_FINAL_V9_ACTIVATION_DECISION="
set "NRS_M10_FINAL_V9_ACTIVATION_PREREQUISITES_PASSED="
if not "%TEST_EXIT%"=="0" goto :fail

set "TRACE_DIR=%NRS_FINGERPRINT_V1_DIAGNOSTICS_DIR%\exact-v9-transitive"
if not exist "%TRACE_DIR%\exact-v9-transitive-diagnostic.zip" (
  echo ERROR: exact-v9 transitive diagnostic ZIP missing.
  goto :fail
)
for %%F in (stage-resolver-selector.tsv stage-resolver-direct.tsv stage-resolver-valves-selector.tsv stage-resolver-valves-direct.tsv) do (
  if not exist "%TRACE_DIR%\capture\%%F" (
    echo ERROR: %%F missing.
    goto :fail
  )
)
copy /y "%TRACE_DIR%\exact-v9-transitive-diagnostic.zip" "%ARTIFACT_DIR%\01-local-exact-v9-stage-resolver-diagnostic.zip" >nul || goto :fail
copy /y "%TRACE_DIR%\exact-v9-transitive-summary.txt" "%ARTIFACT_DIR%\02-local-exact-v9-transitive-summary.txt" >nul || goto :fail
copy /y "%TRACE_DIR%\capture\stage-resolver-selector.tsv" "%ARTIFACT_DIR%\03-local-stage-resolver-selector.tsv" >nul || goto :fail
copy /y "%TRACE_DIR%\capture\stage-resolver-direct.tsv" "%ARTIFACT_DIR%\04-local-stage-resolver-direct.tsv" >nul || goto :fail
copy /y "%TRACE_DIR%\capture\stage-resolver-valves-selector.tsv" "%ARTIFACT_DIR%\05-local-stage-resolver-valves-selector.tsv" >nul || goto :fail
copy /y "%TRACE_DIR%\capture\stage-resolver-valves-direct.tsv" "%ARTIFACT_DIR%\06-local-stage-resolver-valves-direct.tsv" >nul || goto :fail

>>"%ARTIFACT_DIR%\00-status.txt" echo local-exact-v9-frozen-anchor=PASS
>>"%ARTIFACT_DIR%\00-status.txt" echo local-stage-resolver-trace-captured=True
>>"%ARTIFACT_DIR%\00-status.txt" echo hosted-stage-resolver-trace=PENDING

echo.
echo Exact-V9 Cross-Host Stage Mass-Flow Resolver Causal-Seam Diagnostic 3 completed locally.
echo Frozen local Exact-V9 anchor remains GREEN.
echo Return the complete folder:
echo %ARTIFACT_DIR%
echo Then push this exact Diagnostic 3 candidate unchanged and return hosted ordinary-ci-diagnostics.
exit /b 0

:fail
set "NRS_M10_FINAL_V9_ACTIVATION_DECISION="
set "NRS_M10_FINAL_V9_ACTIVATION_PREREQUISITES_PASSED="
echo.
echo Exact-V9 Cross-Host Stage Mass-Flow Resolver Causal-Seam Diagnostic 3 FAILED.
exit /b 1
