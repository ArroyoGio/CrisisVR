import os, re

# 1) Namespace real del simulador (en la copia importada en Assets)
p = r'C:\Users\USER\CrisisVR\Assets\Samples\XR Interaction Toolkit\3.5.1\XR Interaction Simulator'
for root, dirs, files in os.walk(p):
    for fn in files:
        if fn == 'XRInteractionSimulator.cs':
            fp = os.path.join(root, fn)
            t = open(fp, 'r', encoding='utf-8', errors='replace').read()
            m = re.search(r'^namespace\s+([\w\.]+)', t, re.M)
            print('SIM namespace =', m.group(1) if m else '??', '|', fn)
        if fn.endswith('.asmdef'):
            fp = os.path.join(root, fn)
            t = open(fp, 'r', encoding='utf-8', errors='replace').read()
            print('SIM asmdef =', re.search(r'"name":\s*"([^"]+)"', t).group(1))

# 2) Namespace de XRGrabInteractable y NearFarInteractor
pkg = r'C:\Users\USER\CrisisVR\Library\PackageCache\com.unity.xr.interaction.toolkit@63ca3b7ce776'
for target in ('XRGrabInteractable.cs', 'NearFarInteractor.cs', 'XRInputButtonReader.cs'):
    for root, dirs, files in os.walk(pkg):
        if target in files:
            fp = os.path.join(root, target)
            t = open(fp, 'r', encoding='utf-8', errors='replace').read()
            m = re.search(r'^namespace\s+([\w\.]+)', t, re.M)
            print('NS', target, '=', m.group(1) if m else '??')

# 3) API de XRInputButtonReader: metodos publicos con 'Read'
fp = os.path.join(pkg, 'Runtime', 'Inputs', 'Readers', 'XRInputButtonReader.cs')
t = open(fp, 'r', encoding='utf-8', errors='replace').read()
for n, l in enumerate(t.split('\n'), 1):
    ls = l.strip()
    if 'public ' in ls and ('Read' in ls or 'IsPressed' in ls or 'IsActive' in ls):
        print('BTN', n, ls)
