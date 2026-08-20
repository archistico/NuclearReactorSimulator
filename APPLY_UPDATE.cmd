@echo off
setlocal EnableExtensions
set "ROOT=%~dp0"
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo Applying M10.9.5.4 - Observed Response Evidence...
echo Removing stale build outputs...
for /d /r %%D in (bin obj) do @if exist "%%D" rd /s /q "%%D"

echo.
echo M10.9.5.3 Hotfix 2 is the validated baseline.
echo M10.9.5.4 adds only presentation/application evidence after F4 COMMANDS dispatch:
echo   - authored monitor values at the dispatch boundary;
echo   - latest values through a bounded 500 logical-step window;
echo   - actual delta/direction when meaningful;
echo   - accepted/rejected feedback and observed protection state;
echo   - no causal claim, no generic SUCCESS/FAILURE and no fictional effects for rejected commands.
echo Observation samples are derivable JsonIgnored presentation evidence.
echo Physics, Simulation, protection ownership, dispatch semantics and exact-version identities are unchanged.
echo.
echo Run:
echo   dotnet build
echo   dotnet test
echo   scripts\run-m1095-command-observed-response-audit.cmd
echo.
echo If all gates are green, M10.9.5.4 is validated and M10.9.5.5 closure is next.
exit /b 0
