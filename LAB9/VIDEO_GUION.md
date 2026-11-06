# LAB 9 — Guion de video (5 a 7 minutos)

**Formato:** grabación de pantalla + toma del visor. Sin datos personales de participantes.
**Audio:** voz de la desarrolladora, sin música de fondo.

---

## Escena 1 — Presentación (0:00 - 0:30)

Pantalla: panel del proyecto en Unity.

> "Este es el LAB 9 de Crisis VR. Auditoricé cuatro actividades del diseño: el tutorial y los tres
> actos. Encontré tres problemas y los corregí. Luego escribí cinco pruebas automatizadas y un plan
> de pruebas para validar el resultado en Quest 3."

## Escena 2 — Los tres hallazgos (0:30 - 2:00)

Abrir `Assets\_Project\Scripts\Dialogue\TutorialPanel.cs`.

1. Mostrar el indicador de progreso: "El texto de avance estaba superpuesto a la marca CRISIS VR.
   Lo moví a la zona libre del panel y ahora muestra Paso X de Y."
2. Mostrar las guardas nulas: "Si el panel no tenía arrastrado algún campo, la sesión se bloqueaba
   en seco. Ahora Show no lanza excepción."
3. Mostrar `FitInstruction`: "Las instrucciones largas se cortaban. Ahora la fuente se ajusta sola,
   con un límite de 19 a 29 puntos."

## Escena 3 — Las pruebas automatizadas (2:00 - 4:00)

`Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All`.

> "Estas cinco pruebas cubren el enum de pasos, el orden de la secuencia, el guardado de los textos
> del ejercicio, el formato del indicador de avance y el caso de un panel incompleto. Todas pasan."

Captura del Test Runner con las 5 en verde.

## Escena 4 — Prueba en Quest 3 (4:00 - 6:00)

Toma del visor: tutorial completo, luego actos.

> "Recorrí el tutorial de principio a fin y los tres actos. El avance se veía correctamente y ninguna
> opción bloqueaba el avance. El reporte se generó con las tres decisiones."

## Escena 5 — Cierre (6:00 - 6:30)

Pantalla: carpeta `LAB9\` con el PDF.

> "Entregué el informe, el plan de pruebas con sus KPI y las tareas de Trello. Lo que falta validar
> es la percepción de los participantes y la utilidad del reporte para Psicopedagogía."

---

**Grabar a:** 1920×1080, sin cursor de ratón sobre textos privados.
**Revisar antes de enviar:** que no aparezcan nombres de archivos con datos personales.
