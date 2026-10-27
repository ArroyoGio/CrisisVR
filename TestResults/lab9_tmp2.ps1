$ErrorActionPreference = 'Stop'
$cecil = 'C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Data\MonoBleedingEdge\lib\mono\gac\Mono.Cecil\0.11.1.0__0738eb9f132ed756\Mono.Cecil.dll'
Add-Type -Path $cecil
$dll = 'C:\Users\USER\CrisisVR\Library\ScriptAssemblies\Unity.TextMeshPro.dll'
$a = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($dll)
$t = $a.MainModule.GetType('TMPro.TMP_Text')
foreach ($n in @('enableAutoSizing','fontSizeMin','fontSizeMax','text','fontSize')) {
    $p = $t.Properties | Where-Object { $_.Name -eq $n } | Select-Object -First 1
    if ($p) { Write-Output ($n + ' OK set=' + [bool]$p.SetMethod) } else { Write-Output ($n + ' FALTA') }
}
