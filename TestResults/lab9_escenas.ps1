$ErrorActionPreference = 'Stop'
$raiz = 'C:\Users\USER\CrisisVR\Assets'
Write-Output '=== Todas las escenas .unity en Assets ==='
Get-ChildItem $raiz -Recurse -Filter *.unity -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Output ('  ' + $_.FullName.Replace('C:\Users\USER\CrisisVR\','') + '   (' + [math]::Round($_.Length/1KB,1) + ' KB)')
}
Write-Output ''
Write-Output '=== GUIs reales de esas escenas (de los .meta) ==='
Get-ChildItem $raiz -Recurse -Filter *.unity -ErrorAction SilentlyContinue | ForEach-Object {
    $meta = $_.FullName + '.meta'
    $guid = 'sin .meta'
    if (Test-Path $meta) {
        $l = Select-String -Path $meta -Pattern '^guid: (.+)$'
        if ($l) { $guid = $l.Matches[0].Groups[1].Value }
    }
    Write-Output ('  ' + $_.Name.PadRight(18) + ' ' + $guid)
}
Write-Output ''
Write-Output '=== GUIs referenciadas en EditorBuildSettings.asset ==='
Select-String -Path 'C:\Users\USER\CrisisVR\ProjectSettings\EditorBuildSettings.asset' -Pattern 'guid: (\w+)' | ForEach-Object { Write-Output ('  ' + $_.Matches[0].Groups[1].Value) }
