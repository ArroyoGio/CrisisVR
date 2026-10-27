# AGENTS.md — Crisis VR (reglas para OpenCode y Codex)

Este archivo lo leen por igual OpenCode y Codex. Todo el proyecto se desarrolla con agentes.
Estas reglas se aplican a toda tarea. Si una instrucción del usuario las contradice, avisa antes de actuar.

## 1. Contexto
Crisis VR es un prototipo de Realidad Virtual para Meta Quest 3, hecho en Unity 6.3 LTS (6000.3) con URP y C#.
Es una tesis de TECSUP. Registra las decisiones de un estudiante de primer ciclo en tres situaciones académicas
y genera un reporte descriptivo de texto para el área de Psicopedagogía.
Diseño fuente: `docs/GDD.txt` (copia de texto del GDD v1.0). Lee la parte que corresponda a la tarea; no la reescribas.

## 2. Estado real del proyecto
- El proyecto Unity existe pero NO tiene desarrollo. No hay prototipo funcional.
- El código del GDD (Parte 4) es un borrador que nunca se compiló ni se probó. Úsalo como guía, no como código correcto.
- Antes de crear nada, inspecciona `Assets/`, `Packages/` y `git status`. No sobrescribas lo existente.
- El estado vivo del proyecto está en `docs/PROGRESS.md` (ver §8). Léelo al empezar y actualízalo al terminar.

## 3. Alcance (no negociable)
Dentro: tres actos secuenciales (Acto 1 autorregulación, Acto 2 adaptabilidad, Acto 3 trabajo en equipo),
registro de decisiones y reporte `.txt` en `Application.persistentDataPath`.
Variables registradas: `Reintento`, `OpcionAdaptabilidad`, `OpcionTrabajoEnEquipo` y metadatos de sesión
(código seudonimizado, fecha, versión de la app, estado de finalización).

Prohibido añadir, aunque parezca útil:
- Tiempos de respuesta, mirada, movimiento (el único tiempo permitido es el tiempo de espera de diseño del GDD).
- Puntajes, niveles, clasificaciones o mensajes de valoración dentro de la simulación.
- IA, texto generado, llamadas de red, multijugador, analítica, voz o texto libre del estudiante.
- Nombres reales o datos personales. Solo código de sesión seudonimizado.
- Otras competencias, más actos o más escenas de los definidos.

Ninguna ruta puede bloquear el avance: todas las opciones continúan al siguiente acto.

## 4. Verdad y evidencia
Hay cuatro niveles de estado. Usa solo el que puedas respaldar y nunca subas de nivel sin la evidencia indicada.

| Estado | Cuándo se puede usar | Evidencia obligatoria |
|---|---|---|
| **SIN VERIFICAR** | Código escrito, nada ejecutado | Ninguna |
| **COMPILA** | Compilación en batchmode terminó con código de salida 0 y sin errores de C# | Log guardado en `docs/evidence/T-xx_build.log` + comando usado |
| **PROBADO EN EDITOR** | Tests EditMode/PlayMode pasan en batchmode | `docs/evidence/T-xx_tests.xml` + resumen de resultados |
| **PROBADO EN QUEST** | Solo lo marca la desarrolladora | Captura, video, log o archivo del dispositivo |

Reglas:
- Está prohibido escribir "funciona", "probado" o "listo" sin el estado de la tabla y su evidencia.
- No puedes probar en el Meta Quest 3 ni juzgar rendimiento, confort ni interacción en VR. Eso es siempre PROBADO EN QUEST y es de la desarrolladora.
- No inventes nombres de API. Si dudas de una clase o método de Meta XR, Yarn Spinner o Unity, consulta la documentación oficial o el código del paquete en `Library/PackageCache/` y confírmalo. Si sigue la duda, escribe `// VERIFICAR: <duda>`.
- No inventes resultados de pruebas ni datos de ejemplo presentados como reales.
- Si un comando falla, pega el error real. No lo ocultes ni lo "arregles" desactivando tests o advertencias.

## 5. Qué puedes hacer (ya no hay pasos manuales por defecto)
Los agentes construyen el proyecto completo. No dejes trabajo a la desarrolladora si puedes hacerlo tú.

**Unity por línea de comandos**
- Usa Unity en batchmode: `"$UNITY_PATH" -batchmode -nographics -quit -projectPath . -logFile <log> [-executeMethod ...]`.
- La ruta del editor está en la variable de entorno `UNITY_PATH`. Si no existe o la licencia falla, haz UNA pregunta y espera; no intentes instalar Unity.
- No abras el Editor con interfaz. Nunca dos instancias de Unity sobre el mismo proyecto a la vez (OpenCode y Codex no pueden ejecutar Unity en paralelo; ver §7).

**Escenas, prefabs y configuración**
- Crea escenas, prefabs, materiales y objetos mediante scripts de Editor en `Assets/_Project/Editor/` (por ejemplo `CrisisVR/Build/...` con `[MenuItem]`) y ejecútalos con `-executeMethod`. Es más reproducible que editar YAML.
- No edites a mano archivos `.unity` ni `.prefab`, salvo un cambio mínimo y revisable que no pueda hacerse por script. Explica por qué.
- Los scripts de Editor deben ser idempotentes: volver a ejecutarlos no duplica objetos ni rompe referencias.

**Paquetes y ajustes**
- Puedes modificar `Packages/manifest.json` y `ProjectSettings/` cuando la tarea lo requiera (Meta XR, Yarn Spinner, URP, Android/Quest build settings, etc.).
- Solo paquetes necesarios para el GDD §4 o pedidos en la tarea. Fija versión exacta, verifícala en el registro/documentación oficial y repórtala en `docs/PROGRESS.md`.
- Antes de tocar `ProjectSettings/` o `manifest.json`, anota qué cambias y por qué. Mantén el diff pequeño.

**Archivos `.meta`**
- Nunca los escribas a mano. Deja que Unity los genere y inclúyelos en el commit junto a su asset.

**Tests**
- Escribe tests con Unity Test Framework (EditMode para lógica y reporte; PlayMode para flujo de actos cuando sea posible) y ejecútalos en batchmode.
- La lógica de decisiones y generación del reporte debe poder probarse sin hardware VR.

**Builds**
- Puedes generar un build Android/Quest por script (`-executeMethod`) y reportar si termina. Instalar y probar en el casco es de la desarrolladora.

**Borrado**
- Puedes borrar archivos que tú mismo creaste en la tarea actual. Para cualquier otro archivo, proponlo y espera confirmación.

## 6. Reglas de código y contenido
- Estructura de carpetas: la de GDD §4.2 bajo `Assets/_Project/`. Namespace `CrisisVR`.
- Scripts pequeños, con una responsabilidad, comentarios breves en español.
- Todo texto narrativo (diálogos, opciones, frases del reporte, preguntas) va en `.yarn` o en `Report/ReportTexts.cs`, nunca dentro de la lógica, y lleva el comentario `// PROPUESTA – VALIDAR CON CLIENTA`.
- No cambies el texto narrativo ya validado. Si una frase te parece mal, avisa.
- Mantén `.gitignore` de Unity (Library, Temp, Logs, UserSettings, obj, Builds, `*.csproj`, `*.sln`).

## 7. Seguridad, costo y coordinación entre agentes
- No uses claves de API, pagos ni servicios externos. Ninguna llamada de red en el juego.
- No pongas datos de estudiantes, nombres ni correos en el repositorio, en prompts ni en ejemplos.
- No hagas `git push` ni cambies el remoto sin que se te pida.
- **Un solo agente activo a la vez** sobre el proyecto. Antes de empezar, revisa `docs/PROGRESS.md` y `git status`; si hay cambios sin commit que no son tuyos, para y avisa.
- Una rama por tarea: `T-xx-descripcion`. Un commit local al terminar una tarea verificada al nivel que corresponda, con el mensaje `T-xx: descripción corta`. No fusiones a `main` sin que se pida.
- Si OpenCode y Codex no coinciden en una decisión, no la reescribas: regístrala en `docs/DECISIONES.md` y pregunta.

## 8. Cómo trabajar
- Una tarea a la vez, con ID (T-xx, del GDD Parte 7). No adelantes tareas siguientes.
- Si la tarea es ambigua o falta un dato, haz UNA pregunta y espera.
- Al empezar: lee `docs/PROGRESS.md`, la sección del GDD de la tarea y revisa `git status`.
- Al terminar: actualiza `docs/PROGRESS.md` con ID, agente que la hizo (OpenCode o Codex), estado según §4, archivos de evidencia y pendientes.
- Registra decisiones técnicas relevantes en `docs/DECISIONES.md` (fecha, decisión, motivo). Se usarán como evidencia metodológica de la tesis.
- Registra qué se hizo con apoyo de IA (herramienta, modelo, tarea). La tesis debe poder declarar el uso de IA con transparencia. No inventes ni embellezcas ese registro.

## 8b. Pruebas y QA
- Las pruebas completas (tests, capturas, video, reportes, bugs) siguen `docs/QA/QA_PROTOCOL.md`.
- En OpenCode, usa el agente `qa-tester` (`.opencode/agents/qa-tester.md`). En Codex, pide: "haz QA de T-xx siguiendo docs/QA/QA_PROTOCOL.md".
- El agente que implementó una tarea NO es quien la da por probada: la prueba la hace una sesión de QA aparte.
- Al empezar cualquier sesión de QA, lee `docs/QA/LESSONS.md`; al terminar, amplíalo. Los bugs van en `docs/QA/BUGS.md`.

## 9. Formato obligatorio de tu respuesta (máximo 15 líneas)
1. Archivos creados o modificados.
2. Comandos ejecutados y resultado real (código de salida, errores).
3. Evidencia generada (rutas en `docs/evidence/`).
4. Pasos pendientes de la desarrolladora (solo lo que un agente no pueda hacer: probar en Quest, validar con la clienta, decisiones).
5. Dudas o `VERIFICAR` pendientes.
6. Estado según §4: **SIN VERIFICAR**, **COMPILA** o **PROBADO EN EDITOR**.
