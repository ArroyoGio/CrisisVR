# QA_PROTOCOL.md — Protocolo de pruebas de Crisis VR

Lo siguen el agente `qa-tester` (OpenCode) y Codex cuando se le pida "haz QA de T-xx".
Es independiente de la herramienta: si no usas el agente, léelo y síguelo igual.

## 0. Qué es y qué NO es este QA
- Es: compilación, tests automáticos, verificación de alcance, pruebas del flujo completo en el Editor, capturas, video, reportes y bugs con evidencia.
- NO es: prueba en el Meta Quest 3 (confort, tracking, rendimiento real, audio espacial). Eso lo hace la desarrolladora con `QUEST_CHECKLIST.md`.
- El rendimiento medido en el Editor de PC NO representa el Quest. Nunca lo presentes como tal.

## 1. Estructura de evidencia
```
docs/evidence/T-xx/YYYYMMDD-HHMM/
  QA_REPORT.md          <- reporte final (plantilla en §9)
  build.log             <- compilación
  tests_editmode.xml    <- resultados EditMode
  tests_playmode.xml    <- resultados PlayMode
  scope_check.txt       <- resultado de la verificación de alcance
  screenshots/          <- PNG con nombre: <acto>_<paso>_<descripcion>.png
  videos/               <- MP4 por recorrido: <recorrido>.mp4
  reports_generated/    <- copias de los .txt de reporte generados en las pruebas
```
Todo archivo de evidencia lleva fecha y el hash corto del commit probado (`git rev-parse --short HEAD`) dentro de `QA_REPORT.md`.

## 2. Flujo de trabajo (siempre en este orden)

### Paso 1 — Preparar
1. Confirma el commit y que `git status` esté limpio (si no, avisa y para).
2. Confirma que existe `UNITY_PATH`. Si no, haz UNA pregunta y para.
3. Lee la tarea T-xx y la sección del GDD correspondiente para saber QUÉ debe cumplir.
4. Antes de probar, escribe en `QA_REPORT.md` la lista de criterios de aceptación de la tarea. Cada prueba debe mapear a un criterio.

### Paso 2 — Compilación
```
"$UNITY_PATH" -batchmode -nographics -quit -projectPath . -logFile docs/evidence/T-xx/<ts>/build.log
```
- Sin errores de C# y código de salida 0 → estado COMPILA.
- Cuenta y lista las advertencias (warnings) del código propio (`Assets/_Project`). Una advertencia nueva es un hallazgo.
- Si falla: pega el error real, registra bug, para. No sigas con pruebas que dependen de compilar.

### Paso 3 — Verificación de alcance (automática, rápida)
Busca en `Assets/_Project/` (excluye `Tests/` y `Editor/QA/`) y guarda resultados en `scope_check.txt`. Cada coincidencia es un hallazgo a revisar, no un fallo automático:
- Red: `UnityWebRequest`, `HttpClient`, `WebSocket`, `Socket`, `Photon`, `Mirror`, `Netcode`.
- Analítica o servicios: `Analytics`, `Firebase`, `PlayFab`, `Sentry`.
- Medición prohibida: `Time.time`, `Time.realtimeSinceStartup`, `Stopwatch`, `DateTime.Now` usado para medir duraciones; `Gaze`, `EyeTracking`, `OVREyeGaze`; registro de posición/velocidad de cabeza o manos.
- Voz / entrada libre: `Microphone`, `TouchScreenKeyboard`, `InputField` para texto libre.
- Datos personales: campos llamados `nombre`, `apellido`, `correo`, `email`, `dni` en el reporte o en el código de sesión.
- Puntaje o valoración: `score`, `puntaje`, `nivel`, `ranking`, `bueno`, `malo`, `correcto`, `incorrecto`, `excelente` en textos de simulación o reporte.
- Bloqueo: cualquier ruta o estado sin transición al siguiente acto.
- Texto narrativo en la lógica: strings largos en `.cs` fuera de `Report/ReportTexts.cs`; textos sin el comentario `// PROPUESTA – VALIDAR CON CLIENTA`.
Contrasta cada hallazgo con AGENTS.md §3 y con la excepción permitida (el único tiempo permitido es el de espera de diseño del GDD).

### Paso 4 — Tests EditMode
- Ubicación: `Assets/_Project/Tests/EditMode/`. Namespace `CrisisVR.Tests`.
- Cubre como mínimo:
  - Registro de decisiones: cada variable (`Reintento`, `OpcionAdaptabilidad`, `OpcionTrabajoEnEquipo`) guarda cada opción posible y su valor por defecto cuando no se decidió.
  - Generador de reporte: para CADA combinación de decisiones, el `.txt` se genera, incluye metadatos (código, fecha, versión, estado de finalización) y solo usa frases de `ReportTexts.cs`.
  - El reporte NO contiene puntajes, niveles, valoraciones ni datos personales (prueba con lista de palabras prohibidas).
  - Código de sesión: seudonimizado, formato esperado, sin datos personales.
  - Estado de finalización: distingue sesión completa de sesión interrumpida.
  - Escritura de archivo: se crea en `Application.persistentDataPath`, no sobrescribe sesiones anteriores, maneja ruta inexistente o fallo de escritura sin crash.
- Datos de prueba: códigos obviamente falsos (`TEST-0001`). Borra los archivos que las pruebas generaron en `persistentDataPath` al terminar (después de copiarlos a `reports_generated/`).
```
"$UNITY_PATH" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults docs/evidence/T-xx/<ts>/tests_editmode.xml -logFile docs/evidence/T-xx/<ts>/editmode.log
```
(No uses `-quit` junto con `-runTests`.)

### Paso 5 — Matriz de cobertura de recorridos
1. Deriva del GDD todas las opciones de decisión de cada acto y construye la matriz de recorridos (Acto 1 × Acto 2 × Acto 3). No supongas cuántas opciones hay: léelas del GDD y escribe la matriz en el reporte.
2. Cada recorrido de la matriz debe tener una prueba PlayMode, o una justificación escrita de por qué se agrupa con otro.
3. Marca cada celda como PASA / FALLA / NO EJECUTADO. Una celda no ejecutada no cuenta como cubierta.
4. Obligatorios además: recorrido con reintento en Acto 1; recorrido en que el estudiante no actúa y se agota el tiempo de espera de diseño; recorrido interrumpido (salir a mitad); dos sesiones seguidas sin reiniciar la app.
5. Principio central: NINGUNA ruta debe bloquear el avance. Un recorrido que no llegue al final es un bug crítico.

### Paso 6 — Tests PlayMode con captura
- Ubicación: `Assets/_Project/Tests/PlayMode/`.
- Usa ganchos de prueba (por ejemplo una clase `CrisisVR.Tests.TestDriver`) que simulen las decisiones sin hardware VR. Si el código de producción no permite simular decisiones, NO lo modifiques: registra un bug de "testabilidad" proponiendo el punto de extensión.
- En cada paso clave (inicio de acto, momento de decisión, consecuencia, fin de acto, pantalla final) captura una imagen:
  - Espera `yield return new WaitForEndOfFrame()` y usa `ScreenCapture.CaptureScreenshot(ruta)` o renderiza la cámara a un `RenderTexture` y guarda PNG. // VERIFICAR: comportamiento exacto en URP y en modo XR del Editor.
  - Nombre: `acto1_03_decision_reintento.png`.
- Revisa cada captura con la herramienta de vista de imágenes y describe en el reporte lo que ves (qué se ve, qué falta, qué está cortado, texto ilegible, objetos fuera de lugar, artefactos). No escribas "se ve bien" sin describir.
- Estos tests necesitan renderizado: NO uses `-nographics` en esta etapa. Requiere máquina con GPU y sesión de escritorio activa.
```
"$UNITY_PATH" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults docs/evidence/T-xx/<ts>/tests_playmode.xml -logFile docs/evidence/T-xx/<ts>/playmode.log
```
Si Unity no inicia gráficos en batchmode en esta máquina, di exactamente el error y entrega lo demás; no finjas capturas.

### Paso 7 — Video
- Opción recomendada: paquete Unity Recorder (`com.unity.recorder`). Añadirlo toca `Packages/manifest.json`: hazlo solo si la tarea de QA lo autoriza, con versión exacta verificada, y regístralo en `docs/DECISIONES.md`.
- Controla el Recorder desde un script en `Assets/_Project/Editor/QA/` o desde el test PlayMode (inicia al comenzar el recorrido, detiene al terminar). // VERIFICAR: API de `RecorderController`/`RecorderControllerSettings` en la versión instalada.
- Un video por recorrido de la matriz que se considere clave: camino feliz completo, reintento, agotar tiempo, interrupción. Salida en `videos/`.
- No puedes "ver" un video como tal. Para revisarlo, extrae fotogramas (por ejemplo con `ffmpeg`, si está instalado) a intervalos fijos y descríbelos como en el Paso 6. Si `ffmpeg` no está disponible, dilo y entrega el video sin revisión visual.
- Alternativa si Recorder no es posible: secuencia de capturas numeradas (`frame_0001.png`...) y avisa que no es video.

### Paso 8 — Build de Quest (solo compilación)
- Ejecuta el método de build por script (`-executeMethod`) para Android/Quest y guarda el log. El resultado es "el build termina o no". No implica que funcione en el casco.
- Registra tamaño del APK y advertencias de Android/Meta XR.

### Paso 9 — Reporte y cierre
Completa `QA_REPORT.md` (plantilla en §9), registra bugs y lecciones (§6), actualiza `docs/PROGRESS.md`.

## 3. Estados que puedes dar
Los de AGENTS.md §4. En QA, además:
- **Todo lo que no se ejecutó se marca NO EJECUTADO**, con motivo. Nunca se omite en silencio.
- No existe "casi pasa". Es PASA o FALLA.
- Un test flaky (a veces pasa, a veces no): ejecútalo 3 veces, reporta los 3 resultados y márcalo FLAKY; no lo tomes como PASA.

## 4. Severidad de bugs
| Severidad | Definición |
|---|---|
| Crítica | Bloquea el avance entre actos, crash, pérdida o corrupción del reporte, o dato personal en el reporte |
| Alta | Viola el alcance (§3 de AGENTS.md), decisión mal registrada, reporte incompleto o con valoración |
| Media | Texto cortado, objeto mal ubicado, advertencia nueva del código propio |
| Baja | Cosmético, nombre inconsistente, mejora de testabilidad |

## 5. Formato de bug (`docs/QA/BUGS.md`)
```
### BUG-NNN — título corto
- Severidad:
- Tarea / commit:
- Pasos para reproducir:
- Esperado:
- Obtenido:
- Evidencia: (rutas a log / captura / video)
- Prueba de regresión: (nombre del test que lo cubre, o "pendiente")
- Estado: ABIERTO | EN CORRECCIÓN | CORREGIDO (pendiente de reprueba) | CERRADO (reprobado en commit X)
```
Solo la reprueba exitosa con evidencia cierra un bug. Cada bug confirmado debe terminar con un test de regresión.

## 6. Cómo "aprende" el agente (importante: qué es y qué no es)
El modelo no se reentrena. La mejora viene de memoria escrita en el repositorio:
- `docs/QA/LESSONS.md` se lee al inicio de cada sesión y se amplía al final.
- Cada lección tiene este formato:
```
### L-NNN — título corto
- Fecha / tarea:
- Situación:
- Qué pasó (síntoma):
- Causa real:
- Regla a aplicar en adelante:
- Evidencia:
- Veces confirmada: 1
```
- Qué merece una lección: un comando que falló y cómo se arregló, una trampa de Unity/XR/URP, un tipo de bug que se repite, una prueba que dio falsos positivos o negativos, una captura que salió mal y por qué, un criterio de revisión visual que ayudó.
- Qué NO merece una lección: suposiciones sin comprobar. Si no hay evidencia, va marcada `HIPÓTESIS` y no se aplica como regla hasta confirmarla.
- Mantenimiento: si una lección ya existe, sube "Veces confirmada" en vez de duplicarla. Si una lección resultó falsa, márcala `REFUTADA` y explica por qué; no la borres.
- Cada 5 sesiones de QA (o si pasa de ~40 lecciones), propón una consolidación a la desarrolladora: agrupar por tema y promover las más confirmadas a pasos fijos de este protocolo.
- Cada bug confirmado también genera o refuerza un test de regresión: así el conjunto de pruebas crece con el proyecto.

## 7. Lista de comprobación para la desarrolladora en el Quest (`docs/QA/QUEST_CHECKLIST.md`)
El agente genera esta lista por tarea (qué mirar según lo que cambió) y la deja en la carpeta de evidencia. Ejemplos de ítems: se puede completar los tres actos con controles/manos; cada opción continúa al siguiente acto; el texto es legible a distancia real; no hay mareo ni saltos de cámara; el audio se oye; el reporte `.txt` aparece en el dispositivo; la app no se cierra al quitarse el casco; fps estable (usar OVR Metrics Tool); tiempo de espera de diseño se comporta como en el GDD. La desarrolladora la rellena y adjunta capturas/video del casco. Mientras no se rellene, el estado máximo es PROBADO EN EDITOR.

## 8. Cosas que el agente QA nunca hace
- Corregir código de producción "de paso".
- Marcar PASA sin ejecutar.
- Presentar datos de prueba como datos de estudiantes.
- Guardar nombres, correos o datos personales en evidencia, reportes o lecciones.
- Subir de nivel de estado sin la evidencia de la tabla de AGENTS.md §4.
- Borrar evidencia de ejecuciones anteriores.

## 9. Plantilla de `QA_REPORT.md`
```
# QA_REPORT — T-xx — <fecha y hora>
- Commit probado:
- Agente / herramienta / modelo:
- Máquina (SO, GPU), versión de Unity:
- Estado resultante (AGENTS.md §4):

## 1. Criterios de aceptación de la tarea
| # | Criterio | Prueba(s) que lo cubren | Resultado |

## 2. Compilación
(resultado, cantidad de warnings propios, ruta del log)

## 3. Verificación de alcance
(hallazgos de scope_check.txt y juicio sobre cada uno)

## 4. Tests EditMode
(total / pasan / fallan / omitidos, lista de fallos)

## 5. Matriz de recorridos
| Recorrido | Test | Resultado | Captura | Video |

## 6. Revisión visual
(descripción de lo observado en cada captura clave y en fotogramas de video)

## 7. Build Quest
(resultado y tamaño)

## 8. Bugs
(IDs nuevos, abiertos, cerrados en esta sesión)

## 9. No ejecutado y por qué

## 10. Lecciones añadidas o actualizadas
(IDs)

## 11. Pendiente para la desarrolladora
(Quest checklist, decisiones, validaciones con la clienta)
```
