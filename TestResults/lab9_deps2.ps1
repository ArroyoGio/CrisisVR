$ErrorActionPreference = 'Continue'
$root = 'C:\Users\USER\CrisisVR'
$known = @{}

$bases = @(
    (Join-Path $root 'Assets'),
    (Join-Path $root 'Packages'),
    (Join-Path $root 'Library\PackageCache')
)
foreach ($base in $bases) {
    if (-not (Test-Path $base)) { continue }
    Get-ChildItem $base -Recurse -Filter '*.meta' -ErrorAction SilentlyContinue | ForEach-Object {
        $first = Get-Content $_.FullName -TotalCount 3 -ErrorAction SilentlyContinue
        foreach ($ln in $first) {
            if ($ln -match 'guid: ([a-f0-9]{32})') { $known[$Matches[1]] = $_.FullName; break }
        }
    }
}
Write-Output ('Metas indexados (Assets+Packages+PackageCache): ' + $known.Count)

foreach ($sceneName in @('Tutorial','SampleScene','Act1','BasicScene')) {
    $scene = Join-Path $root ('Assets\Scenes\' + $sceneName + '.unity')
    if (-not (Test-Path $scene)) { continue }
    Write-Output ''
    Write-Output ('=== ' + $sceneName + '.unity ===')
    $content = Get-Content $scene -Raw
    $guids = [regex]::Matches($content, 'guid: ([a-f0-9]{32})') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
    $missing = @()
    foreach ($g in $guids) {
        if ($g -eq '0000000000000000f000000000000000') { continue }
        if ($g -eq '0000000000000000e000000000000000') { continue }
        if (-not $known.ContainsKey($g)) { $missing += $g }
    }
    Write-Output ('GUIDs distintos: ' + $guids.Count + ' | Faltantes reales: ' + $missing.Count)
    foreach ($g in $missing) {
        $ctx = ([regex]::Matches($content, '.{0,90}' + $g + '.{0,50}') | Select-Object -First 1).Value
        Write-Output ('  FALTA ' + $g + '  ctx: ' + ($ctx -replace "`r?`n", ' '))
    }
}
