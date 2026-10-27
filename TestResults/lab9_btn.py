import re
fp = r'C:\Users\USER\CrisisVR\Library\PackageCache\com.unity.xr.interaction.toolkit@63ca3b7ce776\Runtime\Inputs\Readers\XRInputButtonReader.cs'
t = open(fp, 'r', encoding='utf-8', errors='replace').read()
lines = t.split('\n')
for n, l in enumerate(lines, 1):
    ls = l.strip()
    if any(k in ls for k in ('bypass', 'interface IXRInputButtonReader', 'ReadIsPerformed', 'TryReadValue', 'ReadValue', 'm_InputSourceMode', 'InputSourceMode')):
        print(n, ls[:150])
print('--- interfaz IXRInputButtonReader ---')
m = re.search(r'public interface IXRInputButtonReader.*?\{(.*?)\n    \}', t, re.S)
print(m.group(1)[:1200] if m else '??')
