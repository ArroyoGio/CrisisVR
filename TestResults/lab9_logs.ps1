$ErrorActionPreference = 'Continue'
$log = Join-Path $env:LOCALAPPDATA 'Unity\Editor\Editor.log'
Write-Output ('LOG: ' + $log)
if (Test-Path $log) {
    Write-Output ('Modified: ' + (Get-Item $log).LastWriteTime)
    Write-Output ('Size: ' + (Get-Item $log).Length)
    Write-Output '=== ULTIMOS error CS ==='
    $m = Select-String -Path $log -Pattern 'error CS[0-9]+' -ErrorAction SilentlyContinue
    if ($m) {
        $m | Select-Object -Last 40 | ForEach-Object { $_.Line.Trim() }
    } else {
        Write-Output 'Sin error CS en Editor.log'
    }
    Write-Output '=== ULTIMAS EXCEPCIONES ==='
    $e = Select-String -Path $log -Pattern 'Exception:|NullReferenceException|compilation failed|CompilerOutput' -ErrorAction SilentlyContinue
    if ($e) { $e | Select-Object -Last 15 | ForEach-Object { $_.Line.Trim() } } else { Write-Output 'Sin excepciones' }
} else {
    Write-Output 'NO Editor.log'
}
