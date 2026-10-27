@echo off
setlocal EnableExtensions
chcp 65001 >nul
title Crisis VR - Tester Automatico
rem ============================================================
rem  INICIAR_TESTER.cmd - Lanzador del tester de Crisis VR
rem  Uso: doble clic. Opciones opcionales (via Coordinador.ps1):
rem    -SoloComprobaciones   no invoca Unity
rem    -TestsEditMode        + pruebas Unity Test Framework EditMode
rem    -TestsPlayMode        + pruebas Unity Test Framework PlayMode
rem    -ModoQuest            prepara/recoge pruebas en Quest 3
rem    -ConOpenCode          permite correccion asistida (3 intentos max)
rem ============================================================

set "PROYECTO=%~dp0"
rem Quitar barra final
if "%PROYECTO:~-1%"=="\" set "PROYECTO=%PROYECTO:~0,-1%"

echo.
echo ============================================================
echo  CRISIS VR - TESTER AUTOMATICO
echo  Proyecto: %PROYECTO%
echo ============================================================
echo.

rem [2] Carpeta del proyecto
if not exist "%PROYECTO%\ProjectSettings\ProjectVersion.txt" (
    echo [ERROR] No parece una carpeta de proyecto Unity: "%PROYECTO%"
    echo         Falta ProjectSettings\ProjectVersion.txt
    echo         Coloca este .cmd en la raiz del proyecto Crisis VR.
    echo.
    pause
    exit /b 3
)

rem [3] PowerShell obligatorio
where powershell >nul 2>&1
if errorlevel 1 (
    echo [ERROR] No se encontro PowerShell. Es obligatorio.
    echo         Windows normalmente lo incluye en C:\Windows\System32\WindowsPowerShell
    echo.
    pause
    exit /b 3
)

rem [4] Evitar dos campanas simultaneas: lock en TestResults
if not exist "%PROYECTO%\TestResults" mkdir "%PROYECTO%\TestResults"
set "LOCK=%PROYECTO%\TestResults\.tester.lock"
if exist "%LOCK%" (
    echo [AVISO] Ya existe una campana previa (%LOCK%).
    powershell -NoProfile -Command "$f=Get-Item -LiteralPath '%LOCK%' -ErrorAction SilentlyContinue; if($f -and ((Get-Date)-$f.LastWriteTime).TotalMinutes -lt 60){ exit 1 } else { exit 0 }"
    if errorlevel 1 (
        echo          Ultima campana con menos de 60 minutos. Para forzar:
        echo          borra el archivo .tester.lock y vuelve a ejecutar.
        echo.
        pause
        exit /b 3
    )
    del /q "%LOCK%" >nul 2>&1
)
> "%LOCK%" echo Inicio %date% %time% - PID %RANDOM%

rem [5-7] Carpeta de resultados con fecha/hora + invocar coordinador
for /f %%I in ('powershell -NoProfile -Command "Get-Date -Format yyyy-MM-dd_HH-MM-ss"') do set "RUN=%%I"
set "RUNDIR=%PROYECTO%\TestResults\%RUN%"
echo [OK] Carpeta de esta ejecucion: %RUNDIR%
echo.

echo [EJECUTAR] powershell -NoProfile -ExecutionPolicy Bypass -File "%PROYECTO%\Tester\Coordinador.ps1" -Proyecto "%PROYECTO%" -RunDir "%RUNDIR%" %*
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%PROYECTO%\Tester\Coordinador.ps1" -Proyecto "%PROYECTO%" -RunDir "%RUNDIR%" %*
set "RC=%ERRORLEVEL%"

del /q "%LOCK%" >nul 2>&1

echo.
echo ============================================================
if "%RC%"=="0" (echo  RESUMEN: campana terminada sin fallos)
if "%RC%"=="1" (echo  RESUMEN: campana terminada CON FALLOS de pruebas)
if "%RC%"=="2" (echo  RESUMEN: campana BLOQUEADA por dependencias del entorno)
if "%RC%"=="3" (echo  RESUMEN: error del propio tester / entorno)
if not "%RC%"=="0" if not "%RC%"=="1" if not "%RC%"=="2" if not "%RC%"=="3" echo  RESUMEN: codigo inesperado %RC% (revisa los logs)
echo  Informes en: %RUNDIR%\Reports
echo ============================================================
pause
endlocal & exit /b %RC%
