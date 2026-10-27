import os, re

pkg = r'C:\Users\USER\CrisisVR\Library\PackageCache\com.unity.xr.interaction.toolkit@63ca3b7ce776'
target = 'da1bb8ac6d49b4bbe8d0a43005a26072'

print('=== buscar XRInteractionSimulator.cs ===')
for root, dirs, files in os.walk(pkg):
    for fn in files:
        if fn == 'XRInteractionSimulator.cs':
            fp = os.path.join(root, fn)
            t = open(fp, 'r', encoding='utf-8', errors='replace').read()
            ns = re.search(r'^namespace\s+([\w\.]+)', t, re.M)
            print('RUTA:', os.path.relpath(fp, pkg))
            print('ns  :', ns.group(1) if ns else '??')
            mp = fp + '.meta'
            if os.path.exists(mp):
                m = re.search(r'guid:\s*([0-9a-f]{32})', open(mp, 'r', encoding='utf-8', errors='replace').read())
                print('guid:', m.group(1) if m else '??', '== target?' , (m and m.group(1) == target))

print('=== quien tiene el guid target ===')
for root, dirs, files in os.walk(pkg):
    for fn in files:
        if fn.endswith('.meta'):
            try:
                t = open(os.path.join(root, fn), 'r', encoding='utf-8', errors='replace').read()
            except Exception:
                continue
            m = re.search(r'guid:\s*([0-9a-f]{32})', t)
            if m and m.group(1) == target:
                print(' ->', os.path.relpath(os.path.join(root, fn[:-5]), pkg))
