@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1
if not exist "NuclearReactorSimulator.sln" exit /b 1

set "PROJECT=tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj"

echo Running M10.9.6.3 multidimensional challenge scoring contract audit...
echo.
echo This gate freezes deterministic observational score dimensions, standard weights, grade thresholds and dominance caps.
echo Safety/procedure dominate; unavailable evidence cannot silently pass; challenge scoring owns no plant command authority.
echo.

dotnet test --project "%PROJECT%" --no-build -- ^
  --filter-class "NuclearReactorSimulator.Application.Tests.Scenarios.Challenges.Scoring.M10963ChallengeScoringContractTests" ^
  --parallel none
if errorlevel 1 exit /b 1

echo.
echo === M10.9.6.3 artifact summary ===
if exist "artifacts\m1096-multidimensional-scoring\01-m1096-multidimensional-scoring-contract.summary.txt" (
  type "artifacts\m1096-multidimensional-scoring\01-m1096-multidimensional-scoring-contract.summary.txt"
) else (
  echo ERROR: expected M10.9.6.3 summary artifact was not written.
  exit /b 1
)

echo.
echo M10.9.6.3 multidimensional challenge scoring contract audit completed.
exit /b 0
