# LAB 9 — 4 tareas de Trello

Importar en Trello (Menú ▸ Crear ▸ Importar desde Trello ▸ pegar este texto) o crearlas a mano.
Asignadas a la desarrolladora. **Estas acciones no se pueden hacer desde el código.**

---

## Tarea 1 — Escenas en el Build Settings — **HECHA**

**Lista:** Hechas · **Prioridad:** Alta · **20/10/2026**

Implementada en el proyecto por `Assets\_Project\Scripts\Build\Editor\EditorBuildSetup.cs`:
al cargar el Editor añade `Assets/Scenes/Tutorial.unity`, `Act1.unity` y `BasicScene.unity`
al Build Settings y deshabilita `SampleScene.unity` (no borra el archivo). También existe el
menú manual `CrisisVR ▸ Build ▸ Añadir escenas al build`.

**Verificado en copia (20/10/2026):** compiló y registró
`CrisisVR: build actualizado con 3 escenas propias`. **SIN VERIFICAR** en el Editor principal.

**Queda solo revisar:** abrir `File ▸ Build Settings` y confirmar las 3 escenas activas.
`Main`, `Act2` y `Act3` **no existen como archivos** en el proyecto (el script lo avisa); se
crean cuando haya diseño de esos actos.

---

## Tarea 2 — Ejecutar las 5 pruebas EditMode en el Editor

**Lista:** En curso · **Prioridad:** Alta

- Abrir el proyecto en Unity 6000.3.9f1.
- `Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All`.
- Verificar que `TutorialFunctionalTests` (T01…T05) aparecen con verde.
- Si alguna falla, copiar el mensaje completo de la pestaña *Error* antes de tocar nada.

**Criterio de terminado:** 5 de 5 en verde, captura adjunta en `LAB9/EVIDENCIAS/`.

---

## Tarea 3 — Prueba en Quest 3 con participantes

**Lista:** Pendiente · **Prioridad:** Alta

- Instalar el APK en el Quest 3.
- Ejecutar CP-01, CP-02 y CP-03 del `04_PlanDePruebas.md` con 2 a 3 participantes.
- Rellenar la checklist de observación y el formulario post-sesión.
- Copiar el reporte `.txt` generado desde `persistentDataPath` (Meta Quest Developer Hub o `adb pull`).

**Criterio de terminado:** 3 checklists + 3 reportes `.txt` + tabulación de K1, K2 y K3.

---

## Tarea 4 — Video y capturas de evidencia

**Lista:** Pendiente · **Prioridad:** Media

- Grabar el video siguiendo `VIDEO_GUION.md` (5 a 7 minutos).
- Capturar pantalla de: panel del tutorial con "Paso X de Y", Test Runner en verde, reporte generado.
- Colocar todo en `LAB9/EVIDENCIAS/` con la fecha en el nombre.
- Entregar el PDF `LAB9_Informe.pdf` (máximo 3 páginas).

**Criterio de terminado:** video + 3 capturas + PDF entregados.
