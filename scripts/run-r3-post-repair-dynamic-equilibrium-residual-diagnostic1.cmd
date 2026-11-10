@echo off
setlocal EnableExtensions
cd /d "%~dp0.."
set "OUT=%CD%\artifacts\r3-dyn-eq-d1"
if not exist "%OUT%" mkdir "%OUT%"
rem Remove only this diagnostic's explicitly owned output files; keep unrelated evidence.
for %%F in (00-static-audit.txt 00-build.txt 00-test.txt 01-node-residuals.csv 02-hydraulics-drum.csv 03-controller-rotor.csv 04-conservation.csv 05-health.csv 06-diagnostic-summary.json 07-manifest.csv) do if exist "%OUT%\%%F" del /q "%OUT%\%%F"
echo [1/4] Frozen production, prior tests and prerequisite audit...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-r3-post-repair-dynamic-equilibrium-residual-diagnostic1.ps1" > "%OUT%\00-static-audit.txt" 2>&1
if errorlevel 1 goto :fail
echo [2/4] Fresh focused Release build with warnings as errors...
dotnet --info > "%OUT%\00-build.txt" 2>&1
if errorlevel 1 goto :fail
dotnet build ".\tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --configuration Release --no-incremental --warnaserror >> "%OUT%\00-build.txt" 2>&1
if errorlevel 1 goto :fail
echo [3/4] Capture repaired mode 2 and canonical mode 1, 100 steps each...
set "NRS_R3_POST_REPAIR_DYNAMIC_EQUILIBRIUM_DIAGNOSTIC1=1"
dotnet test --project ".\tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --configuration Release --no-build --minimum-expected-tests 1 -- --explicit only --filter-class "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.M10FinalVr2R3PostRepairDynamicEquilibriumResidualDiagnostic1Tests" --parallel none > "%OUT%\00-test.txt" 2>&1
if errorlevel 1 goto :fail
set "NRS_R3_POST_REPAIR_DYNAMIC_EQUILIBRIUM_DIAGNOSTIC1="
echo [4/4] Validate complete evidence and emit summary / SHA-256 manifest...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\eng\validate-r3-post-repair-dynamic-equilibrium-residual-diagnostic1.ps1" -Adjudicate
if errorlevel 1 goto :fail
echo Diagnostic complete. Evidence: %OUT%
echo R3 remains RED. Next: evidence review and equilibrium reconstruction planning only.
exit /b 0
:fail
set "NRS_R3_POST_REPAIR_DYNAMIC_EQUILIBRIUM_DIAGNOSTIC1="
echo Diagnostic FAILED. Inspect %OUT%\00-*.txt. Do not retune or start Requalification 3.
exit /b 1
