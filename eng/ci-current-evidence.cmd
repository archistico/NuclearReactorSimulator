@echo off
setlocal EnableExtensions
set "ROOT=%~dp0.."
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo [CI CURRENT EVIDENCE] I.2 audit/CI contract aligned to repaired exact-v4 production...
call scripts\run-phase-i-audit-consolidation-ci-baseline-audit.cmd || exit /b 1

echo [CI CURRENT EVIDENCE] I.5 synchronization exact-v3 activation contract...
call scripts\run-i5-synchronization-corrected-v3-activation-audit.cmd || exit /b 1

echo [CI CURRENT EVIDENCE] I.5 repaired exact-v4 authoritative desktop activation...
call scripts\run-i5-repaired-exact-v4-production-activation-audit.cmd || exit /b 1

echo [CI CURRENT EVIDENCE] Historical H.30 RQ1, I.3 exact-v3 reference and I.4 review remain frozen provenance and are not dynamically rerun against exact @4.
echo [CI CURRENT EVIDENCE] PASSED.
exit /b 0
