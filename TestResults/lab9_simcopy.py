import os, re

pc = r'C:\Users\USER\CrisisVR\Library\PackageCache\com.unity.xr.interaction.toolkit@63ca3b7ce776\Samples~\XR Interaction Simulator'
target = 'da1bb8ac6d49b4bbe8d0a43005a26072'
print('existe carpeta de muestras:', os.path.isdir(pc))
for root, dirs, files in os.walk(pc):
    for fn in files:
        if not fn.endswith('.meta'):
            continue
        fp = os.path.join(root, fn)
        try:
            t = open(fp, 'r', encoding='utf-8', errors='replace').read()
        except Exception:
            continue
        m = re.search(r'guid:\s*([0-9a-f]{32})', t)
        if m and m.group(1) == target:
            print('ENCONTRADO ->', os.path.relpath(fp[:-5], pc))
            t2 = open(fp[:-5], 'r', encoding='utf-8', errors='replace').read()
            ns = re.search(r'^namespace\s+([\w\.]+)', t2, re.M)
            print('   namespace =', ns.group(1) if ns else '??')

# Listar el contenido de la muestra del paquete (recursivo) para copiarlo
print('--- contenido del paquete (sin .meta) ---')
for root, dirs, files in os.walk(pc):
    rel = os.path.relpath(root, pc)
    for fn in sorted(files):
        if fn.endswith('.meta'):
            continue
        print(os.path.join(rel, fn))
