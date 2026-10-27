import os, re

d = r'C:\Users\USER\CrisisVR\Library\PackageCache\com.unity.xr.interaction.toolkit@63ca3b7ce776\Runtime\Inputs\Simulation'
for fn in sorted(os.listdir(d)):
    if not fn.endswith('.cs'):
        continue
    t = open(os.path.join(d, fn), 'r', encoding='utf-8', errors='replace').read()
    ns = re.search(r'^namespace\s+([\w\.]+)', t, re.M)
    print('=====', fn, '| ns=', ns.group(1) if ns else '??')
    for n, l in enumerate(t.split('\n'), 1):
        ls = l.strip()
        if re.match(r'public\s', ls) and ('(' in ls or '=>' in ls or '{ get' in ls):
            if any(k in ls for k in ('DeviceMode', 'deviceMode', 'SetDevice', 'SimulatedDevice', 'grip', 'Grip', 'Activate', 'Present', 'public enum', 'public class', 'public ')):
                print('  ', n, ls[:130])
