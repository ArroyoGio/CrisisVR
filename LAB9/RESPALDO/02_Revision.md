# LAB 9 — Revisión con checklist y 3 hallazgos

**Fecha:** 20/10/2026 · **Alcance:** tutorial y actos del prototipo Crisis VR.
Estado de evidencia: los 3 hallazgos se verificaron por lectura de código y por artefacto compilado;
la validación final en Quest 3 queda **SIN VERIFICAR**.

---

## Checklist de revisión

| # | Criterio | ¿Se cumple? | Evidencia / motivo |
|---|---|---|---|
| C1 | Ninguna ruta bloquea el avance | Sí | `TutorialFlow` y `ActBase` no tienen rama de fin anticipado; todas las opciones continúan |
| C2 | El estudiante sabe en qué paso está | Sí (tras M1) | `TutorialPanel.SetProgress` → "Paso X de Y" |
| C3 | El panel no se superpone a otros elementos | Sí (tras M1) | Progreso reposicionado a (600,160), ver §Mejoras |
| C4 | Un panel mal configurado no bloquea la sesión | Sí (tras M2) | `Show`/`ShowHelp` toleran referencias nulas |
| C5 | Texto largo no se corta en el panel | Sí (tras M3) | Auto-ajuste acotado en `FitInstruction` |
| C6 | No se registran datos personales | Sí | Solo código de sesión seudonimizado |
| C7 | No se registran tiempos, puntuaciones ni valoraciones | Sí | `SessionRecorder` solo registra decisiones y metadatos |
| C8 | El tutorial no registra nada | Sí | Ningún `SessionRecorder` en `TutorialFlow` |
| C9 | Todo texto narrativo vive en `.yarn` o `ReportTexts.cs` | Parcial | Textos de actos en `.yarn`; los textos del panel del tutorial están en `TutorialExercises.cs` (lista editable) |
| C10 | Escenas sin referencias rotas | Sí | Auditoría de GUIDs sobre las escenas que existen (`Tutorial`, `Act1`, `BasicScene`, `SampleScene`) → 0 faltantes reales. `Main`, `Act2` y `Act3` no existen como archivos |
| C11 | Build Settings correcto | Corregido | Solo estaba `SampleScene.unity`; ahora `EditorBuildSetup.cs` añade `Tutorial`, `Act1` y `BasicScene` y deshabilita `SampleScene` al abrir Unity |
| C12 | Flujo de pausa/reinicio completo | Sí (código) | `SessionRecorder` + panel de pausa; **SIN VERIFICAR** en visor |

---

## Hallazgo 1 — El indicador de progreso se superponía a la marca del panel

**Severidad:** media · **Actividad:** tutorial E3

- **Comportamiento observado:** el texto de progreso se colocaba en `(470, 392)` sobre el panel de 1600×940.
  La marca "CRISIS VR" ocupa `(-40, 392)` con tamaño 1400×60: ambos rectángulos se solapan horizontalmente.
- **Impacto:** el estudiante ve texto superpuesto y ilegible; resta claridad al panel de un tutorial que
  debe ser autoexplicativo.
- **Causa:** posición inicial sin verificar contra el resto de elementos del panel.
- **Resolución (M1):** reposicionado a `(600, 160)` con tamaño `280×44` y alineación `MidlineRight`, zona
  libre entre la marca (y=392) y la instrucción (y=100).
- **Verificación:** revisión geométrica de posiciones + compilación sin errores.

## Hallazgo 2 — Un panel incompleto podía bloquear el tutorial

**Severidad:** alta · **Actividad:** tutorial E3

- **Comportamiento observado:** `TutorialPanel.Show` escribía directamente `heading.text`, `instruction.text`,
  `hand.text`, `practice.gameObject`, `continueButton.interactable`. Si alguno quedaba sin arrastrar en el
  Inspector, la llamada lanza `NullReferenceException` y el flujo se detiene.
- **Impacto:** bloqueo total del tutorial y, con él, de la sesión completa. Es el riesgo más grave del prototipo,
  porque el tutorial es la única puerta de entrada a los actos.
- **Causa:** sin defensa ante referencias sin asignar.
- **Resolución (M2):** cada escritura está protegida con guarda nula. `Show` y `ShowHelp` ya no pueden lanzar.
- **Verificación:** prueba `T05_Panel_SinReferencias_NoBloquea` (ver §Pruebas).

## Hallazgo 3 — Instrucciones largas se cortaban sin ajuste de fuente

**Severidad:** baja · **Actividad:** tutorial E3

- **Comportamiento observado:** `instruction` ocupa 670×260 con tamaño de fuente fijo. Instrucciones largas
  (p. ej. la de desplazamiento) exceden el área y quedan recortadas.
- **Impacto:** el estudiante no lee la instrucción completa y pide ayuda o avanza sin comprender.
- **Causa:** sin auto-ajuste de tamaño de texto.
- **Resolución (M3):** `FitInstruction` activa el auto-ajuste solo cuando la instrucción supera 160 caracteres,
  acotado entre 19 y 29 pt. Los textos cortos conservan su tamaño original.
- **Verificación:** revisión de umbrales + compilación sin errores.

---

## Hallazgo cerrado en esta iteración

**Build Settings:** solo `SampleScene.unity` estaba habilitada. Ahora el script
`Assets\_Project\Scripts\Build\Editor\EditorBuildSetup.cs` añade `Tutorial.unity`, `Act1.unity` y
`BasicScene.unity`, y deshabilita `SampleScene.unity` (sin borrar el archivo). Se ejecuta solo al
cargar el Editor y también desde `CrisisVR ▸ Build ▸ Añadir escenas al build`.

**Verificado en copia del proyecto (20/10/2026):** compiló sin errores y registró
`CrisisVR: build actualizado con 3 escenas propias`. **SIN VERIFICAR** en el Editor principal
de la desarrolladora.
