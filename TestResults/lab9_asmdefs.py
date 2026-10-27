import os, re
base = r'C:\Users\USER\CrisisVR\Library\PackageCache\com.unity.xr.interaction.toolkit@63ca3b7ce776'
for root, dirs, files in os.walk(base):
    for fn in files:
        if fn.endswith('.asmdef'):
            p = os.path.join(root, fn)
            t = open(p, 'r', encoding='utf-8', errors='replace').read()
            nm = re.search(r'"name":\s*"([^"]+)"', t)
            if nm and ('Simulator' in nm.group(1) or 'Interaction' in nm.group(1)):
                print(nm.group(1), '|', os.path.relpath(p, base))
