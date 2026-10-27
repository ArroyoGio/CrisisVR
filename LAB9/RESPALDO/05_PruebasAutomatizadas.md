# LAB 9 — Pruebas funcionales ejecutadas (resultados reales)

**Fecha de ejecución:** 20/10/2026, 21:11:59–21:12:00 (hora local del equipo)
**Entorno:** Unity 6000.3.9f1, EditMode, NUnit 3.5.0.0, .NET CLR 4.0.30319.42000
**Ejecución:** Unity en modo lote sobre una copia del proyecto sin `Library`, porque el Editor
principal tenía el proyecto abierto. El código ejecutado es idéntico al del proyecto original.
**Archivo de resultados:** `TestResults\Lab9\tests_editmode.xml`
**Registro de ejecución:** `TestResults\Lab9\editmode.log`

## Resultado: 8 de 8 pruebas aprobadas

| # | Prueba | Aserciones | Resultado | Duración |
|---|---|---|---|---|
| E01 | `E01_EscenasExistenEnProyecto` | `AreEqual` sobre GUIDs faltantes | **Aprobada** | 0,012 s |
| E02 | `E02_ArchivoYarnExiste` | `IsTrue` | **Aprobada** | 0,001 s |
| E03 | `E03_TiposCrisisVRCompilan` | `IsNotNull` por tipo | **Aprobada** | 0,524 s |
| T01 | `T01_PasosDelTutorial_SonUnicosYCompletos` | `AreEqual(11)`, `AllItemsAreUnique`, `IsTrue` | **Aprobada** | 0,010 s |
| T02 | `T02_OrdenDeLaSecuencia_CubreTodosLosPasos` | `AreEquivalent`, `AreEqual` | **Aprobada** | 0,004 s |
| T03 | `T03_Ejercicio_ConservaSusTextos` | `AreEqual`, `StringAssert.Contains`, `IsNotEmpty` | **Aprobada** | 0,002 s |
| T04 | `T04_Panel_MuestraElPasoActual` | `IsNotNull`, `AreEqual("Paso 3 de 10")` | **Aprobada** | 0,010 s |
| T05 | `T05_Panel_SinReferencias_NoBloquea` | `DoesNotThrow`, `AreEqual(string.Empty)` | **Aprobada** | 0,010 s |

**Totales:** aprobadas 8 · fallidas 0 · omitidas 0 · inciertas 0 · duración total 0,61 s.

## Qué comprueba cada prueba nueva

- **T01** — El enum `TutorialStep` declara exactamente 11 pasos, sin duplicados, e incluye `Finished`.
  Evita que un paso se duplique o desaparezca al editar el enum.
- **T02** — `TutorialSequenceUpgrade.StepOrder` cubre cada paso del enum exactamente una vez y
  `Finished` queda el último. Evita que un paso no tenga orden de enseñanza.
- **T03** — `TutorialExercise` conserva `title`, `instruction` y `help` después de escribirlos.
  Protege los textos editables en el Inspector.
- **T04** — `SetProgress(2, 10)` muestra literalmente **"Paso 3 de 10"** y crea el texto de progreso
  automáticamente. Es la prueba directa de la mejora M1.
- **T05** — Un panel con referencias sin asignar no lanza excepción y limpia la pista al cambiar de
  paso. Es la prueba directa de la mejora M2 (el hallazgo de severidad alta).

## Tipos de aserción usados

`Assert.AreEqual`, `Assert.IsNotEmpty`, `Assert.IsNotNull`, `Assert.IsTrue`,
`Assert.DoesNotThrow`, `CollectionAssert.AllItemsAreUnique`, `CollectionAssert.AreEquivalent`,
`StringAssert.Contains` — ocho tipos distintos en 5 pruebas.

## Nota sobre la ejecución

Las pruebas acceden a los tipos de producción por reflexión, porque un archivo `.asmdef` de pruebas
no puede referenciar el ensamblado predefinido `Assembly-CSharp`. Esto evitó refactorizar el proyecto
con un ensamblado de ejecución nuevo, que habría arriesgado el tutorial que ya funciona en Quest 3.

## Lo que estas pruebas NO cubren

**SIN VERIFICAR:** el aspecto final en el visor, el comportamiento de los controles en Quest 3, la
generación real del reporte en `persistentDataPath` durante una sesión y la percepción de los
participantes. Esos puntos corresponden a CP-01, CP-02 y CP-03 del plan de pruebas.
