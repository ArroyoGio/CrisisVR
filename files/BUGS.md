# BUGS.md — Registro de bugs de Crisis VR

Formato y severidades en `QA_PROTOCOL.md` §4 y §5. Un bug se cierra solo con reprueba exitosa y evidencia.

### BUG-001 — Test EditMode exige una escena SampleScene inexistente
- Severidad: Baja (prueba obsoleta; no afecta al runtime de ACT1).
- Tarea / commit: T-09 a T-13; commit no disponible (Plastic `client.conf` ausente).
- Pasos para reproducir: ejecutar todos los tests EditMode con Unity Test Runner.
- Esperado: los tests reflejan las escenas que existen y las escenas que deben estar en Build Settings.
- Obtenido: `CrisisVREditModeTests.E01_EscenasExistenEnProyecto` falla al exigir `Assets/Scenes/SampleScene`, que no existe.
- Evidencia: Test Runner MCP, job `2877149b69a34922bb4f827252374928` (9 totales, 7 pasan, 2 fallan).
- Prueba de regresión: pendiente; actualizar este test para comprobar Act1/Tutorial o las escenas del proyecto.
- Corrección (sprint 1, 2026-10-27): la prueba exige ahora MainMenu, Welcome, Tutorial y Act1; la prueba PlayMode de SampleScene pasó a cargar MainMenu.
- Estado: CORREGIDO, SIN VERIFICAR (falta correr el Test Runner).

### BUG-002 — G01_AgarreConSimulador corre como EditMode y carga escenas en Play Mode
- Severidad: Baja (prueba de interacción no llega a ejecutarse).
- Tarea / commit: T-09 a T-13; commit no disponible (Plastic `client.conf` ausente).
- Pasos para reproducir: ejecutar `CrisisVRGrabPlayTests.G01_AgarreConSimulador` en EditMode.
- Esperado: el test se ejecute en PlayMode o use APIs de Editor compatibles con EditMode.
- Obtenido: falla con `InvalidOperationException: This can only be used during play mode, please use EditorSceneManager.OpenScene() instead.`
- Evidencia: Test Runner MCP, job `2877149b69a34922bb4f827252374928`.
- Prueba de regresión: pendiente; moverlo a una assembly/suite PlayMode y repetirlo.
- Corrección (sprint 1): la prueba entra en Play Mode con `EnterPlayMode` y sale con `ExitPlayMode`. En batchmode el Test Runner se colgó, por lo que se marcó `[Explicit]`: se ejecuta a mano desde el Editor.
- Estado: MITIGADO, SIN VERIFICAR.

### BUG-003 — Excepción intermitente al desactivar el raycaster del diálogo
- Severidad: Baja (excepción de consola en teardown; sin impacto de gameplay demostrado).
- Tarea / commit: QA exploratorio T-09 a T-13; commit no disponible.
- Pasos para reproducir: entrar y salir de Play Mode en Act1.
- Esperado: detener Play Mode sin excepciones.
- Obtenido: una salida registró `KeyNotFoundException` en `TrackedDeviceGraphicRaycaster.OnDisable()`, `Library/PackageCache/com.unity.xr.interaction.toolkit.../TrackedDeviceGraphicRaycaster.cs:335`. No se reprodujo en las salidas posteriores y la última lectura de consola quedó limpia.
- Evidencia: lectura detallada de consola MCP durante esa sesión (stack trace de XRI, línea 335); no hay log persistente por el bloqueo de QA formal.
- Prueba de regresión: pendiente; repetir varias entradas/salidas y añadir una comprobación de consola.
- Estado: ABIERTO (intermitente; repro pendiente).

### BUG-004 — NullReference repetida en receptores de affordance de Tutorial
- Severidad: Baja (feedback visual de los controladores; generaba spam de consola).
- Origen: `BaseAsyncAffordanceStateReceiver.HandleTween()` recibía un `affordanceTheme` nulo en los receptores Color de Poke/NearFar de Tutorial.
- Corrección: `TutorialInputRouter` busca bajo `playerOrigin` (los mandos son descendientes de `TutorialPlayer`, hermano de `TutorialWorld`), inicializa el tema desde `affordanceThemeDatum.Value` y reinicia el receptor para registrarlo con su provider; desactiva y advierte solo si falta el tema.
- Evidencia de reprueba: Unity 6000.3.9f1 recompiló; Tutorial entró a Play Mode con XR Interaction Simulator. Los cuatro receptores Poke/NearFar muestran `affordanceTheme` asignado. Después de la ejecución, `read_console` reportó 0 errores y 0 warnings; la NullReference de XRI no reapareció.
- Estado: CORREGIDO (reprueba exploratoria en Editor; no equivale a prueba en Quest).

### BUG-005 — Espacio en el simulador parece mover al jugador
- Investigación: en `XR Interaction Simulator Controls.inputactions`, Space activa `Toggle Perform Quick Action`; el modo inicial simula el gatillo del mando, no el eje de movimiento.
- Prueba: en Tutorial/Play Mode se pulsó Space con la vista Game enfocada. El paso siguió en «Apunta y selecciona» y el Transform de `TutorialPlayer` permaneció en `(70.98415, 0.0593582, -16.5656815)`; no se reprodujo desplazamiento por Space. El movimiento virtual del tutorial depende del joystick; el simulador también tiene navegación de visor por teclado/mouse.
- Estado: NO REPRODUCIDO como movimiento directo por Space. Si vuelve a ocurrir, registrar el paso activo y si se usó mouse/teclas WASD a la vez.

### BUG-005 — Los nodos de Yarn se encadenaban sin esperar a que terminaran
- Severidad: Alta (el flujo podía mostrar ENTREGAR o las opciones antes de terminar el diálogo y reemplazar nodos activos).
- Origen: en Yarn Spinner 3.2.8, `DialogueRunner.StartDialogue` termina al arrancar el nodo, no al acabarlo (`Runtime/DialogueRunner/DialogueRunner.cs`). Lo detectó la revisión de Codex del sprint 1.
- Corrección: `PlayNode` de los tres actos espera además `DialogueRunner.DialogueTask`.
- Estado: CORREGIDO, SIN VERIFICAR en Play Mode.

### BUG-006 — La espera de 45 s del Acto 1 seguía corriendo en pausa
- Severidad: Media (la guía exige que el reloj se detenga en la pausa).
- Origen: `Act1Flow` comparaba contra `Time.unscaledTime`.
- Corrección: `DecisionTimer` (CrisisVR.Logic) con tiempo de juego y pausa explícita; cubierto por pruebas unitarias (dotnet 44/44) y por S03 (pendiente de ejecutar).
- Estado: CORREGIDO; lógica verificada, Play Mode sin verificar.

### BUG-007 — El build arrancaba en Tutorial e incluía una escena inexistente
- Severidad: Media (se saltaba la pantalla inicial y la bienvenida; `BasicScene.unity` no existe).
- Corrección: `EditorBuildSettings.asset` queda MainMenu → Welcome → Tutorial → Act1, y `EditorBuildSetup` retira las escenas que no existen y respeta el orden.
- Estado: CORREGIDO, SIN VERIFICAR en build.

### BUG-008 — El Test Runner de Unity se cuelga en batchmode
- Severidad: Media (impide generar XML de evidencia desde la línea de comandos).
- Pasos: `Unity -batchmode -nographics -runTests -testPlatform EditMode` con Unity 6000.3.23f1.
- Obtenido: tras «Running tests for ExecutionSettings», Unity recarga los ensamblados y no avanza (log detenido durante días).
- Estado: ABIERTO. Probar desde el Editor abierto o con 6000.3.9f1 y revisar los `[InitializeOnLoad]` que tocan la configuración (p. ej. `EditorBuildSetup`).

