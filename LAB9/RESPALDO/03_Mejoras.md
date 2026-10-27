# LAB 9 — Mejoras implementadas (2 a 3)

**Fecha:** 20/10/2026 · **Archivo tocado:** `Assets\_Project\Scripts\Dialogue\TutorialPanel.cs`
Compilación verificada por artefacto: el Editor de Unity recompiló el proyecto el 20/10/2026 y los
ensamblados contienen los miembros nuevos (evidencia al final).
La corrección final en Quest 3 queda **SIN VERIFICAR**.

---

## M1 — Indicador de avance "Paso X de Y" (resuelve el Hallazgo 1)

- **Qué cambia:** se añade el campo opcional `progress` (`TMP_Text`), el método `SetProgress(index, total)`
  y la creación automática del texto en `EnsureProgress()` cuando el campo queda vacío.
- **Posición final:** `(600, 160)`, tamaño `280×44`, alineación `MidlineRight`, color `(0.60, 0.70, 0.78)`.
  Zona libre: no se superpone a la marca (y=392), a la instrucción (y=100) ni a los botones (y=-388).
- **Compatibilidad:** se conserva `Show(exercise, right, showPractice)` con 3 argumentos; el overload de
  5 argumentos es nuevo. Los 3 llamadores existentes (`TutorialSceneSetup` líneas 61 y 193,
  `TutorialVisualUpgrade` línea 46) siguen compilando sin cambios.
- **Riesgo:** el texto añadido podría competir visualmente con el título. **Mitigación:** tamaño menor
  (25 pt frente a 34 pt del título) y color apagado.
- **No rompe:** ningún texto narrativo se mueve a código; el formato queda en una constante editable.
- **Validar con clienta:** el texto literal "Paso X de Y" (propuesto).

## M2 — Defensa ante referencias sin asignar (resuelve el Hallazgo 2)

- **Qué cambia:** `Show(...)` y `ShowHelp(...)` protegen cada escritura con guarda nula
  (`if (heading != null) ...`). `SetProgress` ignora índices inválidos (`index < 0 || total <= 0`).
- **Por qué:** un panel con un campo sin arrastrar en el Inspector lanzaba `NullReferenceException`
  dentro del tutorial, y el tutorial es la única entrada a los actos: el fallo detenía la sesión completa.
- **Riesgo:** un error de configuración deja de verse en consola y pasa a silencio. **Mitigación:** el
  panel conserva toda su función cuando está completo; solo deja de fallar en seco.
- **Verificación:** prueba `T05` (ver `05_PruebasAutomatizadas.md`).

## M3 — Auto-ajuste acotado de la instrucción (resuelve el Hallazgo 3)

- **Qué cambia:** método privado `FitInstruction()`. Si `instruction.text` supera 160 caracteres, activa
  `enableAutoSizing` con `fontSizeMin = 19` y `fontSizeMax = 29`; si no, lo desactiva.
- **Por qué:** el área de instrucción es 670×260 y las instrucciones largas (p. ej. la de desplazamiento)
  se cortaban.
- **Riesgo:** cambios de legibilidad en textos medianos. **Mitigación:** umbral alto (160 caracteres) y
  límites de fuente estrechos; textos cortos quedan exactamente igual.
- **Reversible:** basta borrar la llamada a `FitInstruction()` en `Show(...)`.

---

## Evidencia de compilación (20/10/2026)

Verificado leyendo los ensamblados con Mono.Cecil 0.11.1:

| Ensamblado | Fecha | Miembros nuevos presentes |
|---|---|---|
| `Assembly-CSharp.dll` | 20/10/2026 21:12 | `Show(TutorialExercise, Boolean, Boolean)`, `Show(..., Int32, Int32)`, `SetProgress(Int32, Int32)`, campo `progress : TMP_Text` |
| `CrisisVR.Tests.EditMode.dll` | — | `T01`…`T05` (ver §Pruebas) |

APIs de TextMeshPro usadas (`enableAutoSizing`, `fontSizeMin`, `fontSizeMax`, `text`, `fontSize`,
`fontSharedMaterial`, `alignment`, `raycastTarget`) confirmadas por reflexión de ensamblado.
**SIN VERIFICAR:** aspecto final en el visor y comportamiento en Quest 3.
