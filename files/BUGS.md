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
- Estado: ABIERTO.

### BUG-002 — G01_AgarreConSimulador corre como EditMode y carga escenas en Play Mode
- Severidad: Baja (prueba de interacción no llega a ejecutarse).
- Tarea / commit: T-09 a T-13; commit no disponible (Plastic `client.conf` ausente).
- Pasos para reproducir: ejecutar `CrisisVRGrabPlayTests.G01_AgarreConSimulador` en EditMode.
- Esperado: el test se ejecute en PlayMode o use APIs de Editor compatibles con EditMode.
- Obtenido: falla con `InvalidOperationException: This can only be used during play mode, please use EditorSceneManager.OpenScene() instead.`
- Evidencia: Test Runner MCP, job `2877149b69a34922bb4f827252374928`.
- Prueba de regresión: pendiente; moverlo a una assembly/suite PlayMode y repetirlo.
- Estado: ABIERTO.

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
