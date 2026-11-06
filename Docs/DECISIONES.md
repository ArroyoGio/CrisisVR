# Decisiones del prototipo

## 2026-11-03 — Bisagra física de la laptop de Acto 1

- El estado serializado de inicio queda cerrado a 0°. El prefab fuente solo aporta la malla; la instancia de escena se desempaca. No hay componentes Animator/Animation en la jerarquía de la tapa, y el transformador cambia el ángulo únicamente mientras el interactable está seleccionado.
- Se separa la malla única de la laptop del participante en dos meshes de escena y se deja el cuerpo fijo. La tapa conserva un pivote hijo en el borde de la bisagra.
- Para mantener compatibilidad con `XRGrabInteractable` y el `NearFarInteractor` existente, un `XRBaseGrabTransformer` interpreta la pose del attach del mando como giro en el eje X local, fija la posición del pivote y limita el rango a 0°–125°. El Rigidbody cinemático y `unparentTransformOnGrab=false` conservan la jerarquía durante el agarre.
- Motivo: el mesh original tiene la tapa y el cuerpo unidos y no incluye articulación ni físicas. El transformador permite agarrar y mover la tapa sin añadir un mecanismo de teclado/touchpad ni cambiar el flujo narrativo.
- Verificación: en Play Mode se comprobó `SelectEnter/SelectExit` con el `NearFarInteractor` derecho y poses de attach controladas; entrada física manual y Quest siguen pendientes. Ver `Docs/PROGRESS.md` (2026-11-03).

## 2026-10-27 — Sprint 1: Actos 2 y 3, pausa, cierre y reporte completo

Respuestas de la tesista (Giovanna Arroyo) a las dudas 1–6 de la guía v1.0; la guía corregida es la v1.1.

- **Locomoción:** se mantiene el desplazamiento con joystick y el giro por pasos (T-23). Los actos no obligan a moverse: todo lo seleccionable está al alcance del rayo desde la posición inicial. `ActsDirector.seated` (falso por defecto) permite desactivarlo si la prueba de confort lo aconseja.
- **SDK:** se queda XR Interaction Toolkit 3.5.1 con XR Origin y Unity OpenXR Meta; no se migra a Meta XR Core SDK.
- **Escenas:** se mantienen MainMenu → Welcome → Tutorial → Act1, en ese orden en Build Settings (antes arrancaba en Tutorial y figuraba `BasicScene`, que no existe). Los Actos 2 y 3 se activan por etapas en el aula de `Act1.unity` (GDD G1). Entre escenas hay fundido a negro (`SceneFader`); la sesión vive en `SessionRuntime` (estático) y se conserva.
- **Nombres:** se conservan los del repositorio; la tabla de la guía es de referencia.
- **Lógica pura en `CrisisVR.Logic`** (asmdef sin UnityEngine): `SessionData`, `SessionRecorder`, `SessionIdFactory`, `DecisionTimer`, `DecisionCodes`, `ReportBuilder` y `ReportTexts`. Se prueba con NUnit en EditMode y también fuera de Unity con `dotnet test`.
- **Código de sesión:** `CVR-AAAAMMDD-HHMMSS-XXXX` (XXXX hexadecimal), según la guía.
- **Esperas:** `DecisionTimer` usa el tiempo de juego y se congela en pausa (45 s en Actos 1 y 2; recordatorio de 90 s sin registro en el Acto 3). Reemplaza el `Time.unscaledTime` anterior, que seguía corriendo en pausa.
- **Pausa:** botón Menú del control izquierdo (Escape en el Editor). Congela `Time.timeScale`, audio y esperas, oculta el panel de diálogo e ignora selecciones. TERMINAR SESIÓN escribe el reporte «Interrumpida» y vuelve a MainMenu con un código nuevo.
- **Reporte:** formato completo de la guía (aviso de límites, tres situaciones, calidad del registro y limitaciones). Se reescribe el mismo archivo de la sesión al cerrar cada acto («En curso», como respaldo si la app se cierra a la fuerza), al completar el Acto 3 («Completa») o al terminar la sesión («Interrumpida»). Nunca incluye el eventLog ni tiempos.
- **Hablantes:** `SpeakerPresenter` (presentador de Yarn sin texto) enciende el borde del recuadro de quien habla en la videollamada y hace cabecear a Miguel y Rodrigo. En el Acto 3 el estudiante habla como «Tú».
- **Resaltado:** `SelectableHighlight` aplica los colores de la guía (gris inactivo, azul activo, celeste al apuntar) y deshabilita el interactable cuando está inactivo.
- **Tutorial:** se conservan agarre, movimiento y giro. Su duración se escribe solo en la consola (`TUTORIAL_DURACION`) para cronometrarlo; no se guarda en la sesión.
- **Pruebas sin visor:** `EditorDebugInput` (F1–F10, solo Editor o build de desarrollo) usa el mismo punto de decisión que el rayo XR.
- **Recursos de terceros:** el aula usa «School assets» v1.0 (A.R.S|T., Unity Asset Store, 2021), comprado el 20/06/2026.
- **Uso de IA:** Claude (Claude Code) coordinó, escribió la capa Unity y verificó; Codex (gpt-6-astra) escribió `CrisisVR.Logic` y sus pruebas, y revisó el diff.

## 2026-10-25 — Presentación Yarn en VR y guiones de los actos

- Se usa un único `CrisisVR.yarnproject` para los nodos de los tres actos y el tutorial ya escrito. El GDD describe las situaciones en la misma aula; Act2 y Act3 no son escenas Unity separadas en el proyecto actual.
- El sistema de diálogo de Yarn se agrega a `Act1.unity` y su Canvas pasa a World Space, como hijo de la cámara del `XR Origin`. El avance queda en un botón legible seleccionado con `XRUIInputModule` y `TrackedDeviceGraphicRaycaster`, compatible con las interacciones XR del proyecto.
- Act1 usa `DialogueRunner.StartDialogue` para mostrar sus nodos. Los textos de Actos 2 y 3 están escritos en Yarn para su conexión desde los flujos de esas situaciones. No se añadieron presentadores de voz ni se alteraron decisiones del GDD.
- Los contenidos narrativos siguen marcados como propuestas pendientes de validación; la interfaz no se ha probado en Play Mode ni en Quest.

## 2026-10-25 — Reporte parcial al cerrar ACT1

- Al terminar ACT1 se escribe un reporte de progreso con `Estado de la sesión: En curso`; no se marca la sesión completa porque los actos 2 y 3 aún no están conectados. El mismo código seudonimizado de sesión permite actualizar el reporte cuando se incorporen los demás actos.
- El eventLog registra únicamente el orden de inicio, entrega, decisión y cierre, sin guardar duración de respuesta. El único intervalo medido es el plazo de diseño de 45 segundos definido para la elección de ACT1.
- La tarjeta de entrada y salida se presenta sobre un Canvas World Space en la cámara XR, y el placeholder de Miguel asiente durante los nodos de diálogo. No se agregaron datos personales ni nuevas decisiones narrativas.

## 2026-10-25 — Yarn Spinner para T-07

- Se instala el paquete oficial `dev.yarnspinner.unity` desde `YarnSpinnerTool/YarnSpinner-Unity`, fijado al tag estable `v3.2.8` según la publicación oficial más reciente consultada. El proyecto usa Unity 6000.3.9f1 y el paquete admite Unity 2022.3 o posterior.
- Unity Package Manager confirmó la instalación y el lockfile registró el commit `bfc5b6a2e8bc8978e777caeea3a827a0d408cc7d`. La compilación posterior del Editor quedó pendiente de verificación porque MCP dejó de recibir respuesta mientras Unity importaba el paquete.

## 2026-10-25 — Base del Acto 1 (T-06)

- Se reutiliza el aula existente `Aula 2` y la mesa del alumno. Laptop, silla del alumno y mesa del profesor usan los prefabs `a laptop`, `chair` y `table3` de `Assets/school/Prefabs/props`; el builder obtiene sus límites de renderizado y alinea laptop con tablero y muebles con piso.
- Se desactiva el monitor/teclado fijo del puesto 08 para que no se superponga con la laptop de la actividad, y se ocultan las piezas geométricas del asiento reemplazado. Los demás puestos del aula quedan visibles.
- La carpeta `Assets/school` no contiene un modelo humano identificado para Miguel; se conserva una cápsula provisional, se apoya en el piso, se orienta hacia el estudiante y se desplaza 0,6 m lateralmente para no bloquear la laptop.
- Los controles 3D quedan en el borde cercano del tablero; la tarjeta de nota se ubica al costado de la laptop. El panel de subtítulos se reduce y solo se activa mientras Yarn muestra líneas.
- El `XR Origin` y `XR Interaction Simulator` se toman de los samples XRI 3.5.1 incluidos en el proyecto para habilitar la validación posterior con teclado y ratón y conservar el camino a Quest.
- La elección espera 45 segundos sin mostrar cuenta regresiva, conforme a la situación descrita. La elección se conserva en memoria y se escribe en la consola para depuración; aún no se conecta al reporte.
- Miguel se representa inicialmente con una cápsula de sustitución. El diálogo y la escala 08/20 permanecen marcados como propuestas pendientes V-02/V-03.
- El XR Origin inicia a 3,1 m aproximadamente del profesor y mira hacia la laptop y el profesor; la escena mantiene el XR Interaction Simulator para PC.
- No se encontró Git ni `docs/PROGRESS.md` preexistentes en la copia del proyecto; se creó esta carpeta de seguimiento. El batch anterior dejó un proceso oculto (PID 38420, iniciado por esta tarea) reteniendo el proyecto. Se terminó ese proceso y se limpió `Temp/UnityLockfile`; después Unity completó el montaje con código de salida 0.
