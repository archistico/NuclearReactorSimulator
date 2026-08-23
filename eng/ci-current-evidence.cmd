@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo [CI CURRENT EVIDENCE] Frozen Phase-I I.2 audit/CI baseline contract...
call scripts\run-phase-i-audit-consolidation-ci-baseline-audit.cmd || exit /b 1

echo [CI CURRENT EVIDENCE] I.5 synchronization exact-v3 activation contract...
call scripts\run-i5-synchronization-corrected-v3-activation-audit.cmd || exit /b 1

echo [CI CURRENT EVIDENCE] M10 Final exact-v9 authoritative desktop production and mission-v3 activation...
call scripts\run-m10-final-v9-authoritative-production-audit.cmd || exit /b 1

echo [CI CURRENT EVIDENCE] Historical H.30 RQ1, I.3 exact-v3 reference, I.4 review and I.5 exact-v4 production activation remain frozen provenance.
echo [CI CURRENT EVIDENCE] PASSED.
exit /b 0
