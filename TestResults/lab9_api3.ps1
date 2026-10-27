$ErrorActionPreference = 'Stop'
$root  = 'C:\Users\USER\CrisisVR'
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Data\Managed\UnityEngine'
$cecil = 'C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Data\MonoBleedingEdge\lib\mono\gac\Mono.Cecil\0.11.1.0__0738eb9f132ed756\Mono.Cecil.dll'
Add-Type -Path $cecil

Write-Output '=== BuildPlayerOptions: fields ==='
$a = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $unity 'UnityEditor.CoreModule.dll'))
$t = $a.MainModule.GetType('UnityEditor.BuildPlayerOptions')
if (-not $t) { Write-Output '  NO TYPE'; exit }
Write-Output ('  BaseType: ' + $t.BaseType.FullName)
$t.Fields | ForEach-Object { Write-Output ('  field  ' + $_.Name + ' : ' + $_.FieldType.FullName) }
Write-Output '=== BuildPlayerOptions: properties ==='
$t.Properties | ForEach-Object { Write-Output ('  prop   ' + $_.Name + ' : ' + $_.PropertyType.FullName) }

Write-Output ''
Write-Output '=== BuildPlayerOptions: todas las propiedades heredadas visibles (recursive) ==='
function Dump($tt, $prefix, $seen) {
    if (-not $tt) { return }
    if ($seen.Contains($tt.FullName)) { return }
    $seen[$tt.FullName] = $true
    $tt.Properties | ForEach-Object { Write-Output ('  ' + $prefix + $_.Name + ' : ' + $_.PropertyType.FullName) }
    if ($tt.BaseType) {
        try { Dump $tt.BaseType.Resolve() ($prefix + '  ') $seen } catch { }
    }
}
Dump $t '' @{}

Write-Output ''
Write-Output '=== Graphic.raycastTarget (UnityEngine.UI.dll) ==='
$ui = Join-Path $root 'Library\ScriptAssemblies\UnityEngine.UI.dll'
$aui = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($ui)
$g = $aui.MainModule.GetType('UnityEngine.UI.Graphic')
Write-Output ('  Graphic=' + [bool]$g)
if ($g) {
    $p = $g.Properties | Where-Object { $_.Name -eq 'raycastTarget' }
    Write-Output ('  raycastTarget=' + [bool]$p + ' set=' + [bool]($p.SetMethod))
    Write-Output ('  BaseType: ' + $g.BaseType.FullName)
}
