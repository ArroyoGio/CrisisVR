# AGENTS.md — Crisis VR (reglas para OpenCode)

Estas reglas se aplican a toda tarea. Si una instrucción del usuario las contradice, avisa antes de actuar.

## 1. Contexto
Crisis VR es un prototipo de Realidad Virtual para Meta Quest 3, hecho en Unity 6.3 LTS (6000.3) con URP y C#.
Es una tesis de TECSUP. Registra las decisiones de un estudiante de primer ciclo en tres situaciones académicas
y genera un reporte descriptivo de texto para el área de Psicopedagogía.
Diseño fuente: `docs/GDD.txt` (copia de texto del GDD v1.0). Lee la parte que corresponda a la tarea; no la reescribas.

## 2. Estado real del proyecto
- El proyecto Unity existe pero NO tiene desarrollo. No hay prototipo funcional.
- El código del GDD (Parte 4) es un borrador que nunca se compiló ni se probó. Úsalo como guía, no como código correcto.
- Antes de crear nada, inspecciona qué hay en `Assets/` y `Packages/`. No sobrescribas lo existente.

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
- No puedes ejecutar Unity ni probar en el Quest. No abras Unity ni intentes compilar.
- Nunca escribas que algo "funciona", "compila" o "está probado". Usa siempre: **SIN VERIFICAR**.
- Solo la desarrolladora marca una tarea como verificada, con evidencia (captura, log o archivo).
- No inventes nombres de API. Si dudas de una clase o método de Meta XR, Yarn Spinner o Unity, escribe `// VERIFICAR: <duda>` y sigue.
- No inventes resultados de pruebas ni datos de ejemplo presentados como reales.

## 5. Reglas de edición
- No modifiques `ProjectSettings/`, `Packages/manifest.json` ni archivos `.meta` salvo que la tarea lo pida.
- No borres archivos. Si algo sobra, propónlo.
- Estructura de carpetas: la de GDD §4.2 bajo `Assets/_Project/`. Namespace `CrisisVR`.
- Scripts pequeños, con una responsabilidad, comentarios breves en español.
- Todo texto narrativo (diálogos, opciones, frases del reporte, preguntas) va en `.yarn` o en `Report/ReportTexts.cs`,
  nunca dentro de la lógica, y lleva el comentario `// PROPUESTA – VALIDAR CON CLIENTA`.
- No configures escenas ni prefabs a mano en archivos `.unity` o `.prefab`. Esos pasos los hace la desarrolladora
  en el Editor; tú escribes el script y listas qué debe arrastrar y dónde.
- Mantén `.gitignore` de Unity (Library, Temp, Logs, UserSettings, obj, Builds, `*.csproj`, `*.sln`).

## 6. Seguridad y costo
- No uses claves de API, pagos ni servicios externos. No agregues dependencias que no estén en la tarea.
- No pongas datos de estudiantes, nombres ni correos en el repositorio, en prompts ni en ejemplos.
- No hagas `git push` ni cambies el remoto sin que se te pida.

## 7. Cómo trabajar
- Una tarea a la vez, con ID (T-xx, del GDD Parte 7). No adelantes tareas siguientes.
- Si la tarea es ambigua o falta un dato, haz UNA pregunta y espera.
- Un commit local por tarea con el mensaje `T-xx: descripción corta` solo cuando la desarrolladora lo pida.

## 8. Formato obligatorio de tu respuesta (máximo 15 líneas)
1. Archivos creados o modificados.
2. Pasos de la desarrolladora en el Editor (qué arrastrar y dónde).
3. Cómo comprobar en 3 pasos.
4. Dudas o `VERIFICAR` pendientes.
5. Estado: **SIN VERIFICAR**.
