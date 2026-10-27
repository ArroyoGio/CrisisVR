Get-Process unity -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Output ('pid=' + $_.Id + ' respond=' + $_.Responding + ' cpu=' + [math]::Round($_.CPU,1) + ' mem=' + [math]::Round($_.WorkingSet64/1MB,0) + 'MB titulo=[' + $_.MainWindowTitle + ']')
}
Write-Output ''
Write-Output ('lockfile escrito: ' + (Get-Item 'C:\Users\USER\CrisisVR\Temp\UnityLockfile' -ErrorAction SilentlyContinue).LastWriteTime)
Write-Output ('lockfile tamanio: ' + (Get-Item 'C:\Users\USER\CrisisVR\Temp\UnityLockfile' -ErrorAction SilentlyContinue).Length)
Write-Output ''
Write-Output '--- quien tiene abierto el lockfile ---'
$h = Get-Process | Where-Object { $_.Modules -ne $null }
Write-Output 'omitido (lento)'
