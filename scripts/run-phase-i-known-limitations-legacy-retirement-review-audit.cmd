@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

set "NRS_I4_LIMITATIONS_RETIREMENT_AUDIT=1"

echo Running M10.9.4.1-I.4 known-limitations / legacy-retirement review...
echo.
echo I.3 must already be validated. This gate reviews frozen production-reference drift observations and H.5/H.21 source retention.
echo It does not change production policy, exact-version identities, runtime physics or numerical mathematics.
echo.

dotnet test --project "tests\NuclearReactorSimulator.Application.Tests\NuclearReactorSimulator.Application.Tests.csproj" --no-build -- ^
  --explicit only ^
  --filter-method "NuclearReactorSimulator.Application.Tests.Scenarios.Gameplay.PhaseIKnownLimitationsLegacyRetirementReviewAuditTests.CurrentLimitationsAndLegacyModes_ProduceConservativeRetirementDecision" ^
  --parallel none
if errorlevel 1 exit /b 1

set "SUMMARY=artifacts\i4-phase-i-known-limitations-legacy-retirement-review\01-phase-i-known-limitations-legacy-retirement-review.summary.txt"
if not exist "%SUMMARY%" (
  echo I.4 summary artifact was not produced.
  exit /b 1
)

type "%SUMMARY%"
echo.
echo M10.9.4.1-I.4 known-limitations / legacy-retirement review gate completed.
exit /b 0
