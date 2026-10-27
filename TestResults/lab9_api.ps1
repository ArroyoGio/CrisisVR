$ErrorActionPreference = 'Continue'
$root = 'C:\Users\USER\CrisisVR'
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Data\Managed'

function Load-RO([string]$p) {
    try { return [System.Reflection.Assembly]::ReflectionOnlyLoadFrom($p) } catch { Write-Output ('  NO CARGA: ' + $p + ' :: ' + $_.Exception.Message); return $null }
}

Write-Output '=== TMP: TextMeshProUGUI ==='
$tmp = Get-ChildItem (Join-Path $root 'Library\PackageCache') -Recurse -Filter 'Unity.TextMeshPro.dll' -ErrorAction SilentlyContinue | Select-Object -First 1
if ($tmp) {
    Write-Output ('DLL: ' + $tmp.FullName)
    $a = Load-RO $tmp.FullName
    if ($a) {
        $t = $a.GetType('TMPro.TextMeshProUGUI')
        if ($t) {
            foreach ($n in @('font','fontSharedMaterial','rectTransform','fontSize','alignment','color','raycastTarget','text')) {
                $p = $t.GetProperty($n)
                if ($p) { Write-Output ('  OK ' + $n + ' set=' + $p.CanWrite) } else { Write-Output ('  FALTA ' + $n) }
            }
        } else { Write-Output '  FALTA tipo TMPro.TextMeshProUGUI' }
        $e = $a.GetType('TMPro.TextAlignmentOptions')
        if ($e) { $names = [Enum]::GetNames($e); Write-Output ('  MidlineRight=' + ($names -contains 'MidlineRight')) } else { Write-Output '  FALTA TextAlignmentOptions' }
    }
} else { Write-Output '  NO se encontro Unity.TextMeshPro.dll' }

Write-Output ''
Write-Output '=== Editor: EditorBuildSettings.scenes ==='
$ed = Join-Path $unity 'UnityEngine\UnityEditor.dll'
$a2 = Load-RO $ed
if ($a2) {
    $t2 = $a2.GetType('UnityEditor.EditorBuildSettings')
    if ($t2) {
        $pr = $t2.GetProperty('scenes')
        if ($pr) { Write-Output ('  OK scenes static=' + $pr.GetGetMethod().IsStatic) } else { Write-Output '  FALTA scenes' }
    } else { Write-Output '  FALTA tipo EditorBuildSettings' }
}

Write-Output ''
Write-Output '=== BuildPlayerOptions / BuildPipeline ==='
$found = $false
foreach ($dll in @('UnityEditor.CoreModule.dll','UnityEditor.BuildProfileModule.dll','UnityEditor.dll')) {
    $p = Join-Path $unity ('UnityEngine\' + $dll)
    if (-not (Test-Path $p)) { continue }
    $a3 = Load-RO $p
    if (-not $a3) { continue }
    $t3 = $a3.GetType('UnityEditor.BuildPlayerOptions')
    $t4 = $a3.GetType('UnityEditor.BuildPipeline')
    if ($t3 -or $t4) {
        Write-Output ('  En ' + $dll + ': BuildPlayerOptions=' + [bool]$t3 + ' BuildPipeline=' + [bool]$t4)
        if ($t3) { Write-Output ('    BuildPlayerOptions.scenes=' + [bool]$t3.GetField('scenes')) }
        if ($t4) { $m = $t4.GetMethods() | Where-Object { $_.Name -eq 'BuildPlayer' -and $_.GetParameters()[0].ParameterType.Name -eq 'BuildPlayerOptions' }; Write-Output ('    BuildPlayer(BuildPlayerOptions)=' + [bool]$m) }
        $found = $true
    }
}
if (-not $found) { Write-Output '  NO localizado' }

Write-Output ''
Write-Output '=== BuildResult ==='
foreach ($dll in @('UnityEditor.CoreModule.dll','UnityEditor.BuildProfileModule.dll','UnityEditor.dll')) {
    $p = Join-Path $unity ('UnityEngine\' + $dll)
    if (-not (Test-Path $p)) { continue }
    $a4 = Load-RO $p
    if (-not $a4) { continue }
    $t5 = $a4.GetType('UnityEditor.Build.Reporting.BuildResult')
    if ($t5) { Write-Output ('  OK en ' + $dll); break }
}

Write-Output ''
Write-Output '=== Application.isBatchMode ==='
$core = Join-Path $unity 'UnityEngine\UnityEngine.CoreModule.dll'
$a5 = Load-RO $core
if ($a5) {
    $t6 = $a5.GetType('UnityEngine.Application')
    if ($t6) { $pr = $t6.GetProperty('isBatchMode'); Write-Output ('  isBatchMode=' + [bool]$pr) } else { Write-Output '  FALTA Application' }
}

Write-Output ''
Write-Output '=== SceneUtility / sceneCountInBuildSettings ==='
$sm = Join-Path $unity 'UnityEngine\UnityEngine.CoreModule.dll'
$a6 = Load-RO $sm
if ($a6) {
    $t7 = $a6.GetType('UnityEngine.SceneManagement.SceneUtility')
    Write-Output ('  SceneUtility=' + [bool]$t7)
    $t8 = $a6.GetType('UnityEngine.SceneManagement.SceneManager')
    if ($t8) { Write-Output ('  sceneCountInBuildSettings=' + [bool]($t8.GetProperty('sceneCountInBuildSettings'))) }
}
