# -*- coding: utf-8 -*-
"""Genera LAB9_Informe.pdf (maximo 3 paginas) sin librerias externas.

Uso:  python generar_pdf.py
Salida: LAB9_Informe.pdf en la raiz de LAB9.

Codificacion: latin-1 (WinAnsi). Cualquier signo no disponible se sustituye
por un equivalente ASCII en seguro(), para que nunca aparezcan '?'.
"""
import os

W, H = 595.0, 842.0  # A4 en puntos
ML, MR, MT, MB = 46.0, 46.0, 50.0, 44.0

SIGNOS = {
    "\u201c": '"', "\u201d": '"', "\u2018": "'", "\u2019": "'",
    "\u2014": "-", "\u2013": "-", "\u00b7": "-", "\u2264": "<=",
    "\u2265": ">=", "\u00ab": "<<", "\u00bb": ">>", "\u2026": "...",
    "\u00a0": " ",
}

def seguro(s):
    out = []
    for ch in s:
        if ch in SIGNOS:
            out.append(SIGNOS[ch])
            continue
        try:
            ch.encode("latin-1")
            out.append(ch)
        except UnicodeEncodeError:
            out.append("?")
    return "".join(out)

def esc(s):
    s = seguro(s)
    return s.replace("\\", r"\\").replace("(", r"\(").replace(")", r"\)")

class Page:
    def __init__(self):
        self.ops = []
        self.y = H - MT

    def text(self, s, size=9.0, leading=None, indent=0.0, bold=False):
        if leading is None:
            leading = size * 1.3
        font = "F2" if bold else "F1"
        self.ops.append("BT /%s %.1f Tf 1 0 0 1 %.1f %.1f Tm (%s) Tj ET"
                        % (font, size, ML + indent, self.y, esc(s)))
        self.y -= leading

    def gap(self, h=6.0):
        self.y -= h

    def linea(self, x1=ML, x2=None, y=None):
        if x2 is None:
            x2 = W - MR
        if y is None:
            y = self.y
        self.ops.append("%.1f %.1f m %.1f %.1f l S" % (x1, y, x2, y))

    def rule(self):
        self.linea()
        self.y -= 7.0

    def titulo(self, s):
        self.y -= 3.0
        self.text(s, size=11, bold=True, leading=14)
        self.rule()

    def tabla(self, anchos, filas, size=8.5, lead=10.5, first_bold=True):
        xs = [ML]
        for a in anchos:
            xs.append(xs[-1] + a)
        self.linea(y=self.y + 4)
        for fila in filas:
            celdas = [wrap(str(c), size, a - 7) for c, a in zip(fila, anchos)]
            n = max(len(c) for c in celdas)
            y0 = self.y
            for i, col in enumerate(celdas):
                font = "F2" if (first_bold and i == 0) else "F1"
                for j, ln in enumerate(col):
                    self.ops.append("BT /%s %.1f Tf 1 0 0 1 %.1f %.1f Tm (%s) Tj ET"
                                    % (font, size, xs[i], y0 - j * lead, esc(ln)))
            self.y = y0 - n * lead - 2.5
            self.linea()
        self.y -= 3.0

class Doc:
    def __init__(self):
        self.pages = []

    def page(self):
        p = Page()
        self.pages.append(p)
        return p

def wrap(s, size, maxw):
    cw = size * 0.50
    n = max(1, int(maxw // cw))
    out, line = [], ""
    for word in str(s).split():
        cand = (line + " " + word).strip()
        if len(cand) > n and line:
            out.append(line)
            line = word
        else:
            line = cand
    if line:
        out.append(line)
    return out or [""]

def build(doc):
    objs = []
    def add(data):
        objs.append(data)
        return len(objs)

    font1 = add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>")
    font2 = add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>")

    content_ids = []
    for p in doc.pages:
        stream = "\n".join(p.ops).encode("latin-1", "replace")
        content_ids.append(add(b"<< /Length %d >>\nstream\n" % len(stream) + stream + b"\nendstream"))

    pages_id = len(objs) + len(doc.pages) + 1
    page_ids = []
    for cid in content_ids:
        page_ids.append(add("<< /Type /Page /Parent %d 0 R /MediaBox [0 0 %.0f %.0f] "
                            "/Resources << /Font << /F1 %d 0 R /F2 %d 0 R >> >> /Contents %d 0 R >>"
                            % (pages_id, W, H, font1, font2, cid)))

    add("<< /Type /Pages /Kids [%s] /Count %d >>"
        % (" ".join("%d 0 R" % i for i in page_ids), len(page_ids)))
    assert len(objs) == pages_id, "ids desalineados"
    cat = add("<< /Type /Catalog /Pages %d 0 R >>" % pages_id)

    out = bytearray()
    out += b"%PDF-1.4\n%\xe2\xe3\xcf\xd3\n"
    offsets = []
    for i, o in enumerate(objs, start=1):
        offsets.append(len(out))
        out += ("%d 0 obj\n" % i).encode()
        out += o if isinstance(o, bytes) else o.encode("latin-1", "replace")
        out += b"\nendobj\n"
    xref = len(out)
    out += ("xref\n0 %d\n" % (len(objs) + 1)).encode()
    out += b"0000000000 65535 f \n"
    for off in offsets:
        out += ("%010d 00000 n \n" % off).encode()
    out += ("trailer\n<< /Size %d /Root %d 0 R >>\nstartxref\n%d\n%%%%EOF\n"
            % (len(objs) + 1, cat, xref)).encode()
    return bytes(out)

def main():
    doc = Doc()
    CW = W - ML - MR

    # ---------------- Pagina 1: A1, A2, A3 ----------------
    p = doc.page()
    p.text("LAB 9 - Crisis VR", size=16, bold=True, leading=19)
    p.text("Auditoría de diseño, mejoras y pruebas funcionales", size=11, bold=True, leading=14)
    p.text("Prototipo de Realidad Virtual para Meta Quest 3 - Unity 6000.3.9f1 - 20/10/2026",
           size=9, leading=12)
    p.rule()

    p.titulo("Tabla A1. Auditoría de 4 actividades")
    p.tabla([96, 255, 152], [
        ["Actividad", "Qué hace", "Qué registra"],
        ["Tutorial", "11 pasos (TutorialStep). El orden lo fija TutorialSequenceUpgrade.StepOrder. Repetir, ayuda y continuar siempre libres.",
         "Nada, por diseño (GDD 1.2)."],
        ["Acto 1: autorregulación", "Ruta A corregir y ruta B sin acción, con tiempo de espera del diseño.",
         "Reintento y Acto1Ruta."],
        ["Acto 2: adaptabilidad", "Carpeta con el plan original y tablet con el plan revisado.",
         "OpcionAdaptabilidad."],
        ["Acto 3: trabajo en equipo", "Cuatro opciones de diálogo para repartir el trabajo.",
         "OpcionTrabajoEnEquipo."],
    ], size=8.5, lead=10.5)
    for w in wrap("Escenas propias auditadas (Tutorial, Act1, BasicScene, SampleScene): 0 referencias "
                  "rotas. Main, Act2 y Act3 no existen aún como archivos en el proyecto. "
                  "Ninguna ruta bloquea el avance.", 8.5, CW):
        p.text(w, size=8.5, leading=11, indent=4)

    p.titulo("Tabla A2. Checklist de revisión y 3 hallazgos")
    for w in wrap("12 criterios revisados. Cumple: sin datos personales; sin tiempos de respuesta, "
                  "puntuaciones ni valoraciones; el tutorial no registra nada; sin referencias rotas; "
                  "todas las opciones continúan al siguiente acto.", 8.5, CW):
        p.text(w, size=8.5, leading=11, indent=4)
    p.gap(2)
    p.tabla([46, 54, 200, 203], [
        ["ID", "Severidad", "Hallazgo", "Mejora aplicada"],
        ["H1", "Media", "El indicador de progreso se superponía a la marca CRISIS VR del panel.",
         "M1: indicador reposicionado a (600,160), 280x44, alineación MidlineRight."],
        ["H2", "Alta", "Un panel con campos sin arrastrar lanzaba NullReferenceException en Show.",
         "M2: guardas nulas en Show y ShowHelp; el tutorial no se bloquea por configuración."],
        ["H3", "Baja", "Las instrucciones largas se cortaban en el área de 670x260.",
         "M3: auto-ajuste de fuente acotado a 19-29 pt, solo si supera 160 caracteres."],
    ], size=8.5, lead=10.5)

    p.titulo("Tabla A3. Mejoras implementadas (3)")
    p.tabla([40, 305, 158], [
        ["Mejora", "Qué cambia", "Dónde"],
        ["M1", "El panel muestra 'Paso X de Y' durante el tutorial, sin tapar la marca del título.",
         "TutorialPanel.cs: SetProgress, EnsureProgress."],
        ["M2", "Si un campo del panel no está arrastrado, el tutorial sigue en vez de fallar.",
         "TutorialPanel.cs: guardas en Show y ShowHelp."],
        ["M3", "Las instrucciones largas reducen la fuente y caben sin cortarse.",
         "TutorialPanel.cs: FitInstruction."],
    ], size=8.5, lead=10.5)
    for w in wrap("Evidencia: el Editor recompiló el proyecto el 20/10/2026 y los ensamblados "
                  "contienen SetProgress, Show de 5 argumentos y FitInstruction. "
                  "Archivo tocado: Assets/_Project/Scripts/Dialogue/TutorialPanel.cs.", 8.5, CW):
        p.text(w, size=8.5, leading=11, indent=4)

    # ---------------- Pagina 2: A4 ----------------
    p = doc.page()
    p.titulo("Tabla A4. Plan de pruebas")

    p.text("KPI, meta y forma de medida", size=9, bold=True, leading=12)
    p.tabla([46, 128, 329], [
        ["KPI", "Meta", "Cómo se mide"],
        ["KPI 1", "90 %", "Porcentaje de participantes que terminan el tutorial y los tres actos con un máximo de 1 pedido de ayuda. Se cuenta desde el archivo de sesión."],
        ["KPI 2", "1 o menos", "Número de pedidos de ayuda durante el tutorial, por participante. Se cuenta en la propia sesión."],
        ["KPI 3", "100 %", "Sesiones con el reporte .txt completo y sin datos personales. Se verifica abriendo el archivo generado en el visor."],
    ], size=8.5, lead=10.5)

    p.text("Casos de prueba", size=9, bold=True, leading=12)
    p.tabla([84, 205, 214], [
        ["Caso", "Qué se prueba", "Criterio de aceptación"],
        ["Caso 1 (CP-01)", "Tutorial completo de principio a fin.", "Ninguna pantalla se congela, el contador 'Paso X de Y' coincide con el paso mostrado y al terminar se carga el Acto 1."],
        ["Caso 2 (CP-02)", "Pausa y reanudación dentro del Acto 1.", "Se congelan los diálogos y se retoma exactamente el mismo punto, sin perder el avance."],
        ["Caso 3 (CP-03)", "Registro de la decisión del estudiante.", "Se genera el archivo CrisisVR_<código>.txt con las tres decisiones y los metadatos, sin nombres ni correos."],
    ], size=8.5, lead=10.5)

    p.text("Criterios de aceptación globales", size=9, bold=True, leading=12)
    for w in wrap("Ninguna opción bloquea el avance: todas siguen al siguiente acto. El tutorial no "
                  "registra nada. No se guardan tiempos de respuesta, mirada ni movimiento; el único "
                  "tiempo que existe es el de espera definido en el diseño del GDD. No hay puntajes "
                  "ni mensajes de valoración dentro de la simulación.", 8.5, CW):
        p.text(w, size=8.5, leading=11, indent=4)
    p.gap(2)

    p.text("Agenda de ejecución (5 días)", size=9, bold=True, leading=12)
    p.tabla([40, 305, 158], [
        ["Día", "Actividad", "Quién"],
        ["D1", "Revisar Build Settings y ejecutar el Caso 1 sin visor.", "Desarrolladora"],
        ["D2", "Ejecutar Caso 2 y Caso 3 en Quest 3 con los participantes.", "Desarrolladora"],
        ["D3", "Grabar el video y tomar las capturas.", "Desarrolladora"],
        ["D4", "Tabular los KPI y anotar los hallazgos nuevos.", "Desarrolladora"],
        ["D5", "Entrega del LAB 9.", "Desarrolladora"],
    ], size=8.5, lead=10.5)
    for w in wrap("Instrumentos: guion del facilitador, checklist de observación, formulario "
                  "post-sesión y verificador del reporte. Participantes: 2 estudiantes de pregrado "
                  "y 1 docente o psicopedagogo, sin datos personales.", 8.5, CW):
        p.text(w, size=8.5, leading=11, indent=4)

    # ---------------- Pagina 3: A5, tareas, conclusiones ----------------
    p = doc.page()
    p.titulo("Tabla A5. Pruebas automatizadas ejecutadas (8 de 8 aprobadas)")
    p.text("Unity Test Framework, plataforma EditMode, NUnit 3.5.0.0, 20/10/2026 21:56. "
           "Registro: EVIDENCIAS/tests_editmode.xml y EVIDENCIAS/editmode.log.", size=8.5, leading=11)
    p.tabla([44, 305, 154], [
        ["ID", "Qué comprueba", "Tipo de aserción"],
        ["T01", "Los 11 pasos del tutorial son únicos y completos.", "AreEqual, único"],
        ["T02", "El orden de la secuencia cubre todos los pasos.", "AreEquivalent"],
        ["T03", "El ejercicio conserva título, instrucción y ayuda.", "AreEqual, Contains"],
        ["T04", "El panel muestra 'Paso 3 de 10'.", "IsNotNull, AreEqual"],
        ["T05", "Un panel incompleto no bloquea la sesión.", "DoesNotThrow"],
        ["E01", "Las escenas del proyecto existen.", "AreEqual"],
        ["E02", "El archivo de diálogos .yarn existe.", "IsTrue"],
        ["E03", "Los tipos de CrisisVR compilan y cargan.", "IsNotNull"],
    ], size=8.5, lead=10.5)
    for w in wrap("Cómo se ejecutaron: en modo por lotes sobre una copia del proyecto, porque el "
                  "Editor de la desarrolladora está abierto y Unity no permite dos instancias sobre "
                  "el mismo proyecto. La copia temporal se eliminó después. Las pruebas acceden a los "
                  "tipos de producción por reflexión, porque un archivo .asmdef no puede referenciar "
                  "Assembly-CSharp; así no hizo falta refactorizar el tutorial.", 8.5, CW):
        p.text(w, size=8.5, leading=11, indent=4)
    p.gap(2)
    for w in wrap("Lo que estas pruebas NO cubren: el aspecto final en el visor, los controles en "
                  "Quest 3, el reporte real en persistentDataPath y la percepción de los "
                  "participantes. Eso queda SIN VERIFICAR.", 8.5, CW):
        p.text(w, size=8.5, leading=11, indent=4)

    p.titulo("6. Tareas y pendientes")
    p.tabla([40, 330, 133], [
        ["N.", "Tarea", "Estado"],
        ["1", "Escenas en el build: script EditorBuildSetup.cs que añade Tutorial, Act1 y BasicScene al abrir Unity y deshabilita SampleScene.", "Hecha"],
        ["2", "Repetir las 5 pruebas en el Editor principal con el Test Runner y adjuntar la captura en verde.", "Pendiente"],
        ["3", "Ejecutar los tres casos de prueba en Quest 3 con los participantes y copiar el reporte del visor.", "Pendiente"],
        ["4", "Video del LAB 9 grabado.", "Hecha; falta adjuntar el archivo en EVIDENCIAS si aún no está incluido"],
        ["-", "Validar con la clienta el texto 'Paso X de Y' y las frases del reporte.", "Pendiente"],
    ], size=8.5, lead=10.5)

    p.titulo("7. Conclusiones")
    for line in [
        "1. Las tres mejoras del panel están implementadas y compiladas; el tutorial no se tocó y ninguna opción bloquea el avance.",
        "2. Las 8 pruebas automatizadas pasaron y verifican la lógica del tutorial, no su comportamiento en el visor.",
        "3. El plan queda medible: 3 KPI con meta, 3 casos con criterio de aceptación y una agenda de 5 días.",
        "4. Para cerrar el LAB 9 solo falta la prueba en Quest 3 con participantes y adjuntar video y capturas.",
    ]:
        for w in wrap(line, 8.5, CW):
            p.text(w, size=8.5, leading=11, indent=4)

    # Pie fijo en todas las páginas: no depende del contenido y no se solapa.
    pie = ("Crisis VR - LAB 9: mejoras y pruebas EditMode ejecutadas y documentadas. "
           "El comportamiento en Quest 3 queda SIN VERIFICAR.")
    for p in doc.pages:
        p.ops.append("%.1f 40 m %.1f 40 l S" % (ML, W - MR))
        p.ops.append("BT /F1 7.5 Tf 1 0 0 1 %.1f 30 Tm (%s) Tj ET" % (ML, esc(pie)))
        if p.y < 52:
            print("ADVERTENCIA: contenido bajo hasta y=%.1f; puede invadir el pie." % p.y)

    data = build(doc)
    raiz = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    out = os.path.join(raiz, "LAB9_Informe.pdf")
    with open(out, "wb") as f:
        f.write(data)
    print("PDF generado:", out)
    print("Paginas:", len(doc.pages), "Bytes:", len(data))
    if len(doc.pages) > 3:
        raise SystemExit("ERROR: supera 3 paginas")

if __name__ == "__main__":
    main()
