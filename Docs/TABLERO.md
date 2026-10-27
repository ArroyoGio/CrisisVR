# Crisis VR — Tablero de control
Actualizado: domingo 11/10/2026 · Entrega: **sábado 31/10/2026** · Visor: lunes a viernes en la institución

## Estado general
Desarrollo desde cero. Sin prototipo, sin pruebas y sin evidencias todavía. Tesis redactada hasta el Capítulo III.

## Registro de decisiones
| ID | Decisión | Estado |
|---|---|---|
| DEC-001 | Orden: Acto 1 con reporte (Hito 1) → tres actos (Hito 2) → marco mínimo → pruebas | Tomada |
| DEC-002 | Entrega el 31/10/2026 (reemplaza al 22/10). Fecha institucional por confirmar | Provisional |
| DEC-003 | Unity 6.3 LTS (6000.3). La documentación de Unity confirma soporte para Quest 3; Meta recomienda 6.1 o posterior. Versión de parche y del Meta XR SDK: anotar al instalar | Provisional |
| DEC-004 | El GDD es la única fuente de diseño; sus textos son propuestas sin validar | Tomada |
| DEC-005 | Pruebas con estudiantes solo con consentimiento y autorización institucional. Se decide a más tardar el 20/10 | Provisional |
| DEC-006 | Nada se cierra sin evidencia (captura, log, archivo o commit) | Tomada |
| DEC-007 | Cero gasto: sin tarjetas, suscripciones ni API de pago. OpenCode con un modelo gratuito (por verificar al configurarlo). Sin datos de estudiantes en prompts | Tomada |
| DEC-008 | Reparto: Claude (chat) diseña, escribe y revisa; OpenCode escribe código en el repositorio; tú operas Unity, el Quest, Git y las reuniones | Tomada |
| DEC-009 | Sábado y domingo: trabajo sin visor (código y redacción). Pruebas en el Quest solo lunes a viernes | Tomada |
| DEC-010 | Una sola tarea activa a la vez; avanzamos cuando confirmas | Tomada |
| DEC-011 | Congelamiento de funciones: viernes 23/10 | Provisional |
| DEC-012 | Este archivo se guarda en el repositorio (docs/) para poder retomar si el chat gratuito llega a su límite | Tomada |

## Tablero de tareas
Responsable: **Tú** · **Claude** (chat) · **OpenCode**

| ID | Entregable | Resp. | Estado | Depende de | Criterio de aceptación |
|---|---|---|---|---|---|
| T-01 | Unity 6.3 con módulos Android y plataforma Android activa | Tú | EN CURSO | — | Captura de Build Profiles con Android activo; consola sin errores |
| T-02 | Meta XR Core SDK, OpenXR, Project Setup Tool y cubo en el Quest | Tú | Pendiente | T-01, visor | Foto o video del cubo en el visor |
| T-03 | OpenCode instalado con modelo gratuito y AGENTS.md en el repositorio | Tú | Pendiente | T-01 | `opencode` responde sin pedir pago |
| T-04 | Repositorio Git con .gitignore y primer commit | Tú | Pendiente | T-01 | Historial con commit |
| T-05 | Acto 1 completo con reporte (Hito 1) | OpenCode + Tú | Pendiente | T-02, T-03 | Reporte .txt coherente con la decisión tomada en el Quest |
| T-06 | Acto 2 | OpenCode + Tú | Pendiente | T-05 | Tres ramas probadas |
| T-07 | Acto 3 y sesión completa (Hito 2) | OpenCode + Tú | Pendiente | T-06 | Reporte con las cuatro opciones |
| T-08 | Marco: bienvenida, tutorial, pausa, cierre | OpenCode + Tú | Pendiente | T-07 | Sesión de principio a fin |
| T-09 | Pruebas CP-01 a CP-13 con evidencia | Tú | Pendiente | T-08 | Tabla 12 con resultados reales |
| A-01 | Reunión con la clienta (checklist C-01 a C-12 del GDD) | Tú | Pendiente | fecha | Respuestas por escrito |
| A-02 | Corrección estructural de la tesis (índice, plantilla, Cap. I, Tabla 4, presupuesto, citas) | Claude + Tú | Pendiente | plantilla TECSUP | Sin restos de plantilla |
| A-03 | Consentimiento informado y autorización | Claude (borrador) + Tú | Pendiente | reglamento | Documento aprobado |
| A-04 | Capítulo IV con capturas y commits reales | Claude + Tú | Pendiente | T-05 en adelante | Sin marcas PENDIENTE |
| A-05 | Capítulo V con resultados reales | Claude + Tú | Pendiente | T-09 | Resultados de la Tabla 12 |
| A-06 | Conclusiones, recomendaciones y anexos | Claude + Tú | Pendiente | A-04, A-05 | Cada conclusión ligada a evidencia |
| A-07 | Revisión cruzada tesis ↔ GDD ↔ software | Claude | Pendiente | todo | Cero contradicciones |

## Cronograma diario (visor solo lunes a viernes)
| Fecha | Con visor / Unity | Sin visor / tesis | Hito |
|---|---|---|---|
| Dom 11 | — | T-01 en casa | — |
| Lun 12 | T-02: Meta XR y cubo en el Quest | T-04 Git | Hito 0 |
| Mar 13 | Rayo y botón en el Quest | T-03 OpenCode; plantilla TECSUP | — |
| Mié 14 | Acto 1: escena y diálogos | Prompt a OpenCode | — |
| Jue 15 | Acto 1: registro y reporte en el Quest | — | Hito 1 |
| Vie 16 | Acto 2 en el Quest | A-02 estructura de la tesis | — |
| Sáb 17 | — | Acto 3 en código (OpenCode); A-03 consentimiento | — |
| Dom 18 | — | Sesión completa en Editor; A-04 borrador | — |
| Lun 19 | Sesión completa en el Quest | — | Hito 2 |
| Mar 20 | Marco: bienvenida, tutorial, pausa, cierre | Decisión DEC-005 | — |
| Mié 21 | Pruebas CP-01 a CP-13 (primera mitad) | — | — |
| Jue 22 | Pruebas CP restantes y correcciones | A-01 clienta (si hay fecha) | — |
| Vie 23 | Build final y capturas de evidencia | Congelamiento de funciones | Prototipo cerrado |
| Sáb 24 | — | A-04 Capítulo IV con evidencia | — |
| Dom 25 | — | A-05 Capítulo V con resultados | — |
| Lun 26 | Repetir CP en el build final / sesión con Psicopedagogía | — | — |
| Mar 27 | Reserva (correcciones) | Análisis de fichas | — |
| Mié 28 | — | A-06 conclusiones y anexos | — |
| Jue 29 | — | A-07 revisión cruzada | — |
| Vie 30 | Verificación final del build | Documento congelado | — |
| Sáb 31 | — | Entrega | **Entrega** |

## Bloqueos activos
1. **Dónde corre Unity en la institución.** Si debes usar un PC de la institución, hay que instalar Unity 6.3 allí (descarga grande) y mover el proyecto con Git. Define el plan de T-02.
2. **Cada día sin visor no se puede probar.** El Hito 1 (jueves 15) no tiene colchón.
3. **Falta la plantilla y el reglamento de TECSUP** (bloquea A-02).
4. **Sin fecha con la clienta** (bloquea A-01 y los textos finales).
5. **Modelo gratuito de OpenCode sin verificar:** puede tener límites de uso o registrar prompts. Plan B: yo escribo el código en el chat y tú lo pegas.

## Prompts de OpenCode
**P-00 (diagnóstico, no modifica nada; úsalo cuando T-03 esté listo):**
```
Lee AGENTS.md. No modifiques ningún archivo. Inspecciona este proyecto Unity y responde en máximo 15 líneas:
1) versión de Unity en ProjectSettings/ProjectVersion.txt; 2) paquetes de Packages/manifest.json relacionados con XR, Meta, OpenXR, Yarn o URP;
3) carpetas y escenas de Assets/; 4) si existe .gitignore de Unity; 5) qué falta respecto a la estructura de docs/GDD.txt §4.2. Estado: SIN VERIFICAR.
```
