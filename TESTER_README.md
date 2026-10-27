# TESTER DE CRISIS VR - Manual de Usuario

## 1. Arquitectura general

El sistema se lanza desde la raiz del proyecto (`C:\Users\USER\CrisisVR`) mediante doble clic en `INICIAR_TESTER.cmd`.

```
C:\Users\USER\CrisisVR\
  INICIAR_TESTER.cmd          (lanzador principal)
  Tester/
    Coordinador.ps1           (motor de la campana)
    Modulos/
      Entorno.ps1             (detecci\\'on de herramientas)
      Integridad.ps1          (comprobaciones iniciales del proyecto)
      UnityBatch.ps1          (compilaci\\'on + pruebas Unity Test Framework)
      ModoQuest.ps1           (modo Meta Quest 3)
      OpenCodeFlujo.ps1       (integrac\\'on OpenCode - m\\'ax 3 intentos)
      Informes.ps1            (generaci\\'on de informes MD + CSV)
    TESTER_README.md          (estas instrucciones)
  TestResults/                (carpetas generadas por cada ejecuci\\'on)
    AAAA-MM-DD_HH-MM-SS/
      Logs/
      Reports/
      Screenshots/
      Videos/
      Evidence/
```

## 2. Prerrequisitos (instalados y comprobados)

| Herramienta | Ruta / Comprobaci\\'on |
|---|---|
| Unity 6000.3.9f1 | `C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Unity.exe` (verificada) |
| Git | `C:\Program Files\Git\cmd\git.exe` (instalado; proyecto **no es repo Git** — hay .plastic) |
| Python 3.14 | `C:\Users\USER\AppData\Local\Programs\Python\Python314\python.exe` |
| dotnet | `C:\Program Files\dotnet\dotnet.exe` |
| ffmpeg | **No instalado** — video PENDIENTE (ver s\\'ecion 6) |
| adb | `C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe` |
| PowerShell 5.1 | Siempre disponible en Windows |
| OpenCode CLI v2.0.19 | `cmd /c opencode ...` (PowerShell lo bloquea por ExecutionPolicy) |

## 3. Modos de ejecuci\\'on (desde INICIAR_TESTER.cmd)

Al dar doble clic, el CMD muestra un men\\' principal:

1. **Comprobaciones** (por defecto si no se pasan banderas):  
   Detecta herramientas, estructura del proyecto, estado de Unity, errores de compilaci\\'on en logs recientes. **No lanza Unity**. Salida: `exit 0` si todo presente, `1` si faltan herramientas.

2. **PC Completo** (`-TestsEditMode` + `-TestsPlayMode`):  
   - Primero verifica que Unity Editor **no** tenga el proyecto abierto (si está, se bloquea con mensaje claro).  
   - Compilaci\\'on batch (`Unity.exe -batchmode -quit -nographics ...`): `exit code 0 = compilaci\\'on exitosa`.  
   - Pruebas Unity Test Framework EditMode (`-testPlatform EditMode`): comprobaci\\'on de escenas, tipos y archivos. Resultados en XML en `TestResults\<run>\Reports\`.  
   - Pruebas PlayMode (`-testPlatform PlayMode`): carga escenas y verifica que no haya excepciones fatales. Resultados en XML.  
   - Salida: `exit 1` si alguna prueba falla, `exit 2` si est\\'a bloqueado, `exit 0` si todo aprobado.

3. **Quest 3** (`-ModoQuest`):  
   Detecta `adb.exe`, si hay dispositivo conectado: intenta instalar el APK (`Builds\CrisisVR.apk`), lanza la app y recoge `logcat -d`.  
   Si no hay dispositivo: `BLOQUEADO` con instrucciones para conectar Quest 3.  
   Si no hay APK compilado: `BLOQUEADO` con indicaci\\'on de compilar primero.

4. **OpenCode asistida** (`-ConOpenCode`):  
   S\\'olo si existen fallos de prueba. Llama a `opencode run --auto` m\\'ax 3 intentos por incidencia, captura las sugerencias y deja un reporte en `TestResults\<run>\Evidence\` para revisi\\'on manual. **No se ejecuta por omisi\\'on** (requiere bandera explicita por tema de coste de tokens DEC-007).

5. **Salir**.

### Ejemplos de l\\'inea de comandos (desde la consola)

```cmd
INICIAR_TESTER.cmd              # Men\\' principal (opci\\'on 1 por defecto)
INICIAR_TESTER.cmd -SoloComprobaciones
INICIAR_TESTER.cmd -TestsEditMode -TestsPlayMode
INICIAR_TESTER.cmd -ModoQuest
INICIAR_TESTER.cmd -ConOpenCode
```

## 4. Qu\\'e se genera por cada ejecuci\\'on

Cada vez que se ejecuta, se crea una carpeta con marca de tiempo:

```
TestResults\AAAA-MM-DD_HH-MM-SS\

Carpetas internas:
- Logs/          -> Logs generados por el propio testerner + Editor.log si est\\' abierto
- Reports/       -> Informe.md (Markdown) + Resumen.csv (estructura simple)
- Screenshots/   -> Capturas de pantalla (modo PC interactivo; ver s\\'ec. 5)
- Videos/        -> Vide\\'os de ejecuci\\'on (marcado PENDIENTE si falta ffmpeg)
- Evidence/      -> Evidencias diversas (logs de prueba, propuestas de OpenCode, logcat)
```

## 5. Modo PC interactivo con XR Simulator

Para pruebas interactivas en PC sin visor:

- El CMD ofrece la opci\\'on 2 (PC Completo) o se puede lanzar directamente con `-ModoPC` (si se agrega esa bandera al men\\'; actualmente el men\\' tiene 1-5 fijas, pero se puede editar INICIAR_TESTER.cmd).
- El motor inicia el Editor Unity (`Unity.exe -projectPath <proyecto>`) para que la usuaria use el XR Simulator con rat\\'on/teclado (consultar controles en la secci\\'on 7 del README).
- El script captura pantalla cada 8 segundos usando `System.Drawing` (PowerShell) y la guarda en `TestResults\<run>\Screenshots\`.
- El bucle dura hasta que la usuaria pulse **Enter** en la consola (se indica al inicio).
- Despu\\'es, se copia el `Editor.log` a `TestResults\<run>\Logs\`.

**Control manual:** la usuaria debe estar atenta al Editor Unity y usar el XR Simulator como en condiciones normales. El tester no autom\\'atica las entradas (sin herramienta de inyecci\\'on de eventos fiable), pero provee evidencia visual.

**Video:** Grabaci\\' de video requiere instalar `ffmpeg` (ver s\\'ec. 6). Sin ella, la secci\\'on Videos permanecer\\'a como `PENDIENTE`.

## 6. Falta ffmpeg (video)

El generador de informes marca `ffmpeg` como **No instalado** y la secci\\'on de vídeos como `PENDIENTE`.

Para habilitar capturas de video:

1. Instalar ffmpeg (recomendado: `winget install ffmpeg` o descargar desde ffmpeg.Org).
2. Tras instalar, volver a ejecutar el tester; el script detectar\\'a `ffmpeg` en PATH y generará vídeos MP4 de la ejecuci\\'on interactiva (basado en los frames capturados por pantalla).

Si no se instala, la funcionalidad de video permanece documentada como PENDIENTE; no se generan vídeos falsos.

## 7. Controles del XR Simulator (rat\\'on + teclado)

Al ejecutar el modo PC, los controles son los definidos en el proyecto XRI (XR Interaction Toolkit 3.5.1) y le\\'idos desde `XR Interaction Simulator Controls.inputactions`:

| Tecla / Mouse | Acci\\'on |
|---|---|
| Bot\\'on derecho del rat\\'on (mantener) | Mirar alrededor |
| Flechas \\u2191\\u2193\\u2190\\u2192 | Girar la vista |
| W | Mover hacia delante |
| S | Mover hacia atr\\'as |
| A | Mover a la izquierda |
| D | Mover a la derecha |
| Q | Subir |
| E | Bajar |
| Rueda del rat\\'on | Acercar / alejar |
| **Espacio** | Disparar acci\\'on r\\'apida (=扳机/trigger: apuntar con el rayo a un objeto y seleccionar/agarrar) |
| ` (backquote) | Cambiar qu\\'e acci\\'on hace Espacio (trigger, grip, bot\\'on...) |
| **Tab** | Alternar control izquierdo/derecho del "mando" |
| [ / ] | Manipular mando izq/der |
| H | Manipular la cabeza |
| R | Resetear posici\\'on |
| X / Y | Men\\'us de UI del simulador |

Flujo habitual: Play → bot\\'on derecho para orientarte → apuntar el rayo a un objeto → Espacio (agarrar) → mover con rat\\'on/teclado → Espacio (soltar).

## 8. Codigos de salida del lanzador

| Código | Significado |
|---|---|
| `0` | Campana terminada sin fallos (todas las fases marcadas OK o PENDIENTE por diseño) |
| `1` | Alguna prueba Unity Test Framework falló (ver informe) |
| `2` | Campana bloqueada por entorno (Unity abierto, herramientas faltantes, sin dispositivo Quest) |
| `3` | Error interno del propio tester (excepción no controlada) |

## 9. Qu\\'e hacer con los resultados

1. Revisar `TestResults\<run>\Reports\Informe.md` para el resumen ejecutable.
2. Consultar `Resumen.csv` para datos estructurados (por ejemplo, importar a Excel).
3. Si hay fallos `FALLIDO`, usar la secci\\'on `Problemas que requieren intervención humana` del informe como gu\\'ia.
4. Si se quiere corregir un error con OpenCode, lanzar con `-ConOpenCode` y revisar `Evidence\opencode_propuesta_*.txt`.
5. Borrar manualmente la carpeta `TestResults\<run>` si se quiere repetir la campana (no se borra automáticamente).

## 10. Limitaciones y decisiones dise\\'oinales

- **No se ejecuta Unity ni se compila mientras el Editor esté abierto** — el tester lo detecta y bloquea esas fases para evitar errores de lock.
- **Los tests Unity Test Framework requieren que el proyecto se compile antes**; si el proyecto tiene errores de compilaci\\'on previos, los tests se saltan con PENDIENTE y se registran en el informe.
- **OpenCode integration es opt-in** (bandera `-ConOpenCode`) para controlar el coste de tokens (DEC-007: s\\'olo modelos gratuitos).
- **No se instalan aplicaciones en Quest 3 sin dispositivo conectado**; el tester solo detecta e informa.
- **No se borran carpetas de ejecuciones previas**; cada campana tiene timestamp propio. Si se quiere repetir, borrar la carpeta manualmente.
- **El proyecto no es un repositorio Git** (`git` instalado pero sin `.git`; existe `.plastic` para Plastic SCM). Las protecciones de lock por git no se aplican.
- **ffmpeg no viene instalado**; la generaci\\'on de vídeo es PENDIENTE hasta que se instale.
- Los scripts usan rutas absolutas verificadas; no se asume que las herramientas est\\'an en rutas m\\'agicas.

## 11. Pruebas de aceptaci\\'on del propio CMD (run por el agente)

Para verificar que el lanzador funciona sin necesidad de ejecutar Unity:

1. `cmd /c "echo.| INICIAR_TESTER.cmd -SoloComprobaciones"`  
   - Debe crear `TestResults\AAAA-MM-DD_HH-MM-SS\` con carpetas `Logs, Reports, Screenshots, Videos, Evidence`.
   - Debe generar `Reports\Informe.md` y `Resumen.csv` con cabecera.
   - Debe mostrar al final c\\'odigo de salida `0` (o `2` si falta alguna herramienta crítica detectada, que est\\'a bien).
   - Debe dejar un archivo `Logs\coordinador.log` con el inicio y detecci\\'on de herramientas.

2. `cmd /c "echo.| INICIAR_TESTER.cmd -TestsEditMode -TestsPlayMode"` (con Unity Editor **cerrado**):  
   - Debe detectar Unity abierto y bloquearse (salir `2`) si el editor está corriendo (como ocurre ahora con 8 procesos).  
   - Si Unity est\\'a cerrado, intentará compilar y correr tests; como no hay assemblies de tests creados con antelaci\\'on bajo Assets/_Project/Tests, marcar\\'a PENDIENTE los tests y `exit 0` por diseño (no hay fallos reales, solo ausencia de pruebas).  
   - En cualquier caso, debe dejar logs coherentes y no cerrarse silenciosamente.

3. `cmd /c "echo.| INICIAR_TESTER.cmd -ModoQuest"` (sin Quest conectado):  
   - Debe marcar Q-02 BLOQUEADO "Sin dispositivo en modo depuracion" y salir con `exit 2`.

4. Verificar que `INICIAR_TESTER.cmd` existe en la ra\\'iz y que `powershell -NoProfile -ExecutionPolicy Bypass -File Tester\Coordinador.ps1` se invoca correctamente.

## 12. Contacte y mantenimiento

- Para any±ar o modificar alguna herramienta, editar los archivos `.ps1` correspondientes en `Tester\Modulos\`.
- Si se instala ffmpeg, el PATH de Windows se actualizará automáticamente y el tester lo detectará en la siguiente ejecuci\\'on.
- Si se cambia la versi\\'on de Unity, actualizar `ProjectSettings\ProjectVersion.txt` y verificar la ruta en `Entorno.ps1`.
- Mantener `TestResults\` fuera del control de versiones (ya est\\'a en .gitignore; si no existe, agregarla manualmente: `echo TestResults/ >> .gitignore`).

---
**Fin del manual.** Para dudas o correcciones, consultar la secci\\'on de dudas en la respuesta final o contactar con la desarrolladora.