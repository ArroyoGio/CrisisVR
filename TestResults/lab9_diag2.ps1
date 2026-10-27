$ErrorActionPreference = 'Continue'
$root = 'C:\Users\USER\CrisisVR'

Write-Output '=== ERRORES UNICOS EN Editor.log ==='
$log = Join-Path $env:LOCALAPPDATA 'Unity\Editor\Editor.log'
if (Test-Path $log) {
    $m = Select-String -Path $log -Pattern 'error CS[0-9]+' -ErrorAction SilentlyContinue
    $m | ForEach-Object { $_.Line.Trim() } | Sort-Object -Unique
} else { Write-Output 'NO Editor.log' }

Write-Output ''
Write-Output '=== GUID DEL PREFAB TutorialRoot ==='
$meta = Join-Path $root 'Assets\_Project\Prefabs\Tutorial\TutorialRoot.prefab.meta'
if (Test-Path $meta) {
    $g = (Select-String -Path $meta -Pattern 'guid: ([a-f0-9]{32})').Matches[0].Groups[1].Value
    Write-Output ('GUID: ' + $g)
    $scene = Join-Path $root 'Assets\Scenes\Tutorial.unity'
    $s = Get-Content $scene -Raw
    Write-Output ('Referencias al prefab en Tutorial.unity: ' + ([regex]::Matches($s, $g).Count))
} else { Write-Output 'NO meta del prefab' }

Write-Output ''
Write-Output '=== TUTORIALROOT PREFAB: bloque TutorialFlow (primeros campos) ==='
$prefab = Join-Path $root 'Assets\_Project\Prefabs\Tutorial\TutorialRoot.prefab'
$lines = Get-Content $prefab
$flowGuid = 'a56315c560ed024449e72cd2126e3b4f'
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match $flowGuid) {
        $start = [Math]::Max(0, $i - 12)
        $end = [Math]::Min($lines.Count - 1, $i + 40)
        for ($j = $start; $j -le $end; $j++) { Write-Output (($j + 1).ToString() + ': ' + $lines[$j]) }
        break
    }
}

Write-Output ''
Write-Output '=== GameObjects raiz en Tutorial.unity (m_Name de GameObject) ==='
$scene2 = Join-Path $root 'Assets\Scenes\Tutorial.unity'
$sl = Get-Content $scene2
for ($i = 0; $i -lt $sl.Count; $i++) {
    if ($sl[$i] -match '^GameObject:') {
        for ($j = $i; $j -lt [Math]::Min($sl.Count, $i + 12); $j++) {
            if ($sl[$j] -match 'm_Name: (.+)$') {
                Write-Output ('  ' + $sl[$j].Trim())
                break
            }
        }
    }
}
