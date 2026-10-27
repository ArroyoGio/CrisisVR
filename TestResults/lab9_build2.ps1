$ErrorActionPreference = 'Stop'
Add-Type -Path 'C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Data\MonoBleedingEdge\lib\mono\gac\Mono.Cecil\0.11.1.0__0738eb9f132ed756\Mono.Cecil.dll'
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Data\Managed\UnityEditor.dll')
$t = $asm.MainModule.Types | Where-Object { $_.FullName -eq 'UnityEditor.EditorBuildSettingsScene' }
Write-Output '--- constructores ---'
foreach ($c in $t.Methods) {
    if ($c.IsConstructor) {
        $ps = ($c.Parameters | ForEach-Object { $_.ParameterType.Name + ' ' + $_.Name }) -join ', '
        Write-Output ('  ' + $(if ($c.IsPublic) {'public '} else {'privado '}) + '(' + $ps + ')')
    }
}
Write-Output ''
Write-Output '--- escenas .unity en el proyecto ---'
Get-ChildItem 'C:\Users\USER\CrisisVR\Assets\_Project' -Recurse -Filter *.unity -ErrorAction SilentlyContinue | ForEach-Object { Write-Output ('  ' + $_.FullName.Replace('C:\Users\USER\CrisisVR\','')) }
Get-ChildItem 'C:\Users\USER\CrisisVR\Assets\Scenes' -Recurse -Filter *.unity -ErrorAction SilentlyContinue | ForEach-Object { Write-Output ('  ' + $_.FullName.Replace('C:\Users\USER\CrisisVR\','')) }
