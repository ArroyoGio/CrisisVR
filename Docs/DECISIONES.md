# Decisiones del prototipo

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
