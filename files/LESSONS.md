# LESSONS.md — Memoria de QA de Crisis VR

El agente QA lee este archivo al empezar y lo amplía al terminar (formato en `QA_PROTOCOL.md` §6).
Una lección con estado HIPÓTESIS NO se aplica como regla hasta que se confirme con evidencia en este proyecto.

### L-001 — `-quit` junto a `-runTests`
- Fecha / tarea: inicial (sin tarea)
- Situación: ejecutar tests de Unity en batchmode
- Qué pasó (síntoma): HIPÓTESIS — según documentación general de Unity, combinar `-quit` con `-runTests` puede cortar la ejecución
- Causa real: sin confirmar en este proyecto
- Regla a aplicar en adelante: no usar `-quit` con `-runTests`; confirmar en la primera ejecución real
- Evidencia: ninguna todavía
- Veces confirmada: 0

### L-002 — Capturas necesitan renderizado
- Fecha / tarea: inicial (sin tarea)
- Situación: tests PlayMode con capturas de pantalla
- Qué pasó (síntoma): HIPÓTESIS — con `-nographics` no hay imagen útil
- Causa real: sin confirmar en este proyecto
- Regla a aplicar en adelante: no usar `-nographics` en la etapa de capturas y video; confirmar en la primera ejecución real
- Evidencia: ninguna todavía
- Veces confirmada: 0

### L-003 — Los tests deben estar en el modo de ejecución que usan
- Fecha / tarea: 2026-10-25 / QA exploratorio T-09 a T-13
- Situación: Unity Test Runner clasificó un test de agarre como EditMode.
- Qué pasó (síntoma): el test llamó `SceneManager.LoadScene` y falló antes de probar el agarre con `InvalidOperationException`.
- Causa real: el test está bajo `Assets/_Project/Tests/PlayMode/Editor/`, y Unity lo incluyó en EditMode aunque requiere Play Mode.
- Regla a aplicar en adelante: confirmar modo, assembly definition y ubicación de los tests en `mcpforunity://tests` antes de interpretar un fallo como bug de runtime.
- Evidencia: job `2877149b69a34922bb4f827252374928`; `CrisisVRGrabPlayTests.G01_AgarreConSimulador`.
- Veces confirmada: 1
