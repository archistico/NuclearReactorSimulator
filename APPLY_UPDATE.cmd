@echo off
setlocal EnableExtensions
set "ROOT=%~dp0"
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo Applying M10.9.4.1-I.5 REV1 Hotfix 17.1 - Final Repaired-v4 Phase-I Closure / Preflight Documentation Alignment...
echo Removing stale build and current final-closure generated outputs...
for /d /r %%D in (bin obj) do @if exist "%%D" rd /s /q "%%D"
for %%D in (
  "artifacts\i5-m10941-cumulative-closure"
  "artifacts\i5-repaired-v4-300s-reference-requalification"
  "artifacts\i5-repaired-exact-v4-production-activation"
  "artifacts\i5-synchronization-corrected-v3-activation"
  "artifacts\i2-phase-i-audit-consolidation-ci-baseline"
  "artifacts\i5-repaired-exact-v4-activation-readiness"
  "artifacts\i5-thermodynamic-repair-performance-cost-operational-soak-stage4"
  "artifacts\i5-thermodynamic-repair-requalification-stage3"
  "artifacts\i5-thermodynamic-repair-long-horizon-cross-profile-requalification-stage2"
  "artifacts\i5-thermodynamic-repair-requalification-stage1"
) do if exist "%%~D" rd /s /q "%%~D"

echo Local tests\NuclearReactorSimulator.Application.Tests\Scenarios\Gameplay\Evidence payloads are optional and are left untouched.
echo Candidate ZIPs do not bundle Gameplay\Evidence, artifacts, bin or obj.
echo Compact frozen prerequisites remain under eng\frozen-evidence\ordinary.

echo Removing superseded root-level M10.9.4.1 chronology already archived under docs\history...
if exist "docs\M10_9_4_1_*.md" del /q "docs\M10_9_4_1_*.md"

echo Consolidating duplicated current-state documentation into docs\PROJECT.md...
for %%F in (
  "docs\PROJECT_STATUS.md"
  "docs\PROJECT_HANDOFF.md"
  "docs\NEW_CHAT_START.md"
  "docs\current\I5_CUMULATIVE_M10941_CLOSURE.md"
  "docs\current\I5_VALIDATION_CHECKLIST.md"
  "docs\milestones\M10.9.4.1.md"
) do if exist "%%~F" del /q "%%~F"
if exist "docs\current" rd "docs\current" 2>nul

echo.
echo Hotfix 16.2 authoritative repaired exact-v4 production activation is locally validated.
echo Hotfix 17/17.1 is the final Phase-I closure candidate: no additional repair/requalification stage is planned.
echo Exact @4 is authoritative desktop production; exact @3 remains immutable historical replay and exact @2 remains fail-closed rollback/reference.
echo Synchronization pre-synchronization-grid-loading@3 remains independently validated and unchanged.
echo Historical I.3 exact-v3 evidence and its 19 frozen budgets remain immutable; the final @4 300 s gate reuses those budgets without retuning.
echo.
echo Run the short preflight:
echo   dotnet build
echo   dotnet test
echo If both are green, run exactly one final long command:
echo   scripts\run-m10941-cumulative-closure-audit.cmd
echo.
echo Do not rerun historical H.30/I.3/I.4 gates separately; they are HISTORICAL-FROZEN provenance.
echo If the cumulative chain is green, M10.9.4.1 / Phase I is closed and M10.9.5 is unblocked.
exit /b 0
