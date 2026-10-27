# -*- coding: utf-8 -*-
"""Valida LAB9_Informe.pdf sin librerias externas.

Comprueba: estructura PDF, maximo 3 paginas, y que el texto contenga todos los
apartados obligatorios del LAB 9 declarados en APARTADOS.
"""
import os, re, unicodedata

APARTADOS = [
    ("Tabla A1 - auditoria de 3 a 5 actividades", ["Tabla A1", "Tutorial", "Acto 1", "Acto 3"]),
    ("Tabla A2 - checklist con 3 hallazgos", ["Tabla A2", "H1", "H2", "H3"]),
    ("Tabla A3 - mejoras implementadas", ["Tabla A3", "M1", "M2", "M3"]),
    ("Tabla A4 - KPI con meta y medida", ["Tabla A4", "KPI 1", "KPI 3", "Como se mide"]),
    ("Tabla A4 - casos de prueba", ["Caso 1", "Caso 2", "Caso 3"]),
    ("Tabla A4 - criterios de aceptacion", ["Criterios de aceptacion", "bloquea el avance"]),
    ("Tabla A4 - agenda de ejecucion", ["Agenda de ejecucion", "D1", "D5"]),
    ("Tabla A5 - 8 pruebas aprobadas", ["Tabla A5", "8 de 8 aprobadas", "T01", "T05", "EditMode"]),
    ("Tabla A5 - ejecucion por lotes sobre copia", ["por lotes sobre una copia del proyecto"]),
    ("6 - tareas con el video en hecha", ["Video del LAB 9 grabado", "falta adjuntar"]),
    ("7 - conclusiones", ["Conclusiones"]),
]

def quitar_acentos(s):
    return "".join(c for c in unicodedata.normalize("NFD", s) if unicodedata.category(c) != "Mn")

def buscar_pdf():
    # Este script vive en LAB9\RESPALDO; el informe entregable esta en LAB9.
    base = os.path.dirname(os.path.abspath(__file__))
    for carpeta in (os.path.dirname(base), base):
        cand = os.path.join(carpeta, "LAB9_Informe.pdf")
        if os.path.exists(cand):
            return cand
    return os.path.join(os.path.dirname(base), "LAB9_Informe.pdf")

ruta = buscar_pdf()
raw = open(ruta, "rb").read()
errores = []

if not raw.startswith(b"%PDF-"):
    errores.append("No empieza con %PDF-")
if not raw.rstrip().endswith(b"%%EOF"):
    errores.append("No termina con %%EOF")

m = re.search(rb"startxref\s+(\d+)\s+%%EOF", raw)
if not m:
    errores.append("Falta startxref")
    xref = 0
else:
    xref = int(m.group(1))
    if raw[xref:xref + 4] != b"xref":
        errores.append("startxref no apunta a la tabla xref")

bloque = raw[xref:].split(b"trailer", 1)[0].split(b"\n")
entradas = [l for l in bloque if re.match(rb"^\d{10} \d{5} [nf]$", l.strip())]
if not entradas:
    errores.append("Tabla xref sin entradas")
else:
    for i, l in enumerate(entradas):
        off = int(l[:10])
        if i == 0:
            continue
        esperado = ("%d 0 obj" % i).encode()
        if raw[off:off + len(esperado)] != esperado:
            errores.append("Offset %d no coincide con '%s'" % (off, esperado.decode()))

paginas = len(re.findall(rb"/Type\s*/Page[^s]", raw))

# extrae el texto real (operadores Tj de cada stream de contenido)
lineas = []
for m2 in re.finditer(rb"stream\n(.*?)\nendstream", raw, re.S):
    cuerpo = m2.group(1)
    for t in re.findall(rb"\(((?:[^()\\]|\\.)*)\)\s*Tj", cuerpo):
        s = t.decode("latin-1")
        s = s.replace("\\(", "(").replace("\\)", ")").replace("\\\\", "\\")
        lineas.append(s)
texto = quitar_acentos("\n".join(lineas))

print("Archivo:", ruta)
print("Tamano:", len(raw), "bytes")
print("Paginas:", paginas, "(maximo permitido: 3)")
print("Lineas de texto extraidas:", len(lineas))
print()

if paginas > 3:
    errores.append("El PDF tiene %d paginas y el maximo es 3" % paginas)

faltan = []
for nombre, claves in APARTADOS:
    faltan_claves = [c for c in claves if quitar_acentos(c) not in texto]
    ok = not faltan_claves
    print(("  OK   " if ok else "  FALTA") + "  " + nombre)
    if not ok:
        faltan.append(nombre)
        for c in faltan_claves:
            print("           clave no encontrada: " + repr(c))
print()

print("ERRORES ESTRUCTURA:", len(errores))
for e in errores:
    print(" -", e)
print("APARTADOS FALTANTES:", len(faltan))
for f in faltan:
    print(" -", f)

if errores or faltan or paginas > 3:
    raise SystemExit(1)
print("\nPDF COMPLETO Y VALIDO:", paginas, "pagina(s),", len(APARTADOS), "apartados obligatorios presentes")
