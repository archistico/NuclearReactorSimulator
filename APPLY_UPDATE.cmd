@echo off
setlocal EnableExtensions
set "ROOT=%~dp0"
cd /d "%ROOT%"
if errorlevel 1 exit /b 1

echo Applying M10.9.8.1 REV1 - Integrated Human / Automation / HMI Validation Matrix Freeze...
echo Removing stale build and M10.9.8.1 audit outputs...
for /d /r %%D in (bin obj) do @if exist "%%D" rd /s /q "%%D"
if exist "artifacts\m1098-integrated-validation-matrix" rd /s /q "artifacts\m1098-integrated-validation-matrix"

echo.
echo Baseline: M10.9.7.5 Hotfix 1 VALIDATED; M10.9.7 CLOSED.
echo M10.9.8.1 REV1 freezes validation contracts only: 19 matrix rows, 11 cross-cutting invariants and owner routing.
echo All src/tests files remain byte-identical to M10.9.7.5 Hotfix 1 VALIDATED; no compiled/runtime/test-surface change.
echo.
echo Run:
echo   dotnet build
echo   dotnet test
echo   scripts\run-m10981-integrated-validation-matrix-audit.cmd
echo Then review:
echo   docs\M10_9_8_1_MATRIX_ACCEPTANCE_CHECKLIST.md
exit /b 0
