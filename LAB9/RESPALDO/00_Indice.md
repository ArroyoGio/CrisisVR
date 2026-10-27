# LAB 9 — Índice de la carpeta

**Fecha:** 20/10/2026 · **Proyecto:** Crisis VR (Meta Quest 3, Unity 6000.3.9f1)

## Estructura final

```
LAB9\
├── LAB9_Informe.pdf      ← INFORME PRINCIPAL (entregable, 2 de 3 páginas)
├── TRELLO_IMPORT.csv      ← 4 tareas importables a Trello (Menú ▸ Crear ▸ Importar)
├── VIDEO_GUION.md         ← Guion del video (parte de la entrega)
├── EVIDENCIAS\            ← XML y log reales + los nombres de lo que falta
└── RESPALDO\              ← documentos de trabajo (no se entregan)
```

## Entregables

| Archivo | Contenido |
|---|---|
| `LAB9_Informe.pdf` | Informe de 3 páginas con las tablas A1 (auditoría), A2 (checklist y 3 hallazgos), A3 (mejoras), A4 (KPI, casos, criterios y agenda) y A5 (8 pruebas), más tareas y conclusiones |
| `TRELLO_IMPORT.csv` | Las 4 tareas en formato importable a Trello; las tareas 1 y 4 están hechas |
| `VIDEO_GUION.md` | Guion de video de 5 a 7 minutos, escena por escena |
| `EVIDENCIAS\tests_editmode.xml` y `editmode.log` | Evidencia real: 8 de 8 pruebas aprobadas (20/10/2026) |

## Respaldo (documentos de trabajo)

| Archivo | Contenido |
|---|---|
| `00_Indice.md` | Este índice |
| `01_Auditoria.md` | Auditoría detallada de tutorial E3 y actos 1, 2 y 3 |
| `02_Revision.md` | Checklist de 12 criterios + 3 hallazgos con causa e impacto |
| `03_Mejoras.md` | M1, M2 y M3 con riesgo, mitigación y evidencia de compilación |
| `04_PlanDePruebas.md` | 3 KPI, 3 casos, instrumentos, participantes, criterios y cronograma |
| `05_PruebasAutomatizadas.md` | 8 pruebas ejecutadas y resultado real: 8 de 8 aprobadas |
| `TRELLO_TAREAS.md` | 4 tareas listas para importar en Trello |
| `VIDEO_GUION.md` | Guion de video de 5 a 7 minutos, escena por escena |
| `generar_pdf.py` | Regenera el PDF (solo Python estándar); escribe en `LAB9\` |
| `validar_pdf.py` | Comprueba estructura, máximo 3 páginas y los 10 apartados obligatorios |

## Evidencias del proyecto (respaldo)

| Ubicación | Contenido |
|---|---|
| `TestResults\Lab9\tests_editmode.xml` | Resultado real de las 8 pruebas EditMode (20/10/2026, 21:12) |
| `TestResults\Lab9\editmode.log` | Registro completo de la ejecución en Unity |
| `TestResults\*.ps1` | Scripts de diagnóstico usados (compilación, APIs, GDD) |

## Código tocado en esta iteración

| Archivo | Cambio |
|---|---|
| `Assets\_Project\Scripts\Dialogue\TutorialPanel.cs` | M1 progreso, M2 guardas nulas, M3 auto-ajuste |
| `Assets\_Project\Tests\EditMode\TutorialFunctionalTests.cs` | 5 pruebas nuevas (T01–T05) |
| `Assets\_Project\Scripts\Build\Editor\EditorBuildSetup.cs` | Nuevo: añade las escenas propias al build al abrir Unity |

## Resultado y estado

- **Compilación:** verificada por artefacto y por ejecución en copia; sin errores de compilación.
- **Pruebas:** 8 de 8 aprobadas en EditMode (3 previas + 5 nuevas), 20/10/2026 21:56.
- **Build Settings:** script implementado y verificado en copia (`build actualizado con 3 escenas`).
- **PDF:** 2 páginas, con los 10 apartados obligatorios presentes.
- **Estado global:** **SIN VERIFICAR** en Quest 3.
