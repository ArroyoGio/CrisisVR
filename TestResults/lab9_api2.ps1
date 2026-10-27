$ErrorActionPreference = 'Stop'
$root  = 'C:\Users\USER\CrisisVR'
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Data\Managed\UnityEngine'
$cecil = 'C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Data\MonoBleedingEdge\lib\mono\gac\Mono.Cecil\0.11.1.0__0738eb9f132ed756\Mono.Cecil.dll'

Add-Type -Path $cecil
$asmCache = @{}
function Get-Asm([string]$path) {
    if ($asmCache.ContainsKey($path)) { return $asmCache[$path] }
    $a = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($path)
    $asmCache[$path] = $a
    return $a
}
function Find-Type([string]$file, [string]$fullName) {
    $p = Join-Path $unity $file
    if (-not (Test-Path $p)) { return $null }
    $a = Get-Asm $p
    return $a.MainModule.GetType($fullName)
}
function Find-TypeAny([string[]]$files, [string]$fullName) {
    foreach ($f in $files) {
        $t = Find-Type $f $fullName
        if ($t) { return @{ Type = $t; File = $f } }
    }
    return $null
}
function Prop-Info($t, [string]$name) {
    $p = $t.Properties | Where-Object { $_.Name -eq $name } | Select-Object -First 1
    if (-not $p) { return ('FALTA ' + $name) }
    $set = if ($p.SetMethod) { 'set' } else { 'no-set' }
    return ('OK ' + $name + ' (' + $set + ')')
}
function Field-Info($t, [string]$name) {
    $f = $t.Fields | Where-Object { $_.Name -eq $name } | Select-Object -First 1
    if (-not $f) { return ('FALTA ' + $name) }
    return ('OK ' + $name)
}
function Method-Info($t, [string]$name, [int]$params) {
    $m = $t.Methods | Where-Object { $_.Name -eq $name -and $_.Parameters.Count -eq $params } | Select-Object -First 1
    if (-not $m) { return ('FALTA ' + $name + '/' + $params) }
    return ('OK ' + $name + '/' + $params)
}

$core = 'UnityEngine.CoreModule.dll'
$tmp  = Join-Path $root 'Library\ScriptAssemblies\Unity.TextMeshPro.dll'
$edFiles = @('UnityEditor.CoreModule.dll','UnityEditor.BuildProfileModule.dll','UnityEditor.dll','UnityEditor.BuildReportingModule.dll')

Write-Output '=== Mono.Cecil ==='
Write-Output ('  version ' + ([Mono.Cecil.AssemblyDefinition]::ReadAssembly($cecil)).Name.Version)

Write-Output ''
Write-Output '=== TMP (Library\ScriptAssemblies\Unity.TextMeshPro.dll) ==='
$tmpAsm = Get-Asm $tmp
$tTmp = $tmpAsm.MainModule.GetType('TMPro.TMP_Text')
$tGui = $tmpAsm.MainModule.GetType('TMPro.TextMeshProUGUI')
$tAli = $tmpAsm.MainModule.GetType('TMPro.TextAlignmentOptions')
Write-Output ('  TMP_Text=' + [bool]$tTmp + '  TextMeshProUGUI=' + [bool]$tGui + '  TextAlignmentOptions=' + [bool]$tAli)
if ($tTmp) {
    foreach ($n in @('text','font','fontSharedMaterial','fontSize','color','alignment','raycastTarget','rectTransform')) { Write-Output ('  ' + (Prop-Info $tTmp $n)) }
}
if ($tAli) {
    $ok = ($tAli.Fields | Where-Object { $_.Name -eq 'MidlineRight' }) -ne $null
    Write-Output ('  MidlineRight enum=' + $ok)
}

Write-Output ''
Write-Output '=== UnityEngine.CoreModule ==='
$tApp = Find-Type $core 'UnityEngine.Application'
$tSm  = Find-Type $core 'UnityEngine.SceneManagement.SceneManager'
$tSu  = Find-Type $core 'UnityEngine.SceneManagement.SceneUtility'
$tRt  = Find-Type $core 'UnityEngine.RectTransform'
Write-Output ('  Application=' + [bool]$tApp + ' SceneManager=' + [bool]$tSm + ' SceneUtility=' + [bool]$tSu + ' RectTransform=' + [bool]$tRt)
if ($tApp) { Write-Output ('  ' + (Prop-Info $tApp 'isBatchMode')) }
if ($tSm)  { Write-Output ('  ' + (Prop-Info $tSm 'sceneCountInBuildSettings')) }
if ($tSu)  { Write-Output ('  ' + (Method-Info $tSu 'GetScenePathByBuildIndex' 1)) }

Write-Output ''
Write-Output '=== UnityEditor: Build ==='
$hit = Find-TypeAny $edFiles 'UnityEditor.BuildPlayerOptions'
Write-Output ('  BuildPlayerOptions en ' + ($(if ($hit) { $hit.File } else { 'NO ENCONTRADO' })))
if ($hit) { foreach ($n in @('scenes','locationPathName','target','options')) { Write-Output ('  ' + (Field-Info $hit.Type $n)) } }

$hit2 = Find-TypeAny $edFiles 'UnityEditor.BuildPipeline'
Write-Output ('  BuildPipeline en ' + ($(if ($hit2) { $hit2.File } else { 'NO ENCONTRADO' })))
if ($hit2) { Write-Output ('  ' + (Method-Info $hit2.Type 'BuildPlayer' 1)) }

$hit3 = Find-TypeAny $edFiles 'UnityEditor.Build.Reporting.BuildResult'
Write-Output ('  BuildResult en ' + ($(if ($hit3) { $hit3.File } else { 'NO ENCONTRADO' })))
if ($hit3) { Write-Output ('  Succeeded=' + [bool]($hit3.Type.Fields | Where-Object { $_.Name -eq 'Succeeded' })) }

$hit4 = Find-TypeAny $edFiles 'UnityEditor.EditorBuildSettings'
Write-Output ('  EditorBuildSettings en ' + ($(if ($hit4) { $hit4.File } else { 'NO ENCONTRADO' })))
if ($hit4) { Write-Output ('  ' + (Prop-Info $hit4.Type 'scenes')) }

$hit5 = Find-TypeAny $edFiles 'UnityEditor.BuildTarget'
if ($hit5) { Write-Output ('  BuildTarget.Android=' + [bool]($hit5.Type.Fields | Where-Object { $_.Name -eq 'Android' })) }
$hit6 = Find-TypeAny $edFiles 'UnityEditor.BuildOptions'
if ($hit6) { Write-Output ('  BuildOptions.None=' + [bool]($hit6.Type.Fields | Where-Object { $_.Name -eq 'None' })) }
$hit7 = Find-TypeAny $edFiles 'UnityEditor.EditorApplication'
if ($hit7) { Write-Output ('  ' + (Method-Info $hit7.Type 'Exit' 1)) }
$hit8 = Find-TypeAny $edFiles 'UnityEditor.MenuItem'
if ($hit8) { Write-Output ('  MenuItem=' + [bool]$hit8) }
