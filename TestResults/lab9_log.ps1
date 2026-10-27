$p = 'C:\Users\USER\AppData\Local\Unity\Editor\Editor.log'
$e = Select-String -Path $p -Pattern 'error CS' -SimpleMatch
Write-Output ('total lineas con error CS: ' + $e.Count)
if ($e.Count -gt 0) {
    Write-Output '--- ultimas 10 ---'
    $e | Select-Object -Last 10 | ForEach-Object { Write-Output $_.Line }
}
Write-Output ''
Write-Output '--- ultimas lineas de compilacion ---'
Select-String -Path $p -Pattern 'Compiling|compilation|Assembly-CSharp' | Select-Object -Last 15 | ForEach-Object { Write-Output ($_.LineNumber.ToString() + ': ' + $_.Line) }
Write-Output ''
Write-Output ('tamano log: ' + (Get-Item $p).Length + '  modificado: ' + (Get-Item $p).LastWriteTime)
