$ErrorActionPreference = 'Stop'
$proc = Get-Process -Id 33248 -ErrorAction SilentlyContinue
if ($proc) { Write-Output "Unity (33248) SIGUE EN MARCHA" } else { Write-Output "Unity (33248) termino" }
$log = 'C:\Users\USER\CrisisVR\TestResults\Lab9\editmode.log'
$xml = 'C:\Users\USER\CrisisVR\TestResults\Lab9\tests_editmode.xml'
Write-Output ('log: ' + $(if (Test-Path $log) { 'existe, ' + [math]::Round((Get-Item $log).Length/1KB,1) + ' KB, ' + (Get-Item $log).LastWriteTime } else { 'no existe' }))
Write-Output ('xml: ' + $(if (Test-Path $xml) { 'existe, ' + (Get-Item $xml).LastWriteTime } else { 'no existe' }))
Write-Output '--- errores de compilacion en el log ---'
$e = Select-String -Path $log -Pattern 'error CS|Compilation failed|Scripts have compiler errors' -ErrorAction SilentlyContinue
if ($e) { $e | Select-Object -First 15 | ForEach-Object { Write-Output ('  ' + $_.Line.Trim()) } } else { Write-Output '  ninguno' }
Write-Output '--- EditorBuildSetup en el log ---'
$b = Select-String -Path $log -Pattern 'EditorBuildSetup|CrisisVR: build' -ErrorAction SilentlyContinue
if ($b) { $b | Select-Object -First 8 | ForEach-Object { Write-Output ('  ' + $_.Line.Trim()) } } else { Write-Output '  sin mencion' }
