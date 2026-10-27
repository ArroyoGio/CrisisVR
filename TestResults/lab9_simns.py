import os, re

p = r'C:\Users\USER\CrisisVR\Assets\Samples\XR Interaction Toolkit\3.5.1\XR Interaction Simulator\Scripts'
print('--- archivos en Scripts ---')
for fn in sorted(os.listdir(p)):
    if fn.endswith('.cs'):
        t = open(os.path.join(p, fn), 'r', encoding='utf-8', errors='replace').read()
        ns = re.search(r'^namespace\s+([\w\.]+)', t, re.M)
        cls = re.search(r'public\s+(?:sealed\s+)?class\s+(\w+)', t)
        print(fn, '| ns=', ns.group(1) if ns else '??', '| class=', cls.group(1) if cls else '??')
