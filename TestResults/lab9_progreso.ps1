$out = 'C:\Users\USER\CrisisVR\TestResults\Lab9'
$log = Join-Path $out 'editmode.log'
$xml = Join-Path $out 'tests_editmode.xml'
$pidFile = Join-Path $out 'unity_pid.txt'
$pidU = Get-Content $pidFile
$proc = Get-Process -Id ([int]$pidU) -ErrorAction SilentlyContinue
Write-Output ('unity pid=' + $pidU + ' vivo=' + [bool]$proc)
if ($proc) { Write-Output ('  cpu=' + [math]::Round($proc.CPU,0) + 's mem=' + [math]::Round($proc.WorkingSet64/1MB,0) + 'MB') }
if (Test-Path $xml) { Write-Output ('xml existe=True tamanio=' + (Get-Item $xml).Length) } else { Write-Output 'xml existe=False' }
if (Test-Path $log) {
    Write-Output ('log tamanio=' + (Get-Item $log).Length + ' mod=' + (Get-Item $log).LastWriteTime)
    Write-Output '--- ultimas 12 lineas ---'
    Get-Content $log -Tail 12
}
