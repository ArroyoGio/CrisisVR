# Estado del prototipo

## 2026-10-25 — T-06, base del Acto 1

- Agente: Codex.
- Estado actual: **SIN VERIFICAR**. La escena fue ajustada desde Unity Editor por MCP y el builder actualizado se ejecutó; no se hizo una compilación batch ni se probó en Play Mode en esta revisión visual.
- Evidencia de la revisión: `docs/evidence/T-06_act1_vista_inicial.png` (captura de la cámara principal en Edit Mode; no representa una prueba de Play Mode).
- Se usaron los prefabs `a laptop`, `chair` y `table3` de `Assets/school/Prefabs/props`. La colocación se calcula con `Renderer.bounds`: laptop sobre el escritorio del alumno; silla y mesa del profesor sobre el piso. El monitor/teclado del puesto 08 y la silla geométrica fueron desactivados para evitar solapamientos; los otros puestos continúan en el aula.
- El profesor sigue siendo una cápsula provisional porque `Assets/school` no contiene un modelo humano identificado. Su base está en el piso, está orientado hacia el estudiante y se desplazó ligeramente del eje de la laptop. El panel de subtítulos se oculta cuando no tiene texto.
- Comprobación en Editor por MCP: Unity 6000.3.9f1 terminó la compilación solicitada y volvió a `ready_for_tools`; no se observaron errores C# nuevos en la consola. La consola conserva errores `NoSubscription` de Generators.ai y un aviso de iluminación heredada, ajenos a esta escena. No se ejecutaron pruebas Play Mode.
- Pendiente: probar la selección con teclado/ratón del XR Interaction Simulator. El resultado en Quest corresponde a la desarrolladora.
- Pendiente de implementación: la decisión se mantiene en memoria durante la sesión y se imprime en la consola; todavía no se conecta a `SessionRecorder` ni al reporte. No se añadió audio ambiente.
- Contenido del docente y nota 08/20: propuestas V-02 y V-03, pendientes de validación de la clienta según el GDD.
- Uso de IA: Codex (GPT-6) preparó el código y contenido de prototipo en esta tarea.
- Evidencia histórica: `docs/evidence/T-06_build.log` documenta una compilación batch previa a esta revisión de modelos y encuadre; no se usa para elevar el estado de la revisión actual.

## 2026-10-25 — T-07, instalación de Yarn Spinner

- Agente: Codex.
- Estado: **SIN VERIFICAR**. Unity Package Manager confirmó que `dev.yarnspinner.unity` 3.2.8 quedó instalado desde Git; `Packages/packages-lock.json` fija el tag `v3.2.8` y el commit `bfc5b6a2e8bc8978e777caeea3a827a0d408cc7d`.
- La importación inició compilación en Unity 6000.3.9f1, pero el Editor aún reportaba `is_compiling` y después MCP dejó de recibir respuesta al ping. No se pudo confirmar la compilación ni revisar la consola tras la importación.
- Archivos modificados: `Packages/manifest.json`, `Packages/packages-lock.json`, `Docs/DECISIONES.md`, `Docs/PROGRESS.md`.
- Pendiente: reconectar cuando Unity termine la importación, revisar errores de consola y continuar luego con la configuración del Yarn Project y Dialogue System; esta tarea solo instaló el paquete.
- Uso de IA: Codex (GPT-6) instaló el paquete solicitado y registró la decisión técnica.

## 2026-10-25 — T-08, diálogo VR y guiones de los tres actos

- Agente: Codex.
- Estado: **SIN VERIFICAR**. El Editor recompiló los scripts y terminó la importación del Yarn Project; la consola no mostró errores de Yarn ni de C# nuevos. No se ejecutó Play Mode, batchmode ni una prueba en Quest, por lo que la presentación y selección en el dispositivo siguen pendientes.
- Se creó `Assets/_Project/Yarn/CrisisVR.yarnproject`, que incluye `Marco.yarn`, `Act2.yarn`, `Act3.yarn` y `Scripts/Acts/Act1.yarn`. Los textos se basan en el GDD y conservan marcas de propuesta pendientes de validar.
- Se configuró en `Assets/Scenes/Act1.unity` un Dialogue System de Yarn con Canvas world-space anclado a la cámara XR, `TrackedDeviceGraphicRaycaster`, presentador de líneas y botón CONTINUAR. El EventSystem existente ya usa `XRUIInputModule`.
- `Act1Flow` ahora ejecuta los nodos de Acto 1 con `DialogueRunner`. Los nodos de Actos 2 y 3 están compilados dentro del mismo proyecto Yarn para invocarlos desde sus flujos cuando se implementen. El diseño del GDD ubica las tres situaciones en la misma aula; no hay escenas Unity `Act2` y `Act3` separadas en el proyecto.
- Archivos modificados: `Assets/Scenes/Act1.unity`, `Assets/_Project/Scripts/Acts/Act1Flow.cs`, `Assets/_Project/Scripts/Acts/Act1.yarn`, `Assets/_Project/Scripts/Debug/Editor/Act1SceneSetup.cs`, `Assets/_Project/Scripts/Debug/Editor/YarnDialogueSetup.cs`, `Assets/_Project/Yarn/Act2.yarn`, `Assets/_Project/Yarn/Act3.yarn`, `Assets/_Project/Yarn/CrisisVR.yarnproject` y metadatos `.meta` generados por Unity.
- Pendiente: validar con la clienta los textos V-02 a V-12; implementar los flujos e interacciones de los Actos 2 y 3; verificar el recorrido en Play Mode y probar el resultado en Quest.
- Uso de IA: Codex (GPT-6) conectó Yarn Spinner con la UI XR existente y trasladó a Yarn los diálogos del GDD.

## 2026-10-25 — T-09 a T-13, cierre funcional del prototipo ACT1

- Agente: Codex.
- Estado: **SIN VERIFICAR**. El Editor Unity 6000.3.9f1 compiló los scripts tras importar los assets; la última lectura de consola no devolvió errores de C#. No se generó el log batchmode exigido para declarar COMPILA y no se ejecutaron pruebas ni Play Mode.
- ACT1 ahora inicia con tarjeta de transición en World Space, presenta el nodo de entrada, activa ENTREGAR, muestra la retroalimentación y la calificación, y permite CORREGIR PROYECTO o dejar vencer los 45 segundos. La ruta de no acción oculta el botón antes de continuar. Miguel hace un movimiento de asentimiento mientras se reproducen nodos de diálogo.
- Se registran `Reintento`, `Act1Ruta`, `act1Registered`, `act1Completed` y un eventLog ordenado sin tiempos de respuesta. El código de sesión que genera el menú inicia el estado de sesión y se conserva durante la transición entre escenas.
- Al cerrar ACT1 se muestra la tarjeta de salida y se guarda un reporte parcial en `Application.persistentDataPath`. El reporte refleja ACT1 y deja el estado general `En curso`, porque ACT2 y ACT3 todavía no están conectados.
- La escena fue guardada desde el configurador de Editor y `manage_scene(validate)` informó cero problemas, scripts faltantes o prefabs rotos. No se probó la interacción de los botones, la temporización en Play Mode ni el visor Quest.
- Tras la ejecución de la desarrolladora, la consola registró `DialogueException: No node named Act1_Cierre has been loaded.` El nodo se había quedado vacío y Yarn no lo incluyó en el programa; el flujo ahora cierra con la tarjeta de salida y ya no intenta iniciar ese nodo. Unity reimportó los cambios y terminó de compilar sin errores C#; no se volvió a ejecutar Play Mode.
- Archivos modificados: `Assets/Scenes/Act1.unity`, `Assets/_Project/Scripts/Acts/Act1Flow.cs`, `Assets/_Project/Scripts/Acts/Act1.yarn`, `Assets/_Project/Scripts/Acts/Act1ProfessorMotion.cs`, `Assets/_Project/Scripts/Acts/Act1TransitionCard.cs`, `Assets/_Project/Scripts/Core/SessionData.cs`, `Assets/_Project/Scripts/Core/SessionRuntime.cs`, `Assets/_Project/Scripts/Flow/MainMenuController.cs`, `Assets/_Project/Scripts/Report/ReportBuilder.cs`, `Assets/_Project/Scripts/Report/ReportTexts.cs`, `Assets/_Project/Scripts/Report/ReportWriter.cs`, `Assets/_Project/Scripts/Debug/Editor/Act1SceneSetup.cs` y metadatos `.meta` generados por Unity.
- Pendiente: QA separado en Editor y prueba Quest por la desarrolladora; validar textos V-02 a V-05. Plastic SCM no permitió consultar el estado porque falta su `client.conf`; no se creó commit.
- Uso de IA: Codex (GPT-6) implementó el flujo del primer acto, su registro, reporte parcial y presentación VR.

## 2026-10-25 — QA exploratorio T-09 a T-13 (Codex)

- Estado: **SIN VERIFICAR**. No se elevó el nivel: el protocolo exige commit limpio y evidencia batchmode; este workspace no tiene `.git` y Plastic no tiene `client.conf`. El Unity CLI instalado no pudo arrancar desde el entorno (`unity.exe`: acceso denegado al shim de WindowsApps), así que no se generaron logs batchmode ni XML de resultados.
- Unity Test Runner por MCP: EditMode 7/9 pasaron; fallaron `CrisisVREditModeTests.E01_EscenasExistenEnProyecto` porque espera `Assets/Scenes/SampleScene` inexistente, y `CrisisVRGrabPlayTests.G01_AgarreConSimulador` porque está ubicado/clasificado como EditMode pero llama `SceneManager.LoadScene` (excepción: solo se puede usar durante Play Mode). PlayMode 2/3 pasaron: Act1 y Tutorial cargan; el test de SampleScene se omite porque no está en Build Settings.
- Play Mode exploratorio de ACT1: confirmé que el Editor avanzó (Time.frameCount subió), la escena activa fue Act1, Yarn inició el diálogo y ENTREGAR mostró “Entrega enviada.”, la tarjeta 08/20 y CORREGIR PROYECTO. Activé el callback de ENTREGAR mediante reflexión para aislar el flujo; no cuenta como prueba de entrada real XR. Las rutas de corrección/no acción hasta la tarjeta final y el reporte no quedaron verificadas.
- Se observó una vez al salir de Play Mode una `KeyNotFoundException` en `TrackedDeviceGraphicRaycaster.OnDisable()` de XRI (PackageCache, línea 335). No se reprodujo en las siguientes salidas de Play Mode y la última lectura de consola mostró cero errores; queda como hallazgo intermitente, no como fallo confirmado del recorrido.
- La salida de Play Mode también mostró 14 avisos “The referenced script (Unknown) … is missing”; `manage_scene(validate)` previamente había reportado cero scripts faltantes y esos avisos no volvieron a aparecer después. No se atribuyen a un objeto concreto de Act1.
- Pendiente: arreglar/retirar el test obsoleto de SampleScene, mover G01 a una suite PlayMode, añadir pruebas de regresión del flujo Act1 y repetir QA con VCS configurado y Unity batchmode. Recorrer ambas rutas con entrada real del XR Interaction Simulator y validar el Quest sigue pendiente.
- No se modificó código de producción durante la revisión QA. Uso de IA: Codex (GPT-6) ejecutó pruebas exploratorias y el Test Runner integrado de Unity 6000.3.9f1.

## 2026-10-26 — Corrección del spam de consola en Tutorial

- Se corrigieron los `NullReferenceException` repetidos en `BaseAsyncAffordanceStateReceiver.HandleTween()` al iniciar los receptores de color Poke/NearFar desde su `AffordanceThemeDatum` en `TutorialInputRouter`.
- Evidencia: Unity 6000.3.9f1 compiló el script; Tutorial se ejecutó en Play Mode con XR Interaction Simulator y mostró el ejercicio «Apunta y selecciona» con los dos mandos. La consola MCP devolvió 0 errores y 0 warnings tras la prueba; el error de XRI no se reprodujo.
- Estado del build completo y Quest: **SIN VERIFICAR**. Es una reprueba exploratoria de Editor, no una verificación de build ni de hardware.
- Se observaron 4 entradas de tipo Log de Unity AI Assistant con `NoSubscription`; no son errores ni warnings del proyecto y no bloquean la escena.

## 2026-10-26 — Revisión de movimiento con Space y corrección del árbol de mandos

- La primera revisión del spam de affordance no alcanzaba ningún receptor: `TutorialWorld` y `TutorialPlayer` son raíces hermanas. Ahora `TutorialInputRouter` busca los receptores bajo `playerOrigin`, que sí contiene los cuatro componentes Poke/NearFar de los dos mandos.
- Unity recompiló; en Play Mode los cuatro receptores tienen `affordanceTheme` y la consola no reporta errores XRI. Probé Space en la vista Game: está ligado al gatillo simulado, no al movimiento; el Transform de `TutorialPlayer` permaneció en `(70.98415, 0.0593582, -16.5656815)`.
- Tutorial queda en Play Mode para continuar la revisión. Build y hardware Quest: **SIN VERIFICAR**.
