@echo off
setlocal EnableExtensions

cd /d "%~dp0"

if not exist "NuclearReactorSimulator.sln" (
    echo ERRORE: eseguire APPLY_UPDATE.cmd dalla radice del progetto.
    echo Il file NuclearReactorSimulator.sln non e' stato trovato.
    exit /b 1
)

echo Nuclear Reactor Simulator - applicazione progetto completo M10.9.4.1-E.3.2 HOTFIX 2 CANDIDATE

echo.
echo Pulizia compatibile per eventuale estrazione sopra checkpoint precedenti...

call :DeleteIfPresent "docs\adr\0080-generation-ready-condenser-cooling-is-capacity-not-forced-inventory-depletion.md"
if errorlevel 1 exit /b 1
call :DeleteIfPresent "docs\adr\0101-governor-actuator-tracking-is-measured-before-anti-windup-retuning.md"
if errorlevel 1 exit /b 1
call :DeleteIfPresent "docs\adr\0102-reference-plant-scale-target-is-a-10-mwe-educational-unit.md"
if errorlevel 1 exit /b 1
call :DeleteIfPresent "docs\adr\0103-current-v2-reference-plant-is-10-mwe-with-bidirectional-grid-coupling.md"
if errorlevel 1 exit /b 1
call :DeleteIfPresent "docs\adr\0104-bidirectional-grid-motoring-uses-an-internal-signed-rotor-torque-seam.md"
if errorlevel 1 exit /b 1

echo.
echo Applicazione completata. E.3.2 Hotfix 2 non richiede nuove cancellazioni o rinominazioni; la pulizia sopra serve solo per vecchi checkpoint documentali.
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
