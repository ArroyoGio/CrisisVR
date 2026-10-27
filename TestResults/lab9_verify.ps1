$p = 'C:\Users\USER\AppData\Local\Unity\Editor\Editor.log'
$err = Select-String -Path $p -Pattern 'error CS' -SimpleMatch
$lastErr = ($err | Select-Object -Last 1).LineNumber
$ok = Select-String -Path $p -Pattern 'CopyFiles Library/ScriptAssemblies/Assembly-CSharp.dll' -SimpleMatch | Select-Object -Last 1
Write-Output ('ultimo error CS en linea: ' + $lastErr)
Write-Output ('ultima copia de Assembly-CSharp.dll en linea: ' + $ok.LineNumber)
Write-Output ('errores anteriores a la ultima compilacion: ' + ($lastErr -lt $ok.LineNumber))

Write-Output ''
Write-Output ('UNITY_PATH=' + $env:UNITY_PATH)
Write-Output ('UnityLockfile existe=' + (Test-Path 'C:\Users\USER\CrisisVR\Temp\UnityLockfile'))
$procs = Get-Process unity -ErrorAction SilentlyContinue
Write-Output ('procesos unity: ' + $procs.Count)
$procs | ForEach-Object { Write-Output ('  pid=' + $_.Id + ' inicio=' + $_.StartTime.ToString('HH:mm:ss')) }
