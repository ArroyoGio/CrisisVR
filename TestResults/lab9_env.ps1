$root = 'C:\Users\USER\CrisisVR'
foreach ($p in @('Assets','Packages','ProjectSettings','Library')) {
    $f = Join-Path $root $p
    if (Test-Path $f) {
        $s = (Get-ChildItem $f -Recurse -Force -File -ErrorAction SilentlyContinue | Measure-Object Length -Sum).Sum
        Write-Output ($p + ': ' + [math]::Round($s/1MB,1) + ' MB')
    }
}
$d = Get-PSDrive C
Write-Output ('C: libre ' + [math]::Round($d.Free/1GB,1) + ' GB')
Write-Output ''
Write-Output '--- procesos unity (linea de comandos) ---'
Get-CimInstance Win32_Process -Filter "Name='unity.exe'" | ForEach-Object {
    $cl = $_.CommandLine
    if ($cl -eq $null) { $cl = '' }
    $proj = ''
    $m = [regex]::Match($cl, '-projectPath\s+("[^"]+"|\S+)')
    if ($m.Success) { $proj = $m.Groups[1].Value }
    Write-Output ('pid=' + $_.ProcessId + ' proj=' + $proj)
}
Write-Output ''
Write-Output ('cache global paquetes: ' + (Test-Path (Join-Path $env:LOCALAPPDATA 'Unity\cache\packages')))
Write-Output ('Unity.exe: ' + (Test-Path 'C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Unity.exe'))
