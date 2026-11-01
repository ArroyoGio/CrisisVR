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
- Estado: CORREGIDO y VERIFICADO en Play Mode (S01–S04, sprint 2).

### BUG-006 — La espera de 45 s del Acto 1 seguía corriendo en pausa
- Severidad: Media (la guía exige que el reloj se detenga en la pausa).
- Origen: `Act1Flow` comparaba contra `Time.unscaledTime`.
- Corrección: `DecisionTimer` (CrisisVR.Logic) con tiempo de juego y pausa explícita; cubierto por pruebas unitarias y por S03.
- Estado: CORREGIDO y VERIFICADO en Play Mode (S03: 3 s en pausa sin que avance la espera; la selección en pausa se ignora).

### BUG-007 — El build arrancaba en Tutorial e incluía una escena inexistente
- Severidad: Media (se saltaba la pantalla inicial y la bienvenida; `BasicScene.unity` no existe).
- Corrección: `EditorBuildSettings.asset` queda MainMenu → Welcome → Tutorial → Act1, y `EditorBuildSetup` retira las escenas que no existen y respeta el orden.
- Estado: CORREGIDO, SIN VERIFICAR en build.

### BUG-008 — El Test Runner de Unity se cuelga en batchmode
- Severidad: Media (impide generar XML de evidencia desde la línea de comandos).
- Pasos: `Unity -batchmode -nographics -runTests -testPlatform EditMode` con Unity 6000.3.23f1.
- Obtenido: tras «Running tests for ExecutionSettings», Unity recarga los ensamblados y no avanza (log detenido durante días).
- Causa principal (sprint 2): `EditorBuildSetup` reescribía en cada carga el orden de escenas, que nunca quedó guardado, y además se editaban scripts durante la corrida. Corregido el orden en `EditorBuildSettings.asset`, PlayMode corrió completo (7/7).
- Persiste: la recarga de dominio con que el Test Runner inicia EditMode se cuelga de forma intermitente (1 de 3 corridas). Mitigación: reintentar con tope de tiempo.
- Estado: MITIGADO.

### BUG-009 — La sesión se trababa en el Acto 2 después del aviso
- Severidad: Alta (la sesión no llegaba a la decisión del Acto 2 ni al reporte final).
- Pasos: recorrer Acto 1 → Acto 2 en Play Mode (prueba S01 del sprint 1).
- Obtenido: tras `ACTO2_AVISO` no avanzaba. La continuación de `await DialogueTask` se ejecuta dentro de `DialogueRunner.OnDialogueCompleteAsync`, y llamar ahí a `StartDialogue` deja el diálogo nuevo en un estado inconsistente.
- Corrección: antes de cada `StartDialogue`, esperar `!IsDialogueRunning` y `YarnTask.Yield()` (Act1Flow, Act2Flow y Act3Flow).
- Prueba de regresión: S01–S04 (PlayMode), 7/7 aprobadas.
- Estado: CORREGIDO y VERIFICADO en Play Mode.

### BUG-010 — Presentación de los Actos 2 y 3
- Severidad: Media (afecta la validez del orden fijo A–D y la legibilidad).
- Obtenido en las capturas del sprint 1: opciones en orden D–A, botones que tapaban la videollamada, rótulos ilegibles en la carpeta y la tablet, y Rodrigo demasiado cerca de la cámara.
- Corrección: `ActsSceneSetup` (orden según la izquierda del estudiante, botones más bajos, carteles verticales, Rodrigo a ~2,6 m).
- Evidencia: `Docs/evidence/S1_act2_*.png` y `S1_act3_*.png` regeneradas.
- Estado: CORREGIDO en Editor; pendiente de revisar dentro del visor.

### BUG-011 — El gatillo no seleccionaba los objetos 3D de los actos
- Severidad: Alta (en el Quest, el estudiante apretaría el gatillo y no pasaría nada; la guía dice que el gatillo selecciona).
- Origen: en `XRI Default Input Actions`, «Select» = `{GripButton}` y «UI Press» = `{TriggerButton}`. `MainMenuController` ya reasignaba el gatillo; los actos no.
- Corrección: `ActsDirector.UseTriggerToSelect()` asigna `selectInput = uiPressInput` en todos los NearFarInteractor, incluidos los inactivos.
- Prueba de regresión: S07 comprueba que la acción de selección está ligada a `{TriggerButton}` y recorre apuntar, resaltar y seleccionar.
- Estado: CORREGIDO y VERIFICADO en Editor; pendiente con mandos reales.

### BUG-012 — Copias duplicadas de los Actos 2 y 3 en Act1.unity
- Severidad: Media (objetos invisibles duplicados; una prueba tomaba la copia equivocada).
- Origen: `ActsSceneSetup` buscaba las raíces con `GameObject.Find`, que ignora objetos inactivos.
- Corrección: recorrer `scene.GetRootGameObjects()`. La escena regenerada tiene una copia de cada raíz.
- Estado: CORREGIDO.

### BUG-013 — Excepción de Yarn al salir a mitad de un diálogo
- Severidad: Baja (excepción en consola; sin efecto en el registro).
- Origen: `LinePresenter` sigue su fundido asíncrono sobre un `CanvasGroup` ya destruido al cambiar de escena.
- Corrección: `PauseMenu.EndSession` llama a `DialogueRunner.Stop()` antes de salir; S07 detiene su diálogo al terminar.
- Estado: CORREGIDO y VERIFICADO (suite PlayMode completa sin excepciones).

