@echo off
setlocal EnableExtensions

cd /d "%~dp0"

if not exist "NuclearReactorSimulator.sln" (
    echo ERRORE: eseguire APPLY_UPDATE.cmd dalla radice del progetto.
    echo Il file NuclearReactorSimulator.sln non e' stato trovato.
    exit /b 1
)

echo Nuclear Reactor Simulator - pulizia checkpoint M10.9.4.1-D.4

echo.
echo Eliminazione degli ADR obsoleti o rinumerati...

call :DeleteIfPresent "docs\adr\0080-generation-ready-condenser-cooling-is-capacity-not-forced-inventory-depletion.md"
call :DeleteIfPresent "docs\adr\0101-governor-actuator-tracking-is-measured-before-anti-windup-retuning.md"
call :DeleteIfPresent "docs\adr\0102-reference-plant-scale-target-is-a-10-mwe-educational-unit.md"
call :DeleteIfPresent "docs\adr\0103-current-v2-reference-plant-is-10-mwe-with-bidirectional-grid-coupling.md"
call :DeleteIfPresent "docs\adr\0104-bidirectional-grid-motoring-uses-an-internal-signed-rotor-torque-seam.md"

echo.
echo Pulizia completata. Non sono necessarie rinominazioni.
exit /b 0

:DeleteIfPresent
if exist "%~1" (
    del /f /q "%~1"
    if errorlevel 1 (
        echo ERRORE durante l'eliminazione: %~1
        exit /b 1
    )
    echo Eliminato: %~1
) else (
    echo Gia' assente: %~1
)
exit /b 0
