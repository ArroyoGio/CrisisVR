# LAB 9 — Plan de pruebas

**Objetivo:** comprobar que el estudiante comprende el tutorial y llega a los actos sin bloqueo, y que
el sistema registra sus decisiones sin añadir puntuaciones ni valoraciones.
**Alcance:** tutorial E3 y los tres actos, en Meta Quest 3.
**Cumplimiento:** no se miden tiempos de respuesta, mirada ni movimiento; el único tiempo es el de
espera de diseño definido en el GDD (45 s en Acto 1, 90 s en Acto 3, 5 s en T3, 4 s en T4).

## KPIs (3)

| KPI | Definición | Meta |
|---|---|---|
| K1 — Finalización sin bloqueo | Participantes que completan el tutorial y los 3 actos pidiendo como máximo 1 ayuda | ≥ 90 % |
| K2 — Peticiones de ayuda | Número de veces que el participante usa AYUDA durante el tutorial | ≤ 1 por participante |
| K3 — Registro correcto | Sesiones con el archivo `.txt` generado con las 3 decisiones y los metadatos completos | 100 % |

## Casos de prueba (3)

### CP-01 — Tutorial completo sin bloqueo
- **Precondición:** sesión nueva, `Tutorial.unity` en Build Settings.
- **Pasos:** iniciar → tutorial → pasar T1 a T5 → COMENZAR.
- **Criterio de aceptación:** ninguna pantalla se congela; "Paso X de Y" coincide con el paso mostrado;
  repetir y ayuda funcionan; al terminar se carga `Act1.unity`. Ninguna acción se registra.
- **Instrumento:** checklist de observación (1 fila por paso).

### CP-02 — Pausa y reanudación
- **Precondición:** en el Acto 1, tras ENTREGAR.
- **Pasos:** botón Menú → panel de pausa → CONTINUAR.
- **Criterio de aceptación:** los diálogos y el tiempo de espera se congelan; al continuar se retoma
  exactamente donde estaba; no se genera reporte parcial.
- **Instrumento:** checklist de observación.

### CP-03 — Registro de decisiones y reporte
- **Precondición:** sesión completa con el Acto 3 finalizado.
- **Pasos:** terminar la sesión → conectar el visor → copiar el archivo desde `persistentDataPath`.
- **Criterio de aceptación:** existe un archivo `CrisisVR_<código>.txt`; contiene `Reintento`,
  `OpcionAdaptabilidad`, `OpcionTrabajoEnEquipo`, código seudonimizado, fecha, versión y estado;
  no contiene nombres ni datos personales; el texto es descriptivo y sin valoraciones.
- **Instrumento:** formulario de verificación del reporte.

## Instrumentos

1. **Guion de facilitador** — secuencia literal de la sesión (5 líneas por etapa, sin juicios).
2. **Checklist de observación** — una fila por paso con columnas: ¿se completó?, ¿pidió ayuda?, ¿hubo bloqueo?
3. **Formulario breve post-sesión** — 3 preguntas abiertas de comprensión; se llena a mano.
4. **Verificador del reporte** — lista de comprobación del archivo `.txt` generado.

## Participantes (2 a 3)

| Perfil | Cantidad | Motivo |
|---|---|---|
| Estudiante de pregrado no involucrado en la tesis | 2 | Comprueba comprensión sin conocimiento previo |
| Psicopedagogo o docente | 1 | Valida que el reporte sea útil para la conversación posterior |

**No se piden datos personales** de los participantes; solo el código de sesión seudonimizado.

## Cronograma (5 días)

| Día | Actividad | Responsable |
|---|---|---|
| D1 | Añadir escenas al Build Settings y probar CP-01 sin visor | Desarrolladora |
| D2 | CP-01 y CP-02 en Quest 3 con el primer participante | Desarrolladora |
| D3 | CP-03 con los 3 participantes; capturas y video | Desarrolladora |
| D4 | Tabular KPIs; corregir hallazgos nuevos | Desarrolladora |
| D5 | Entrega: PDF, video, capturas y evidencias | Desarrolladora |

## Criterios de aceptación globales

- K1, K2 y K3 alcanzan su meta.
- Ningún participante queda bloqueado en el tutorial.
- El reporte generado contiene solo las variables de alcance.
- Ningún hallazgo de severidad alta queda abierto.
