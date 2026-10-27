$ErrorActionPreference = 'Stop'
$root  = 'C:\Users\USER\CrisisVR'
$cecil = 'C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Data\MonoBleedingEdge\lib\mono\gac\Mono.Cecil\0.11.1.0__0738eb9f132ed756\Mono.Cecil.dll'
Add-Type -Path $cecil

function Type-Info([string]$dll, [string]$type) {
    $p = Join-Path (Join-Path $root 'Library\ScriptAssemblies') $dll
    if (-not (Test-Path $p)) { Write-Output ('  ' + $dll + ': NO EXISTE'); return }
    $t0 = (Get-Item $p).LastWriteTime
    try { $a = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($p) } catch { Write-Output ('  ' + $dll + ': ERROR ' + $_.Exception.Message); return }
    $t = $a.MainModule.GetType($type)
    Write-Output ('--- ' + $dll + '  (' + $t0.ToString('yyyy-MM-dd HH:mm:ss') + ')')
    if (-not $t) { Write-Output ('  TIPO NO ENCONTRADO: ' + $type); return }
    $t.Methods | Where-Object { $_.IsPublic } | ForEach-Object {
        $ps = ($_.Parameters | ForEach-Object { $_.ParameterType.Name }) -join ', '
        Write-Output ('  ' + $_.Name + '(' + $ps + ')')
    }
    if ($t.HasFields) { $t.Fields | Where-Object { $_.IsPublic } | ForEach-Object { Write-Output ('  campo ' + $_.Name + ' : ' + $_.FieldType.Name) } }
}

Write-Output '=== Fechas de los .cs modificados ==='
foreach ($f in @(
    'Assets\_Project\Scripts\Dialogue\TutorialPanel.cs',
    'Assets\_Project\Scripts\Flow\TutorialFlow.cs',
    'Assets\_Project\Scripts\Build\Editor\BuildAndroid.cs',
    'Assets\_Project\Tests\EditMode\CrisisVREditModeTests.cs',
    'Assets\_Project\Tests\PlayMode\CrisisVRPlayModeTests.cs')) {
    $p = Join-Path $root $f
    Write-Output ('  ' + (Get-Item $p).LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss') + '  ' + $f)
}

Write-Output ''
Write-Output '=== Contenido compilado ==='
Type-Info 'Assembly-CSharp.dll' 'CrisisVR.TutorialPanel'
Write-Output ''
Type-Info 'Assembly-CSharp-Editor.dll' 'CrisisVR.Editor.BuildAndroid'
Write-Output ''
Type-Info 'CrisisVR.Tests.EditMode.dll' 'CrisisVREditModeTests'
Write-Output ''
Type-Info 'CrisisVR.Tests.PlayMode.dll' 'CrisisVRPlayModeTests'
