@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo [CI CURRENT EVIDENCE] H.30 Requalification 1 production policy...
call scripts\run-h30-rq1-production-policy-rereview-audit.cmd || exit /b 1

echo [CI CURRENT EVIDENCE] I.2 audit/CI contract...
call scripts\run-phase-i-audit-consolidation-ci-baseline-audit.cmd || exit /b 1

echo [CI CURRENT EVIDENCE] PASSED.
exit /b 0
