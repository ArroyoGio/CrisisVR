$root = 'C:\Users\USER\CrisisVR'
$dll  = Join-Path $root 'Library\ScriptAssemblies\Assembly-CSharp.dll'
$ini  = (Get-Item $dll).LastWriteTime
Write-Output ('Inicio Assembly-CSharp.dll: ' + $ini.ToString('yyyy-MM-dd HH:mm:ss'))
for ($i = 0; $i -lt 40; $i++) {
    Start-Sleep -Seconds 3
    $now = (Get-Item $dll).LastWriteTime
    if ($now -ne $ini) { Write-Output ('CAMBIO a ' + $now.ToString('yyyy-MM-dd HH:mm:ss') + ' tras ' + (($i+1)*3) + ' s'); break }
}
$fin = (Get-Item $dll).LastWriteTime
Write-Output ('Final: ' + $fin.ToString('yyyy-MM-dd HH:mm:ss') + '  cambio=' + ($fin -ne $ini))
