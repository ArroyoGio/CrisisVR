$ErrorActionPreference = 'Stop'
$gac = 'C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Data\MonoBleedingEdge\lib\mono\gac\Mono.Cecil\0.11.1.0__0738eb9f132ed756\Mono.Cecil.dll'
Add-Type -Path $gac
$ue = 'C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Data\Managed\UnityEditor.dll'

$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($ue)
Write-Output '=== UnityEditor.dll ==='
foreach ($tn in @('UnityEditor.EditorBuildSettings','UnityEditor.EditorBuildSettingsScene')) {
    $t = $asm.MainModule.Types | Where-Object { $_.FullName -eq $tn }
    if (-not $t) { Write-Output ('NO ENCONTRADO: ' + $tn); continue }
    Write-Output ('--- ' + $tn + ' ---')
    foreach ($f in $t.Fields) { if ($f.IsPublic) { Write-Output ('  campo ' + $f.Name + ' : ' + $f.FieldType.Name) } }
    foreach ($p in $t.Properties) { if ($p.GetMethod -and $p.GetMethod.IsPublic) { Write-Output ('  propiedad ' + $p.Name + ' : ' + $p.PropertyType.Name + '  setter=' + [bool]$p.SetMethod) } }
    foreach ($m in $t.Methods) {
        if ($m.IsPublic -and $m.IsStatic -and $m.Name -match '^(Get|Set|Add|Remove)') {
            $ps = ($m.Parameters | ForEach-Object { $_.ParameterType.Name }) -join ', '
            Write-Output ('  metodo ' + $m.Name + '(' + $ps + ')')
        }
    }
}

Write-Output ''
Write-Output '=== EditorBuildSettings.asset (actual) ==='
$asset = 'C:\Users\USER\CrisisVR\ProjectSettings\EditorBuildSettings.asset'
if (Test-Path $asset) {
    Get-Content $asset | ForEach-Object { Write-Output $_ }
} else { Write-Output 'no existe' }
