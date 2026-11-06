# LAB 9 — Auditoría de diseño (3 a 5 actividades)

**Proyecto:** Crisis VR · Unity 6.3 LTS (6000.3.9f1) · Meta Quest 3
**Fecha de auditoría:** 20/10/2026
**Fuentes:** `docs/GDD.txt` (v1.0), código en `Assets/_Project/` y escenas reales: `Tutorial.unity`, `Act1.unity`, `BasicScene.unity`, `SampleScene.unity` (en `Assets/Scenes/`). **Aclaración:** el GDD define además `Main`, `Act2` y `Act3`, pero esas escenas **no existen como archivos** en el proyecto; se auditan solo las que hay.
**Alcance:** se auditan 4 actividades del flujo (tutorial + tres actos).

> Regla de evidencia del proyecto: ninguna afirmación de "funciona" está probada en Quest 3.
> Lo que se compila se declara con la evidencia del artefacto compilado; lo que requiere el visor
> queda marcado **SIN VERIFICAR**.

---

## A1 — Tutorial de controles (escena `Tutorial`, etapa E3)

| Campo | Contenido |
|---|---|
| Objetivo | Enseñar raycast, selección, desplazamiento y pausa antes de los actos |
| Pasos | `TutorialStep`: Orientation, Movement, Turn, Interface, Object, Options, Grab, Wait, PauseInfo, Integration, Finished (11 pasos) |
| Textos | `Assets/_Project/Scripts/Dialogue/TutorialExercises.cs` (lista editable en Inspector) |
| Registro | Ninguno (GDD §1.2: "Nada de lo que se haga aquí se registra") |
| Salida | Según el GDD continúa a `Main`; en el proyecto actual la siguiente escena construida es `Act1` |

**Observaciones de auditoría**
- El orden lógico de enseñanza lo define `TutorialSequenceUpgrade.StepOrder` (Editor), no la lista del Inspector: hay dos fuentes de verdad para el orden.
- `TutorialFlow.cs` controla avance/repetición/ayuda. El avance nunca se bloquea: repetir y continuar están siempre disponibles.
- El panel enseña el avance "Paso X de Y" (implementado en `TutorialPanel.SetProgress`, añadido en esta iteración).

**Evidencia de estado**
- Escena `Tutorial.unity`: 0 GUIDs de referencia faltantes (auditoría de dependencias, 20/10/2026).
- Prefab `TutorialRoot.prefab` (GUID `2ec5ddb429718ad4c83b4307108b46b3`): 0 referencias en escena, el tutorial está desagrupado (unpacked) en la jerarquía.
- **SIN VERIFICAR:** comportamiento real en Quest 3 (colisiones, confort de desplazamiento, mando).

---

## A2 — Acto 1 «Entrega del proyecto» (autorregulación emocional)

| Campo | Contenido |
|---|---|
| Objetivo | Registrar si el estudiante actúa tras una retroalimentación negativa |
| Situación | Proyecto con 08/20 y razones del docente Miguel (GDD §2.1) |
| Rutas | A: CORREGIR PROYECTO · B: sin acción durante el tiempo de espera de diseño |
| Registro | `Reintento` (true/false), metadato `Acto1Ruta` (Corregir/NoActuo), `Acto1Completado` (true en ambas) |
| Frase de reporte | Descriptiva, sin valoración (GDD §2.1, tabla "Información descriptiva") |

**Observaciones de auditoría**
- Ambas rutas llegan al Acto 2: no existe rama de bloqueo.
- En el momento de decisión no se muestra instrucción (diseño anti-inducción), solo el botón.
- **SIN VERIFICAR:** generación real del registro en `persistentDataPath` durante una sesión en Quest 3.

---

## A3 — Acto 2 «Aviso de cambio» (adaptabilidad)

| Campo | Contenido |
|---|---|
| Objetivo | Registrar la reacción ante un cambio de modalidad y de grupo |
| Situación | Aviso de práctica virtual; compañero nuevo Rodrigo (GDD §2.2) |
| Objetos | `folder_plan`, `tablet_plan`, panel de aviso |
| Registro | `OpcionAdaptabilidad` + metadato de opción |
| Avance | Avanza al Acto 3 al terminar el nodo de reacción, sea cual sea la opción |

**Observaciones de auditoría**
- Hay tiempo de espera con salida por inacción: no actuar también es una decisión válida registrada.
- **SIN VERIFICAR:** interacción de agarrar la carpeta/tablet con los controles en Quest 3.

---

## A4 — Acto 3 «Reunión de equipo» (trabajo en equipo)

| Campo | Contenido |
|---|---|
| Objetivo | Registrar la opción de diálogo elegida ante un conflicto de equipo |
| Situación | Reunión virtual; Lucía y Mateo no avanzan (GDD §2.3) |
| Opciones | A repartir tareas · B ayudar a Lucía · C excluir a Mateo · D esperar |
| Registro | `OpcionTrabajoEnEquipo` + metadato de opción elegida |
| Instrucción | Solo a los 90 s sin selección aparece "Selecciona una opción para continuar" (no se registra) |

**Observaciones de auditoría**
- El reporte reproduce el texto literal de la opción, sin etiquetas de valoración ("mediador", "excluyente"): correcto según alcance.
- Las 4 opciones continúan al cierre; ninguna rama termina la sesión antes de tiempo.
- **SIN VERIFICAR:** reproducción de voz de la opción elegida en Quest 3.

---

## Resumen de cobertura

| Actividad | Escena | Riesgo residual | Estado de compilación |
|---|---|---|---|
| Tutorial E3 | `Tutorial.unity` | Medio: confort y mando | Escena existe; script del tutorial compilado (evidencia §Mejoras) |
| Acto 1 | `Act1.unity` | Alto: flujo Yarn completo | Escena existe |
| Acto 2 | *(no existe)* | — | **No creado**; el GDD lo define, el proyecto no |
| Acto 3 | *(no existe)* | — | **No creado**; el GDD lo define, el proyecto no |

**Aclaración:** `BasicScene.unity` y `SampleScene.unity` también existen; la primera no se usa en el flujo del GDD y la segunda es la escena de plantilla de Unity.

**Pendiente de la desarrolladora:** ejecutar cada actividad en Quest 3 con el checklist de la §Revisión.
