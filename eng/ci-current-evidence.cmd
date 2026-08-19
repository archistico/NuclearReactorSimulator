@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo [CI CURRENT EVIDENCE] H.30 closure...
call scripts\run-phase-h-closure-production-qualification-decision-audit.cmd || exit /b 1

echo [CI CURRENT EVIDENCE] I.1 compatibility baseline...
call scripts\run-profile-compatibility-legacy-retirement-inventory-audit.cmd || exit /b 1

echo [CI CURRENT EVIDENCE] I.2 audit/CI baseline...
call scripts\run-phase-i-audit-consolidation-ci-baseline-audit.cmd || exit /b 1

echo [CI CURRENT EVIDENCE] PASSED.
exit /b 0
